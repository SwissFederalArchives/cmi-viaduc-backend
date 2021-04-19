using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using CMI.Web.Common.Helpers;
using Microsoft.Owin.Security.OAuth;

namespace CMI.Web.Common.Auth
{
    public class ClaimInfo
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string Issuer { get; set; }
    }

    public class AuthenticationHelper : IAuthenticationHelper
    {
        public static int TokenExpiryInMinutes => WebHelper.GetIntSetting("tokenExpiryInMinutes", 12 * 60);

        public static OAuthBearerAuthenticationOptions WebApiBearerAuthenticationOptions { get; set; }

        public IList<ClaimInfo> GetClaims(IIdentity identity)
        {
            var claimsIdentity = identity as ClaimsIdentity;
            var claims = claimsIdentity?.Claims ?? Enumerable.Empty<Claim>();
            var transformedClaims = claims.Select(c => new ClaimInfo
            {
                Type = c.Type,
                Value = c.Value,
                Issuer = c.Issuer
            }).ToList();

            return transformedClaims;
        }

        public IList<ClaimInfo> GetClaims(IPrincipal user)
        {
            return GetClaims(user?.Identity);
        }

        public IList<ClaimInfo> GetClaimsForRequest(IIdentity identity, HttpRequestMessage request)
        {
            var claims = GetClaims(identity);
            return claims;
        }

        public IList<ClaimInfo> GetClaimsForRequest(IPrincipal user, HttpRequestMessage request)
        {
            return GetClaimsForRequest(user?.Identity, request);
        }
    }
}