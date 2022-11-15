using System.Net.Http;
using System.Net.Http.Formatting;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using CMI.Access.Sql.Viaduc;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using Serilog;

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

        [AllowAnonymous]
        [Route("Auth/ExternalSignIn")]
        [HttpGet]
        public async Task<IHttpActionResult> OnExternalSignIn()
        {
            try
            {
                Log.Information("Entering OnExternalSignIn for user");
                await authControllerHelper.OnExternalSignIn(Request.GetOwinContext(), false);
            }
            catch (AuthenticationException e)
            {
                Log.Error(e, "Fehler beim Anmelden");
            }

            Log.Information("Redirecting user to {ManagementAuthReturnUrl}", WebHelper.ManagementAuthReturnUrl);
            return Redirect(WebHelper.ManagementAuthReturnUrl);
        }

        [AllowAnonymous]
        [Route("Auth/ExternalSignOut")]
        [HttpGet]
        public IHttpActionResult OnExternalSignOut()
        {
            try
            {
                authControllerHelper.OnExternalSignOut(Request.GetOwinContext(), false);
            }
            catch (AuthenticationException e)
            {
                Log.Error(e, "Fehler beim Abmelden");
            }

            return Redirect(WebHelper.ManagementLogoutReturnUrl);            
        }

        [AllowAnonymous]
        [Route("Auth/Test")]
        [HttpGet]
        public IHttpActionResult OnExternalTest()
        {
            Log.Information("Entering Test Methode");
            return Json(new 
            { 
                Timestamep = $"{System.DateTime.Now}",
                Request = Request.RequestUri.OriginalString,
                Controller = ControllerContext.Controller.GetType().FullName
            });
        }

        // This method is called when IAM-authentication was successful
        [HttpGet]
        public IHttpActionResult GetIdentity()
        {
            return Ok(authControllerHelper.GetIdentity(Request, User, false));
        }
    }
}