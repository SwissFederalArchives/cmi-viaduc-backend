using System.Collections.Generic;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.Auth;

namespace CMI.Web.Common.api
{
    public class Identity
    {
        public ClaimInfo[] IssuedClaims { get; set; }
        public string[] Roles { get; set; }
        public string[] IssuedAccessTokens { get; set; }
        public AuthStatus AuthStatus { get; set; }
        public string RedirectUrl { get; set; }
        public List<ApplicationRole> ApplicationRoles { get; set; }
        public IEnumerable<ApplicationFeatureInfo> ApplicationFeatures { get; set; }
    }
}