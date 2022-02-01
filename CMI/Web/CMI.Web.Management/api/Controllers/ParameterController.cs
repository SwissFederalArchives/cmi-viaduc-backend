using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.GetParameter;
using CMI.Contract.Parameter.SaveParameter;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Management.Auth;
using MassTransit;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    [NoCache]
    public class ParameterController : ApiManagementControllerBase
    {
        private readonly IBus bus;

        public ParameterController(IBus bus)
        {
            this.bus = bus;
        }
        [HttpGet]
        public async Task<IHttpActionResult> GetAllParameters()
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationEinstellungenEinsehen);

            var a = bus.Address;
            var uri = new Uri(a, "GetParameterQueue");
            var requestClient = bus.CreateRequestClient<GetParameterRequest>(uri, TimeSpan.FromSeconds(35));
            var result = await requestClient.GetResponse<GetParameterResponse>(new GetParameterRequest());

            return Ok(result.Message.Parameters);
        }

        [HttpPost]
        public async Task<IHttpActionResult> SaveParameter(Parameter parameter)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationEinstellungenBearbeiten);

            var uri = new Uri(bus.Address, "SaveParameterQueue");
            var requestClient = bus.CreateRequestClient<SaveParameterRequest>(uri, TimeSpan.FromSeconds(20));
            var result = (await requestClient.GetResponse<SaveParameterResponse>(new SaveParameterRequest(parameter))).Message;

            if (result?.ErrorMessages == null || result.ErrorMessages.Length == 0)
            {
                return Ok("No service responded. Setting was not saved! Is the service up and running?");
            }

            if (result.ErrorMessages.Any(m => m == string.Empty))
            {
                return StatusCode(HttpStatusCode.NoContent);
            }

            return Ok(result.ErrorMessages.First(m => m != string.Empty));
        }
    }
}