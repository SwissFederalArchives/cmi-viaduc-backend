using System;

namespace CMI.Contract.Monitoring;

public class SolrTestResponse
{
    public bool Ok { get; set; }

    public string SolrResponse { get; set; }

    public Exception Exception { get; set; }
}