using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security.OAuth;
using Serilog;

namespace CMI.Web.Common.Auth
{
    public class AuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();
            return Task.CompletedTask;
        }

        public override async Task GrantClientCredentials(OAuthGrantClientCredentialsContext context)
        {
            try
            {
                var owinContext = context.OwinContext;
                var authResult = await owinContext.Authentication.AuthenticateAsync(DefaultAuthenticationTypes.ExternalCookie);
                var identity = authResult.Identity;
                context.OwinContext.Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

                context.Validated(identity);
            }
            catch (Exception ex)
            {
                // ignore, user has no external cookie
                Log.Error(ex, "Error when Granting Client Credentials");
            }
            finally
            {
                await base.GrantClientCredentials(context);
            }
        }
    }
}