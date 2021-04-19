using System.Collections.Generic;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Management.api.Configuration;

namespace CMI.Web.Management.Auth
{
    public class ManagementUserAccess : UserAccess, IApplicationRolesAccess, IApplicationFeaturesAccess
    {
        public ManagementUserAccess(string userId, string eiamRole, string[] asTokens, string language = null) : base(userId, null, eiamRole,
            asTokens, false, language)
        {
            var user = new UserDataAccess(ManagementSettingsViaduc.Instance.SqlConnectionString).GetUser(userId);
            ApplicationRoles = user?.Roles ?? new List<ApplicationRole>();
            ApplicationFeatures = user?.Features ?? new List<ApplicationFeature>();
        }

        public IList<ApplicationFeature> ApplicationFeatures { get; }
        public IList<ApplicationRole> ApplicationRoles { get; }
    }
}