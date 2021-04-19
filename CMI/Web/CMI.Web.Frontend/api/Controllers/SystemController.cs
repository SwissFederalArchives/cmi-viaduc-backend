using System.Net.Http;
using System.Web.Http;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Frontend.Helpers;

namespace CMI.Web.Frontend.api.Controllers
{
    [Authorize]
    [NoCache]
    public class SystemController : ApiControllerBase
    {
        [AllowAnonymous]
        [HttpGet]
        public HttpResponseMessage GenerateTranslations(string language = null)
        {
            return new FrontendTranslationHelper().RunGeneration(language, Request);
        }
    }
}