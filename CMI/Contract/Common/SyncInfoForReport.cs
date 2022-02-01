using System;

namespace CMI.Contract.Common
{
    public class SyncInfoForReport
    {
        public int MutationId { get; set; }
        public DateTime ErstellungsdatumPrimaerdatenVerbindung { get; set; }
        public DateTime StartErsterSynchronisierungsversuch { get; set; }
        public DateTime StartLetzterSynchronisierungsversuch { get; set; }
        public DateTime AbschlussSynchronisierung { get; set; }
        public int AnzahlNotwendigerSynchronisierungsversuche { get; set; }
    }
}
