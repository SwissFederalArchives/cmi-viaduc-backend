using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Utilities.Bus.Configuration;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.api.Configuration;
using MassTransit;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Web.Management.api.Controllers
{
    [NoCache]
    public class MonitoringController : ApiManagementControllerBase
    {
        private readonly IBus bus;
        private readonly HttpClient rabbitMqHttpClient;


        public MonitoringController(IBus bus)
        {
            this.bus = bus;

            rabbitMqHttpClient = new HttpClient {Timeout = TimeSpan.FromSeconds(ServiceTestRequestTimeout)};
            var credentials = Encoding.ASCII.GetBytes($"{BusConfigurator.UserName}:{BusConfigurator.Password}");
            rabbitMqHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
        }

        public int ServiceHeartbeatRequestTimeout => WebHelper.GetIntSetting("serviceHeartbeatRequestTimeout", 5);
        public int ServiceTestRequestTimeout => WebHelper.GetIntSetting("serviceTestRequestTimeout", 15);
        public int ServiceRequestTimeToLive => WebHelper.GetIntSetting("serviceRequestTimeToLive", 15);

        [HttpGet]
        public async Task<IHttpActionResult> GetServicesStatus()
        {
            return Ok(await GetMonitoringResultForServiceStatus());
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetServicesStatusHtml()
        {
            var servicesStatus = await GetMonitoringResultForServiceStatus();
            var html = CreateHtmlFromMonitoringResult(servicesStatus);
            return Ok(html);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetTestStatus()
        {
            var result = await GetMonitoringResultForTestStatus();
            return Ok(result);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetTestStatusHtml()
        {
            var result = await GetMonitoringResultForTestStatus();
            var html = CreateHtmlFromMonitoringResult(result);
            return Ok(html);
        }

        private async Task<MonitoringResult[]> GetMonitoringResultForServiceStatus()
        {
            var tasks = new List<Task<MonitoringResult>>();

            try
            {
                // Send heartbeat request async to every service
                foreach (var serviceName in Enum.GetNames(typeof(MonitoredServices)).Where(n => n != MonitoredServices.NotMonitored.ToString()))
                {
                    var t = GetHeartbeatFromService(bus, serviceName);
                    tasks.Add(t);
                }

                var results = await Task.WhenAll(tasks);
                return results;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error in {Name}", nameof(GetMonitoringResultForServiceStatus));
                throw;
            }
        }

        /// <summary>
        ///     Sends a hearbeat request to the service.
        /// </summary>
        /// <param name="monitoringBus">The message bus.</param>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns>HeartbeatResponse.</returns>
        private async Task<MonitoringResult> GetHeartbeatFromService(IBus monitoringBus, string serviceName)
        {
            var stopwatch = new Stopwatch();
            var requestTimeout = TimeSpan.FromSeconds(ServiceHeartbeatRequestTimeout);

            var client = monitoringBus.CreateRequestClient<HeartbeatRequest>(new Uri(monitoringBus.Address, string.Format(BusConstants.MonitoringServiceHeartbeatRequestQueue, serviceName)), requestTimeout);
            try
            {
                stopwatch.Restart();
                var r = (await client.GetResponse<HeartbeatResponse>(new HeartbeatRequest())).Message;
                stopwatch.Stop();
                r.HartbeatResponseTime = stopwatch.ElapsedMilliseconds;
                return new MonitoringResult
                {
                    MonitoredServices = r.Type.ToString(),
                    Message = r.Status.ToString(),
                    Status = r.Status.ToString(),
                    ExecutionTime = r.HartbeatResponseTime
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new MonitoringResult
                {
                    MonitoredServices = serviceName,
                    Message = ex.Message,
                    Status = HeartbeatStatus.Nok.ToString()
                };
            }
        }
        
        private string CreateHtmlFromMonitoringResult(MonitoringResult[] monitoringResults)
        {
            var html = new StringBuilder();
            html.Append(@"<table>
    <tr>
	    <th>Status</th>
	    <th>Service</th>
	    <th>Execution Time</th>
	    <th>Message</th>
    </tr>");
            foreach (var result in monitoringResults)
            {
                html.Append($@"
    <tr>
        <td>{result.Status}</td>
        <td>{result.MonitoredServices}</td>
        <td>{result.ExecutionTime} ms</td>
        <td>{result.Message}</td>
    </tr>");
                var status = result.Status == HeartbeatStatus.Ok.ToString() ? 0 : 1;
                html.Append($@"
<!-- DYNMON_ENTRY_PREFIX {result.MonitoredServices};{status};{result.ExecutionTime}; DYNMON_ENTRY_SUFFIX -->");
            }

            html.Append(@"
</table>");
            return html.ToString();
        }

        private async Task<MonitoringResult[]> GetMonitoringResultForTestStatus()
        {
            var results = await RunTasks(bus);
            return results;
        }

        private async Task<MonitoringResult[]> RunTasks(IBus monitoringBus)
        {
            var timeout = TimeSpan.FromSeconds(ServiceTestRequestTimeout);
            var address = monitoringBus.Address;

            var elasticSearch = monitoringBus.CreateRequestClient<TestElasticsearchRequest>(new Uri(address, BusConstants.MonitoringElasticSearchTestQueue), timeout);
            var dirTest = monitoringBus.CreateRequestClient<DirCheckRequest>(new Uri(address, BusConstants.MonitoringDirCheckQueue), timeout);
            var aisDbTest = monitoringBus.CreateRequestClient<AisDbCheckRequest>(new Uri(address, BusConstants.MonitoringAisDbCheckQueue), timeout);
            var documentConverterInfo = monitoringBus.CreateRequestClient<DocumentConverterInfoRequest>(new Uri(address, BusConstants.MonitoringDocumentConverterInfoQueue), timeout);
            var abbyyOcrTest = monitoringBus.CreateRequestClient<AbbyyOcrTestRequest>(new Uri(address, BusConstants.MonitoringAbbyyOcrTestQueue), timeout);

            var anonymizedTest = monitoringBus.CreateRequestClient<AnonymizationTestRequest>(new Uri(address, BusConstants.IndexManagerAnonymizeTestMessageQueue), timeout);

            var t1 = Task.Run(() => TestDb());
            var t2 = TestRabbitMq();
            var t3 = TestElasticsearch(elasticSearch);
            var t4 = TestDir(dirTest);
            var t5 = TestAisDb(aisDbTest);
            var t6 = TestAbbyyLicence(documentConverterInfo);
            var t7 = TestAbbyyExecute(abbyyOcrTest);
            var t8 = TestAnonymizedService(anonymizedTest);

            var results = await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8);
            return results;
        }

        private async Task<MonitoringResult> TestAnonymizedService(IRequestClient<AnonymizationTestRequest> requestClient)
        {
            var watch = new Stopwatch();
            MonitoringResult result;
            watch.Start();

            try
            {
                var response = await requestClient.GetResponse<AnonymizationTestResponse>(new AnonymizationTestRequest() { Value = "Test" });
                var success = response.Message.IsSuccess;

                result = new MonitoringResult
                {
                    MonitoredServices = "Anonymisierungsdienst",
                    Status = success ? HeartbeatStatus.Ok.ToString() : HeartbeatStatus.Nok.ToString(),
                    Message = success ? "Ok, checks done. " : $"Nok, Exception: {response.Message.Exception?.Message}",
                    ExecutionTime = watch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                result = new MonitoringResult
                {
                    MonitoredServices = "Anonymisierungsdienst",
                    Status = HeartbeatStatus.Nok.ToString(),
                    Message = $"Viaduc service call failed which execute the test. Exception: {ex.Message}",
                    ExecutionTime = watch.ElapsedMilliseconds
                };
            }

            return result;
        }

        private async Task<MonitoringResult> TestAisDb(IRequestClient<AisDbCheckRequest> requestClient)
        {
            var watch = new Stopwatch();
            MonitoringResult result;
            try
            {
                watch.Start();
                var response = (await requestClient.GetResponse<AisDbCheckResponse>(new AisDbCheckRequest())).Message;
                watch.Stop();

                result = new MonitoringResult
                {
                    MonitoredServices = "AIS DB",
                    ExecutionTime = watch.ElapsedMilliseconds
                };

                if (response.Ok)
                {
                    result.Status = HeartbeatStatus.Ok.ToString();
                    result.Message = $"Ok, DB Version = {response.DbVersion}";
                }
                else
                {
                    result.Status = HeartbeatStatus.Nok.ToString();
                    result.Message = $"Nok, Exception: {response.Exception.Message}";
                }
            }
            catch (Exception ex)
            {
                result = new MonitoringResult
                {
                    MonitoredServices = "AIS DB",
                    Status = HeartbeatStatus.Nok.ToString(),
                    Message = $"Viaduc service call failed which execute the test. Exception: {ex.Message}",
                    ExecutionTime = watch.ElapsedMilliseconds
                };
            }

            return result;
        }

        private MonitoringResult TestDb()
        {
            var connectionString = ManagementSettingsViaduc.Instance.SqlConnectionString;
            var watch = new Stopwatch();

            watch.Start();
            var data = new UserDataAccess(connectionString);
            var testResult = data.TestDbAccess();
            watch.Stop();

            string message;
            if (testResult <= 0)
            {
                message = "DB Version not found";
            }
            else if (testResult == 9999)
            {
                message = "DB Upgrade failed";
            }
            else
            {
                message = "Ok";
            }

            var result = new MonitoringResult
            {
                MonitoredServices = "DB",
                Status = testResult > 0 && testResult != 9999
                    ? HeartbeatStatus.Ok.ToString()
                    : HeartbeatStatus.Nok.ToString(),
                ExecutionTime = watch.ElapsedMilliseconds,
                Message = $"{message}, DB Version = {testResult}"
            };
            return result;
        }

        private async Task<MonitoringResult> TestElasticsearch(IRequestClient<TestElasticsearchRequest> requestClient)
        {
            MonitoringResult result;
            try
            {
                var response = await requestClient.GetResponse<TestElasticsearchResponse>(new TestElasticsearchRequest());
                result = response.Message.MonitoringResult;
            }
            catch (Exception ex)
            {
                result = new MonitoringResult
                {
                    MonitoredServices = "Elasticsearch",
                    Status = HeartbeatStatus.Nok.ToString(),
                    Message = $"Viaduc service call failed which execute the test. Exception: {ex.Message}"
                };
            }

            return result;
        }

        private async Task<MonitoringResult> TestRabbitMq()
        {
            var result = new MonitoringResult {MonitoredServices = "RabbitMQ"};
            var watch = new Stopwatch();
            var baseUri = WebHelper.GetStringSetting("rabbitMqManagementUri", string.Empty);

            try
            {
                watch.Start();

                var uriBuilder = new UriBuilder(baseUri);
                uriBuilder.Path = "api/healthchecks/node";

                var response = await rabbitMqHttpClient.GetAsync(uriBuilder.Uri);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JsonConvert.DeserializeObject<RabbitMcHealthCheckResponse>(responseString);

                if (responseJson.Status == "ok")
                {
                    result.Message = "Ok, Health checks done";
                    result.Status = HeartbeatStatus.Ok.ToString();
                }
                else
                {
                    result.Message = $"Nok, Reason from RabbitMQ: {responseJson.Reason}";
                    result.Status = HeartbeatStatus.Nok.ToString();
                }

                watch.Stop();
            }
            catch (Exception ex)
            {
                result.Message = $"Nok, Exception: {ex.Message}";
                result.Status = HeartbeatStatus.Nok.ToString();
            }

            result.Message += $", Request uri: {baseUri}";
            result.ExecutionTime = watch.ElapsedMilliseconds;

            return result;
        }

        private async Task<MonitoringResult> TestDir(IRequestClient<DirCheckRequest> requestClient)
        {
            var result = new MonitoringResult {MonitoredServices = "DIR"};
            var watch = new Stopwatch();

            try
            {
                watch.Start();
                var response = (await requestClient.GetResponse<DirCheckResponse>(new DirCheckRequest())).Message;
                watch.Stop();

                if (response.Ok)
                {
                    result.Status = HeartbeatStatus.Ok.ToString();
                    result.Message =
                        $"Ok, Product Version: {response.ProductVersion}, Product Name: {response.ProductName}, Repository Name: {response.RepositoryName}";
                }
                else
                {
                    result.Status = HeartbeatStatus.Nok.ToString();
                    result.Message = "Nok";
                }
            }
            catch (Exception ex)
            {
                result.Status = HeartbeatStatus.Nok.ToString();
                result.Message = $"Viaduc service call failed which execute the test. Exception: {ex.Message}";
            }

            result.ExecutionTime = watch.ElapsedMilliseconds;

            return result;
        }

        private async Task<MonitoringResult> TestAbbyyLicence(
            IRequestClient<DocumentConverterInfoRequest> requestClient)
        {
            var watch = new Stopwatch();
            MonitoringResult result;

            try
            {
                watch.Start();
                var infoResponse = (await requestClient.GetResponse<DocumentConverterInfoResponse>(new DocumentConverterInfoRequest())).Message;
                watch.Stop();

                result = new MonitoringResult
                {
                    MonitoredServices = "Abbyy Licence",
                    Status = infoResponse.PagesRemaining > 0 ? HeartbeatStatus.Ok.ToString() : HeartbeatStatus.Nok.ToString(),
                    ExecutionTime = watch.ElapsedMilliseconds
                };

                if (infoResponse.PagesRemaining == null)
                {
                    result.Message = "Nok, Information missing. Abbyy not installed?";
                }
                else
                {
                    result.Message = infoResponse.PagesRemaining > 0 ? "Ok" : "Nok";
                    result.Message += $", Remaining pages: {infoResponse.PagesRemaining}";
                }
            }
            catch (Exception ex)
            {
                result = new MonitoringResult
                {
                    MonitoredServices = "Abbyy Licence",
                    Status = HeartbeatStatus.Nok.ToString(),
                    Message = $"Viaduc service call failed. Exception: {ex.Message}",
                    ExecutionTime = watch.ElapsedMilliseconds
                };
            }

            return result;
        }

        private async Task<MonitoringResult> TestAbbyyExecute(IRequestClient<AbbyyOcrTestRequest> requestClient)
        {
            var watch = new Stopwatch();
            MonitoringResult result;
            try
            {
                watch.Start();
                var testResponse = (await requestClient.GetResponse<AbbyyOcrTestResponse>(new AbbyyOcrTestRequest())).Message;
                watch.Stop();

                result = new MonitoringResult
                {
                    MonitoredServices = "Abbyy Execute",
                    Status = testResponse.Success ? HeartbeatStatus.Ok.ToString() : HeartbeatStatus.Nok.ToString(),
                    Message = testResponse.Success ? "Ok" : "Nok, Error: " + testResponse.Error,
                    ExecutionTime = watch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                result = new MonitoringResult
                {
                    MonitoredServices = "Abbyy Execute",
                    Status = HeartbeatStatus.Nok.ToString(),
                    Message = $"Viaduc service call failed. Exception: {ex.Message}",
                    ExecutionTime = watch.ElapsedMilliseconds
                };
            }

            return result;
        }


        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class RabbitMcHealthCheckResponse
        {
            public string Status { get; set; }

            public string Reason { get; set; }
        }
    }
}