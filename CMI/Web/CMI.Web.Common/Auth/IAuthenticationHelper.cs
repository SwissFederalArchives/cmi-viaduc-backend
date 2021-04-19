using System.Collections.Generic;
using System.Net.Http;
using System.Security.Principal;

namespace CMI.Web.Common.Auth
{
    public interface IAuthenticationHelper
    {
        IList<ClaimInfo> GetClaims(IIdentity identity);
        IList<ClaimInfo> GetClaims(IPrincipal user);
        IList<ClaimInfo> GetClaimsForRequest(IIdentity identity, HttpRequestMessage request);
        IList<ClaimInfo> GetClaimsForRequest(IPrincipal user, HttpRequestMessage request);
    }
}