using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Schema;

namespace CMI.Tools.ManifestImprover
{
    internal class ManifestImprover : IDisposable
    {
        public void Improve(string sourceFolder, string pattern, string substitution)
        {
            var sourceFiles = new DirectoryInfo(sourceFolder).GetFiles("*.json", SearchOption.AllDirectories);
            int counter = 0;
            foreach (var sourceFile in sourceFiles)
            {
                try
                {
                    Console.WriteLine(sourceFile.FullName);
                    Console.WriteLine(string.Empty);

                    var manifestText = File.ReadAllText(sourceFile.FullName);

                    RegexOptions options = RegexOptions.Multiline;

                    Regex regex = new Regex(pattern, options);

                    if (regex.IsMatch(manifestText))
                    {
                        counter++;
                        Console.WriteLine($"{sourceFile.FullName} : pattern match");
                    }
                    string result = regex.Replace(manifestText, substitution);

                    File.WriteAllText(sourceFile.FullName, result);
                    Console.WriteLine(string.Empty);
                    Console.WriteLine("############## Finish File");
                    Console.WriteLine(string.Empty);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("############## Error");
                    Console.WriteLine(sourceFile.FullName);
                    Console.WriteLine("##############");
                }
            }
            Console.WriteLine(string.Empty);
            Console.WriteLine(string.Empty);
            Console.WriteLine("#########################################################################");
            Console.WriteLine("#########################################################################");
           
            Console.WriteLine($"Changed {counter} files from {sourceFiles.Length} Json files changed");
            Console.WriteLine(string.Empty);
            Console.WriteLine(string.Empty);
            Console.WriteLine("#########################################################################");
            Console.WriteLine("#########################################################################");
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}
