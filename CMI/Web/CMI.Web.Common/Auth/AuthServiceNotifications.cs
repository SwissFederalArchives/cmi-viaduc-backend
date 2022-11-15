using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Xml;
using CMI.Web.Common.Helpers;
using Kentor.AuthServices.Configuration;
using Kentor.AuthServices.Saml2P;
using Kentor.AuthServices.WebSso;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace CMI.Web.Common.Auth
{
    public class AuthServiceNotifications
    {
        private static SPOptions spOptions;
        private readonly bool isPublicClient;

        public AuthServiceNotifications(SPOptions options, bool isPublicClient)
        {
            spOptions = options;
            this.isPublicClient = isPublicClient;
        }

        public void AcsCommandResultCreated(CommandResult result, Saml2Response response)
        {
            spOptions.Logger.WriteVerbose("SAML2 {Saml2Response}:" + JsonConvert.SerializeObject(response, Formatting.Indented));
            // Bereits in Anwendung auf Mandant BAR vorhanden
            if (!HasValidMandant(result))
            {
                spOptions.Logger.WriteInformation("User hat noch keinen Antrag gestellt");
                HttpContext.Current.Response.Redirect(GetLoginMandantErstellenUrl(), false);
                return;
            }

            var assertion = response.XmlElement.ChildNodes.OfType<XmlNode>()
                .First(node => node.Name == "saml2:Assertion");
            var authnStatement = assertion.ChildNodes.OfType<XmlNode>()
                .First(node => node.Name == "saml2:AuthnStatement");
            var authnContext = authnStatement.ChildNodes.OfType<XmlNode>()
                .First(node => node.Name == "saml2:AuthnContext");
            var authnContextClassRef = authnContext.ChildNodes.OfType<XmlNode>()
                .First(node => node.Name == "saml2:AuthnContextClassRef");
            var authType = authnContextClassRef.InnerText;

            var roles = result.Principal.Claims.Where(
                c => c.Type == "http://schemas.eiam.admin.ch/ws/2013/12/identity/claims/role" ||
                     c.Type == "http://schemas.eiam.admin.ch/ws/2013/12/identity/claims/e-id/profile/role");


            // Prüfen ob eine Rolle Deny enthält oder leer ist (Rolle Deny?)
            var prefexRole = isPublicClient ? "BAR-recherche" : "BAR-recherche-management-client";
            if (roles.Any(
                role => role.Value.EndsWith($"{prefexRole}.DENY", StringComparison.InvariantCultureIgnoreCase) || role.Value == string.Empty))
            {
                spOptions.Logger.WriteError("User enthält DENY Rolle {Saml2Response}" + JsonConvert.SerializeObject(response, Formatting.Indented),
                    null);
                result.HttpStatusCode = HttpStatusCode.Forbidden;
                return;
            }

            // Prüfen ob mind. eine Rolle Allow (Rolle Allow?)
            if (!roles.Any(role => role.Value.EndsWith($"{prefexRole}.ALLOW", StringComparison.InvariantCultureIgnoreCase)))
            {
                spOptions.Logger.WriteError(
                    "User enthält keine ALLOW Rolle {Saml2Response}" + JsonConvert.SerializeObject(response, Formatting.Indented), null);
                result.HttpStatusCode = HttpStatusCode.Forbidden;
                return;
            }

            // Prüfen ob valide Anmeldeart (Bekannte Anmeldeart?)
            if (!IsValidLoginType(authType))
            {
                spOptions.Logger.WriteError("User verwendet eine nicht zugelassen Anmeldeart {authType}" + authType, null);
                result.HttpStatusCode = HttpStatusCode.Forbidden;
                return;
            }

            spOptions.Logger.WriteVerbose("(AuthServiceNotifications:AcsCommandResultCreated()): {COMMANDRESULT}" +
                                          JsonConvert.SerializeObject(result, Formatting.Indented,
                                              new JsonSerializerSettings {ReferenceLoopHandling = ReferenceLoopHandling.Ignore}));
            spOptions.Logger.WriteVerbose("(AuthServiceNotifications:AcsCommandResultCreated()): {Saml2Response}" +
                                          JsonConvert.SerializeObject(response, Formatting.Indented));

            // Fehlende Claims hinzufügen (Custom-Roles kommen sonst nicht mit!)
            var options = Options.FromConfiguration;
            var identity = response.GetClaims(options).FirstOrDefault(); // Gemäss BIT-Doku gibt es nur eine Assertion in der SAML Response
            if (identity != null)
            {
                var claims = identity.Claims ?? Enumerable.Empty<Claim>();
                foreach (var claim in claims)
                {
                    if (!((ClaimsIdentity) result.Principal.Identity).HasClaim(claim.Type, claim.Value))
                    {
                        spOptions.Logger.WriteVerbose("Adding Claim, because it is missing {CLAIM}: " + claim);
                        ((ClaimsIdentity) result.Principal.Identity).AddClaim(claim);
                    }
                    else
                    {
                        spOptions.Logger.WriteVerbose("Not Adding Claim, because it already exists {CLAIM}: " + claim);
                    }
                }
            }
            
            spOptions.Logger.WriteVerbose("(AuthServiceNotifications:AcsCommandResultCreated()): {READCLAIMS}" +
                                              JsonConvert.SerializeObject(((ClaimsIdentity) result.Principal.Identity).Claims, Formatting.Indented,
                                                  new JsonSerializerSettings {ReferenceLoopHandling = ReferenceLoopHandling.Ignore}));
        }

        private bool HasValidMandant(CommandResult result)
        {
            var claimsRoles =
                result.Principal.Claims.Where(c => c.Type == "http://schemas.eiam.admin.ch/ws/2013/12/identity/claims/e-id/profile/role");
            return claimsRoles.Any();
        }

        internal bool IsValidLoginType(string authType)
        {
            if (string.IsNullOrWhiteSpace(authType))
            {
                return false;
            }

            switch (authType)
            {
                case "urn:oasis:names:tc:SAML:2.0:ac:classes:Kerberos":
                case "urn:oasis:names:tc:SAML:2.0:ac:classes:SmartcardPKI":
                case "urn:oasis:names:tc:SAML:2.0:ac:classes:NomadTelephony":
                case "urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport":
                    return true;
                default:
                    return false;
            }
        }

        private string GetLoginMandantErstellenUrl()
        {
            var url = WebHelper.GetStringSetting("loginMandantErstellenUrl", "https://www.recherche.bar.admin.ch/recherche/private");
            return url;
        }
    }
}