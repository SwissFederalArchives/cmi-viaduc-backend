using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CMI.Web.Frontend.api.Interfaces;

namespace CMI.Web.Frontend.api
{
    public class FileWoerterbuch : IWoerterbuch
    {
        private readonly Dictionary<string, List<SynonymGroup>> dictionary = new Dictionary<string, List<SynonymGroup>>();
        private readonly IFileSystem fileSystem;

        public FileWoerterbuch(IFileSystem fileSystem, string directory)
        {
            this.fileSystem = fileSystem;
            var sources = ReadSourcesFromFile(Path.Combine(directory, "SourceMapping.txt"));

            foreach (var source in sources)
            {
                var filePath = Path.Combine(directory, source.FileName);
                if (!fileSystem.FileExists(filePath))
                {
                    continue;
                }


                var lines = fileSystem.ReadAllLines(filePath);
                foreach (var l in lines)
                {
                    AddLine(l, source);
                }
            }
        }


        public List<SynonymGroup> FindGroups(string input)
        {
            return dictionary.TryGetValue(input.ToLower(), out var valueSource) ? valueSource : null;
        }

        private void AddLine(string line, Source quelle)
        {
            var begriffe = line.Split('|').ToList();

            if (begriffe.Count <= 1)
            {
                throw new FormatException("Die Zeile ist ungültig: " + line);
            }

            var newGroup = new SynonymGroup(begriffe, quelle);

            foreach (var begriff in begriffe)
            {
                if (dictionary.TryGetValue(begriff.ToLower(), out var synonymGroups))
                {
                    var existing = synonymGroups.FirstOrDefault(g => g.HasSameEntriesAs(newGroup));

                    if (existing != null)
                    {
                        if (!existing.Sources.Contains(quelle))
                        {
                            existing.Sources.Add(quelle);
                        }
                    }
                    else
                    {
                        synonymGroups.Add(newGroup);
                    }
                }
                else
                {
                    var groupList = new List<SynonymGroup>(1);

                    groupList.Add(newGroup);
                    dictionary.Add(begriff.ToLower(), groupList);
                }
            }
        }

        private List<Source> ReadSourcesFromFile(string mappingFile)
        {
            var result = new List<Source>();


            foreach (var line in fileSystem.ReadAllLines(mappingFile))
            {
                result.Add(new Source(line));
            }

            return result;
        }
    }
}