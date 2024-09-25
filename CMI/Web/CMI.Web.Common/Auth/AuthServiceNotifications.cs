using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Xml;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using Newtonsoft.Json;
using Serilog;
using Sustainsys.Saml2;
using Sustainsys.Saml2.Configuration;
using Sustainsys.Saml2.Metadata;
using Sustainsys.Saml2.Saml2P;
using Sustainsys.Saml2.WebSso;
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
            spOptions.Logger.WriteVerbose("SAML2 {Saml2Response}:" + JsonConvert.SerializeObject(response, Formatting.None));
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
                                          JsonConvert.SerializeObject(result, Formatting.None,
                                              new JsonSerializerSettings {ReferenceLoopHandling = ReferenceLoopHandling.Ignore}));
            spOptions.Logger.WriteVerbose("(AuthServiceNotifications:AcsCommandResultCreated()): {Saml2Response}" +
                                          JsonConvert.SerializeObject(response, Formatting.None));

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
                                              JsonConvert.SerializeObject(((ClaimsIdentity) result.Principal.Identity).Claims, Formatting.None,
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
                case "urn:qoa.eiam.admin.ch:names:tc:ac:classes:20":
                case "urn:qoa.eiam.admin.ch:names:tc:ac:classes:30":
                case "urn:qoa.eiam.admin.ch:names:tc:ac:classes:35":
                case "urn:qoa.eiam.admin.ch:names:tc:ac:classes:40":
                case "urn:qoa.eiam.admin.ch:names:tc:ac:classes:50":
                case "urn:qoa.eiam.admin.ch:names:tc:ac:classes:60":
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

        public IdentityProvider SelectIdentityProvider(EntityId entityRequest, IDictionary<string, string> args)
        {
            Log.Information("Initiated {method} with the Idp Id of {id}", nameof(SelectIdentityProvider), entityRequest.Id);

            // Im Public-Client erfolgt die Unterscheidung zwischen verschiedenen QoA-Levels
            if (isPublicClient)
            {
                // Wir setzen zuerst den tiefsten QoA Level als Default
                spOptions.RequestedAuthnContext = new Saml2RequestedAuthnContext(new Uri("urn:qoa.eiam.admin.ch:names:tc:ac:classes:20"),
                    AuthnContextComparisonType.Minimum);

                // Wenn wir einen idp angegeben haben, dann bedeutet dies, dass wir 
                // denselben idp verwenden wollen, aber mit einer höheren QoA
                // Damit umgehen wir die Limitation vom eIAM, die für einen im Bundesnetz angemeldeten Benutzer mit Smartcard
                // nur der Level 40 zurückgegeben wird, und nicht wie 60 wie gefordert.
                if (!string.IsNullOrEmpty(entityRequest.Id))
                {
                    switch (entityRequest.Id.ToLower())
                    {
                        case "level-60":
                            spOptions.RequestedAuthnContext = new Saml2RequestedAuthnContext(new Uri("urn:qoa.eiam.admin.ch:names:tc:ac:classes:60"),
                                AuthnContextComparisonType.Minimum);
                            break;
                        case "level-50":
                            spOptions.RequestedAuthnContext = new Saml2RequestedAuthnContext(new Uri("urn:qoa.eiam.admin.ch:names:tc:ac:classes:50"),
                                AuthnContextComparisonType.Minimum);
                            break;
                        default:
                            throw new InvalidEnumArgumentException("Diese Option für den idp wird nicht unterstützt");
                    }
                }
            }

            // Indem wir die ID auf null zurücksetzen, wird der standardmässige IdP verwendet,
            // aber eben mit einer Option die eine höheren AuthnContext verlangt.
            entityRequest.Id = null;

            // Dadurch wird der Standard Idp gemäss gewählter Konfiguration zurückgegeben.
            return null;
        }

        public void AuthenticationRequestCreated(Saml2AuthenticationRequest authRequest, IdentityProvider idp, IDictionary<string, string> args)
        {
            Log.Information("Initiated AuthRequest is \n{xml}", authRequest.ToXml());
        }
    }
}