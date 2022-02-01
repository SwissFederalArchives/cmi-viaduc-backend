using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using CMI.Contract.DocumentConverter;
using CMI.Manager.DocumentConverter.Properties;
using Rebex;
using Rebex.IO.FileSystem;
using Rebex.Net;
using Rebex.Net.Servers;
using Serilog;

namespace CMI.Manager.DocumentConverter
{
    internal class SftpServer
    {
        private const string password = @"dn9@<Lpw)u3\KCRH";

        private static readonly ConcurrentDictionary<string, JobInfoDetails> jobStorage = new ConcurrentDictionary<string, JobInfoDetails>();
        private string baseAddress;

        private FileServer fileServer;
        private readonly object fileServerLock = new object();
        private readonly object directoryInformationLock = new object();
        public int port;

        public SftpServer()
        {
            Licensing.Key = DocumentConverterSettings.Default.SftpLicenseKey;
            ConfigureAndStartFileServer();
        }

        private EventHandler<FileTransferredEventArgs> FileServerOnFileDownloaded => (sender, e) => RemoveJobInternal(e.User);

        ~SftpServer()
        {
            Log.Information("Disposing of sftp server instance in DocumentConverter");
            lock (fileServerLock)
            {
                if (fileServer.IsRunning)
                {
                    fileServer?.Stop();
                }

                fileServer?.Dispose();
            }
        }

        private void ConfigureAndStartFileServer()
        {
            Log.Information("About to configure and start sftp server.");

            try
            {
                port = DocumentConverterSettings.Default.Port;
                baseAddress = DocumentConverterSettings.Default.BaseAddress.Replace("{MachineName}", Environment.MachineName);

                Log.Information(
                    "Using the following settings for SFTP server: port = {port}, baseAddress = {baseAddress}, baseDirectory = {settings.BaseDirectory}",
                    port, baseAddress,
                    DocumentConverterSettings.Default.BaseDirectory);

                lock (fileServerLock)
                {
                    fileServer = new FileServer
                    {
                        LogWriter = new SerilogWriter(),
                        Keys = { ServerKey.GetServerPrivateKey() }
                    };

                    fileServer.Settings.SshParameters.EncryptionModes &= ~SshEncryptionMode.CBC; // Disable CBC algorithm (security vulnerability)
                    fileServer.FileUploaded += (sender, e) =>
                    {
                        Log.Information("File successfully uploaded: user={User}, full path={FullPath}, file name={Path}", e.User, e.FullPath,
                            e.Path);
                    };
                    fileServer.FileDownloaded += FileServerOnFileDownloaded;
                    fileServer.Bind(port, FileServerProtocol.Sftp);
                    fileServer.Start();
                }
                Log.Information($"Sftp server is listening on port '{port}'");
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                throw;
            }
        }


        public JobInitResult RegisterNewJob(JobInitRequest jobInitRequest)
        {
            if (fileServer == null)
            {
                throw new InvalidOperationException("Sftp server is not configured and started");
            }

            try
            {
                // Create a new unique user and a unique directory for that user 
                var newJobGuid = Guid.NewGuid().ToString("N");
                var user = new FileServerUser(newJobGuid, password);
                Debug.Assert(user != null);
                var di = GetJobDirectory(newJobGuid);
                di.Create();

                // Create a sftp file system object and assign it to the user.
                var localFileSystem = CreateFileSystem(di.FullName);
                user.SetFileSystem(localFileSystem);
                lock (fileServerLock)
                {
                    fileServer.Users.Add(user);
                }

                var jobInitResult = new JobInitResult
                {
                    JobGuid = newJobGuid,
                    User = user.Name,
                    Password = password,
                    UploadUrl = baseAddress,
                    Port = port
                };

                // Add the information about the job to our local in memory storage
                var jobInfoDetails = new JobInfoDetails { Request = jobInitRequest, Result = jobInitResult };
                jobStorage.AddOrUpdate(newJobGuid, jobInfoDetails, (k, v) => v);

                return jobInitResult;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                return new JobInitResult
                {
                    IsInvalid = true,
                    ErrorMessage = e.Message
                };
            }
        }

        public DirectoryInfo GetJobDirectory(string newJobGuid)
        {
            return new DirectoryInfo(Path.Combine(DocumentConverterSettings.Default.BaseDirectory, newJobGuid));
        }

        public JobInfoDetails GetJobInfo(string jobId)
        {
            return jobStorage[jobId];
        }

        /// <summary>
        ///     Removes the job and all associated files of that job/user.
        /// </summary>
        /// <param name="jobGuid">The job unique identifier.</param>
        public void RemoveJob(string jobGuid)
        {
            // JobGuid is same as username
            lock (fileServerLock)
            {
                var user = fileServer.Users[jobGuid];
                RemoveJobInternal(user);
            }
        }

        private LocalFileSystemProvider CreateFileSystem(string rootDir)
        {
            var localFileSystem = new LocalFileSystemProvider(rootDir);
            return localFileSystem;
        }

        private void RemoveJobInternal(FileServerUser user)
        {
            if (user == null)
            {
                return;
            }
            try
            {
                lock (directoryInformationLock)
                {
                    Log.Information("Cleaning up after download...");
                    var di = GetJobDirectory(user.Name);

                    if (di.Exists)
                    {
                        try
                        {
                            di.Delete(true);
                            Log.Information("Folder '{FullName}' and contents removed", di.FullName);
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e, "Unable to delete folder '{FullName}'", di.FullName);
                        }
                    }
                }

                var jobInfo = GetJobInfo(user.Name);

                if (jobInfo == null)
                {
                    return;
                }


                if (!jobStorage.ContainsKey(jobInfo.Result.JobGuid))
                {
                    return;
                }

                if (jobStorage.TryRemove(jobInfo.Result.JobGuid, out var jobDetails))
                {
                    Log.Information("JobInfo and conversion settings for job id '{JobGuid}' removed.", jobDetails.Result.JobGuid);
                }
                else
                {
                    Log.Warning("Failed to remove jobInfo and conversion settings for job id '{JobGuid}'.", jobDetails.Result.JobGuid);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
            finally
            {
                lock (fileServerLock)
                {
                    fileServer.Users.Remove(user);
                }

                Log.Information("User '{Name}' removed from sftp server", user.Name);
            }
        }
    }
}