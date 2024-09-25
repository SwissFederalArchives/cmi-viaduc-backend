using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.api;
using WebGrease.Css.Extensions;

namespace CMI.Web.Common.Auth
{
    public static class AuthorizationHelper
    {
        public static string RoleClaimPart => ClaimValueNames.EIdProfileRole;

        static AuthorizationHelper()
        {
            AllRoles = new List<string>();
            AllRoles.Add(AccessRoles.RoleOe1);
            AllRoles.Add(AccessRoles.RoleOe2);
            AllRoles.Add(AccessRoles.RoleOe3);
            AllRoles.Add(AccessRoles.RoleAS);
            AllRoles.Add(AccessRoles.RoleBAR);
            AllRoles.Add(AccessRoles.RoleBVW);
            AllRoles.Add(AccessRoles.RoleMgntAllow);
            AllRoles.Add(AccessRoles.RoleMgntAppo);

            AllRolesMapping = new Dictionary<string, string>();
            AllRoles.ForEach(role => AllRolesMapping.Add(StringHelper.ReplaceDiacritics(role.ToLowerInvariant()), role));
        }

        public static IList<string> AllRoles { get; }
        public static IDictionary<string, string> AllRolesMapping { get; }

        private static IEnumerable<string> GetRoles(IEnumerable<ClaimInfo> claims)
        {
            return claims.Where(claim => claim.Type.Contains(RoleClaimPart)).Select(claim => ExtractRoleFromValue(claim.Value));
        }

        private static string ExtractRoleFromValue(string value)
        {
            // Role should look like this:
            // 168871\BAR-recherche.ALLOW
            // 168871\BAR-recherche.BAR
            // 168871\BAR-recherche.OE1

            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var lastIndexOf = value.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);
            if (lastIndexOf > 0 && lastIndexOf + 1 < value.Length)
            {
                value = StringHelper.ReplaceDiacritics(value).Substring(lastIndexOf + 1);
            }

            // And shold return this:
            // ALLOW, BAR, OE1, etc.
            return value;
        }

        public static IEnumerable<string> GetAccessRoles(IEnumerable<string> roles)
        {
            return roles.Where(role => AllRolesMapping.ContainsKey(role.ToLowerInvariant())).Select(role => AllRolesMapping[role.ToLowerInvariant()]);
        }

        public static IEnumerable<string> GetAccessRoles(IEnumerable<ClaimInfo> claims)
        {
            return GetAccessRoles(GetRoles(claims));
        }
    }
}