using System.Collections.Generic;

namespace CMI.Contract.Common.Entities
{
    public class ManuelleKorrekturDetailItem
    {
        public ManuelleKorrekturDto ManuelleKorrektur { get; set; }
        public IEnumerable<ArchiveRecordContextItem> ArchivplanKontext { get; set; }
        public IEnumerable<ArchiveRecordContextItem> UntergeordneteVEs { get; set; }
        public IEnumerable<ArchiveRecordContextItem> VerweiseVEs { get; set; }
    }
}
