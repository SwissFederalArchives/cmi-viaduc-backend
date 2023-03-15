using System;

namespace CMI.Engine.Asset.PostProcess;

public class IiifManifestSettings
{
    public Uri ApiServerUri { get; set; }
    public Uri ImageServerUri { get; set; }
    // The URI where the manifests can be found
    public Uri PublicManifestWebUri { get; set; }
    // The URI to locate a detail element when adding the id of the record
    public Uri PublicDetailRecordUri { get; set; }
    public Uri PublicContentWebUri { get; set; }
    public Uri PublicOcrWebUri { get; set; }
}