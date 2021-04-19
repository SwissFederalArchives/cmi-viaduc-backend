using System;
using System.IO;
using CMI.Contract.Common;
using Rebex;
using Rebex.IO.FileSystem;
using Rebex.IO.FileSystem.Notifications;
using Rebex.Net;
using Rebex.Net.Servers;
using Serilog;

namespace CMI.Manager.Cache
{
    public class SftpServer
    {
        private FileServer fileServer;

        public void Start()
        {
            Licensing.Key = Properties.CacheSettings.Default.SftpLicenseKey;

            fileServer = new FileServer
            {
                LogWriter = new SerilogWriter()
            };

            var port = ((long?) Properties.CacheSettings.Default.Port).Value;

            var rsaKey = ServerKey.GetServerPrivateKey();
            fileServer.Keys.Add(rsaKey);

            foreach (var category in Enum.GetNames(typeof(CacheRetentionCategory)))
            {
                var dirInfo = new DirectoryInfo(Path.Combine(Properties.CacheSettings.Default.BaseDirectory, category));
                if (!dirInfo.Exists)
                {
                    dirInfo.Create();
                }

                var fs = CreateFileSystem(dirInfo.FullName);

                var user = new FileServerUser(category, Password.Current);
                fileServer.Users.Add(user);
                user.SetFileSystem(fs);
            }

            fileServer.Settings.SshParameters.EncryptionModes &= ~SshEncryptionMode.CBC; // Disable CBC algorithm (security vulnerability)
            fileServer.FileUploaded += FileServerOnFileUploaded;
            fileServer.FileDownloaded += FileServerOnFileDownloaded;
            fileServer.Bind((int) port, FileServerProtocol.Sftp);
            fileServer.Start();
        }

        private LocalFileSystemProvider CreateFileSystem(string rootDir)
        {
            var localFileSystem = new LocalFileSystemProvider(rootDir);

            // Anlegen eines neuen Directories verbieten:
            localFileSystem.GetFileSystemNotifier().CreatePreview += (sender, args) =>
            {
                if (args.Node.IsDirectory)
                {
                    args.CancelOperation();
                }
            };

            // jede Art von Löschen verhindern
            localFileSystem.GetFileSystemNotifier().DeletePreview += (sender, args) => { args.CancelOperation(); };

            return localFileSystem;
        }

        /// <summary>
        ///     Remove any extensions from uploaded files
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="fileTransferredEventArgs">The <see cref="FileTransferredEventArgs" /> instance containing the event data.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void FileServerOnFileUploaded(object o, FileTransferredEventArgs fileTransferredEventArgs)
        {
            var file = Path.Combine(Properties.CacheSettings.Default.BaseDirectory, fileTransferredEventArgs.User.Name,
                fileTransferredEventArgs.FullPath);
            var fi = new FileInfo(file);
            if (fi.Exists)
            {
                var fileWithoutExtension = fi.FullName.Remove(fi.FullName.Length - fi.Extension.Length);
                if (File.Exists(fileWithoutExtension))
                {
                    File.Delete(fileWithoutExtension);
                }

                fi.MoveTo(fileWithoutExtension);
            }
        }

        private void FileServerOnFileDownloaded(object sender, FileTransferredEventArgs fileTransferredEventArgs)
        {
            var file = Path.Combine(Properties.CacheSettings.Default.BaseDirectory, fileTransferredEventArgs.User.Name,
                fileTransferredEventArgs.FullPath);
            var fi = new FileInfo(file);
            if (fi.Exists)
            {
                fi.LastAccessTime = DateTime.Now;
            }
        }

        public void Stop()
        {
            Log.Information("FileServer stopping");
            fileServer.Stop();
            Log.Information("FileServer stopped");
            Log.CloseAndFlush();
        }
    }
}