using System.ComponentModel;

namespace CMI.Contract.Common
{
    public enum ActionStatus
    {
        [Description("Warte auf Synchronisation")]
        WaitingForSync = 0,

        [Description("Synchronisation wird durchgeführt")]
        SyncInProgress,

        [Description("Synchronisation erfolgreich beendet")]
        SyncCompleted,

        [Description("Synchronisation mit Fehler beendet")]
        SyncFailed,

        [Description("Synchronisation vorzeitig abgebrochen")]
        SyncAborted
    }
}