using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Results;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Dto;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using CMI.Web.Frontend.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CMI.Web.Frontend.api.Controllers
{
    [AllowAnonymous]
    [NoCache]
    public class DataController : ApiFrontendControllerBase
    {
        private static readonly UsageAnalyzer usageAnalyzer;
        private readonly IEntityProvider entityProvider;
        private readonly VeExportRecordHelper veExportRecordHelper;

        static DataController()
        {
            usageAnalyzer = new UsageAnalyzer(GetUsageSettings(), UsageType.Display);
        }

        public DataController(IEntityProvider entityProvider, VeExportRecordHelper veExportRecordHelper)
        {
            this.entityProvider = entityProvider;
            this.veExportRecordHelper = veExportRecordHelper;
        }

        [HttpGet]
        public IHttpActionResult GetPermissions(int entityId)
        {
            var access = GetUserAccess(WebHelper.GetClientLanguage(Request));

            var role = GetUserPublicClientRole();

            if (role != AccessRoles.RoleBAR)
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            var ear = entityProvider.GetEntity<ElasticArchiveRecord>(entityId, access);
            if (ear?.Data == null)
            {
                return StatusCode(HttpStatusCode.NotFound);
            }

            var permissionInfo = new PermissionInfo
            {
                MetadataAccessToken = ear.Data.MetadataAccessTokens?.ToArray(),
                PrimaryDataFulltextAccessTokens = ear.Data.PrimaryDataFulltextAccessTokens?.ToArray(),
                PrimaryDataDownloadAccessTokens = ear.Data.PrimaryDataDownloadAccessTokens?.ToArray()
            };

            return Ok(permissionInfo);
        }

        [HttpGet]
        public string GetArchivplanHtml(int id)
        {
            var role = GetUserPublicClientRole();
            var language = WebHelper.GetClientLanguage(Request);
            var access = GetUserAccess(language);

            return entityProvider.GetArchivplanHtml(id, access, role, language);
        }

        [HttpGet]
        public string GetArchivplanChildrenHtml(int id)
        {
            var role = GetUserPublicClientRole();
            var language = WebHelper.GetClientLanguage(Request);
            var access = GetUserAccess(language);
            return entityProvider.GetArchivplanChildrenHtml(id, access, role, language);
        }

        [HttpGet]
        public Entity<DetailRecord> GetEntity(int id, string language = null, [FromUri] string paging = null)
        {
            var access = GetUserAccess(language ?? WebHelper.GetClientLanguage(Request));

            // Auto-Deserialize did not work for some reason, so we have to do it manually..
            Paging p = null;
            if (!string.IsNullOrWhiteSpace(paging))
            {
                p = JsonConvert.DeserializeObject<Paging>(paging);
            }

            return entityProvider.GetEntity<DetailRecord>(id, access, p);
        }

        [HttpGet]
        public EntityResult<TreeRecord> GetEntities(string ids, string language = null, [FromUri] Paging paging = null)
        {
            var access = GetUserAccess(language ?? WebHelper.GetClientLanguage(Request));

            var idList = !string.IsNullOrEmpty(ids)
                ? ids.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries).Select(i => int.Parse(i)).ToList()
                : new List<int>();

            return entityProvider.GetEntities<TreeRecord>(idList, access, paging);
        }


        [HttpGet]
        public IHttpActionResult ExportSearchResultToExcel(string searchText)
        {
            ISearchResult searchResult;
            var search = JsonConvert.DeserializeObject<SearchParameters>(searchText);

            try
            {
                string language = WebHelper.GetClientLanguage(Request);
                var error = entityProvider.CheckSearchParameters(search, language);
                
                if (!string.IsNullOrEmpty(error))
                {
                    return new BadRequestErrorMessageResult(error, this);
                }
                var access = GetUserAccess(language);

                search.Paging.Skip = 0;
                search.Paging.Take = 10000;
                searchResult = entityProvider.Search<SearchRecord>(search, access);

                if (searchResult is SearchResult<SearchRecord> searchRecords)
                {
                    return ResponseMessage(veExportRecordHelper.CreateExcelFile(
                        ConvertExportData(searchRecords.Entities.Items.Select(i => i.Data).ToList()), language,
                        FrontendSettingsViaduc.Instance.GetTranslation(language, "veExportRecord.fileName") +
                        $"-{DateTime.Now.ToString("yyyy-MM-dd-hh_mm_ss")}.xlsx",
                        FrontendSettingsViaduc.Instance.GetTranslation("en", "veExportRecord.fileName") +
                        $"-{DateTime.Now.ToString("yyyy-MM-dd-hh_mm_ss")}.xlsx"));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Search for {searchQuery} failed", JsonConvert.SerializeObject(search, Formatting.Indented));
                return InternalServerError(ex);
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]
        public ISearchResult Search([FromBody] SearchParameters search, string language = null)
        {
            ISearchResult result;

            try
            {
                language = language ?? WebHelper.GetClientLanguage(Request);

                var error = entityProvider.CheckSearchParameters(search, language);
                if (!string.IsNullOrEmpty(error))
                {
                    return new ErrorSearchResult
                    {
                        Error = new ApiError
                        {
                            StatusCode = (int) HttpStatusCode.Forbidden,
                            Message = error,
                            Details = string.Empty
                        }
                    };
                }

                var access = GetUserAccess(language);
                var userId = ControllerHelper.GetCurrentUserId();

                if (usageAnalyzer.GetExceededThreshold(userId, Request) != null)
                {
                    if (string.IsNullOrEmpty(search?.Captcha?.Token))
                    {
                        return GetCaptchaMissing(language);
                    }

                    if (!SecurityHelper.IsValidCaptcha(search.Captcha, FrontendSettingsViaduc.Instance.GetServerSettings()))
                    {
                        return GetCaptchaInvalid(language);
                    }

                    usageAnalyzer.Reset(userId, Request);
                }

                result = entityProvider.Search<SearchRecord>(search, access);

                if (result is SearchResult<SearchRecord> searchResult)
                {
                    usageAnalyzer.UpdateUsageStatistic(userId, Request, searchResult.Entities.Items.Count);
                    if (usageAnalyzer.GetExceededThreshold(userId, Request) != null)
                    {
                        return GetCaptchaMissing(language);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Search for {searchQuery} failed", JsonConvert.SerializeObject(search, Formatting.Indented));
                result = new ErrorSearchResult
                {
                    Error = new ApiError
                    {
                        StatusCode = (int) HttpStatusCode.InternalServerError,
                        Message = FrontendSettingsViaduc.Instance.GetTranslation(language, "search.unexpectedSystemError",
                            "Es ist ein unerwarteter Fehler aufgetreten.")
                    }
                };
            }

            return result;
        }

        [HttpGet]
        public IHttpActionResult SearchBySignatur(string signatur)
        {
            var result = string.Empty;

            try
            {
                var records = SearchByReferenceCodeWithoutSecurity(signatur);
                if (records.Count > 1)
                {
                    ThrowBadRequestMessageForNonUniqueRefCodeSearch();
                }

                if (records.Count == 1)
                {
                    result = records[0].Data.ArchiveRecordId;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Fehler beim Prüfen der Signatur:={signatur}");
                throw;
            }

            return Ok(result);
        }

        [HttpGet]
        // Returns true if the current user is Ö2 user and has a Einsichtsgesucht for the given signatur.
        public IHttpActionResult HasCurrentOe2UserEinsichtsgesuchForSignatur(string signatur)
        {
            var result = false;

            try
            {
                var records = SearchByReferenceCodeWithoutSecurity(signatur);
                if (records.Count > 1)
                {
                    ThrowBadRequestMessageForNonUniqueRefCodeSearch();
                }

                if (records.Count == 1)
                {
                    var user = GetUserAccess();
                    if (user.CombinedTokens.Contains(AccessRoles.RoleOe2))
                    {
                        var downloadTokens = records[0].Data.PrimaryDataDownloadAccessTokens;
                        result = downloadTokens.Contains($"EB_{user.UserId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Fehler beim Ermitteln ob OE2 Einsichtsgesucht vorliegt. Signatur:={signatur}");
                throw;
            }

            // Need to convert to Json, because Frontent client cannot parse direct value
            return Ok(new BooleanResponseDto {Value = result});
        }


        private void ThrowBadRequestMessageForNonUniqueRefCodeSearch()
        {
            var settings = FrontendSettingsViaduc.Instance;
            var msg = settings.GetTranslation(WebHelper.GetClientLanguage(Request),
                "searchErrors.multipleResultsFound",
                "Die Suche nach dieser Signatur hat mehrere Resultate erbracht. Bitte geben Sie eine eindeutige Signatur ein.");

            throw new BadRequestException(msg);
        }

        private List<Entity<SearchRecord>> SearchByReferenceCodeWithoutSecurity(string signatur)
        {
            var searchResult = entityProvider.SearchByReferenceCodeWithoutSecurity<SearchRecord>(signatur);

            var searchRecordResult = searchResult as SearchResult<SearchRecord>;
            return searchRecordResult?.Entities?.Items;
        }

        private static SearchUsageSettings GetUsageSettings()
        {
            var settings = FrontendSettingsViaduc.Instance.GetServerSettings();
            var captcha = JsonHelper.FindTokenValue<JObject>(settings, "captcha");
            var usage = JsonHelper.GetByPath<JObject>(captcha, "server.usage");
            var usageSettings = usage != null ? usage.ToObject<SearchUsageSettings>() : new SearchUsageSettings();

            usageSettings.Update();

            return usageSettings;
        }

        private string GetUserPublicClientRole()
        {
            var role = ControllerHelper.UserDataAccess.GetRoleForClient(ControllerHelper.GetCurrentUserId());
            return !string.IsNullOrWhiteSpace(role) ? role : AccessRoles.RoleOe1;
        }

        private ErrorSearchResult GetCaptchaMissing(string language)
        {
            var settings = FrontendSettingsViaduc.Instance;

            return new ErrorSearchResult
            {
                Error = new ApiError
                {
                    StatusCode = (int) HttpStatusCode.PreconditionFailed,
                    Identifier = "Captcha.Missing",
                    Message = settings.GetTranslation(language, "search.captchaMissing", "Es fehlen die Daten zur Captcha-Überprüfung.")
                }
            };
        }

        private ErrorSearchResult GetCaptchaInvalid(string language)
        {
            var settings = FrontendSettingsViaduc.Instance;

            return new ErrorSearchResult
            {
                Error = new ApiError
                {
                    StatusCode = (int) HttpStatusCode.PreconditionFailed,
                    Identifier = "Captcha.Invalid",
                    Message = settings.GetTranslation(language, "search.captchaInvalid", "Die Daten zur Captcha-Überprüfung sind ungültig."),
                    Details = settings.GetTranslation(language, "search.captchaInvalidDetails", "Bitte versuchen Sie es erneut.")
                }
            };
        }

        private List<VeExportRecord> ConvertExportData(List<SearchRecord> searchResults)
        {
            return searchResults.Select(item => new VeExportRecord
            {
                ReferenceCode = item.ReferenceCode,
                FileReference = VeExportRecordHelper.GetCustomField(item.CustomFields, "aktenzeichen"),
                Title = item.Title,
                CreationPeriod = item.CreationPeriod?.Text,
                WithinInfo = item.WithinInfo,
                Level = item.Level,
                Accessibility = VeExportRecordHelper.GetCustomField(item.CustomFields, "zugänglichkeitGemässBga")
            }).ToList();
        }
    }
}