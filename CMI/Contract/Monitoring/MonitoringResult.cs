namespace CMI.Contract.Monitoring
{
    public class MonitoringResult
    {
        public string MonitoredServices { get; set; }
        public string Status { get; set; }
        public long? ExecutionTime { get; set; }
        public string Message { get; set; }
    }
}