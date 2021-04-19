using CMI.Web.Common.Helpers;
using CMI.Web.Management.api.Controllers;

namespace CMI.Web.Management.Auth
{
    public static class ManagementAuthExtensions
    {
        public static ManagementUserAccess GetManagementAccess(this ApiManagementControllerBase controller, string language = null)
        {
            return controller.ManagementControllerHelper.GetUserAccess(language ?? WebHelper.GetClientLanguage(controller.Request));
        }
    }
}