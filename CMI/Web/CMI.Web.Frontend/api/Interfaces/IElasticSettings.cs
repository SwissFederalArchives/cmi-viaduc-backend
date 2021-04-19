using CMI.Web.Frontend.api.Configuration;

namespace CMI.Web.Frontend.api.Interfaces
{
    public interface IElasticSettings
    {
        string BaseUrl { get; set; }

        string DefaultIndex { get; }
        string DefaultTypeName { get; }

        string IdKey { get; }
        string ArchivePlanContextKey { get; }

        string IdField { get; }
        string ParentIdField { get; }

        string Username { get; set; }
        string Password { get; set; }

        ElasticSettingDebug Debug { get; set; }
    }
}