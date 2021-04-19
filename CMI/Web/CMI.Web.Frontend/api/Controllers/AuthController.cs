using System.Net;
using System.Security.Authentication;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Frontend.api.Controllers
{
    [Authorize]
    [NoCache]
    public sealed class AuthController : ApiFrontendControllerBase
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
            try
            {
                var identity = authControllerHelper.GetIdentity(Request, User, true);
                return Ok(identity);
            }
            catch (AuthenticationException e)
            {
                return Content(HttpStatusCode.Forbidden, e.Message);
            }
        }
    }
}