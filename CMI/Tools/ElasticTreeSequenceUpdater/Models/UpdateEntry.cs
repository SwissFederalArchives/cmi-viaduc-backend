namespace CMI.Tools.ElasticTreeSequenceUpdater.Models
{
    public class UpdateEntry
    {
        public long DbId { get; set; }   // Primary key from SQLite
        public string ScopeId { get; set; }
        public string ActaProId { get; set; }
        public int TreeSeq { get; set; }
        public bool Updated { get; set; }

        public string Id => !string.IsNullOrWhiteSpace(ScopeId) ? ScopeId : ActaProId;
    }
}

namespace CMI.Tools.ElasticTreeSequenceUpdater.Models
{
    /// <summary>
    /// Minimal model for archive index documents.
    /// Only includes the fields needed for TreeSequence updates.
    /// </summary>
    public class ArchiveRecord
    {
        /// <summary>
        /// The archive record ID (can be scopeId or actaProId depending on sync state).
        /// </summary>
        public string ArchiveRecordId { get; set; }

        /// <summary>
        /// The TreeSequence number for ordering.
        /// </summary>
        public int? TreeSequence { get; set; }
    }
}
