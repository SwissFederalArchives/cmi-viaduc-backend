namespace CMI.Contract.DocumentConverter
{
    public class ConversionStartRequest
    {
        public string JobGuid { get; set; }
        public string VideoQuality { get; set; }
        public string DestinationExtension { get; set; }
        public string PackageId { get; set; }
        public string ArchiveRecordId { get; set; }
    }
}
