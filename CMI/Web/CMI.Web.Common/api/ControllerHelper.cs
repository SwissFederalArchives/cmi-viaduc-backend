using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Common.api
{
    public class ControllerHelper : IControllerHelper
    {
        private readonly ApiController apiController;

        private IApplicationRoleUserDataAccess applicationRoleUserDataAccess;
        private IUserDataAccess userDataAccess;

        public ControllerHelper(ApiController apiController)
        {
            this.apiController = apiController;
        }

        public IUserDataAccess UserDataAccess
        {
            get
            {
                if (userDataAccess == null)
                {
                    userDataAccess = new UserDataAccess(WebHelper.Settings["sqlConnectionString"]);
                }

                return userDataAccess;
            }
        }

        public IApplicationRoleUserDataAccess ApplicationRoleUserDataAccess => applicationRoleUserDataAccess ??
                                                                               (applicationRoleUserDataAccess =
                                                                                   new ApplicationRoleUserDataAccess(
                                                                                       WebHelper.Settings["sqlConnectionString"]));

        public string GetCurrentUserId()
        {
            var identity = apiController.User?.Identity as ClaimsIdentity;
            var uidClaim = identity != null && identity.Claims != null
                ? identity.Claims.FirstOrDefault(c => c.Type.Contains("/identity/claims/e-id/userExtId"))
                : null;
            return uidClaim?.Value;
        }

        public bool IsKerberosAuthentication()
        {
            var isKerberos = GetFromClaim("/identity/claims/authenticationmethod")?.ToLowerInvariant().Contains("kerberos".ToLowerInvariant());
            return isKerberos.GetValueOrDefault(false);
        }

        public bool IsSmartcartAuthentication()
        {
            var isSmartcard = GetFromClaim("/identity/claims/authenticationmethod")?.ToLowerInvariant().Contains("Smartcard".ToLowerInvariant());
            return isSmartcard.GetValueOrDefault(false);
        }

        public bool IsMTanAuthentication()
        {
            var isMTan = GetFromClaim("/identity/claims/authenticationmethod")?.ToLowerInvariant().Contains("nomadtelephony".ToLowerInvariant());
            return isMTan.GetValueOrDefault(false);
        }

        /// <summary>
        ///     Kerberos-/Smartcard Anmeldung
        /// </summary>
        /// <returns></returns>
        public bool IsInternalUser()
        {
            var validInternalAuth = new List<string> {"smartcardpki", "kerberos"};
            var authType = GetFromClaim("/identity/claims/authenticationmethod")?.ToLowerInvariant();
            bool? isInternal = !string.IsNullOrWhiteSpace(authType) && validInternalAuth.Any(x => authType.EndsWith(x));
            return isInternal.GetValueOrDefault(false);
        }

        public string GetFromClaim(string field)
        {
            var identity = apiController.User?.Identity as ClaimsIdentity;
            var uidClaim = identity != null && identity.Claims != null ? identity.Claims.FirstOrDefault(c => c.Type.Contains(field)) : null;

            return uidClaim?.Value;
        }

        public string GetMgntRoleFromClaim()
        {
            var identity = apiController.User?.Identity as ClaimsIdentity;
            var uidClaim = identity != null && identity.Claims != null
                ? identity.Claims.Where(c => c.Type.Contains("/identity/claims/role") || c.Type.Contains("/identity/claims/e-id/profile/role"))
                : null;

            var mgntRoleList = uidClaim?.Where(c => !string.IsNullOrEmpty(c.Value) && c.Value.Contains("BAR-recherche-management-client"));

            string mgntRole = null;
            if (mgntRoleList.Any())
            {
                mgntRole = mgntRoleList.Any(c => c.Value.EndsWith("APPO", StringComparison.InvariantCultureIgnoreCase))
                    ? "APPO"
                    : "ALLOW";
            }

            return mgntRole;
        }

        public bool HasClaims()
        {
            var identity = apiController.User?.Identity as ClaimsIdentity;
            return identity?.Claims != null && identity.Claims.Any();
        }
    }
}