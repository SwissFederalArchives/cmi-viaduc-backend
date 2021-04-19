using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace CMI.Tools.Termdat
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Dem Programm müssen genau 2 Parameter übergeben werden:\n" +
                                  "Eingabeverzeichnis Ausgabeverzeichnis\n\n" +
                                  "Eingabeverzeichnis: Verzeichnis das ausschliesslich XML Termdat Exportdatei(en) enthält\n" +
                                  "Ausgabeverzeichnis: Hier werden die generierten Textdatei(en) abgelegt");
                return;
            }

            Console.WriteLine("Für folgende Einträge wird eine Gruppe erstellt:\n" +
                              "Einträge die mehr als eine Definition enthalten, welche das folgende erfüllt:\n" +
                              " - Typ = ab, ap oder ve\n" +
                              " - Sprache = DE, EN, FR, IT oder RM\n" +
                              "Definitionen welche einer Gruppe mehrfach den gleichen Text hinzufügen würden werden ignoriert.\n");

            foreach (var inputFileName in Directory.GetFiles(args[0]))
            {
                Console.WriteLine($"Lese Termdat Datei '{inputFileName}'");

                var doc = new XmlDocument();
                doc.Load(inputFileName);

                var root = doc.DocumentElement;

                if (root == null)
                {
                    continue;
                }

                var entries = 0;
                var groups = 0;

                var outputFileName = Path.Combine(args[1], Path.GetFileNameWithoutExtension(inputFileName) + ".txt");

                using (var outputfile = new StreamWriter(outputFileName, false, Encoding.UTF8))
                {
                    // ReSharper disable once PossibleNullReferenceException
                    foreach (XmlNode entryNode in root.SelectNodes("//Eintraege/Eintrag"))
                    {
                        var list = new List<string>();

                        // ReSharper disable once PossibleNullReferenceException
                        foreach (XmlNode elementNode in entryNode.SelectNodes(
                            "./Sprachzonen/Sprachzone[@Sprache='DE' or @Sprache='EN' or @Sprache='FR' or @Sprache='IT' or @Sprache='RM']" +
                            "//Definition[Typ='ab' or Typ='ap' or Typ='ve']/Text"))
                        {
                            var text = elementNode.InnerText;

                            if (!string.IsNullOrEmpty(text))
                            {
                                text = text.Replace("\n", " ");
                                text = text.Replace("\r", string.Empty);
                                text = text.TrimEnd(' ');

                                // Falls der Text z.B. mit (2) endet diese Endung entfernen
                                if (text.Length >= 3 &&
                                    text[text.Length - 1] == ')' &&
                                    char.IsDigit(text[text.Length - 2]) &&
                                    text[text.Length - 3] == '(')
                                {
                                    text = text.Remove(text.Length - 3);
                                    text = text.TrimEnd(' ');
                                }

                                if (!list.Contains(text))
                                {
                                    list.Add(text);
                                }
                            }
                        }

                        if (list.Count >= 2)
                        {
                            outputfile.WriteLine(string.Join("|", list));
                            groups++;
                        }

                        entries++;
                    }
                }

                Console.WriteLine($"Total Einträge:                    {entries}\n" +
                                  $"Gruppen mit Synonymen geschrieben: {groups}\n");
            }
        }
    }
}