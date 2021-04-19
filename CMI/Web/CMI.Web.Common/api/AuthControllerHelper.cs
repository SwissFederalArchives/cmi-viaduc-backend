using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Principal;
using System.Web;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CMI.Web.Common.api
{
    public class AuthControllerHelper
    {
        private const string roleIdentifier = "Standard";
        private const string loginSystem = "System-Login";
        private readonly IApplicationRoleUserDataAccess applicationRoleUserDataAccess;
        private readonly IAuthenticationHelper authenticationHelper;
        private readonly IControllerHelper controllerHelper;
        private readonly IUserDataAccess userDataAccess;
        private readonly IWebCmiConfigProvider webCmiConfigProvider;

        public AuthControllerHelper(IApplicationRoleUserDataAccess applicationRoleUserDataAccess,
            IUserDataAccess userDataAccess,
            IControllerHelper controllerHelper,
            IAuthenticationHelper authenticationHelper,
            IWebCmiConfigProvider webCmiConfigProvider)
        {
            this.applicationRoleUserDataAccess = applicationRoleUserDataAccess;
            this.userDataAccess = userDataAccess;
            this.controllerHelper = controllerHelper;
            this.webCmiConfigProvider = webCmiConfigProvider;
            this.authenticationHelper = authenticationHelper;
        }

        public Identity GetIdentity(HttpRequestMessage request, IPrincipal user, bool isPublicClient)
        {
            var userId = controllerHelper.GetCurrentUserId();
            var claims = authenticationHelper.GetClaimsForRequest(user, request);

            if (!HasValidMandant(claims))
            {
                Log.Warning("User hat noch keinen Antrag gestellt");
                throw new AuthenticationException("User hat noch keinen Antrag gestellt");
            }

            var isNewUser = !TryUpdateUser(userId, claims);

            if (isNewUser)
            {
                return new Identity
                {
                    IssuedClaims = claims.ToArray(),
                    Roles = new[]
                    {
                        isPublicClient
                            ? controllerHelper.IsInternalUser() ? AccessRoles.RoleBVW : AccessRoles.RoleOe2
                            : controllerHelper.GetMgntRoleFromClaim()
                    },
                    IssuedAccessTokens = new string[] { },
                    AuthStatus = AuthStatus.NeuerBenutzer,
                    RedirectUrl = GetReturnUrl(AuthStatus.NeuerBenutzer, isPublicClient)
                };
            }

            var role = isPublicClient
                ? userDataAccess.GetRoleForClient(userId)
                : userDataAccess.GetEiamRoles(userId);

            var authStatus = IsValidAuthRole(role, isPublicClient);

            // Fehlerhafte Rolle oder Anmeldung
            if (authStatus == AuthStatus.KeineRolleDefiniert)
            {
                Log.Error(
                    "Es wurde für den Benutzer keine Rolle definiert in der Datenbank oder Authentifikation hat fehlgeschlagen UserId:={UserId}, AuthStatus={AuthStatus}",
                    userId, authStatus);
                throw new AuthenticationException(
                    $"Es wurde für den Benutzer keine Rolle definiert in der Datenbank oder Authentifikation hat fehlgeschlagen UserId:={userId}, AuthStatus='{authStatus}'");
            }

            var accessTokens = userDataAccess.GetTokensDesUser(userId);

            var identity = new Identity
            {
                IssuedClaims = claims.ToArray(),
                Roles = new[] {role},
                IssuedAccessTokens = accessTokens,
                AuthStatus = authStatus,
                RedirectUrl = GetReturnUrl(authStatus, isPublicClient)
            };
            AddAppRolesAndFeatures(userId, identity);

            try
            {
                Log.Debug("(AuthController:GetClaims()): {CLAIMS}", JsonConvert.SerializeObject(identity, Formatting.Indented));
            }
            catch
            {
                // ignored
            }


            return identity;
        }

        internal bool TryUpdateUser(string userId, IList<ClaimInfo> claims)
        {
            var user = userDataAccess.GetUser(userId);
            if (user == null)
            {
                return false;
            }

            var isInternal = controllerHelper.IsInternalUser();
            var mgntRole = controllerHelper.GetMgntRoleFromClaim();
            try
            {
                var userDataOnLogin = new User
                {
                    Id = userId,
                    IsInternalUser = isInternal,
                    EiamRoles = mgntRole,
                    UserExtId = controllerHelper.GetFromClaim("/identity/claims/e-id/userExtId"),
                    Claims = new JObject {{"claims", JArray.FromObject(claims)}},
                    FamilyName = isInternal ? controllerHelper.GetFromClaim("/identity/claims/surname") : user.FamilyName,
                    FirstName = isInternal ? controllerHelper.GetFromClaim("/identity/claims/givenname") : user.FirstName,
                    EmailAddress = isInternal ? controllerHelper.GetFromClaim("/identity/claims/emailaddress") : user.EmailAddress
                };

                // Prüfen User Änderung enthält, falls ja Daten aktualisieren 
                if (HasUserChanges(userDataOnLogin, user))
                {
                    userDataAccess.UpdateUserOnLogin(userDataOnLogin, userId, loginSystem);
                }

                // Falls der Benutzer für M-C berechtigt ist, soll die Standardrolle zugewiesen werden
                if (!string.IsNullOrWhiteSpace(mgntRole) && mgntRole.Equals(AccessRoles.RoleMgntAllow))
                {
                    applicationRoleUserDataAccess.InsertRoleUser(roleIdentifier, userId);
                }
                else if (string.IsNullOrWhiteSpace(mgntRole))
                {
                    applicationRoleUserDataAccess.RemoveRolesUser(userId, roleIdentifier);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not insert or update user on signin");
            }

            return true;
        }

        private bool HasUserChanges(User newUser, User originalUser)
        {
            if (newUser.EiamRoles != originalUser.EiamRoles)
            {
                return true;
            }

            if (newUser.FamilyName != originalUser.FamilyName)
            {
                return true;
            }

            if (newUser.FirstName != originalUser.FirstName)
            {
                return true;
            }

            if (newUser.EmailAddress != originalUser.EmailAddress)
            {
                return true;
            }

            return false;
        }

        internal AuthStatus IsValidAuthRole(string role, bool isPublicClient)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return AuthStatus.KeineRolleDefiniert;
            }

            if ((role == AccessRoles.RoleOe2 || role == AccessRoles.RoleOe3) &&
                (controllerHelper.IsKerberosAuthentication() || controllerHelper.IsSmartcartAuthentication()))
            {
                throw new AuthenticationException("Kerberos oder Smartcard dürfen nicht für Ö2 und Ö3 verwendet werden");
            }

            if ((role == AccessRoles.RoleBVW || role == AccessRoles.RoleAS || role == AccessRoles.RoleBAR) &&
                !(controllerHelper.IsKerberosAuthentication() || controllerHelper.IsSmartcartAuthentication()))
            {
                throw new AuthenticationException("Interne Benutzerrollen (BVW, AS und BAR) müssen Kerberos oder Smartcard verwenden");
            }

            // Public-Client
            if (isPublicClient)
            {
                switch (role.GetRolePublicClientEnum())
                {
                    // Keine spezial Behandlung
                    case AccessRolesEnum.Ö2:
                    case AccessRolesEnum.BVW:
                        return AuthStatus.Ok;

                    // SMS-Anmeldung 
                    case AccessRolesEnum.Ö3:
                        return controllerHelper.IsMTanAuthentication()
                            ? AuthStatus.Ok
                            : AuthStatus.KeineMTanAuthentication;

                    // Kerberos Pflicht
                    case AccessRolesEnum.AS:
                    case AccessRolesEnum.BAR:
                        return controllerHelper.IsKerberosAuthentication()
                            ? AuthStatus.Ok
                            : AuthStatus.KeineKerberosAuthentication;

                    default:
                        throw new InvalidOperationException("Nicht definiertes Rollen handling");
                }
            }

            // Management-Client
            switch (role)
            {
                // Kerberos Pflicht
                case AccessRoles.RoleMgntAllow:
                case AccessRoles.RoleMgntAppo:
                    return controllerHelper.IsKerberosAuthentication()
                        ? AuthStatus.Ok
                        : AuthStatus.KeineKerberosAuthentication;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), "Nicht definiertes Rollen handling");
            }
        }

        internal void AddAppRolesAndFeatures(string userId, Identity identity)
        {
            try
            {
                var user = userDataAccess.GetUser(userId);
                if (user != null && user.Roles.Any())
                {
                    identity.ApplicationRoles = user.Roles;
                    identity.ApplicationFeatures = user.Features.ToInfos();
                }

                if (HttpContext.Current?.Session != null)
                {
                    HttpContext.Current.Session.SetApplicationUser(user);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not fetch user info");
            }
        }

        private bool HasValidMandant(IList<ClaimInfo> claims)
        {
            var claimsRoles = claims.FirstOrDefault(c => c.Type.EndsWith("/identity/claims/e-id/profile/role"))?.Value;
            Log.Information($"Claim Rolle {claimsRoles}");
            return !string.IsNullOrWhiteSpace(claimsRoles);
        }

        private string GetOeDreiKeineMobilenummerErfasst()
        {
            return webCmiConfigProvider.GetStringSetting("oeDreiKeineMobilenummerErfasst",
                "www.recherche.bar.admin.ch/_pep/myaccount?returnURI=/my-appl/private/welcome.html&op=reg-mobile");
        }

        private string GetPublicClientUrl()
        {
            return webCmiConfigProvider.GetStringSetting("publicClientUrl",
                "www.recherche.bar.admin.ch/recherche");
        }

        private string GetReturnUrl(AuthStatus authStatus, bool isPublicClient)
        {
            if (authStatus == AuthStatus.KeineMTanAuthentication)
            {
                return GetOeDreiKeineMobilenummerErfasst();
            }

            if (!isPublicClient && authStatus == AuthStatus.NeuerBenutzer)
            {
                return GetPublicClientUrl();
            }

            return string.Empty;
        }
    }
}