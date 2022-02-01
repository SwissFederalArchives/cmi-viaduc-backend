using System;
using System.Collections.Generic;
using System.IO;
using CMI.Contract.Common;
using CMI.Engine.Asset;

namespace CMI.Manager.Asset.PdfManipulatorTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var repositoryFiles = new List<RepositoryFile>();
            var directoryName = string.Empty;

            foreach (var file in args)
            {
                var sourceFile = new FileInfo(file);
                directoryName = sourceFile.DirectoryName;   // We assume, that all pdf are from the same directory
                if (!sourceFile.Exists)
                {
                    Console.WriteLine("Pass one or more valid pdfs to be splitted as an argument");
                    return;
                }

                // Add the file to this list
                repositoryFiles.Add(new RepositoryFile
                {
                    PhysicalName = sourceFile.Name,
                    Exported = true,
                    Id = Guid.NewGuid().ToString()
                });
            }

            var manipulator = new PdfManipulator();

            // First test the extraction process
            Console.WriteLine("Testing the extraction");
            Console.WriteLine("======================\n");
            var textExtractionFiles = manipulator.ConvertToTextExtractionFiles(repositoryFiles, directoryName);

            foreach (var createdFile in textExtractionFiles)
            {
                Console.WriteLine($"Created text extraction file {createdFile.FullName}.");
            }

            // Add some sample texts
            // We are not doing a real detection
            Console.WriteLine("");
            foreach (var file in textExtractionFiles)
            {
                file.ContentText = $"This is a sample text from part {file.SplitPartNumber:d4}";
            }

            manipulator.TransferExtractedText(textExtractionFiles, repositoryFiles);

            Console.WriteLine("");
            foreach (var file in repositoryFiles)
            {
                Console.WriteLine($"Text for file {file.PhysicalName} is \n{file.ContentText}");
            }


            // Now we test the conversion
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Testing the conversion");
            Console.WriteLine("======================\n\n");

            var conversionFiles = manipulator.ConvertToConversionFiles(repositoryFiles, directoryName, false);

            Console.WriteLine("");
            foreach (var createdFile in conversionFiles)
            {
                Console.WriteLine($"Created file for conversion {createdFile.FullName}.");
            }

            // Now we should do something with it 

            // Now we merge the files back together
            manipulator.MergeSplittedFiles(conversionFiles);

            // Now output the list again. Should contain the original files only, and the parts should be deleted
            Console.WriteLine("");
            foreach (var createdFile in conversionFiles)
            {
                Console.WriteLine($"Resulting file is {createdFile.FullName}.");
            }

            Console.ReadLine();
        }
    }
}
