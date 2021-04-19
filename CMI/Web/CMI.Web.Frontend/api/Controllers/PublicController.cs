using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using CMI.Contract.Common;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Entities;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.App_Start;
using CMI.Web.Frontend.ParameterSettings;
using Newtonsoft.Json.Linq;
using Ninject;
using Serilog;

namespace CMI.Web.Frontend.api.Controllers
{
    [AllowAnonymous]
    [NoCache]
    [EnableCors("*", "*", "*")]
    public class PublicController : ApiFrontendControllerBase
    {
        private readonly IEntityProvider entityProvider;
        private readonly IModelData modelData;
        private readonly SynonymFinder synonymFinder;

        public PublicController(IEntityProvider entityProvider, IModelData modelData)
        {
            this.entityProvider = entityProvider;
            this.modelData = modelData;

            var woerterbuch = new FileWoerterbuch(new PhysicalFileSystem(), Path.Combine(DirectoryHelper.Instance.ConfigDirectory, "Synonyme"));
            var settingAsText = ServiceHelper.Settings["synonymMaxInputWords"];
            int maxInputWords;
            if (!string.IsNullOrEmpty(settingAsText))
            {
                maxInputWords = Convert.ToInt32(settingAsText);
            }
            else
            {
                maxInputWords = 15;
            }

            synonymFinder = new SynonymFinder(woerterbuch, maxInputWords);
        }

        [HttpGet]
        public JObject GetTranslations([FromUri] ApiClientInfo info)
        {
            return Settings.GetTranslations(info.language);
        }

        [HttpGet]
        public JObject GetSettings([FromUri] ApiClientInfo info)
        {
            var settings = Settings.GetSettings().DeepClone() as JObject;
            var archiveplan = JsonHelper.FindTokenValue<JToken>(settings, "archiveplan");
            var entryNodes = archiveplan != null ? JsonHelper.FindTokenValue<JArray>(archiveplan, "entryNodes") : null;
            var managementSettings = NinjectWebCommon.Kernel.Get<ManagementClientSettings>();
            JsonHelper.AddOrSet(settings, "managementClientSettings", JObject.FromObject(managementSettings));

            if (entryNodes != null)
            {
                try
                {
                    var access = GetUserAccess(WebHelper.GetClientLanguage(Request));

                    var ids = new List<int>();
                    foreach (var node in entryNodes.Children())
                    {
                        ids.Add(JsonHelper.FindTokenValue<int>(node, "archiveRecordId"));
                    }

                    var result = entityProvider.GetEntities<TreeRecord>(ids, access);
                    JsonHelper.Replace(entryNodes, JArray.FromObject(result.Items.Select(i => i.Data).ToArray()));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "failed to decorate entry nodes");
                }
            }

            return settings;
        }

        [HttpGet]
        public JObject GetModelData([FromUri] ApiClientInfo info)
        {
            var model = new JObject();
            try
            {
                if (!WebHelper.EnableModelDataCaching)
                {
                    modelData.Reset();
                }

                model = modelData.Data;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load model");
            }

            return model;
        }

        [HttpGet]
        public SynonymTreffer[] GetSynonyme(string fieldContent)
        {
            return synonymFinder.GetSynonyme(fieldContent, WebHelper.GetClientLanguage(Request));
        }

        [HttpPost]
        public JObject VerifyCaptcha([FromBody] CaptchaVerificationData data)
        {
            return SecurityHelper.VerifyCaptcha(data, FrontendSettingsViaduc.Instance.GetServerSettings());
        }
    }
}