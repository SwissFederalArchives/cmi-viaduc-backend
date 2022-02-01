using System.Web.Http;
using System.Web.Http.Cors;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Management.api.Controllers
{
    [AllowAnonymous]
    [NoCache]
    [EnableCors("*", "*", "*")]
    public class PublicController : ApiManagementControllerBase
    {
        [HttpGet]
        public JObject GetTranslations([FromUri] ApiClientInfo info)
        {
            return Settings.GetTranslations(info.language);
        }

        [HttpGet]
        public JObject GetSettings([FromUri] ApiClientInfo info)
        {
            var settings = Settings.GetSettings().DeepClone() as JObject;

            JsonHelper.AddOrSet(settings, "publicClientUrl", WebHelper.PublicClientUrl);

            return settings;
        }
    }
}