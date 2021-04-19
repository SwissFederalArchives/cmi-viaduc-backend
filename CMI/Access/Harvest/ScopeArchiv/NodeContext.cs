namespace CMI.Access.Harvest.ScopeArchiv
{
    /// <summary>
    ///     The node context holds the ids of the surrounding nodes
    ///     of an archive record
    /// </summary>
    public class NodeContext
    {
        public string PreviousArchiveRecordId { get; set; }
        public string NextArchiveRecordId { get; set; }
        public string ParentArchiveRecordId { get; set; }
        public string FirstChildArchiveRecordId { get; set; }
        public string ArchiveRecordId { get; set; }
    }
}