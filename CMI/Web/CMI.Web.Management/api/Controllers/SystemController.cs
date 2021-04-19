using System.Net.Http;
using System.Web.Http;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Management.Helpers;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    [NoCache]
    public class SystemController : ApiControllerBase
    {
        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage GenerateTranslations(string language = null)
        {
            return new ManagementTranslationHelper().RunGeneration(language, Request);
        }
    }
}