using System;
using System.IO;

namespace CMI.Tools.UrbanCode
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Usage();
                return;
            }

            switch (args[0].Substring(0, 1))
            {
                case "d":
                    if (args.Length != 3)
                    {
                        Usage();
                        return;
                    }

                    var doc = new Documenter(args[1]);
                    var html = doc.GetHtml();
                    File.WriteAllText(args[2], html);
                    break;
                case "t":
                    if (args.Length != 2)
                    {
                        Usage();
                        return;
                    }

                    var tagger = new Tagger(args[1]);
                    tagger.ReplaceTags();
                    break;
                case "e":
                    if (args.Length != 3)
                    {
                        Usage();
                        return;
                    }

                    var ersetzer = new Ersetzer(args[2]);
                    ersetzer.Ersetze(args[1]);
                    ersetzer.ReplaceTags();
                    break;
                default:
                    return;
            }
        }

        private static void Usage()
        {
            Console.WriteLine("CMI.Tools.UrbanCode.exe [Command] ...");
            Console.WriteLine(" [Command]:   ");
            Console.WriteLine("   d=Dokument erstellen");
            Console.WriteLine("     Erstellt eine HTML Datei, welche die Parameter dokumentiert.");
            Console.WriteLine("     [directory]: zu scannendes Directory");
            Console.WriteLine("     [htmlFile] : zu schreibende HTML Datei");
            Console.WriteLine("  t=Tags schreiben");
            Console.WriteLine("     Ersetzt in den *.exe.config Dateien die Werte mit den UrbanCode Platzhaltern.");
            Console.WriteLine("     [directory]: zu scannendes Directory");
            Console.WriteLine("  e=Tags ersetzen");
            Console.WriteLine("     Ersetzt die Platzhalter in den *.config Dateien durch die entsprechenden Werte");
            Console.WriteLine("      aus der Wertedatei.");
            Console.WriteLine("      [directory]: zu scannendes Directory, worin die *.exe.config Dateien liegen");
            Console.WriteLine("      [wertedatei]: Datei, welche die zu ersetzenden Platzhalter mit ihren Werten enthält.");
            Console.WriteLine("        Inhalt z.B.");
            Console.WriteLine("        ------------------------myValues.json---------------------");
            Console.WriteLine("        {");
            Console.WriteLine("          \"@@CMI.Manager.Test.Properties.Settings.Port@@\": 9926,");
            Console.WriteLine("          \"@@CMI.Manager.Test.Properties.Settings.Uri@@\": \"http://a.ch\"");
            Console.WriteLine("        }");
            Console.WriteLine("        ----------------------------------------------------------");
        }
    }
}