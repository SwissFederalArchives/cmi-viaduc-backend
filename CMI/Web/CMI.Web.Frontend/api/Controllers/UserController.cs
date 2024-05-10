using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Parameter;
using CMI.Manager.Order.Status;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Frontend.api.Controllers
{
    [Authorize]
    [NoCache]
    public class UserController : ApiFrontendControllerBase
    {
        private readonly IAuthenticationHelper authenticationHelper;
        private readonly IUserAccessProvider userAccessProvider;
        private readonly IParameterHelper parameterHelper = new ParameterHelper();
        private readonly UserDataAccess userDataAccess = new UserDataAccess(WebHelper.Settings["sqlConnectionString"]);


        public UserController(IAuthenticationHelper authenticationHelper,
        IUserAccessProvider userAccessProvider)
        {
            this.authenticationHelper = authenticationHelper;
            this.userAccessProvider = userAccessProvider;
        }

        /// <summary>
        ///     Gibt Informationen zum aktuell eingeloggten Benutzer
        /// </summary>
        [HttpGet]
        public User GetUser()
        {
            return userDataAccess.GetUser(ControllerHelper.GetCurrentUserId());
        }

        /// <summary>
        ///     Aktualisiert Daten des eingeloggten Benutzers
        /// </summary>
        [HttpPost]
        public void UpdateUserProfile([FromBody] User user)
        {
            if (user == null || user.Id != GetUserAccess().UserId)
            {
                throw new ForbiddenException("Sie können nur Ihren eigenen Benutzer bearbeiten.");
            }

            CheckCustomAttributes.CheckRequiredAttribute(user);

            var originalUser = userDataAccess.GetUser(user.Id);

            CheckCustomAttributes.CheckEditNotAllowedAttribute(originalUser, user);
            CheckCustomAttributes.CheckEditNotAllowedForAttribute(originalUser, user);

            userDataAccess.UpdateUserProfile(ControllerHelper.GetCurrentUserId(), user);
        }

        [HttpGet]
        public JObject GetUserSettings()
        {
            return userDataAccess.GetUser(ControllerHelper.GetCurrentUserId())?.Setting;
        }

        [HttpGet]
        public User GetUserDataFromClaims()
        {
            return new User
            {
                FamilyName = ControllerHelper.GetFromClaim("/identity/claims/surname"),
                FirstName = ControllerHelper.GetFromClaim("/identity/claims/givenname"),
                EmailAddress = ControllerHelper.GetFromClaim("/identity/claims/emailaddress")
            };
        }

        [HttpPost]
        public void InsertUser([FromBody] User user)
        {
            var claims = authenticationHelper.GetClaimsForRequest(User, Request);

            user.Id = ControllerHelper.GetCurrentUserId();
            user.UserExtId = ControllerHelper.GetFromClaim("/identity/claims/e-id/userExtId");
            user.Claims = new JObject {{"claims", JArray.FromObject(claims)}};
            user.EiamRoles = ControllerHelper.GetMgntRoleFromClaim();
            user.IsInternalUser = ControllerHelper.IsInternalUser();

            if (user.IsInternalUser)
            {
                user.FamilyName = ControllerHelper.GetFromClaim("/identity/claims/surname");
                user.FirstName = ControllerHelper.GetFromClaim("/identity/claims/givenname");
                user.EmailAddress = ControllerHelper.GetFromClaim("/identity/claims/emailaddress");
            }

            userDataAccess.InsertUser(user);
        }

        [HttpPost]
        public void UpdateUserSettings(JObject settings)
        {
            userDataAccess.UpdateUserSetting(settings, ControllerHelper.GetCurrentUserId());
        }

        [HttpGet]
        public IEnumerable<User> GetUsers(string search)
        {
            var userId = ControllerHelper.GetCurrentUserId();
            var language = WebHelper.GetClientLanguage(Request);

            var userAccess = userAccessProvider.GetUserAccess(language, userId);
            if (userAccess.CombinedTokens.Contains(AccessRoles.RoleBAR) && search.Length > 2)
            {
                var users = userDataAccess.GetUsersByName(search).Where(u => !Users.IsSystemUser(u.Id));
                return users.Count() > 0 ? users : null;
            }
            
            return null;
        }
    }
}