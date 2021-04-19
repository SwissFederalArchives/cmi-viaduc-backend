using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Manager.Onboarding.Properties;
using CMI.Utilities.Template;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using IBus = MassTransit.IBus;

namespace CMI.Manager.Onboarding
{
    public class OnboardingController : ApiController
    {
        private readonly IBus bus;
        private readonly UserDataAccess dataAccess;
        private readonly DataBuilder dataBuilder;
        private readonly IMailHelper mailHelper;
        private readonly IParameterHelper parameterHelper;


        public OnboardingController(IBus bus)
        {
            this.bus = bus;

            dataAccess = new UserDataAccess(DbConnectionSetting.Default.ConnectionString);
            parameterHelper = new ParameterHelper();
            mailHelper = new MailHelper();
            dataBuilder = new DataBuilder(bus);
        }


        [HttpPost]
        [Route("api/v1/onboarding/postback")]
        public IHttpActionResult Postback([FromBody] PostbackParameters parameters)
        {
            try
            {
                // Ausgabe auf Console wegen AK3.5: Der Ablehnungsgrund und der Ablehnungsprozesse ganz allgemein wird nie geloggt.
                Console.WriteLine("Post-back has been called with the following parameters:\n" +
                                  $"your_transaction_id: {parameters.your_transaction_id}\n" +
                                  $"webid_confirmed: {parameters.webid_confirmed}\n" +
                                  $"webid_response: {parameters.webid_response}");
                Log.Information("Post-back has been called with parameter your_transaction_id={your_transaction_id}", parameters.your_transaction_id);

                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parameters.webid_response));

                // Ausgabe auf Console wegen AK3.5: Der Ablehnungsgrund und der Ablehnungsprozesse ganz allgemein wird nie geloggt.
                Console.WriteLine($"Webid_response decoded: {decoded}");

                var jObject = (JObject) JsonConvert.DeserializeObject(decoded);
                var rejectionReason = jObject?.SelectToken("$.webid_result.rejection_reason.reason_name")?.ToString();
                var confirmed = parameters.webid_confirmed == "1" || parameters.webid_confirmed.ToLower() == "true";

                if (!confirmed)
                {
                    // Ausgabe auf Console wegen AK3.5: Der Ablehnungsgrund und der Ablehnungsprozesse ganz allgemein wird nie geloggt.
                    Console.WriteLine("Run code for the case 'not confirmed'...");

                    // Die folgende Bediengung sollte im Produktivsystem nie zutreffen
                    if (string.IsNullOrEmpty(rejectionReason))
                    {
                        rejectionReason = "RejectionWithNoReason";
                    }

                    var user = dataAccess.GetUserWitExtId(parameters.your_transaction_id);
                    if (user != null)
                    {
                        dataAccess.UpdateRejectionReason(rejectionReason, user.Id);

                        SendMail(user.Id);
                    }
                    else
                    {
                        Log.Information("User not found in Viaduc during post-back call.");
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error on processing post-back call.");
                return InternalServerError();
            }
        }

        private void SendMail(string userId)
        {
            try
            {
                var template = parameterHelper.GetSetting<IdentifizierungFehlgeschlagen>();
                var dataContext = dataBuilder
                    .AddUser(userId)
                    .Create();

                mailHelper.SendEmail(bus, template, dataContext, false)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while sending an EMail");
                throw;
            }
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public class PostbackParameters
        {
            public string webid_response { get; set; }
            public string your_transaction_id { get; set; }
            public string webid_action_id { get; set; }
            public string webid_confirmed { get; set; }
            public string webid_doc_signed { get; set; }
            public string webid_server_timestamp { get; set; }
        }
    }
}