using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CMI.Tools.CacheFilesAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (Directory.Exists(args[0]))
                {
                    string directoryName = args[0];
                    Console.WriteLine("Directory: " + directoryName);
                    var list = new List<CacheFile>();
                    StartAnalysis(new List<string>{directoryName}, list);
                   
                    list = list.Where(c => !string.IsNullOrEmpty(c.Name) && c.FilesInZip.Count > 0).ToList();
                    var createExcelFile = new CreateExcelFile();
                    createExcelFile.CreateExcel(list, directoryName);

                    Console.WriteLine(string.Empty);
                    Console.WriteLine(string.Empty);
                    Console.WriteLine("#########################################################################");
                    Console.WriteLine(string.Empty);
                    Console.WriteLine(list.Count +  " Dateien analysiert");
                    Console.WriteLine("Ausgabe Datei: " + createExcelFile.FileFullname);
                    Console.WriteLine(string.Empty);
                    Console.WriteLine("#########################################################################");
                }
                else
                {
                    Console.WriteLine("Argument ist kein Verzeichnis: "  + args[0]);
                }
            }
        }

        private static void StartAnalysis(List<string> filesOrDirectories, List<CacheFile> list)
        {
            int counter = 0;
            int count = filesOrDirectories.Count;
            foreach (var fileOrDirectory in filesOrDirectories)
            {
                counter++;
                if (new FileInfo(fileOrDirectory).Exists)
                {
                    try
                    {
                        ZipArchive archive = ZipFile.OpenRead(fileOrDirectory);
                        list.Add(new CacheFile(archive, new FileInfo(fileOrDirectory)));
                        Console.WriteLine ($"Datei {counter} von {count} erledigt. Dateiname: {fileOrDirectory}");
                    }
                    // No zip File
                    catch (Exception)
                    {
                        Console.WriteLine("      Anaylyse fehlgeschlagen: " + fileOrDirectory);
                    }
                }

                else if (new DirectoryInfo(fileOrDirectory).Exists)
                {
                    List<string> directories = Directory.GetDirectories(fileOrDirectory).ToList();
                    foreach (var directory in directories)
                    {
                        Console.WriteLine(directory);
                        filesOrDirectories = Directory.GetFiles(directory).ToList();
                        StartAnalysis(filesOrDirectories, list);
                    }

                    filesOrDirectories = Directory.GetFiles(fileOrDirectory).ToList();
                    StartAnalysis(filesOrDirectories, list);
                }
            }

        }
        
    }
}
