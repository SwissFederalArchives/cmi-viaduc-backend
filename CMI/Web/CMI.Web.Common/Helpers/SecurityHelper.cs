using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CMI.Web.Common.Helpers
{
    public static class SecurityHelper
    {
        public static JObject VerifyCaptcha(CaptchaVerificationData data, JObject settings)
        {
            Log.Debug("SecurityHelper:VerifyCaptcha: {data}", JsonConvert.SerializeObject(data, Formatting.Indented));
            var result = new JObject();
            try
            {
                var captcha = JsonHelper.FindTokenValue<JObject>(settings, "captcha");
                var verifyUrl = JsonHelper.GetByPath<string>(captcha, "server.verifyUrl");
                var secret = JsonHelper.GetByPath<string>(captcha, "server.secret");
                var verifyParams = $"secret={secret}&response={data.Token}";
                if (!string.IsNullOrEmpty(data.Ip))
                {
                    verifyParams += $"&remoteip={data.Ip}";
                }

                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    var response = client.UploadString(verifyUrl, verifyParams);
                    if (!string.IsNullOrEmpty(response) && response.Contains("{"))
                    {
                        result.Add("verify", JsonConvert.DeserializeObject<JToken>(response));
                    }
                    else
                    {
                        result.Add("verify", response);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not verify captcha");
            }

            return result;
        }

        public static bool IsValidCaptcha(CaptchaVerificationData data, JObject settings)
        {
            var result = VerifyCaptcha(data, settings);

            return result != null && JsonHelper.GetByPath<bool>(result, "verify.success");
        }

        public static string GetClientIp(HttpRequestMessage request)
        {
            return request.GetOwinContext()?.Request?.RemoteIpAddress;
        }
    }

    public class CaptchaVerificationData
    {
        public string Ip { get; set; }
        public string Token { get; set; }
    }
}