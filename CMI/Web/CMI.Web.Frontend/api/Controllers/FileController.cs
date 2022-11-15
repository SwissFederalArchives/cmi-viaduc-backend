using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.File;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Utilities.Cache.Access;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.Helpers;
using MassTransit;
using Serilog;

namespace CMI.Web.Frontend.api.Controllers
{
    [Authorize]
    public class FileController : ApiControllerBase
    {
        private readonly ICacheHelper cacheHelper;
        private readonly IRequestClient<DownloadAssetRequest> downloadClient;
        private readonly IFileDownloadHelper downloadHelper;
        private readonly IDownloadLogDataAccess downloadLogDataAccess;
        private readonly IDownloadTokenDataAccess downloadTokenDataAccess;
        private readonly IElasticService elasticService;
        private readonly IKontrollstellenInformer kontrollstellenInformer;
        private readonly IOrderDataAccess orderDataAccess;
        private readonly IRequestClient<PrepareAssetRequest> prepareClient;
        private readonly IRequestClient<GetAssetStatusRequest> statusClient;
        private readonly ITranslator translator;
        private readonly IUsageAnalyzer usageAnalyzer;
        private readonly IUserDataAccess userDataAccess;

        public FileController(IRequestClient<DownloadAssetRequest> downloadClient,
            IRequestClient<GetAssetStatusRequest> statusClient,
            IRequestClient<PrepareAssetRequest> prepareClient,
            IDownloadTokenDataAccess downloadTokenDataAccess,
            IDownloadLogDataAccess downloadLogDataAccess,
            IElasticService elasticService,
            IUsageAnalyzer usageAnalyzer,
            IUserAccessProvider userAccessProvider,
            ITranslator translator,
            ICacheHelper cacheHelper,
            IUserDataAccess userDataAccess,
            IOrderDataAccess orderDataAccess,
            IFileDownloadHelper downloadHelper,
            IKontrollstellenInformer kontrollstellenInformer)
        {
            this.usageAnalyzer = usageAnalyzer;
            this.translator = translator;
            this.cacheHelper = cacheHelper;
            this.downloadClient = downloadClient;
            this.statusClient = statusClient;
            this.prepareClient = prepareClient;
            this.downloadTokenDataAccess = downloadTokenDataAccess;
            this.downloadLogDataAccess = downloadLogDataAccess;
            this.elasticService = elasticService;
            this.userDataAccess = userDataAccess;
            this.orderDataAccess = orderDataAccess;
            this.downloadHelper = downloadHelper;
            this.kontrollstellenInformer = kontrollstellenInformer;

            // Workaround für Unit-Test
            GetUserAccessFunc = userId =>
            {
                userId = string.IsNullOrWhiteSpace(userId) ? ControllerHelper.GetCurrentUserId() : userId;
                var language = WebHelper.GetClientLanguage(Request);

                return userAccessProvider.GetUserAccess(language, userId);
            };
        }

        public Func<string, UserAccess> GetUserAccessFunc { get; set; }

        private ElasticArchiveRecord GetRecord(int archiveRecordId, UserAccess access)
        {
            var entityResult = elasticService.QueryForId<ElasticArchiveRecord>(archiveRecordId, access);
            return entityResult.Entries.FirstOrDefault()?.Data;
        }

        [HttpPost]
        public async Task<IHttpActionResult> PrepareAsset(int id, string link, string lang)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(link))
                {
                    Log.Warning("'deepLinkToVe' is null or white spaces");
                }

                if (string.IsNullOrWhiteSpace(lang))
                {
                    Log.Warning("'language' is null or white spaces");
                }

                var access = GetUserAccessFunc(null);

                var userId = access.UserId;
                var record = GetRecord(id, access);

                if (record == null)
                {
                    return NotFound();
                }

                var packageId = record.PrimaryData.FirstOrDefault()?.PackageId ?? string.Empty;
                var status = CheckStatusAsync(packageId, record, access);
                if (!(status is StatusCodeResult) || ((StatusCodeResult) status).StatusCode != HttpStatusCode.OK)
                {
                    return status;
                }

