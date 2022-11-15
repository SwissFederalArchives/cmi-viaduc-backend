
using System.Net.Http;
using System.Threading.Tasks;
using CMI.Access.Onboarding.Response;

namespace CMI.Access.Onboarding
{
    public interface IOnboardingConnector
    {
        Task<string> GetAccessToken();
        Task<string> StartProcess(string json);
        Task<Status> GetProcessById(string id);
        Task<byte[]> GetDocumentByUri(string uri);
    }
}
