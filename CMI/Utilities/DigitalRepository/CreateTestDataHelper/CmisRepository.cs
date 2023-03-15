
using CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties;
using DotCMIS;
using DotCMIS.Client;
using DotCMIS.Client.Impl;
using DotCMIS.Data.Impl;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using DotCMIS.Enums;


namespace CMI.Utilities.DigitalRepository.CreateTestDataHelper
{
    internal class CmisRepository
    {
        private IFolder barFolder;
        private ISession session;
        public string FileCopyDestinationPath { get; private set; }

        public bool OverrideFiles { get; private set; }

        public Dictionary<string, string> OnlyThisIdsWithThisFileUpdate { get; private set; }


        public CmisRepository()
        {
            OnlyThisIdsWithThisFileUpdate = new Dictionary<string, string>();
            OverrideFiles = false;
            FileCopyDestinationPath = Settings.Default.FileCopyDestinationPath;
        }

        /// <summary>
        ///     Starts the Harvest Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public void Start()
        {
            Log.Information("service started");
        }


        public void StartDataUpload(List<AipData> aipData)
        {
            if (Directory.Exists(FileCopyDestinationPath))
            {
                var directory = Directory.CreateDirectory(FileCopyDestinationPath);

                var counter = 0;
                var fileInfos = directory.GetFiles().Where(f => f.Extension == ".zip").ToList();

                FileInfo fileInfo;
                foreach (var aip in aipData)
                {
                    try
                    {

                        if (OnlyThisIdsWithThisFileUpdate.Count > 0 && OnlyThisIdsWithThisFileUpdate.ContainsKey(aip.Id))
                        {
                            fileInfo = fileInfos.First(f => f.Name.StartsWith(OnlyThisIdsWithThisFileUpdate[aip.Id]));
                            StartFileUpload(fileInfo, aip);
                        }
                        else if (this.OnlyThisIdsWithThisFileUpdate.Count == 0)
                        {
                            if (fileInfos.Count > counter)
                            {
                                fileInfo = fileInfos[counter++];
                            }
                            else
                            {
                                counter = 0;
                                fileInfo = fileInfos[counter];
                            }
                            StartFileUpload(fileInfo, aip);
                        }
                    }
                    catch (Exception e)
                    {
                       Log.Warning(aip.Id + ": " + e);
                    }
                }
            }
        }

