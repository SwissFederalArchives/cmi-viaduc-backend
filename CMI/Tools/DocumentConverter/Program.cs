using System;
using System.IO;

namespace CMI.Tools.DocumentConverter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("CMI.Tools.DocumentConverter starting");

            if (args.Length == 0 || !Directory.Exists(args[0]))
            {
                Console.WriteLine("You need to provide a directory with sample files to process as an argument");
                Console.ReadLine();
                return;
            }

            // Read source folder
            var sourceFolder = args[0];

            // Do the tests
            using (var tester = new DocumentConverterTester())
            {
                tester.Test(sourceFolder);
            }

            Console.WriteLine("CMI.Tools.DocumentConverter finished");
            Console.WriteLine("Press any key to continue... ");
            Console.ReadLine();
        }
    }
}