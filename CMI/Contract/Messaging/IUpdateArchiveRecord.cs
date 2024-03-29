﻿using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public interface IUpdateArchiveRecord
    {
        long MutationId { get; set; }
        ArchiveRecord ArchiveRecord { get; set; }
        int PrimaerdatenAuftragId { get; set; }
        ElasticArchiveDbRecord ElasticArchiveDbRecord { get; set; }
        // This flag can be used to surpress the ArchiveRecordUpdatedEvent
        bool DoNotReportCompletion { get; set; }
    }
}