                var prepareAssetRequest = new PrepareAssetRequest
                {
                    ArchiveRecordId = id.ToString(),
                    AssetType = AssetType.Gebrauchskopie,
                    CallerId = access.UserId,
                    AssetId = packageId,
                    RetentionCategory = await cacheHelper.GetRetentionCategory(record, access.RolePublicClient, orderDataAccess),
                    Recipient = userId,
                    DeepLinkToVe = link,
                    Language = lang ?? access.Language
                };
                var result = (await prepareClient.GetResponse<PrepareAssetResult>(prepareAssetRequest)).Message;
                return Ok(result);
            }
            catch (Exception e)
            {
                Log.Error(e, "(FileController:PrepareAsset({ID}))", id);
                throw;
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetAssetInfo(int id)
        {
            try
            {
                var access = GetUserAccessFunc(null);
                var record = GetRecord(id, access);

                if (record == null)
                {
                    return NotFound();
                }

                var packageId = record.PrimaryData?.FirstOrDefault()?.PackageId ?? string.Empty;
                var status = CheckStatusAsync(packageId, record, access);
                if (!(status is StatusCodeResult) || ((StatusCodeResult) status).StatusCode != HttpStatusCode.OK)
                {
                    return status;
                }

                var assetStatusRequest = new GetAssetStatusRequest
                {
                    ArchiveRecordId = id.ToString(),
                    AssetType = AssetType.Gebrauchskopie,
                    CallerId = access.UserId,
                    AssetId = packageId,
                    RetentionCategory = await cacheHelper.GetRetentionCategory(record, access.RolePublicClient, orderDataAccess)
                };
                var assetStatus = (await statusClient.GetResponse<GetAssetStatusResult>(assetStatusRequest)).Message;
                return Ok(assetStatus);
            }
            catch (Exception e)
            {
                Log.Error(e, "(FileController:GetAssetInfo({ID}))", id);
                throw;
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IHttpActionResult> DownloadFile(int id, string token, int reason = 0)
        {
            var archiveRecordId = id;
            if (string.IsNullOrWhiteSpace(token))
            {
                return Content(HttpStatusCode.Forbidden, "Invalid token");
            }

            var ipAdress = downloadHelper.GetClientIp(Request);
            if (!downloadTokenDataAccess.CheckTokenIsValidAndClean(token, archiveRecordId, DownloadTokenType.ArchiveRecord, ipAdress))
            {
                return BadRequest("Token expired or is not valid");
            }

            var userId = downloadTokenDataAccess.GetUserIdByToken(token, archiveRecordId, DownloadTokenType.ArchiveRecord, ipAdress);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Content(HttpStatusCode.Forbidden, "No User found for the requested Downloadtoken");
            }

            downloadTokenDataAccess.CleanUpOldToken(token, archiveRecordId, DownloadTokenType.ArchiveRecord);

            var access = GetUserAccessFunc(userId);
            var user = userDataAccess.GetUser(userId);

            var entityResult = elasticService.QueryForId<ElasticArchiveRecord>(archiveRecordId, access);
            var record = entityResult.Entries.FirstOrDefault()?.Data;

            if (record == null)
            {
                return NotFound();
            }

            var packageId = record.PrimaryData.FirstOrDefault()?.PackageId ?? "";
            if (string.IsNullOrEmpty(packageId))
            {
                return BadRequest("VE does not contain any primarydata and/or a valid packageid");
            }

            if (!CheckUserHasDownloadTokensForVe(access, record))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            try
            {
                if (reason != 0)
                {
                    userDataAccess.StoreDownloadReasonInHistory(record, user, access, reason);
                }

                var downloadAssetResult = (await downloadClient.GetResponse<DownloadAssetResult>(new DownloadAssetRequest
                {
                    ArchiveRecordId = archiveRecordId.ToString(),
                    AssetType = AssetType.Gebrauchskopie,
                    Recipient = userId,
                    AssetId = record.PrimaryData.FirstOrDefault()?.PackageId,
                    RetentionCategory = await cacheHelper.GetRetentionCategory(record, access.RolePublicClient, orderDataAccess)
                })).Message;

                var stream = cacheHelper.GetStreamFromCache(downloadAssetResult.AssetDownloadLink);
                var result = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(stream)
                };

                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = archiveRecordId + ".zip"
                };

                await kontrollstellenInformer.InformIfNecessary(access, new[] {new VeInfo(archiveRecordId, reason)});
                downloadLogDataAccess.LogVorgang(token, "Download");

                return ResponseMessage(result);
            }
            catch (Exception e)
            {
                Log.Error(e, "(FileController:DownloadFile({ID}))", archiveRecordId);
                throw;
            }
        }

        private IHttpActionResult CheckStatusAsync(string packageId, ElasticArchiveRecord record, UserAccess access)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                return BadRequest("VE does not contain any primarydata and/or a valid packageid");
            }

            if (!CheckUserHasDownloadTokensForVe(access, record))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            return StatusCode(HttpStatusCode.OK);
        }

        private bool CheckUserHasDownloadTokensForVe(UserAccess access, int id)
        {
            var record = GetRecord(id, access);
            return CheckUserHasDownloadTokensForVe(access, record);
        }

        private bool CheckUserHasDownloadTokensForVe(UserAccess access, ElasticArchiveRecord record)
        {
            if (record == null)
            {
                return false;
            }

            return access.HasAnyTokenFor(record.PrimaryDataDownloadAccessTokens);
        }

        [HttpGet]
        public IHttpActionResult GetOneTimeToken(int archiveRecordId)
        {
            var access = GetUserAccessFunc(null);
            var userId = access.UserId;
            var user = userDataAccess.GetUser(userId);

            if (!CheckUserHasDownloadTokensForVe(access, archiveRecordId))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            if (user.DownloadLimitDisabledUntil == null || user.DownloadLimitDisabledUntil < DateTime.Today)
            {
                usageAnalyzer.UpdateUsageStatistic(userId, Request, 1);
                var exceededThreshold = usageAnalyzer.GetExceededThreshold(userId, Request);
                if (exceededThreshold != null)
                {
                    var usageInterval = usageAnalyzer.GetText(exceededThreshold.Value.UsageInterval, access.Language);
                    var isEndingIn = usageAnalyzer.GetText(exceededThreshold.Value.IsEndingIn, access.Language);
                    var messageTemplate = translator.GetTranslation(access.Language,
                        "download.thresholdExceeded",
                        "Sie haben in den letzten {0} bereits {1} Dateien heruntergeladen. Die Maximal erlaubte Anzahl von Dateien ist damit erschöpft. Ein weiterer Download wird in {2} wieder möglich sein. Alternativ können Sie beim Applikationseigner ein Gesuch auf Anhebung Ihrer Download-Quota stellen.");

                    return Content(HttpStatusCode.PreconditionFailed,
                        string.Format(messageTemplate, usageInterval, exceededThreshold.Value.Usages, isEndingIn));
                }
            }

            var ipAdress = downloadHelper.GetClientIp(Request);
            var expires = DateTime.Now.AddMinutes(downloadHelper.GetConfigValueTokenValidTime());
            var token = downloadHelper.CreateDownloadToken();
            LogTokenGeneration(archiveRecordId, token);

            downloadTokenDataAccess.CreateToken(token, archiveRecordId, DownloadTokenType.ArchiveRecord, expires, ipAdress, userId);
            return Content(HttpStatusCode.OK, token);
        }

        private void LogTokenGeneration(int archiveRecordId, string token)
        {
            var access = GetUserAccessFunc(null);
            var ear = GetRecord(archiveRecordId, access);
            var signatur = ear?.ReferenceCode ?? "unbekannt";
            var titel = ear?.Title ?? "unbekannt";
            var schutzfrist = ear?.ProtectionEndDate?.Date;

            downloadLogDataAccess.LogTokenGeneration(token, access.UserId, string.Join(", ", access.CombinedTokens),
                signatur, titel, schutzfrist?.ToString("dd.MM.yyyy") ?? "unbekannt");
        }
    }
}