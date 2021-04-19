using System;

namespace CMI.Contract.Monitoring
{
    public class AisDbCheckResponse
    {
        public bool Ok { get; set; }

        public string DbVersion { get; set; }

        public Exception Exception { get; set; }
    }
}