using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CMI.Tools.AnonymizeServiceMock
{
    public class WordList
    {
        public List<string> WordsToBeAnonymized { get; set; }

        public WordList()
        {
            LoadTextFile();
        }
        
        private void LoadTextFile()
        {
            var location = AppDomain.CurrentDomain.BaseDirectory;
            var text = File.ReadAllText(Path.Combine(location, "words.txt"));

            WordsToBeAnonymized = text.Split('\n').Select(t => t.Trim()).ToList();
        }
    }
}
