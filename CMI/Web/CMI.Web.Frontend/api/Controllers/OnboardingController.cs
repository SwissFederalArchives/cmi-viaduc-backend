using System;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Web.Common.api.Attributes;
using MassTransit;
using Serilog;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;

namespace CMI.Web.Frontend.api.Controllers
{
    [Authorize]
    [NoCache]
    public class OnboardingController : ApiFrontendControllerBase
    {
        private readonly IRequestClient<StartOnboardingProcessRequest> onboardingClient;
        private readonly IRequestClient<HandleOnboardingCallbackRequest> handleClient;

        public OnboardingController(IRequestClient<StartOnboardingProcessRequest> onboardingClient,
            IRequestClient<HandleOnboardingCallbackRequest> handleClient)
        {
            this.onboardingClient = onboardingClient;
            this.handleClient = handleClient;
        }

        [HttpPost]
        public async Task<IHttpActionResult> StartProcess([FromBody] OnboardingModel model)
        {
            var userAccess = GetUserAccess();
            if (userAccess.RolePublicClient != AccessRoles.RoleOe2)
            {
                return BadRequest("Benutzer hat nicht die Role Ö2.");
            }

            var date = DateTimeOffset.Parse(model.DateOfBirth);
            model.DateOfBirth = $"{date.Year}-{date.Month.ToString().PadLeft(2, '0')}-{date.Day.ToString().PadLeft(2, '0')}"; 
            model.Language = userAccess.Language;
            model.UserId = userAccess.UserId;

            Log.Information("Sending onboarding start request to Onboarding Manager: {model}", JsonConvert.SerializeObject(model));

            var result = (await onboardingClient.GetResponse<StartOnboardingProcessResponse>(new StartOnboardingProcessRequest()
            {
                OnboardingModel = model
            })).Message.Result;

            if (!result.Success)
            {
                throw new InvalidOperationException("Unable to start onboarding process. See log for details.");
            }

            return Ok(result.ProcessUrl);
        }

        [HttpPost]
        [Route("api/v1/onboarding/success")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Success([FromBody] PostbackParameters parameters)
        {
            var result = (await handleClient.GetResponse<HandleOnboardingCallbackResponse>(new HandleOnboardingCallbackRequest
            {
                CallbackType = CallbackType.Success,
                Parameters = parameters
            })).Message;

            return result.Success ? Ok() : InternalServerError();
        }

        [HttpPost]
        [Route("api/v1/onboarding/error")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Error([FromBody] PostbackParameters parameters)
        {
            var result = (await handleClient.GetResponse<HandleOnboardingCallbackResponse>(new HandleOnboardingCallbackRequest
            {
                CallbackType = CallbackType.Error,
                Parameters = parameters
            })).Message;

            return result.Success ? Ok() : InternalServerError();
        }

        [HttpPost]
        [Route("api/v1/onboarding/warn")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Warn([FromBody] PostbackParameters parameters)
        {
            var result = (await handleClient.GetResponse<HandleOnboardingCallbackResponse>(new HandleOnboardingCallbackRequest
            {
                CallbackType = CallbackType.Warn,
                Parameters = parameters
            })).Message;

            return result.Success ? Ok() : InternalServerError();
        }

        [HttpPost]
        [Route("api/v1/onboarding/review")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Review([FromBody] PostbackParameters parameters)
        {
            var result = (await handleClient.GetResponse<HandleOnboardingCallbackResponse>(new HandleOnboardingCallbackRequest
            {
                CallbackType = CallbackType.Review,
                Parameters = parameters
            })).Message;

            return result.Success ? Ok() : InternalServerError();
        }
    }
}