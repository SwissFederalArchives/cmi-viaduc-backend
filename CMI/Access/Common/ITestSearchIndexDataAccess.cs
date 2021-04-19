using System.Threading.Tasks;

namespace CMI.Access.Common
{
    public interface ITestSearchIndexDataAccess
    {
        Task<ElasticTestResponse> GetElasticIndexHealth();
    }
}