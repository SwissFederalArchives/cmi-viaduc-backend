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
                    var requiresDifferentResponseAddress = IsOtherZoneService(serviceName);
                    var t = GetHeartbeatFromService(bus, serviceName, requiresDifferentResponseAddress);
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
        private async Task<MonitoringResult> GetHeartbeatFromService(IBus monitoringBus, string serviceName, bool requiresDifferentResponseAddress)
        {
            var stopwatch = new Stopwatch();
            var requestTimeout = TimeSpan.FromSeconds(ServiceHeartbeatRequestTimeout);
            var timeToLive = TimeSpan.FromSeconds(ServiceRequestTimeToLive);

            // If we need a different respone address, get one, if not, just return an empty method.
            var callback = requiresDifferentResponseAddress
                ? BusConfigurator.ChangeResponseAddress
                : new Action<SendContext<HeartbeatRequest>>(context => { });

            var client = monitoringBus.CreateRequestClient<HeartbeatRequest, HeartbeatResponse>(
                new Uri(monitoringBus.Address, string.Format(BusConstants.MonitoringServiceHeartbeatRequestQueue, serviceName)),
                requestTimeout,
                timeToLive,
                callback);
            try
            {
                stopwatch.Restart();
                var r = await client.Request(new HeartbeatRequest());
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

        /// <summary>
        ///     Determines whether a service belongs to a different network zone than the current service/program
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        private bool IsOtherZoneService(string serviceName)
        {
            if (Enum.TryParse(serviceName, true, out MonitoredServices result))
            {
                switch (result)
                {
                    case MonitoredServices.DataFeedService:
                    case MonitoredServices.ExternalContentService:
                    case MonitoredServices.RepositoryService:
                    case MonitoredServices.HarvestService:
                        return true;
                    default:
                        return false;
                }
            }

            return false;
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
            var timeToLive = TimeSpan.FromSeconds(ServiceRequestTimeToLive);
            var address = monitoringBus.Address;

            var elasticSearch = monitoringBus.CreateRequestClient<TestElasticsearchRequest, TestElasticsearchResponse>(
                new Uri(address, BusConstants.MonitoringElasticSearchTestQueue), timeout, timeToLive);
            var dirTest = monitoringBus.CreateRequestClient<DirCheckRequest, DirCheckResponse>(
                new Uri(address, BusConstants.MonitoringDirCheckQueue), timeout, timeToLive, BusConfigurator.ChangeResponseAddress);
            var aisDbTest = monitoringBus.CreateRequestClient<AisDbCheckRequest, AisDbCheckResponse>(
                new Uri(address, BusConstants.MonitoringAisDbCheckQueue), timeout, timeToLive, BusConfigurator.ChangeResponseAddress);
            var documentConverterInfo = monitoringBus.CreateRequestClient<DocumentConverterInfoRequest, DocumentConverterInfoResponse>(
                new Uri(address, BusConstants.MonitoringDocumentConverterInfoQueue), timeout, timeToLive);
            var abbyyOcrTest = monitoringBus.CreateRequestClient<AbbyyOcrTestRequest, AbbyyOcrTestResponse>(
                new Uri(address, BusConstants.MonitoringAbbyyOcrTestQueue), timeout, timeToLive);

            var t1 = Task.Run(() => TestDb());
            var t2 = TestRabbitMq();
            var t3 = TestElasticsearch(elasticSearch);
            var t4 = TestDir(dirTest);
            var t5 = TestAisDb(aisDbTest);
            var t6 = TestAbbyyLicence(documentConverterInfo);
            var t7 = TestAbbyyExecute(abbyyOcrTest);

            var results = await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7);
            return results;
        }


        private async Task<MonitoringResult> TestAisDb(IRequestClient<AisDbCheckRequest, AisDbCheckResponse> requestClient)
        {
            var watch = new Stopwatch();
            MonitoringResult result;
            try
            {
                watch.Start();
                var response = await requestClient.Request(new AisDbCheckRequest());
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

        private async Task<MonitoringResult> TestElasticsearch(IRequestClient<TestElasticsearchRequest, TestElasticsearchResponse> requestClient)
        {
            MonitoringResult result;
            try
            {
                var response = await requestClient.Request(new TestElasticsearchRequest());
                result = response.MonitoringResult;
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

        private async Task<MonitoringResult> TestDir(IRequestClient<DirCheckRequest, DirCheckResponse> requestClient)
        {
            var result = new MonitoringResult {MonitoredServices = "DIR"};
            var watch = new Stopwatch();

            try
            {
                watch.Start();
                var response = await requestClient.Request(new DirCheckRequest());
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
            IRequestClient<DocumentConverterInfoRequest, DocumentConverterInfoResponse> requestClient)
        {
            var watch = new Stopwatch();
            MonitoringResult result;

            try
            {
                watch.Start();
                var infoResponse = await requestClient.Request(new DocumentConverterInfoRequest());
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

        private async Task<MonitoringResult> TestAbbyyExecute(IRequestClient<AbbyyOcrTestRequest, AbbyyOcrTestResponse> requestClient)
        {
            var watch = new Stopwatch();
            MonitoringResult result;
            try
            {
                watch.Start();
                var testResponse = await requestClient.Request(new AbbyyOcrTestRequest());
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