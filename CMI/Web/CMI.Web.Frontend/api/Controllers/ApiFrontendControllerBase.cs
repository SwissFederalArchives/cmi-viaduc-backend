using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;

namespace CMI.Web.Frontend.api.Controllers
{
    [CamelCaseJson]
    public abstract class ApiFrontendControllerBase : ApiControllerBase
    {
        private UserAccessProvider userAccessProvider;

        protected FrontendSettingsViaduc Settings => FrontendSettingsViaduc.Instance;

        /// <summary>
        ///     Falls die Methode true zurückgibt, muss der der Benutzer
        ///     A) wählen das keine Personendaten vorhanden sind oder
        ///     B) ein Grund auswählen
        /// </summary>
        internal static bool CouldNeedAReason(ElasticArchiveRecord record, UserAccess access)
        {
            return record.HasCustomProperty("zugänglichkeitGemässBga")
                   && access.RolePublicClient == AccessRoles.RoleAS
                   && access.HasAsTokenFor(record.PrimaryDataDownloadAccessTokens)
                   && (record.CustomFields.zugänglichkeitGemässBga == "In Schutzfrist" ||
                       record.CustomFields.zugänglichkeitGemässBga == "Prüfung nötig");
        }

        protected UserAccess GetUserAccess(string language = null, string userId = null)
        {
            userAccessProvider = userAccessProvider ?? new UserAccessProvider(ControllerHelper.UserDataAccess);

            userId = userId ?? ControllerHelper.GetCurrentUserId();
            language = language ?? WebHelper.GetClientLanguage(Request);

            return userAccessProvider.GetUserAccess(language, userId);
        }

        protected UserAccess GetAnonymizedAccess(string accessRole, string language = null)
        {
            userAccessProvider = userAccessProvider ?? new UserAccessProvider(ControllerHelper.UserDataAccess);
            language = language ?? WebHelper.GetClientLanguage(Request);

            var userAccess = userAccessProvider.GetUserAccess(language, null);
            userAccess.RolePublicClient = accessRole;

            return userAccess;
        }
    }
}