        private void StartFileUpload(FileInfo fileInfo, AipData aip)
        {
            string tempDirectory = fileInfo.Directory.FullName + "\\temp_" + aip.Id;
            try
            {
                Directory.CreateDirectory(tempDirectory);
                ZipFile.ExtractToDirectory(fileInfo.FullName, tempDirectory);
                if (Directory.Exists(tempDirectory + "\\Content"))
                {
                    if (Directory.Exists(tempDirectory + "\\header") && File.Exists(tempDirectory + "\\header\\metadata.xml"))
                    {
                        File.Move(tempDirectory + "\\header\\metadata.xml", tempDirectory + "\\Content\\metadata.xml");
                        DeleteFoldersAndFiles(tempDirectory + "\\header");
                        Directory.Delete(tempDirectory + "\\header");
                    }
                    else
                    {
                        Log.Warning("No metadata.xml found");
                    }

                    UploadTestData(tempDirectory + "\\Content", aip);
                    Directory.Delete(tempDirectory + "\\Content");
                }
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
            finally
            {
                Directory.Delete(tempDirectory,true);
            }
        }

        public ISession ConnectToFirstRepository()
        {
            var parameters = new Dictionary<string, string>();

            var serviceUrl = Settings.Default.AlfrescoServiceUrl;

            parameters[SessionParameter.BindingType] = BindingType.AtomPub;
            parameters[SessionParameter.AtomPubUrl] = serviceUrl;
            parameters[SessionParameter.User] = Settings.Default.AlfrescoUser;
            parameters[SessionParameter.Password] = Settings.Default.AlfrescoPassword;

            var factory = SessionFactory.NewInstance();
            var repositories = factory.GetRepositories(parameters);
            session = repositories[0].CreateSession();

            return session;
        }

        /// <summary>
        /// BAR Folder muss immer da sein
        /// </summary>
        public void CheckRootFolder()
        {
            var root = session.GetRootFolder();
            barFolder = root.GetDescendants(1)?.FirstOrDefault(c => ComparisonByIdIfPossible(c.Item, "BAR"))?.Item as IFolder;
        }


        public Arguments CheckArguments(string[] args)
        {
            var lastFoundTagWasIdTag = false;
            var result = Arguments.Default;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "":
                        break;
                    case "-o":
                    case "-override":
                        if (lastFoundTagWasIdTag && OnlyThisIdsWithThisFileUpdate.Count == 0)
                        {
                            return Arguments.Error;
                        }

                        lastFoundTagWasIdTag = false;
                        if (args.Length > i + 1 && bool.TryParse(args[i + 1], out bool overrideFiles))
                        {
                            i++;
                            OverrideFiles = overrideFiles;
                            result = Arguments.UserConfig;
                        }
                        else
                        {
                            return Arguments.Error;
                        }
                        break;
                    case "-p":
                    case "-path":
                        if (lastFoundTagWasIdTag && OnlyThisIdsWithThisFileUpdate.Count == 0)
                        {
                            return Arguments.Error;
                        }
                        lastFoundTagWasIdTag = false;
                        if (args.Length > i + 1)
                        {
                            i++;
                            FileCopyDestinationPath = args[i];
                            if (!Directory.Exists(FileCopyDestinationPath))
                            {
                                return Arguments.DestinationPathNotExists;
                            }
                        }
                        else
                        {
                            return Arguments.Error;
                        }

                        
                        result = Arguments.UserConfig;
                        break;
                    case "-id":
                    case "-ids":
                        if (args.Length > i + 1)
                        {

                            var entry = args[i + 1];
                            if (entry.Count(f => f == '|') == 1)
                            {
                                i++;
                                var ventrY = entry.Split('|');
                                int.TryParse(ventrY[0],  out var veId);
                                OnlyThisIdsWithThisFileUpdate.Add(veId.ToString(), ventrY[1]);
                            }
                        }
                        if (lastFoundTagWasIdTag)
                        {
                            return Arguments.Error;
                        }
                        lastFoundTagWasIdTag = true;
                        break;
                    case "-h":
                    case "-help":
                    case "help":
                        return Arguments.Help;
                    default:
                        if (lastFoundTagWasIdTag)
                        {
                            var entry = args[i];
                            if (entry.Count(f => f == '|') == 1)
                            {
                                var ventrY = entry.Split('|');
                                int.TryParse(ventrY[0], out var veId);
                                OnlyThisIdsWithThisFileUpdate.Add(veId.ToString(), ventrY[1]);
                            }
                            else
                            {
                                return Arguments.IdImportError;
                            }

                        }
                        else
                        {
                            return Arguments.Unknow;
                        }
                        break;
                }
            }

            return result;
        }


