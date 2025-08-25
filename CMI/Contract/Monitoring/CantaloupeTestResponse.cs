using System;

namespace CMI.Contract.Monitoring;

public class CantaloupeTestResponse
{
    public bool Ok { get; set; }

    public string CantaloupeResponse { get; set; }

    public Exception Exception { get; set; }

}