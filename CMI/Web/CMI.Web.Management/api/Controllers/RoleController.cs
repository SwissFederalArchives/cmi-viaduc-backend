using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Transactions;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.Auth;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    [NoCache]
    public sealed class RoleController : ApiManagementControllerBase
    {
        private readonly IApplicationRoleDataAccess applicationRoleDataAccess;
        private readonly IApplicationRoleUserDataAccess applicationRoleUserDataAccess;
        private readonly IUserDataAccess userDataAccess;

        public RoleController(
            IApplicationRoleDataAccess applicationRoleDataAccess,
            IApplicationRoleUserDataAccess applicationRoleUserDataAccess, IUserDataAccess userDataAccess)
        {
            this.applicationRoleDataAccess = applicationRoleDataAccess;
            this.applicationRoleUserDataAccess = applicationRoleUserDataAccess;
            this.userDataAccess = userDataAccess;
        }

        #region Roles

        [HttpGet]
        public JObject GetRoles([FromUri] Paging paging = null, string language = null)
        {
            var result = new JObject();
            try
            {
                result.Add("items", JArray.FromObject(applicationRoleDataAccess.GetRoles()));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not get roles");
                result.Add("error", ServiceHelper.GetExceptionInfo(ex));
            }

            return result;
        }

        [HttpGet]
        public JObject GetRoleInfos([FromUri] Paging paging = null, string language = null)
        {
            var result = new JObject();
            try
            {
                result.Add("features", JArray.FromObject(ApplicationFeatures.ApplicationFeatureInfos.OrderBy(f => f.Name)));
                result.Add("items", JArray.FromObject(applicationRoleDataAccess.GetFeaturesInfoForRoles()));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not get roles infos");
                result.Add("error", ServiceHelper.GetExceptionInfo(ex));
            }

            return result;
        }

        [HttpGet]
        public JObject GetRoleInfo(int roleId, string language = null)
        {
            var result = new JObject();
            try
            {
                result.Add("features", JArray.FromObject(ApplicationFeatures.ApplicationFeatureInfos));
                result.Add("item", JObject.FromObject(applicationRoleDataAccess.GetFeaturesInfoForRole(roleId)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not get roles infos");
                result.Add("error", ServiceHelper.GetExceptionInfo(ex));
            }

            return result;
        }

        public class ApiRoleFeaturesPostData
        {
            public IList<string> FeatureIds { get; set; }
        }

        [HttpPost]
        public HttpResponseMessage SetRoleFeatures(int roleId, [FromBody] ApiRoleFeaturesPostData data)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            JObject result = null;
            try
            {
                var access = this.GetManagementAccess();
                if (access.EiamRole != AccessRoles.RoleMgntAppo)
                {
                    response = new HttpResponseMessage(HttpStatusCode.Forbidden);
                }
                else
                {
                    var existing = applicationRoleDataAccess.GetFeaturesInfoForRole(roleId)
                        .Features.Select(f => f.Id.ToString()).ToList();
                    var removeIds = existing.Where(id => !data.FeatureIds.Contains(id)).ToList();
                    var insertIds = data.FeatureIds.Where(id => !existing.Contains(id)).ToList();
                    var errors = 0;
                    foreach (var featureId in insertIds)
                    {
                        if (!applicationRoleDataAccess.InsertRoleFeature(access, roleId,
                            ApplicationFeatures.ApplicationFeaturesById[Convert.ToInt32(featureId)]))
                        {
                            errors += 1;
                        }
                    }

                    foreach (var featureId in removeIds)
                    {
                        if (!applicationRoleDataAccess.RemoveRoleFeature(access, roleId,
                            ApplicationFeatures.ApplicationFeaturesById[Convert.ToInt32(featureId)]))
                        {
                            errors += 1;
                        }
                    }

                    result = new JObject {{"success", errors == 0}};
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not set role features");
                result = new JObject {{"error", ServiceHelper.GetExceptionInfo(ex)}};
                response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (result != null)
            {
                response.Content = new JsonContent(result);
            }

            return response;
        }

        #endregion

        #region User Roles

        [HttpGet]
        public JObject GetUserInfo(string userId, string language = null)
        {
            var result = new JObject();
            try
            {
                result.Add("roles", JArray.FromObject(applicationRoleDataAccess.GetRoles()));
                result.Add("item", JObject.FromObject(userDataAccess.GetUser(userId)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not get roles for user");
                result.Add("error", ServiceHelper.GetExceptionInfo(ex));
            }

            return result;
        }

        public class ApiUserRolesPostData
        {
            public IList<string> RoleIds { get; set; }

            public List<int> AblieferndeStelleIds { get; set; }
        }

        [HttpPost]
        public HttpResponseMessage SetUserRoles(string userId, [FromBody] ApiUserRolesPostData data)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            JObject result = null;
            var access = this.GetManagementAccess();

            if (access.EiamRole != AccessRoles.RoleMgntAppo)
            {
                throw new ForbiddenException("Sie haben keine 'APPO' Rechte");
            }

            var userToEdit = userDataAccess.GetUser(userId);
            if (string.IsNullOrEmpty(userToEdit.EiamRoles))
            {
                throw new ForbiddenException("Der zu bearbeitende Benutzer hat keinen Zugriff auf den Management-Client");
            }

            using (var tran = new TransactionScope())
            {
                var existing = userToEdit.Roles.Select(r => r.Id.ToString()).ToList();
                var removeIds = existing.Where(id => !data.RoleIds.Contains(id)).ToList();
                var insertIds = data.RoleIds.Where(id => !existing.Contains(id)).ToList();

                foreach (var roleId in insertIds)
                {
                    applicationRoleUserDataAccess.InsertRoleUser(Convert.ToInt32(roleId), userId, access.UserId);
                }

                foreach (var roleId in removeIds)
                {
                    applicationRoleUserDataAccess.RemoveRoleUser(Convert.ToInt32(roleId), userId, access.UserId);
                }

                tran.Complete();
            }


            result = new JObject {{"success", true}};


            response.Content = new JsonContent(result);
            return response;
        }

        #endregion
    }
}