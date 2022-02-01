using System;
using CMI.Contract.DocumentConverter;

namespace CMI.Web.Management.api.Data
{
    public class ProgressDetail
    {
        public string DetailId { get; set; }
        public string FileName { get; set; }
        public int Percentage { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public DateTime LastUpdate { get; set; }
        public DateTime StartedOn { get; set; }
        public string ProcessType { get; set; }
        public string ProcessState { get; set; }
        public bool Completed { get; set; }
        public bool Failed { get; set; }
        public JobContext Context { get; set; }
    }
}
