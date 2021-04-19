using System.ComponentModel;
using CMI.Contract.Parameter;

namespace CMI.Manager.Asset.ParameterSettings
{
    public class AssetPriorisierungSettings : ISetting
    {
        [Description("Definiert wie die Asset Packages gemäss ihrer Grösse in Kategorien S-XL eingeteilt werden sollen. " +
                     "Die Einstellung ist als JSON String einzugeben. Details dazu im Handbuch oder gemäss Default-Einstellung.")]
        [DefaultValue(@"
                        {
	                        ""MaxSmallSizeInMB"": 250,
	                        ""MaxMediumSizeInMB"": 1000,
	                        ""MaxLargeSizeInMB"": 4000,
	                        ""ExtraLargeSizeInMB"": 2147483647
                        }")]
        public string PackageSizes { get; set; }

        [Description("Definiert welche Priorisierungskategorien (1-9) auf welche Kanäle aufgeteilt werden sollen. " +
                     "Die Einstellung ist als JSON String einzugeben. Details dazu im Handbuch oder gemäss Default-Einstellung.")]
        [DefaultValue(@"
                        {
                            ""Channel1"": ""1,2,3"",
                            ""Channel2"": ""1,2,3,4,5,6,7"",
                            ""Channel3"": ""1,2,3,4,5,6,7,8,9"",
                            ""Channel4"": ""6,7,8,9,1,2,3,4,5"",
                        }")]
        public string ChannelAssignments { get; set; }

        [Description("Definiert, in welchem Intervall Aufträge zur Synchonisation / Aufbereitung aufgegeben werden. (default 30 Sekunden)")]
        [DefaultValue("0/30 0/1 * 1/1 * ? *")]
        public string CheckAuftraegeJobIntervalAsCron { get; set; }

        [Description("Definiert, in welchem Intervall verwaiste Aufträge für die Löschung überprüft werden (default 1x Monat)")]
        [DefaultValue("0 0 12 1 1/1 ? *")]
        public string AlteAuftraegeLoeschenJobIntervalAsCron { get; set; }

        [Description("Aufträge die nach X Tagen nicht mehr aktualisiert wurden, werden gelöscht.")]
        [DefaultValue(30)]
        public int AuftraegeLoeschenAelterAlsXTage { get; set; }
    }
}