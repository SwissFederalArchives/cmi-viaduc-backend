using System;
using System.Collections.Generic;
using CMI.Contract.Common;

namespace CMI.Access.Common.Tests
{
    public static class TestDataGenerator
    {
        public static ElasticArchiveRecord Generate(long id)
        {
            var record = new ElasticArchiveRecord
            {
                ArchiveRecordId = id.ToString()
            };

            record.ArchiveRecordId = "ID" + 71 * id;
            record.MetadataAccessTokens = GenerateAccessTokens(id);
            record.Title = GenerateTitel(id);
            record.ReferenceCode = "RC" + 3 * id;
            record.Level = PickFromArray(new[] {"Abteilung", "Bestand", "Serie", "Dossier", "Dokument"},
                id);

            record.CreationPeriod = GetTimePeriod(id);
            record.WithinInfo =
                PickFromArray(
                    new[] {"Diverse", "Gut erhaltene", "Teilweise historisch interessante", "Von Säure zerfressene"}, id) +
                " " +
                PickFromArray(
                    new[]
                    {
                        "Fotos", "Briefe", "Siegel", "Urkunden aus Pergament", "diplomatische Depeschen",
                        "verschlüsselte Nachrichten"
                    }, id);

            record.HasImage = false;
            record.HasAudioVideo = false;
            record.FormerReferenceCode = string.Empty;

            record.PlayingLengthInS = id % 31 == 0 ? (int) (91 * id) % 3600 : 0;
            record.Extent = PickFromArray(
                                new[] {"1", "2", "5", "21"}, id) +
                            " " +
                            PickFromArray(
                                new[]
                                {
                                    "Laufmeter", "Archivschachteln", "Dossiermappen", "Mikrofilme"
                                }, id);

            record.CanBeOrdered = id % 19 < 18;

            return record;
        }

        private static ElasticTimePeriod GetTimePeriod(long id)
        {
            var tup = GenerateVonBis(id);

            var p = new ElasticTimePeriod();

            p.StartDate = tup.Item1;
            p.EndDate = tup.Item2;

            return p;
        }

        private static List<string> GenerateAccessTokens(long id)
        {
            var tokens = new List<string>();

            tokens.Add("BAR");

            if (id % 21 < 18)
            {
                tokens.Add("Internal");

                if (id % 37 < 34)
                {
                    tokens.Add("Public");
                }
            }

            return tokens;
        }

        private static string GenerateTitel(long id)
        {
            // Baugesuch Peter Muster in Adliswil 
            // A         B     C         D

            string[] a =
            {
                "Baugesuch",
                "Foto von",
                "Urkunde betreffend",
                "Krankengeschichte von",
                "Wappen der Familie von",
                "Memoiren von",
                "Briefe an"
            };

            string[] b =
            {
                "Arthur", "Beat", "Charlotte", "Dominique", "Emiliane", "Freddy", "Georg", "Hans", "Ida", "Jakobus",
                "Klaus-Maria", "Ludwig", "Martha", "Norbert", "Otto", "Pauline", "Quéry", "Rudolf", "Sophia", "Theodore",
                "Udo", "Veronique", "Xaver", "Yvonne", "Zoé"
            };

            string[] c =
            {
                "Adenauer", "Brunner", "Chima", "Dormann", "Escher", "Fuhrmann", "Gerber", "Huber", "Ivanhoe",
                "Johnson", "Keller", "Leemann", "Nurber"
            };

            string[] d =
            {
                "Musterwil", "Thalwil", "Bern", "Zürich", "Adlikon", "London", "Berlin", "Gümligen", "Solothurn",
                "Bellinzona"
            };

            return PickFromArray(a, id) + " " + PickFromArray(b, id) + " " + PickFromArray(c, id) + " in " + PickFromArray(d, id);
        }

        private static string PickFromArray(string[] arr, long id)
        {
            return arr[id % arr.Length];
        }

        private static Tuple<DateTime, DateTime> GenerateVonBis(long id)
        {
            var x = new DateTime(2016, 1, 1);

            var years = (int) id % 167 + 1;

            var ago = (int) (id % 17 + id % 301);

            return new Tuple<DateTime, DateTime>(x.AddYears(-years - ago), x.AddYears(-ago));
        }
    }
}