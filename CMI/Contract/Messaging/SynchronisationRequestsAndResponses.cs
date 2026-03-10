using CMI.Contract.Common.Entities;
using System.Collections.Generic;

namespace CMI.Contract.Messaging
{
    public class GetLogDataResponse
    {
        public List<SyncActionLogDto> LogData { get; set; }
    }

    public class GetLogDataRequest
    {
        public long SyncActionId { get; set; }
    }

    public class BatchAddSyncActionsResponse
    {
        public bool Ok { get; set; }
    }

    public class BatchAddSyncActionsRequest
    {
        public string[] Ids { get; set; }
        public int Action { get; set; }
    }

    public class GetSyncDataResponse
    {
        public List<SyncActionDto> SyncData { get; set; }
    }

    public class GetSyncDataRequest
    {
        public int FilterId { get; set; }
    }

    public interface ISchedulerTrigger {}

    public class SchedulerTrigger : ISchedulerTrigger
    {
    }

    public class GetSyncNumberPerHourResponse
    {
        public List<VSyncNumberPerHourDto> SyncNumberPerHourItems { get; set; }
    }

    public class GetSyncNumberPerHourRequest
    {
        public int FilterDays { get; set; }
    }
}
