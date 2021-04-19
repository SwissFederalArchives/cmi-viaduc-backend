namespace CMI.Contract.DocumentConverter
{
    public class ConversionStartRequest
    {
        public string JobGuid { get; set; }
        public string VideoQuality { get; set; }
        public string DestinationExtension { get; set; }
    }
}