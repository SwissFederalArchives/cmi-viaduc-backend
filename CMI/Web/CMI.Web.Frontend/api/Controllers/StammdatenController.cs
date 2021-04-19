using System.Collections.Generic;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.ParameterSettings;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Frontend.api.Controllers
{
    [AllowAnonymous]
    [NoCache]
    public class StammdatenController : ApiFrontendControllerBase
    {
        private readonly IEntityProvider entityProvider;
        private readonly IParameterHelper parameterHelper = new ParameterHelper();
        private readonly StammdatenDataAccess stammdatenAccess = new StammdatenDataAccess(WebHelper.Settings["sqlConnectionString"]);

        public StammdatenController(IEntityProvider entityProvider)
        {
            this.entityProvider = entityProvider;
        }

        [HttpGet]
        public IEnumerable<NameAndId> GetReasons()
        {
            return stammdatenAccess.GetReasons(WebHelper.GetClientLanguage(Request));
        }

        [HttpGet]
        public IEnumerable<NameAndId> GetArtDerArbeiten()
        {
            return stammdatenAccess.GetArtDerArbeiten(WebHelper.GetClientLanguage(Request));
        }

        [HttpGet]
        public JArray GetCountries(string language = null)
        {
            return parameterHelper.GetSetting<LaenderSetting>().GetCountries(language ?? WebHelper.GetClientLanguage(Request));
        }

        [HttpGet]
        public string[] GetCountriesElastic()
        {
            return entityProvider.GetCountriesFromElastic();
        }
    }
}