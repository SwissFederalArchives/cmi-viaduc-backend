using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.ModelBinding;
using CMI.Access.Sql.Viaduc;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using Serilog;

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

        [AllowAnonymous]
        [Route("Auth/ExternalSignIn")]
        [HttpGet]
        public async Task<IHttpActionResult> OnExternalSignIn()
        {
            try
            {
                await authControllerHelper.OnExternalSignIn(Request.GetOwinContext(), true);
            }
            catch (AuthenticationException e)
            {
                Log.Error(e, "Fehler beim Anmelden");
            }

            return Redirect(WebHelper.FrontendAuthReturnUrl);
        }

        [AllowAnonymous]
        [Route("Auth/ExternalSignOut")]
        [HttpGet]
        public IHttpActionResult OnExternalSignOut()
        {
            try
            {
                authControllerHelper.OnExternalSignOut(Request.GetOwinContext(), true);
            }
            catch (AuthenticationException e)
            {
                Log.Error(e, "Fehler beim Abmelden");
            }

            return Redirect(WebHelper.FrontendLogoutReturnUrl);
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