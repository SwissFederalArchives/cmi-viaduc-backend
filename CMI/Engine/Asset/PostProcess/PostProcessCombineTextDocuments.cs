using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CMI.Contract.Common;
using Serilog;

namespace CMI.Engine.Asset.PostProcess
{
    public class PostProcessCombineTextDocuments : ProcessAnalyzerBase
    {
        public string RootFolder { get; set; }

        protected override void AnalyzeFiles(string rootOrSubFolder, List<RepositoryFile> files)
        {
            CombineTextFiles(rootOrSubFolder);
        }

        private void CombineTextFiles(string tempFolder)
        {
            var directory = new DirectoryInfo(tempFolder);
            if (directory.Exists)
            {
                var files = directory.GetFiles();
                List<FileInfo> fileInfos = new List<FileInfo>();
                foreach (var file in files.Where(f => f.Extension.EndsWith(".alto", StringComparison.InvariantCultureIgnoreCase)))
                {
                    var foundFile = new FileInfo(Path.ChangeExtension(file.FullName, ".txt"));

                    if (foundFile.Exists)
                    {
                        fileInfos.Add(foundFile);
                    }
                    else
                    {
                        Log.Warning("File has no Text, File: {FullName}", file.FullName);
                    }
                }

                if (fileInfos.Count > 0)
                {
                    var di = new DirectoryInfo(tempFolder);
                    CombineTexts(fileInfos, di);
                    CopySingleTextFileToRootFolder(di);
                }
            }
        }

        private void CombineTexts(List<FileInfo> ocrTextFileList, DirectoryInfo directory)
        {
            var ocrTextDirectoryName = directory.Name + "_OCR.txt";
            var txtDirectoriesFileName = Path.Combine(directory.FullName, ocrTextDirectoryName);

            using var directoryStreamWriter = File.CreateText(txtDirectoriesFileName);
            foreach (var textFile in ocrTextFileList)
            {
                if (textFile.Exists)
                {
                    directoryStreamWriter.WriteLine(File.ReadAllText(textFile.FullName));
                }
            }
        }

        private void CopySingleTextFileToRootFolder(DirectoryInfo directory)
        {
            var ocrTextDirectoryName = directory.Name + "_OCR.txt";
            var txtDirectoriesFileName = Path.Combine(directory.FullName, ocrTextDirectoryName);
            var zipPath = Path.Combine(RootFolder, "OCR-Text-komplett");
            if (!Directory.Exists(zipPath))
            {
                Directory.CreateDirectory(zipPath);
            }

            if (File.Exists(Path.Combine(zipPath, ocrTextDirectoryName)))
            {
                using var directoryStreamWriter = File.CreateText(txtDirectoriesFileName);
                directoryStreamWriter.WriteLine(File.ReadAllText(Path.Combine(zipPath, ocrTextDirectoryName)));
                File.Delete(Path.Combine(zipPath, ocrTextDirectoryName));
                directoryStreamWriter.Close();
            }
            File.Copy(txtDirectoriesFileName, Path.Combine(zipPath, ocrTextDirectoryName));
        }

        public void ZipTextFiles()
        {
            var zipPath = Path.Combine(RootFolder, "OCR-Text-komplett");
            if (Directory.Exists(zipPath))
            {
                // Create zip file with all ocr texts
                ZipFile.CreateFromDirectory(zipPath, zipPath + ".zip");
                Directory.Delete(zipPath, true);
            }
        }
    }
}
