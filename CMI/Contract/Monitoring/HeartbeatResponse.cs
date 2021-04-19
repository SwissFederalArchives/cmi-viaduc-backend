namespace CMI.Contract.Monitoring
{
    public class HeartbeatResponse
    {
        public HeartbeatResponse(MonitoredServices type, HeartbeatStatus status)
        {
            Type = type;
            Status = status;
        }

        public HeartbeatStatus Status { get; set; }
        public MonitoredServices Type { get; set; }
        public long? HartbeatResponseTime { get; set; }
    }
}