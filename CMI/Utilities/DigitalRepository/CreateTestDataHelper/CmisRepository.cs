using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties;
using DotCMIS;
using DotCMIS.Client;
using DotCMIS.Client.Impl;
using DotCMIS.Data.Impl;
using FSBlog.GoogleSearch.GoogleClient;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Utilities.DigitalRepository.CreateTestDataHelper
{
    internal class CmisRepository
    {
        private readonly List<SampleData> sampleData;
        private readonly List<SampleFile> sampleFiles = new List<SampleFile>();
        private IFolder barFolder;
        private ISession session;

        public CmisRepository()
        {
            sampleData = JsonConvert.DeserializeObject<List<SampleData>>(File.ReadAllText("sampleData.json"));
            if (File.Exists("sampleFiles.json"))
            {
                sampleFiles = JsonConvert.DeserializeObject<List<SampleFile>>(File.ReadAllText("sampleFiles.json"));
            }
            else
            {
                LoadRandomSampleFiles("pdf", 1000);
                File.WriteAllText("sampleFiles.json", JsonConvert.SerializeObject(sampleFiles));
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

        public void CheckOrCreateRootFolder()
        {
            var root = session.GetRootFolder();
            barFolder = CreateOrUpdateFolder("BAR", "Just a description for the root", root);
        }

        public IFolder CreateOrUpdateFolder(string name, string description, IFolder parent)
        {
            try
            {
                // Too long names gives problems
                if (name.Length > 240)
                {
                    name = name.Substring(0, 240);
                }

                name = GetValidFileName(name);
                var folder = parent.GetDescendants(1)?.FirstOrDefault(c => ComparisonByIdIfPossible(c.Item, name))?.Item as IFolder;

                if (folder == null)
                {
                    Log.Information($"Create folder: {name}");
                    folder = parent.CreateFolder(new Dictionary<string, object>
                    {
                        {PropertyIds.ObjectTypeId, "cmis:folder"},
                        {PropertyIds.Name, name.Trim()},
                        {"cmis:description", description}
                    });
                }
                else
                {
                    Log.Information($"Update folder: {name}");
                    folder.UpdateProperties(new Dictionary<string, object>
                    {
                        {PropertyIds.Name, name},
                        {"cmis:description", description}
                    });
                }

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
        /// <param name="aipData">The aip data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void CreateTestData(List<AipData> aipData)
        {
            // Loop through every entry in the list.
            foreach (var data in aipData.OrderBy(a => a.Title))
            {
                var existingFolder = GetFolder(data.AipAtDossierId);
                if (existingFolder == null)
                {
                    // Create a folder with sub-folders and files for each of the items
                    var newFolder = CreateOrUpdateFolder($"{data.Id} - {data.Title}", data.AipAtDossierId, barFolder);
                    CreateFoldersAndFiles(newFolder, 1);
                }
                else
                {
                    CreateOrUpdateFolder($"{data.Id} - {data.Title}", data.AipAtDossierId, barFolder);
                    Log.Information("Existing folder: {AipAtDossierId} - {Title}", data.AipAtDossierId, data.Title);
                }
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

        private void LoadRandomSampleFiles(string fileType, int limit)
        {
            var limiter = 0;
            var rnd = new Random(DateTime.Now.Millisecond);
            while (sampleFiles.Count(s => s.Type == fileType) < limit && limiter < 20)
            {
                var sc = new SearchClient($"{sampleData[rnd.Next(sampleData.Count) - 1].Title} filetype:{fileType}");
                var result = sc.Query(20).Where(r => r.CleanUri.AbsoluteUri.EndsWith(fileType)).ToList();

                sampleFiles.AddRange(result.Select(r => new SampleFile
                {
                    Type = fileType,
                    Url = r.CleanUri,
                    Title = r.Text,
                    FileName = GetValidFileName(r.Text) + $".{fileType}"
                }));
                if (!result.Any())
                {
                    limiter++;
                }
            }
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

        private void CreateFoldersAndFiles(IFolder folder, int level)
        {
            var rnd = new Random(DateTime.Now.Millisecond);
            var randomFolderNumbers = rnd.Next(5 - 2 * level < 0 ? 0 : 5 - 2 * level);
            var randomFileNumbers = rnd.Next(1, 5);

            for (var i = 0; i < randomFolderNumbers; i++)
            {
                IFolder newFolder = null;
                while (newFolder == null)
                {
                    newFolder = CreateOrUpdateFolder(sampleData[rnd.Next(sampleData.Count)].Title, "", folder);
                }

                CreateFoldersAndFiles(newFolder, level + 1);
            }

            for (var i = 0; i < randomFileNumbers; i++)
            {
                var file = DownloadNewFile();
                if (File.Exists(file))
                {
                    CreateNewFile(folder, file);
                }
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
                    MimeType = "application/pdf",
                    Length = content.Length,
                    Stream = new MemoryStream(content)
                };

                var doc = folder.CreateDocument(properties, contentStream, null);

                Log.Information("Uploaded new document");
                return doc;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        private string DownloadNewFile()
        {
            var success = false;
            var limiter = 0;
            while (success == false && limiter < 50)
            {
                var itemIndex = new Random(DateTime.Now.Millisecond).Next(sampleFiles.Count - 1);
                var file = sampleFiles[itemIndex];
                var tempFileName = Path.Combine(Path.GetTempPath(), file.FileName);
                try
                {
                    if (!File.Exists(tempFileName))
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(file.Url, tempFileName);
                            success = true;
                        }

                        Log.Information($"Create file: {tempFileName}");
                    }

                    return tempFileName;
                }
                catch (Exception ex)
                {
                    limiter++;
                    sampleFiles.RemoveAt(itemIndex);
                    Log.Error($"Failed to download file from {file.Url}. Reason: {ex.Message}");
                }
            }

            return null;
        }
    }

    internal class SampleData
    {
        public string Title { get; set; }
    }

    internal class SampleFile
    {
        public Uri Url { get; set; }
        public string FileName { get; set; }

        public string Title { get; set; }
        public string Type { get; set; }
    }
}