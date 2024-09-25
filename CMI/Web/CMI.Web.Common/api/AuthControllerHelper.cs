using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using SameSiteMode = Microsoft.Owin.SameSiteMode;
using CMI.Contract.Common.Extensions;

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

        /// <summary>
        /// Wird aufgerufen, wenn das EIAM / SAML2-Login durchgeführt wurde.
        /// Erstellt die Session innerhalb der Viaduc Applikation
        /// </summary>
        /// <param name="owinContext"></param>
        /// <returns></returns>
        public async Task OnExternalSignIn(IOwinContext owinContext, bool isPublicClient)
        {
            var authManager = owinContext.Authentication;
            var authResult = await authManager.AuthenticateAsync(DefaultAuthenticationTypes.ExternalCookie);
            if (authResult == null)
            {
                return;
            }
               
            var identity = authResult.Identity;
            Log.Information("Found identity for user {Name}", identity.Name);

            authManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            Log.Information("Getting claims");
            var claims = identity.Claims.ToList();
            var appCookieKey = isPublicClient ? WebHelper.CookiePcAppliationCookieKey : WebHelper.CookieMcAppliationCookieKey;
            var ci = new ClaimsIdentity(claims, appCookieKey);
            authManager.SignIn(ci);

            var aspSessionIdCookieyKey = isPublicClient ? WebHelper.CookiePcAspNetSessionIdKey : WebHelper.CookieMcAspNetSessionIdKey;
            var sessionId = owinContext.Request.Cookies[aspSessionIdCookieyKey];
            var userId = GetUserId(ci.Claims);
            Log.Information("Got userId from claims: {userId}", userId);

            // Die SessionId wird zusätzlich in einem eigenem Cookie gespeichert, damit diese nach dem Logout kurzzeitig verwendet werden kann.
            var cookieUserIdKey = isPublicClient ? WebHelper.CookiePcViaducUserIdKey : WebHelper.CookieMcViaducUserIdKey;
            AddViaducSessionCookie(owinContext, userId, cookieUserIdKey);

            // Wir merken uns die aktive SessionId um sie bei einem Logout zurückzusetzen. 
            // Dies wird beim Überprüfen der Identity genutzt um zu verhindern, dass vor einem Logout
            // das Session-Cookie abgegriffen - und nach dem Logout weiter verwendet werden kann.
            userDataAccess.UpdateActiveSessionId(userId, sessionId);
        }

        /// <summary>
        /// Wird nach dem Abmelden vom EIAM / SAMl2 aufgerufen.
        /// Die Methode entfernt die notwendigen Cookies und setzt die aktive SessionId des Benutzers auf der DB zurück.
        /// </summary>
        /// <param name="owinContext"></param>
        public virtual void OnExternalSignOut(IOwinContext owinContext, bool isPublicClient)
        {
            var cookieUserIdKey = isPublicClient ? WebHelper.CookiePcViaducUserIdKey : WebHelper.CookieMcViaducUserIdKey;
            var appCookieKey = isPublicClient ? WebHelper.CookiePcAppliationCookieKey : WebHelper.CookieMcAppliationCookieKey;

            var userId = owinContext.Request.Cookies[cookieUserIdKey];
            userDataAccess.UpdateActiveSessionId(userId, null);

            var authManager = owinContext.Authentication;
            authManager.SignOut(appCookieKey);
            owinContext.Response.Cookies.Delete(cookieUserIdKey, new CookieOptions
            {
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict
            });
        }

        private static void AddViaducSessionCookie(IOwinContext owinContext, string userId, string cookieUserIdKey)
        {
            owinContext.Response.Cookies.Append(cookieUserIdKey, userId,
                new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
        }

        private static void AddViaducElevatedLoginCookie(IOwinContext owinContext, bool isElevatedLogin, string cookieIdKey)
        {
            if (owinContext == null)
            {
                return;
            }

            owinContext.Response.Cookies.Append(cookieIdKey, isElevatedLogin.ToString(),
                new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
        }

        private static void DeleteViaducElevatedLoginCookie(IOwinContext owinContext, string cookieIdKey)
        {
            if (owinContext == null)
            {
                return;
            }

            owinContext.Response.Cookies.Delete(cookieIdKey,
                new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
        }

        private static bool GetViaducElevatedLoginCookie(IOwinContext owinContext, string cookieIdKey)
        {
            if (owinContext == null)
                return false;

            var cookie = owinContext.Request.Cookies[cookieIdKey];
            if (!string.IsNullOrEmpty(cookie) && bool.TryParse(cookie, out var result))
            {
                return result;
            }

            return false;
        }

        private string GetUserId(IEnumerable<Claim> claims)
        {
            return claims?.FirstOrDefault(c => c.Type.Contains(ClaimValueNames.UserExtId))?.Value;
        }

        public Identity GetIdentity(HttpRequestMessage request, IPrincipal user, bool isPublicClient)
        {
            var userId = controllerHelper.GetCurrentUserId();
            var claims = authenticationHelper.GetClaimsForRequest(user, request);
            var owinContext = request?.GetOwinContext();

            // Get the cookie to see if we are in an elevated login phase.
            // If so, directly delete the cookie, so we do not enter an endless loop
            var elevatedLoginCookieKey = isPublicClient ? WebHelper.CookiePcViaducElevatedLoginKey : WebHelper.CookieMcViaducElevatedLoginKey;
            var isElevatedLogin = GetViaducElevatedLoginCookie(owinContext, elevatedLoginCookieKey);
            if (isElevatedLogin)
            {
                DeleteViaducElevatedLoginCookie(owinContext, elevatedLoginCookieKey);
            }

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
                            ? controllerHelper.GetInitialRoleFromClaim()
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

            var authStatus = IsValidAuthRole(role, isPublicClient, isElevatedLogin);

            // Fehlerhafte Rolle oder Anmeldung
            if (authStatus == AuthStatus.KeineRolleDefiniert)
            {
                Log.Error(
                    "Es wurde für den Benutzer keine Rolle definiert in der Datenbank oder Authentifikation hat fehlgeschlagen UserId:={UserId}, AuthStatus={AuthStatus}",
                    userId, authStatus);
                throw new AuthenticationException(
                    $"Es wurde für den Benutzer keine Rolle definiert in der Datenbank oder Authentifikation hat fehlgeschlagen UserId:={userId}, AuthStatus='{authStatus}'");
            }

            Identity identity;
            if (authStatus == AuthStatus.Ok || authStatus == AuthStatus.NeuerBenutzer)
            {
                var accessTokens = userDataAccess.GetTokensDesUser(userId);

                identity = new Identity
                {
                    IssuedClaims = claims.ToArray(),
                    Roles = new[] {role},
                    IssuedAccessTokens = accessTokens,
                    AuthStatus = authStatus,
                    RedirectUrl = GetReturnUrl(authStatus, isPublicClient)
                };
                AddAppRolesAndFeatures(userId, identity);
            }
            else
            {
                if (authStatus == AuthStatus.RequiresElevatedCheck)
                {
                    AddViaducElevatedLoginCookie(owinContext, true, elevatedLoginCookieKey);
                }

                identity = new Identity
                {
                    IssuedClaims = new ClaimInfo[]{},
                    Roles = new string[]{},
                    IssuedAccessTokens = new string[] { },
                    AuthStatus = authStatus,
                    RedirectUrl = GetReturnUrl(authStatus, isPublicClient)
                };
                OnExternalSignOut(owinContext, isPublicClient);
            }

            try
            {
                Log.Debug("(AuthController:GetClaims()): {CLAIMS}", JsonConvert.SerializeObject(identity, Formatting.None));
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

            var isIdentified = controllerHelper.IsIdentifiedUser();
            var mgntRole = controllerHelper.GetMgntRoleFromClaim();
            try
            {
                var userDataOnLogin = new User
                {
                    Id = userId,
                    IsIdentifiedUser = isIdentified,
                    EiamRoles = mgntRole,
                    QoAValue = controllerHelper.GetQoAFromClaim(),
                    HomeName = controllerHelper.GetFromClaim(ClaimValueNames.HomeName),
                    UserExtId = controllerHelper.GetFromClaim(ClaimValueNames.UserExtId),
                    Claims = new JObject {{"claims", JArray.FromObject(claims)}},
                    FamilyName = isIdentified ? controllerHelper.GetFromClaim(ClaimValueNames.FamilyName) : user.FamilyName,
                    FirstName = isIdentified ? controllerHelper.GetFromClaim(ClaimValueNames.FirstName) : user.FirstName,
                    EmailAddress = isIdentified ? controllerHelper.GetFromClaim(ClaimValueNames.Email) : user.EmailAddress
                };

                // Prüfen ob User Änderung enthält, falls ja Daten aktualisieren 
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

            if (newUser.QoAValue != originalUser.QoAValue)
            {
                return true;
            }

            if (newUser.HomeName != originalUser.HomeName)
            {
                return true;
            }

            return false;
        }

        internal AuthStatus IsValidAuthRole(string role, bool isPublicClient, bool isElevatedLogin = false)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return AuthStatus.KeineRolleDefiniert;
            }

            if (role == AccessRoles.RoleOe2  && controllerHelper.GetQoAFromClaim() < 20)
            {
                return AuthStatus.ZuTieferQoAWert;
            }

            // Ö3 muss mindestens ein 20er haben
            if (role == AccessRoles.RoleOe3 && controllerHelper.GetQoAFromClaim() < 20)
            {
                return AuthStatus.ZuTieferQoAWert;
            }

            if (role == AccessRoles.RoleOe3 && controllerHelper.GetQoAFromClaim() < 30)
            {
                Log.Warning("Ö3 Benutzer mit einem QoA-Wert kleiner 30 hat sich angemeldet. Er wird im Falle des Public Clients auf die MTan-Registrierungsseite weitergeleitet.");
            }

            if (role == AccessRoles.RoleBVW && controllerHelper.GetQoAFromClaim() < 40)
            {
                return AuthStatus.ZuTieferQoAWert;
            }

            if (role == AccessRoles.RoleAS && controllerHelper.GetQoAFromClaim() < 50)
            {
                // Ist man nicht schon im einem elevated Login, dann 
                // muss der Level schon mindestens 40 (Windows/Kerberos) sein, damit
                // wir einen zweiten Versuch machen.
                if (!isElevatedLogin && controllerHelper.GetQoAFromClaim() >= 40)
                {
                    return AuthStatus.RequiresElevatedCheck;
                }
                return AuthStatus.ZuTieferQoAWert;
            }

            if (role == AccessRoles.RoleBAR && controllerHelper.GetQoAFromClaim() < 60)
            {
                // Ist man nicht schon im einem elevated Login, dann 
                // muss der Level schon mindestens 40 (Windows/Kerberos) sein, damit
                // wir einen zweiten Versuch machen.
                if (!isElevatedLogin && controllerHelper.GetQoAFromClaim() >= 40)
                {
                    return AuthStatus.RequiresElevatedCheck;
                }
                return AuthStatus.KeineSmartcardAuthentication;
            }

            if (role == AccessRoles.RoleBAR && !controllerHelper.GetFromClaim(ClaimValueNames.HomeName).Contains("FED-LOGIN", StringComparison.CurrentCultureIgnoreCase) )
            {
                throw new AuthenticationException("Die BAR-Rolle verlangt zwingend ein FED-Login");
            }

            
            // Public-Client
            if (isPublicClient)
            {
                switch (role.GetRolePublicClientEnum())
                {
                    // Keine weitere spezial Behandlung
                    case AccessRolesEnum.Ö2:
                    case AccessRolesEnum.BVW:
                    case AccessRolesEnum.AS:
                    case AccessRolesEnum.BAR:
                        return AuthStatus.Ok;

                    // Ö3 Anmeldung kann mit QoA-20 daher kommen. In diesem Fall entsprechenden Status zurücksenden 
                    case AccessRolesEnum.Ö3:
                        return controllerHelper.GetQoAFromClaim() >= 30
                            ? AuthStatus.Ok
                            : AuthStatus.KeineMTanAuthentication;

                    default:
                        throw new InvalidOperationException("Nicht definiertes Rollen handling");
                }
            }

            // Management-Client
            switch (role)
            {
                // Mindestens QoA 60 (smartcard) notwendig
                case AccessRoles.RoleMgntAllow:
                case AccessRoles.RoleMgntAppo:
                    if (controllerHelper.GetQoAFromClaim() >= 60)
                    {
                        return AuthStatus.Ok;
                    }

                    // Ist man nicht schon im einem elevated Login, dann 
                    // muss der Level schon mindestens 40 (Windows/Kerberos) sein, damit
                    // wir einen zweiten Versuch machen.
                    if (!isElevatedLogin && controllerHelper.GetQoAFromClaim() >= 40)
                    {
                        return AuthStatus.RequiresElevatedCheck;
                    }
                    return AuthStatus.KeineSmartcardAuthentication;

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
            var claimsRoles = claims.FirstOrDefault(c => c.Type.EndsWith(ClaimValueNames.EIdProfileRole))?.Value;
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