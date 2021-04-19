using System;

namespace CMI.Web.Frontend.api
{
    public class Source
    {
        public Source(string line)
        {
            var entries = line.Split('\t');

            if (entries.Length != 5)
            {
                throw new FormatException("Die Zeile ist ungültig: " + line);
            }

            Key = entries[0];
            Name_de = entries[1];
            Name_fr = entries[2];
            Name_it = entries[3];
            Name_en = entries[4];
        }

        public string Key { get; }
        public string Name_de { get; }
        public string Name_fr { get; }
        public string Name_it { get; }
        public string Name_en { get; }

        public string FileName => $"{Key}.txt";

        public string GetName(string language)
        {
            switch (language)
            {
                case "fr":
                    return Name_fr;

                case "it":
                    return Name_it;

                case "en":
                    return Name_en;

                default:
                    return Name_de;
            }
        }
    }
}