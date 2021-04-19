using System;

namespace CMI.Manager.Asset
{
    public class PreparationStatus
    {
        public bool PackageIsInPreparationQueue { get; set; }
        public DateTime AddedToQueueOn { get; set; }
        public TimeSpan EstimatedPreparationDuration { get; set; }
    }
}