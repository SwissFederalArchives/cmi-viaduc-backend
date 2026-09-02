using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Entities;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.ParameterSettings;
using MassTransit;
using Newtonsoft.Json.Linq;
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
        private readonly ManagementClientSettings managementClientSettings;
        private readonly FrontendDynamicTextSettings frontendDynamicTextSettings;
        private readonly SynonymFinder synonymFinder;
        private readonly IElasticService elasticService;
        private readonly IRequestClient<IGetSecurityTokens> securityTokenClient;


        public PublicController(IEntityProvider entityProvider, IModelData modelData, ManagementClientSettings managementClientSettings, FrontendDynamicTextSettings frontendDynamicTextSettings, 
            IElasticService elasticService, IRequestClient<IGetSecurityTokens> securityTokenClient)
        {
            this.entityProvider = entityProvider;
            this.modelData = modelData;
            this.managementClientSettings = managementClientSettings;
            this.frontendDynamicTextSettings = frontendDynamicTextSettings;
            this.elasticService = elasticService;
            this.securityTokenClient = securityTokenClient;
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

        internal class TranslatedFrontendDynamicTextSettings
        {
            public string DeliveryTypeDigital { get; set; }
            public string DeliveryTypeReadingRoom { get; set; }
            public string DeliveryTypeCommission { get; set; }
        }

        [HttpGet]
        public async Task<JObject> GetSettings([FromUri] ApiClientInfo info)
        {
            var settings = Settings.GetSettings().DeepClone() as JObject;
            var archiveplan = JsonHelper.FindTokenValue<JToken>(settings, "archiveplan");
            var entryNodes = archiveplan != null ? JsonHelper.FindTokenValue<JArray>(archiveplan, "entryNodes") : null;
            
            JsonHelper.AddOrSet(settings, "managementClientSettings", JObject.FromObject(managementClientSettings));

            var selectedLanguage = WebHelper.GetClientLanguage(Request);
            JsonHelper.AddOrSet(settings, "frontendDynamicTextSettings", JObject.FromObject(GetTranslatedFrontendDynamicTextSettings(selectedLanguage)));

            if (entryNodes != null)
            {
                try
                {
                    var access = GetUserAccess(selectedLanguage);

                    var ids = new List<string>();
                    foreach (var node in entryNodes.Children())
                    {
                        ids.Add(JsonHelper.FindTokenValue<string>(node, "archiveRecordId"));
                    }

                    var result = await entityProvider.GetEntities<TreeRecord>(ids, access);
                    JsonHelper.Replace(entryNodes, JArray.FromObject(result.Items.Select(i => i.Data).ToArray()));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "failed to decorate entry nodes");
                }
            }

            return settings;
        }

        private TranslatedFrontendDynamicTextSettings GetTranslatedFrontendDynamicTextSettings(string selectedLanguage)
        {
            TranslatedFrontendDynamicTextSettings translatedFrontendDynamicTextSettings;
            switch (selectedLanguage)
            {
                case "de":
                    translatedFrontendDynamicTextSettings = new TranslatedFrontendDynamicTextSettings()
                    {
                        DeliveryTypeDigital = frontendDynamicTextSettings.DeliveryTypeDigitalDE,
                        DeliveryTypeReadingRoom = frontendDynamicTextSettings.DeliveryTypeReadingRoomDE,
                        DeliveryTypeCommission = frontendDynamicTextSettings.DeliveryTypeCommissionDE
                    };
                    break;
                case "fr":
                    translatedFrontendDynamicTextSettings = new TranslatedFrontendDynamicTextSettings()
                    {
                        DeliveryTypeDigital = frontendDynamicTextSettings.DeliveryTypeDigitalFR,
                        DeliveryTypeReadingRoom = frontendDynamicTextSettings.DeliveryTypeReadingRoomFR,
                        DeliveryTypeCommission = frontendDynamicTextSettings.DeliveryTypeCommissionFR
                    };
                    break;
                case "it":
                    translatedFrontendDynamicTextSettings = new TranslatedFrontendDynamicTextSettings()
                    {
                        DeliveryTypeDigital = frontendDynamicTextSettings.DeliveryTypeDigitalIT,
                        DeliveryTypeReadingRoom = frontendDynamicTextSettings.DeliveryTypeReadingRoomIT,
                        DeliveryTypeCommission = frontendDynamicTextSettings.DeliveryTypeCommissionIT
                    };
                    break;
                case "en":
                    translatedFrontendDynamicTextSettings = new TranslatedFrontendDynamicTextSettings()
                    {
                        DeliveryTypeDigital = frontendDynamicTextSettings.DeliveryTypeDigitalEN,
                        DeliveryTypeReadingRoom = frontendDynamicTextSettings.DeliveryTypeReadingRoomEN,
                        DeliveryTypeCommission = frontendDynamicTextSettings.DeliveryTypeCommissionEN
                    };
                    break;
                default:
                    translatedFrontendDynamicTextSettings = new TranslatedFrontendDynamicTextSettings()
                    {
                        DeliveryTypeDigital = frontendDynamicTextSettings.DeliveryTypeDigitalDE,
                        DeliveryTypeReadingRoom = frontendDynamicTextSettings.DeliveryTypeReadingRoomDE,
                        DeliveryTypeCommission = frontendDynamicTextSettings.DeliveryTypeCommissionDE
                    };
                    break;
            }

            return translatedFrontendDynamicTextSettings;
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
        [HttpGet]
        [Route("api/Public/GetAccessTokens")]
        public async Task<IHttpActionResult> GetAccessTokens(string id)
        {
            var result = new AccessTokenCheckResult
            {
                VeId = id,
                Calculated = new AccessTokens(),
                Elastic = new AccessTokens(),
                CheckError = false
            };

            try
            {
                var securityResponse = await securityTokenClient.GetResponse<GetSecurityTokensResponse>(new { ArchiveRecordId = id });
                result.Calculated = securityResponse.Message.Calculated ?? new AccessTokens();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SecurityTokens not available for id {Id}", id);
            }

            try
            {
                var elastictokens = await elasticService.QueryTokensForId(id);
                if (elastictokens != null)
                {
                    result.Elastic = elastictokens;
                    result.CheckError = !AccessTokenComparer.TokensMatch(result.Calculated, result.Elastic);
                    
                }

              
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Elastic record not available for id {Id}", id);
            }

            var json = JObject.FromObject(result); // Keep as JObject, not .ToString()
            return Json(json, new Newtonsoft.Json.JsonSerializerSettings
            {
                Formatting = Newtonsoft.Json.Formatting.Indented
            });


        }

        
    }
}