using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Interfaces;

namespace CMI.Web.Frontend.api.Configuration
{
    public class ElasticSettings : SettingsEntry, IElasticSettings
    {
        public static string RelativePath = "configuration.elastic";

        public string DefaultIndex { get; } = "archive";
        public string DefaultTypeName { get; } = "elasticarchiverecord";

        public string IdKey { get; } = nameof(ElasticArchiveRecord.ArchiveRecordId).ToLowerCamelCase();
        public string ArchivePlanContextKey { get; } = nameof(ElasticArchiveRecord.ArchiveplanContext).ToLowerCamelCase();

        public string IdField { get; } = "archiveRecordId";
        public string ParentIdField { get; } = "parentArchiveRecordId";

        public string BaseUrl { get; set; }

        public string Username { get; set; } = "elastic";
        public string Password { get; set; } = "changeme";

        public ElasticSettingDebug Debug { get; set; } = new ElasticSettingDebug();
    }

    public class ElasticSettingDebug
    {
        public bool FetchRequestJson { get; set; } = true;
        public bool FetchResponseJson { get; set; } = true;
    }
}