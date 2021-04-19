using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    [NoCache]
    public sealed class AuthController : ApiControllerBase
    {
        private readonly AuthControllerHelper authControllerHelper;

        public AuthController(
            IUserDataAccess userDataAccess,
            IAuthenticationHelper authenticationHelper,
            IWebCmiConfigProvider webCmiConfigProvider,
            IApplicationRoleUserDataAccess applicationRoleUserDataAccess)
        {
            authControllerHelper = new AuthControllerHelper(applicationRoleUserDataAccess, userDataAccess, ControllerHelper, authenticationHelper,
                webCmiConfigProvider);
        }

        // This method is called when IAM-authentication was successful
        [HttpGet]
        public IHttpActionResult GetIdentity()
        {
            return Ok(authControllerHelper.GetIdentity(Request, User, false));
        }
    }
}