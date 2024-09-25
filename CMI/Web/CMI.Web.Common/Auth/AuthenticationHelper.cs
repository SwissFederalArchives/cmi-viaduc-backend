using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Owin.Security.OAuth;

namespace CMI.Web.Common.Auth
{
    public class ClaimInfo
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string Issuer { get; set; }

        public static ClaimInfo ConvertClaimToClaimInfo(Claim claim)
        {
            if (claim == null)
            {
                return null;
            }

            return new ClaimInfo()
            {
                Type = claim.Type,
                Value = claim.Value,
                Issuer = claim.Issuer
            };
        }
    }

    public class AuthenticationHelper : IAuthenticationHelper
    {
        public static OAuthBearerAuthenticationOptions WebApiBearerAuthenticationOptions { get; set; }

        public IList<ClaimInfo> GetClaims(IIdentity identity)
        {
            var claimsIdentity = identity as ClaimsIdentity;
            var claims = claimsIdentity?.Claims ?? Enumerable.Empty<Claim>();
            var transformedClaims = claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList();

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