
using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;

namespace CMI.Web.Common.Auth
{
    /// <summary>
    /// https://github.com/blowdart/AspNetSameSiteSamples/blob/master/AspNet472CSharpMVC5/SameSiteCookieManager.cs
    /// </summary>
    public class SameSiteCookieManager : ICookieManager
    {
        private readonly ICookieManager innerManager;

        public SameSiteCookieManager() : this(new CookieManager())
        {
        }

        public SameSiteCookieManager(ICookieManager innerManager)
        {
            this.innerManager = innerManager;
        }

        public void AppendResponseCookie(IOwinContext context, string key, string value,
            CookieOptions options)
        {
            CheckSameSite(context, options);
            innerManager.AppendResponseCookie(context, key, value, options);
        }

        public void DeleteCookie(IOwinContext context, string key, CookieOptions options)
        {
            CheckSameSite(context, options);
            innerManager.DeleteCookie(context, key, options);
        }

        public string GetRequestCookie(IOwinContext context, string key)
        {
            return innerManager.GetRequestCookie(context, key);
        }

        private void CheckSameSite(IOwinContext context, CookieOptions options)
        {
            if (options.SameSite == SameSiteMode.None && BrowserDetection.DisallowsSameSiteNone(context.Request.Headers["User-Agent"]))
            {
                options.SameSite = null;
            }
        }
    }

}
