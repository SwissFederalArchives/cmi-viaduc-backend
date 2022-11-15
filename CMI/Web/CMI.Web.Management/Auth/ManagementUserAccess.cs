using System;
using System.Collections.Generic;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Management.api.Configuration;

namespace CMI.Web.Management.Auth
{
    public class ManagementUserAccess : UserAccess, IManagementUserAccess
    {
        public ManagementUserAccess(string userId, string eiamRole, string[] asTokens, string language = null) : base(userId, null, eiamRole,
            asTokens, false, language)
        {
            var user = new UserDataAccess(ManagementSettingsViaduc.Instance.SqlConnectionString).GetUser(userId);
            ApplicationRoles = user?.Roles ?? new List<ApplicationRole>();
            ApplicationFeatures = user?.Features ?? new List<ApplicationFeature>();
        }

        public ManagementUserAccess() : base (string.Empty, string.Empty, string.Empty,new []{string.Empty}, false, string.Empty )
        {
            ApplicationRoles =  new List<ApplicationRole>();
            ApplicationFeatures = new List<ApplicationFeature>();
        }

        public IList<ApplicationFeature> ApplicationFeatures { get; }
        public IList<ApplicationRole> ApplicationRoles { get; }
      
    }

    public interface IManagementUserAccess : IApplicationRolesAccess, IApplicationFeaturesAccess
    {
    }
}