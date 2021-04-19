using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.File;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Utilities.Cache.Access;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.Auth;
using MassTransit;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    public class FileController : ApiManagementControllerBase
    {
        private readonly ICacheHelper cacheHelper;
        private readonly IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse> doesExistInCacheClient;
        private readonly IRequestClient<DownloadAssetRequest, DownloadAssetResult> downloadClient;
        private readonly IFileDownloadHelper downloadHelper;
        private readonly IDownloadTokenDataAccess downloadTokenDataAccess;
        private readonly IPublicOrder orderManagerClient;

        public FileController(IPublicOrder orderManagerClient,
            IDownloadTokenDataAccess downloadTokenDataAccess,
            IFileDownloadHelper downloadHelper,
            IRequestClient<DownloadAssetRequest, DownloadAssetResult> downloadClient,
            IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse> doesExistInCacheClient,
            ICacheHelper cacheHelper)
        {
            this.orderManagerClient = orderManagerClient;
            this.downloadTokenDataAccess = downloadTokenDataAccess;
            this.downloadHelper = downloadHelper;
            this.downloadClient = downloadClient;
            this.doesExistInCacheClient = doesExistInCacheClient;
            this.cacheHelper = cacheHelper;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IHttpActionResult> DownloadFile(int id, string token)
        {
            var orderItemId = id;
            if (string.IsNullOrWhiteSpace(token))
            {
                return Content(HttpStatusCode.Forbidden, "Invalid token");
            }

            var ipAdress = downloadHelper.GetClientIp(Request);
            if (!downloadTokenDataAccess.CheckTokenIsValidAndClean(token, orderItemId, DownloadTokenType.OrderItem, ipAdress))
            {
                return BadRequest("Token expires or is not valid");
            }

            var userId = downloadTokenDataAccess.GetUserIdByToken(token, orderItemId, DownloadTokenType.OrderItem, ipAdress);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Content(HttpStatusCode.Forbidden, "No User found for the requested Downloadtoken");
            }

            var orderItem = (await orderManagerClient.FindOrderItems(new[] {orderItemId})).FirstOrDefault();
            if (orderItem == null)
            {
                return BadRequest("OrderItem does not exist in DB");
            }

            if (!orderItem.Benutzungskopie.HasValue || !orderItem.Benutzungskopie.Value)
            {
                return BadRequest("OrderItem is not a Benutzungskopie");
            }

            downloadTokenDataAccess.CleanUpOldToken(token, orderItemId, DownloadTokenType.OrderItem);

            var downloadAssetResult = await downloadClient.Request(new DownloadAssetRequest
            {
                ArchiveRecordId = orderItem.VeId.ToString(),
                OrderItemId = orderItemId,
                AssetType = AssetType.Benutzungskopie,
                Recipient = userId,
                RetentionCategory = CacheRetentionCategory.UsageCopyBenutzungskopie,
                ForceSendPasswordMail = true
            });

            // If item is not in cache, indicate that it is gone
            if (string.IsNullOrEmpty(downloadAssetResult.AssetDownloadLink))
            {
                return ResponseMessage(new HttpResponseMessage(HttpStatusCode.Gone));
            }

            var stream = cacheHelper.GetStreamFromCache(downloadAssetResult.AssetDownloadLink);
            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };

            var fileName = orderItemId + ".zip";
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = fileName
            };

            return ResponseMessage(result);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetOneTimeToken(int orderItemId)
        {
            var access = this.GetManagementAccess();
            var userId = access.UserId;

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtAuftraegeKannDownloadGebrauchskopieAusfuehren))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            var orderItem = (await orderManagerClient.FindOrderItems(new[] {orderItemId})).FirstOrDefault();
            if (orderItem == null)
            {
                return BadRequest("OrderItem does not exist in DB");
            }

            var doesExistInCacheResponse = await doesExistInCacheClient.Request(new DoesExistInCacheRequest
            {
                Id = orderItemId.ToString(),
                RetentionCategory = CacheRetentionCategory.UsageCopyBenutzungskopie
            });

            if (!doesExistInCacheResponse.Exists)
            {
                return StatusCode(HttpStatusCode.Gone);
            }

            var ipAddress = downloadHelper.GetClientIp(Request);
            var expires = DateTime.Now.AddMinutes(downloadHelper.GetConfigValueTokenValidTime());
            var token = downloadHelper.CreateDownloadToken();

            downloadTokenDataAccess.CreateToken(token, orderItemId, DownloadTokenType.OrderItem, expires, ipAddress, userId);
            return Content(HttpStatusCode.OK, token);
        }
    }
}