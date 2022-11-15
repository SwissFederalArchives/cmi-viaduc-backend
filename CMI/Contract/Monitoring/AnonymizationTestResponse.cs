using CMI.Contract.Messaging;
using System;

namespace CMI.Contract.Monitoring
{
    public class AnonymizationTestResponse 
    {
        public bool IsSuccess { get; set; }
        public Exception Exception { get; set; }
    }
}