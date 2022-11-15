using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Manager.Order.Status;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.api.Data;
using CMI.Web.Management.Auth;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    [NoCache]
    public class UserController : ApiManagementControllerBase
    {
        private readonly IUserDataAccess userDataAccess;

        public UserController(IUserDataAccess userDataAccess)
        {
            this.userDataAccess = userDataAccess;
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
        ///     Gibt Informationen zum aktuell eingeloggten Benutzer
        /// </summary>
        [HttpGet]
        public User[] GetUsers([FromUri] string[] userIds)
        {
            return userDataAccess.GetUsers(userIds).ToArray();
        }

        [HttpPost]
        public HttpResponseMessage UpdateUser([FromBody] UserPostData postData)
        {
            var access = this.GetManagementAccess();

            if (string.IsNullOrEmpty(postData?.Id))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            // Logic validieren 
            if (postData.ResearcherGroup && postData.BarInternalConsultation)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            if (postData.ResearcherGroup && (postData.RolePublicClient == null || !postData.RolePublicClient.Equals(AccessRoles.RoleOe3)))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }


            // Pflichtfelder validieren
            if (string.IsNullOrEmpty(postData.FamilyName))
            {
                throw new BadRequestException("Name muss angegeben werden.");
            }

            if (string.IsNullOrEmpty(postData.FirstName))
            {
                throw new BadRequestException("Vorname muss angegeben werden.");
            }

            if (string.IsNullOrEmpty(postData.Street))
            {
                throw new BadRequestException("Strasse muss angegeben werden.");
            }

            if (string.IsNullOrEmpty(postData.ZipCode))
            {
                throw new BadRequestException("PLZ muss angegeben werden.");
            }

            if (string.IsNullOrEmpty(postData.Town))
            {
                throw new BadRequestException("Ort muss angegeben werden.");
            }

            if (string.IsNullOrEmpty(postData.CountryCode))
            {
                throw new BadRequestException("Land muss angegeben werden.");
            }

            if (string.IsNullOrEmpty(postData.EmailAddress))
            {
                throw new BadRequestException("E-Mail muss angegeben werden.");
            }

            if (!string.IsNullOrEmpty(postData.BirthdayString))
            {
                if (DateTime.TryParse(postData.BirthdayString, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out var birthday))
                {
                    postData.Birthday = birthday;
                }
                else
                {
                    throw new BadRequestException("The property BirthdayString is not in the expected format dd.mm.yyyy.");
                }
            }

            if (!string.IsNullOrEmpty(postData.DownloadLimitDisabledUntilString))
            {
                if (DateTime.TryParse(postData.DownloadLimitDisabledUntilString, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None,
                    out var downloadLimitDisabledUntil))
                {
                    postData.DownloadLimitDisabledUntil = downloadLimitDisabledUntil;
                }
                else
                {
                    throw new BadRequestException("The property DownloadLimitDisabledUntilString is not in the expected format dd.mm.yyyy.");
                }
            }

            if (!string.IsNullOrEmpty(postData.DigitalisierungsbeschraenkungString))
            {
                if (DateTime.TryParse(postData.DigitalisierungsbeschraenkungString, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None,
                    out var digitalisierungsbeschraenkungAufgehobenBis))
                {
                    postData.DigitalisierungsbeschraenkungAufgehobenBis = digitalisierungsbeschraenkungAufgehobenBis;
                }
                else
                {
                    throw new BadRequestException("The property DigitalisierungsbeschraenkungString is not in the expected format dd.mm.yyyy.");
                }
            }

            var originalUser = userDataAccess.GetUser(postData.Id);
            CheckCustomAttributes.CheckEditNotAllowedAttribute(originalUser, postData);
            CheckCustomAttributes.CheckEditNotAllowedForAttribute(originalUser, postData);
            CheckCustomAttributes.CheckEditRequiresFeatureAttribute(GetUser().Features, originalUser, postData);

            userDataAccess.UpdateUser(postData, access.UserId);


            // Alle zugewiesen Abliefernde Stellen löschen
            if (postData.RolePublicClient != AccessRoles.RoleAS)
            {
                userDataAccess.DeleteAllAblieferdeStelleFromUser(postData.Id);
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new JsonContent(new JObject {{"success", true}})
            };
            return response;
        }
        
        [HttpGet]
        public JObject GetUserSettings()
        {
            return userDataAccess.GetUser(ControllerHelper.GetCurrentUserId())?.Setting;
        }

        [HttpPost]
        public void UpdateUserSettings(JObject settings)
        {
            userDataAccess.UpdateUserSetting(settings, ControllerHelper.GetCurrentUserId());
        }

        [HttpGet]
        public JObject GetAllUsers()
        {
            var result = new JObject();
            try
            {
                result.Add("Users", JArray.FromObject(userDataAccess.GetAllUsers().Where(u => !Users.IsSystemUser(u.Id))));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Fehler beim laden der User");
                result.Add("error", ServiceHelper.GetExceptionInfo(ex));
            }

            return result;
        }

        [HttpPost]
        public HttpResponseMessage DeleteAblieferdeStelle(string userId, int amtId)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.BenutzerUndRolleBenutzerverwaltungZustaendigeStelleEdit);

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            JObject result;
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                if (amtId <= 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                userDataAccess.DeleteAblieferdeStelleFromUser(userId, amtId);
                result = new JObject {{"success", true}};
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Fehler beim entfernen der Zuordnung eines Amtes zum User; UserId:='{userId}', ablieferndeStelleId:={amtId}");
                result = new JObject {{"error", ServiceHelper.GetExceptionInfo(ex)}};
                response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            response.Content = new JsonContent(result);

            return response;
        }

        [HttpPost]
        public HttpResponseMessage CleanAndAddAblieferndeStelle(string userId, [FromBody] UserPostData postData)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.BenutzerUndRolleBenutzerverwaltungZustaendigeStelleEdit);

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            JObject result;
            try
            {
                if (access.HasRole(AccessRoles.RoleAS))
                {
                    response = new HttpResponseMessage(HttpStatusCode.Forbidden);
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                if (postData == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                userDataAccess.CleanAndAddAblieferndeStelleToUser(userId, postData.AblieferndeStelleIds, access.UserId);
                result = new JObject {{"success", true}};
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    $"Fehler beim zuordnen der Abliefernde Stelle zum User; UserId:='{userId}', ablieferndeStelleId:={postData?.AblieferndeStelleIds}");
                result = new JObject {{"error", ServiceHelper.GetExceptionInfo(ex)}};
                response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            response.Content = new JsonContent(result);

            return response;
        }

        [HttpGet]
        public IHttpActionResult GetIdentifizierungsmittelPdf(string userId)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungHerunterladenAusfuehren);
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest();
                }

                var identifierDocument = userDataAccess.GetIdentifierDocument(userId);
                Stream stream = new MemoryStream(identifierDocument);

                var result = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(stream)
                };

                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = userId + ".pdf"
                };

                return ResponseMessage(result);
            }
            catch (Exception e)
            {
                Log.Error(e, "GetIdentifizierungsmittelPdf({ID})", userId);
                return InternalServerError(e);
            }
        }

        [HttpPost]
        public IHttpActionResult SetIdentifizierungsmittelPdf(string userId, string rolePublicClient)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungUploadAusfuehren,
                ApplicationFeature.BenutzerUndRollenBenutzerverwaltungFeldBenutzerrollePublicClientBearbeiten);

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            var httpRequest = HttpContext.Current.Request;
            if (httpRequest.ContentLength <= GetMaxRequestSize())
            {
                byte[] identifizierungsmittel = null;
                if (httpRequest.Files.Count > 0)
                {
                    var postedFile = httpRequest.Files[0];
                    var buffer = new byte[16 * 1024];
                    using (var ms = new MemoryStream())
                    {
                        int read;
                        while ((read = postedFile.InputStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }

                        identifizierungsmittel = ms.ToArray();
                    }
                }

                userDataAccess.SetIdentifierDocument(userId, identifizierungsmittel, rolePublicClient);
                return Ok(true);
            }

            Log.Warning($"Versuch ein zu grosses Identifizierungsmittel hochzuladen. UserId:='{userId}'");
            return BadRequest("Die Datei überschreitet die maximal zulässige Grösse.");
        }

        private int GetMaxRequestSize()
        {
            var section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
            if (section != null)
                // Request length is in KB
            {
                return section.MaxRequestLength * 1024;
            }

            // Default is 4 MB
            return 4096 * 2014;
        }
    }
}