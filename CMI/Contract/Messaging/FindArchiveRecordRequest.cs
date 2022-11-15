namespace CMI.Contract.Messaging
{
    public class FindArchiveRecordRequest
    {
        public string ArchiveRecordId { get; set; }
        public bool IncludeFulltextContent { get; set; }
        /// <summary>
        /// If set to true, the returned record contains the unprotected texts for title,
        /// withinInfo and other fields. Use with caution and only in circumstances where
        /// the user making the call was checked to be in the BAR role or has the right for this record.
        /// </summary>
        public bool UseUnanonymizedData { get; set; }
    }
}