using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.ParameterSettings;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Management.api.Controllers
{
    [AllowAnonymous]
    [NoCache]
    [EnableCors("*", "*", "*")]
    public class StammdatenController : ApiManagementControllerBase
    {
        private readonly IParameterHelper parameterHelper = new ParameterHelper();
        private readonly StammdatenDataAccess stammdatenAccess = new StammdatenDataAccess(WebHelper.Settings["sqlConnectionString"]);


        [HttpGet]
        public JArray GetCountries(string language = null)
        {
            return parameterHelper.GetSetting<LaenderSetting>().GetCountries(language ?? WebHelper.GetClientLanguage(Request));
        }

        [HttpGet]
        public IEnumerable<NameAndId> GetArtDerArbeiten()
        {
            return stammdatenAccess.GetArtDerArbeiten(WebHelper.GetClientLanguage(Request));
        }

        [HttpGet]
        public IEnumerable<NameAndId> GetReasons()
        {
            return stammdatenAccess.GetReasons(WebHelper.GetClientLanguage(Request));
        }
    }
}