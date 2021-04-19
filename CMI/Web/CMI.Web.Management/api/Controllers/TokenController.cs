using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.AblieferndeStellen;
using CMI.Contract.Common;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.api.Data;
using CMI.Web.Management.Auth;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    [NoCache]
    public class TokenController : ApiManagementControllerBase
    {
        private readonly IAblieferndeStelleTokenDataAccess tokenDataAccess;

        public TokenController(IAblieferndeStelleTokenDataAccess tokenDataAccess)
        {
            this.tokenDataAccess = tokenDataAccess;
        }

        [HttpGet]
        public HttpResponseMessage GetAllTokens()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var result = new JObject();
            try
            {
                result.Add("tokens", JArray.FromObject(tokenDataAccess.GetAllTokens()));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Fehler beim laden der Tokens");
                result.Add("error", ServiceHelper.GetExceptionInfo(ex));
            }

            response.Content = new JsonContent(result);
            return response;
        }

        [HttpGet]
        public HttpResponseMessage GetToken(int tokenId)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var result = new JObject();
            try
            {
                if (tokenId <= 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                result.Add("token", JObject.FromObject(tokenDataAccess.GetToken(tokenId)));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Fehler beim laden des Tokens; TokenId:={tokenId}");
                result.Add("error", ServiceHelper.GetExceptionInfo(ex));
            }

            response.Content = new JsonContent(result);
            return response;
        }

        [HttpPost]
        public HttpResponseMessage CreateToken([FromBody] TokenPostData tokenData)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.BehoerdenzugriffAccessTokensBearbeiten);

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            JObject result = null;
            try
            {
                if (string.IsNullOrEmpty(tokenData?.Token))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                if (string.IsNullOrEmpty(tokenData.Bezeichnung))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                var newToken = tokenDataAccess.CreateToken(tokenData.Token, tokenData.Bezeichnung);
                if (newToken != null)
                {
                    result = new JObject {{"token", JObject.FromObject(newToken)}};
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Fehler beim erstellen eines Tokens; token:='{tokenData?.Token}', bezeichnung:='{tokenData?.Bezeichnung}'");
                result = new JObject {{"error", ServiceHelper.GetExceptionInfo(ex)}};
                response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (result != null)
            {
                response.Content = new JsonContent(result);
            }

            return response;
        }

        [HttpPost]
        public IHttpActionResult DeleteToken(int[] tokenIds)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.BehoerdenzugriffAccessTokensBearbeiten);

            tokenDataAccess.DeleteToken(tokenIds);
            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]
        public HttpResponseMessage UpdateToken([FromBody] TokenPostData tokenData)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.BehoerdenzugriffAccessTokensBearbeiten);

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            JObject result;
            try
            {
                if (string.IsNullOrEmpty(tokenData?.Token))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                if (tokenData.TokenId <= 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                if (string.IsNullOrEmpty(tokenData.Bezeichnung))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                tokenDataAccess.UpdateToken(tokenData.TokenId, tokenData.Token, tokenData.Bezeichnung);
                result = new JObject {{"success", true}};
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    $"Fehler beim aktualisieren des Tokens; tokenId:='{tokenData?.TokenId}', token:='{tokenData?.Token}', bezeichnung:='{tokenData?.Bezeichnung}'");
                result = new JObject {{"error", ServiceHelper.GetExceptionInfo(ex)}};
                response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            response.Content = new JsonContent(result);
            return response;
        }
    }
}