        private IFolder CreateFolder(string name, string description, IFolder parent)
        {
            Log.Information("Create or Update folder: {name}", name);
            try
            {
                // Too long names gives problems
                if (name.Length > 240)
                {
                    name = name.Substring(0, 240);
                }

                name = GetValidFileName(name);

                Log.Information($"Create folder: {name}");
                var folder = parent.CreateFolder(new Dictionary<string, object>
                {
                    {PropertyIds.ObjectTypeId, "cmis:folder"},
                    {PropertyIds.Name, name.Trim()},
                    {"cmis:description", description}
                });
                return folder;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to create or update folder: {name}");
                return null;
            }
        }

        
        private bool ComparisonByIdIfPossible(IFileableCmisObject folder, string name)
        {
            if (name.Contains("-"))
            {
                var id = name.Substring(0, name.IndexOf("-", StringComparison.Ordinal));
                return folder.Name.Substring(0, folder.Name.IndexOf("-", StringComparison.Ordinal)) == id;
            }

            return folder.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase);
        }
        
        
        /// <summary>
        ///     Creates test data.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        private void UploadTestData(string file, AipData data)
        {
            var alfrescoName = data.Id + " - " + data.Title;
            Log.Information(alfrescoName);
            var existingFolder = GetFolder(data.AipAtDossierId);
            if (existingFolder == null)
            {
                Log.Information("New folder: {alfrescoName}", alfrescoName);
                UploadFoldersAndFiles(CreateFolder(alfrescoName, data.AipAtDossierId, barFolder), file);
            }
            else if (OverrideFiles)
            {
                Log.Information("Existing folder override: {alfrescoName}", alfrescoName);

                try
                {
                    existingFolder.DeleteTree(true, UnfileObject.Delete, true);
                    existingFolder.Delete(true);
                    Log.Information("Dateien gelöscht {alfrescoName}", alfrescoName);
                }
                catch (Exception e)
                {
                    // Exception wenn nicht vorhandene Daten gelöscht werden sollen
                    Log.Information("alfrescoName: " + e.Message);
                }

                UploadFoldersAndFiles(CreateFolder(alfrescoName, data.AipAtDossierId, barFolder), file);
            }
            else 
            {
                Log.Information("Existing folder not override: {alfrescoName}", alfrescoName);
            }
        }

        private string GetValidFileName(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            while (fileName.EndsWith("."))
            {
                fileName = fileName.Substring(0, fileName.Length - 1);
            }

            return fileName;
        }
        
        private IFolder GetFolder(string aipAtDossierId)
        {
            var result = session.Query($"Select * from cmis:folder where cmis:description = '{aipAtDossierId}'", false);
            foreach (var queryResult in result)
            {
                if (queryResult["cmis:description"].FirstValue.ToString() == aipAtDossierId)
                {
                    var folder = session.GetObject(queryResult["cmis:objectId"].FirstValue.ToString());
                    return (IFolder) folder;
                }
            }

            return null;
        }

        private void UploadFoldersAndFiles(IFolder folder, string currentDirectory)
        {
            Log.Information("Upload Directory {currentDirectory}", currentDirectory);
            var directoryInfo = new DirectoryInfo(currentDirectory);
            foreach (var directory in directoryInfo.GetDirectories())
            {
                UploadFoldersAndFiles(CreateFolder(directory.Name, "", folder), directory.FullName);
                Directory.Delete(directory.FullName);
            }

            foreach (var newFile in Directory.GetFiles(currentDirectory))
            {
                var fileInfo = new FileInfo(newFile);
                CreateNewFile(folder, fileInfo.FullName);

                File.Delete(fileInfo.FullName);
            }
        }

        private void DeleteFoldersAndFiles(string currentDirectory)
        {
            var directoryInfo = new DirectoryInfo(currentDirectory);
            foreach (var directory in directoryInfo.GetDirectories())
            {
                DeleteFoldersAndFiles(directory.FullName);
                Directory.Delete(directory.FullName);
            }

            foreach (var newFile in Directory.GetFiles(currentDirectory))
            {
                var fileInfo = new FileInfo(newFile);
                File.Delete(fileInfo.FullName);
            }
        }

        private IDocument CreateNewFile(IFolder folder, string file)
        {
            try
            {
                var fi = new FileInfo(file);
                IDictionary<string, object> properties = new Dictionary<string, object>();
                properties[PropertyIds.Name] = fi.Name;
                properties[PropertyIds.ObjectTypeId] = "cmis:document";

                var content = File.ReadAllBytes(fi.FullName);
                var contentStream = new ContentStream
                {
                    FileName = fi.Name,
                    MimeType = MimeMapping.GetMimeMapping(file),
                     Length = content.Length,
                    Stream = new MemoryStream(content)
                };

                var doc = folder.CreateDocument(properties, contentStream, null);

                Log.Information("Uploaded new document");
                return doc;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                throw;
            }
        }
    }
}

public enum Arguments
{
    Default,
    Help,
    Error,
    UserConfig,
    Unknow,
    DestinationPathNotExists,
    IdImportError
}