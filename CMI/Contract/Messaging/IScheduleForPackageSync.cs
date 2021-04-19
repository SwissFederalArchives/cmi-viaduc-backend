namespace CMI.Contract.Messaging
{
    public interface IScheduleForPackageSync
    {
        ArchiveRecordAppendPackage Workload { get; set; }
    }
}