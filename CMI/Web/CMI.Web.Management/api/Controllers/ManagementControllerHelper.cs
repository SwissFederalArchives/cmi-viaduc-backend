using System.Web.Http;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.Auth;

namespace CMI.Web.Management.api.Controllers
{
    public class ManagementControllerHelper : IManagementControllerHelper
    {
        private readonly ApiController controller;
        private readonly ControllerHelper helper;

        public ManagementControllerHelper(ApiController controller, ControllerHelper helper)
        {
            this.controller = controller;
            this.helper = helper;
        }
        

        public IManagementUserAccess GetUserAccess(string language = null)
        {
            var userId = helper.GetCurrentUserId();
            return new ManagementUserAccess(
                userId,
                helper.UserDataAccess.GetEiamRoles(userId),
                helper.UserDataAccess.GetAsTokensDesUser(userId),
                language ?? WebHelper.GetClientLanguage(controller.Request)
            );
        }
    }

    public interface IManagementControllerHelper
    {
        IManagementUserAccess GetUserAccess(string language = null);
    }
}