using Newtonsoft.Json;

namespace CMI.Utilities.Transkribus
{
    internal class TranskribusDaten
    {
        [JsonProperty(PropertyName = "--verbose")]
        public int Verbose { get; set; }

        [JsonProperty(PropertyName = "--license.path")]
        public string LicensePath { get; set; }

        [JsonProperty(PropertyName = "--log")]
        public string Log { get; set; }

        [JsonProperty(PropertyName = "--log.performance")]
        public string LogPerformance { get; set; }

        [JsonProperty(PropertyName = "--log.performance_time")]
        public string LogPerformanceTime { get; set; }

        [JsonProperty(PropertyName = "mode")]
        public string Mode { get; set; }

        [JsonProperty(PropertyName = "--input_folder")]
        public string InputFolder { get; set; }

        [JsonProperty(PropertyName = "--output_folder")]
        public string OutputFolder { get; set; }

        [JsonProperty(PropertyName = "--work_dir")]
        public string WorkDir { get; set; }

        [JsonProperty(PropertyName = "--allow_pdf")]
        public bool? AllowPDF { get; set; }
    }


    public class TranskribusMode
    {
        public static string decoding = "decoding";
        public static string classification = "classification";
    }

}