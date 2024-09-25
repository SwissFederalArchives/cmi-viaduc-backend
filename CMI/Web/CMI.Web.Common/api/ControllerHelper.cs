using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.Helpers;
using CMI.Contract.Common.Extensions;

namespace CMI.Web.Common.api
{
    public class ControllerHelper : IControllerHelper
    {
        private readonly List<Claim> claims;

        private IApplicationRoleUserDataAccess applicationRoleUserDataAccess;
        private IUserDataAccess userDataAccess;

        public ControllerHelper(IEnumerable<Claim> claims)
        {
            this.claims = claims.ToList();
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
            var uidClaim = claims?.Find(c => c.Type.Contains(ClaimValueNames.UserExtId));
            return uidClaim?.Value;
        }

        /// <summary>
        /// Identifizierte Benutzer sind solche mit QoA >= 40
        /// </summary>
        /// <returns></returns>
        public bool IsIdentifiedUser()
        {
            var qoAValue = GetQoAFromClaim();
            return qoAValue >= 40;
        }

        public string GetFromClaim(string field)
        {
            var uidClaim = claims?.Find(c => c.Type.Contains(field));

            return uidClaim?.Value;
        }

        public string GetMgntRoleFromClaim()
        {
            var uidClaim = claims?.Where(c => c.Type.Contains(ClaimValueNames.Role) || c.Type.Contains(ClaimValueNames.EIdProfileRole));

            var mgntRoleList = uidClaim?.Where(c => !string.IsNullOrEmpty(c.Value) && c.Value.Contains("BAR-recherche-management-client")).ToList();

            string mgntRole = null;
            if (mgntRoleList != null && mgntRoleList.Any())
            {
                mgntRole = mgntRoleList.Any(c => c.Value.EndsWith("APPO", StringComparison.InvariantCultureIgnoreCase))
                    ? "APPO"
                    : "ALLOW";
            }

            return mgntRole;
        }

        public bool HasClaims()
        {
            return claims != null && claims.Any();
        }

        public string GetInitialRoleFromClaim()
        {
            var qoAValue = GetQoAFromClaim();
            var homeName = GetFromClaim(ClaimValueNames.HomeName)?.ToLowerInvariant();

            switch (qoAValue)
            {
                case 20:
                case 30:
                    return AccessRoles.RoleOe2;
                case 40:
                case 50:
                case 60:
                    if (homeName != null && homeName.Contains("FED-LOGIN", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return AccessRoles.RoleBVW;
                    }

                    return AccessRoles.RoleOe3;
                default: 
                    throw new ArgumentException($"The passed QoAValue of {qoAValue} is not handled");
            }
          
        }

        public int GetQoAFromClaim()
        {
            var qoAValue = GetFromClaim(ClaimValueNames.AuthenticationMethod)?.ToLowerInvariant();
            qoAValue = qoAValue?.Replace("urn:qoa.eiam.admin.ch:names:tc:ac:classes:", "");
            if (int.TryParse(qoAValue, out int qoA))
            {
                return qoA;
            }

            return 0;
        }
    }
}