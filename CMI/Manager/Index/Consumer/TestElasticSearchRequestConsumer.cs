using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CMI.Access.Common;
using CMI.Contract.Monitoring;
using MassTransit;
using Serilog;

namespace CMI.Manager.Index.Consumer
{
    public class TestElasticSearchRequestConsumer : IConsumer<TestElasticsearchRequest>
    {
        private readonly ITestSearchIndexDataAccess dbAccess;

        public TestElasticSearchRequestConsumer(ITestSearchIndexDataAccess dbAccess)
        {
            this.dbAccess = dbAccess;
        }

        public async Task Consume(ConsumeContext<TestElasticsearchRequest> context)
        {
            var watch = new Stopwatch();
            var result = new MonitoringResult
            {
                MonitoredServices = "Elasticsearch"
            };

            try
            {
                watch.Start();
                var testResponse = await dbAccess.GetElasticIndexHealth();

                if (testResponse.IsReadOnly)
                {
                    result.Status = HeartbeatStatus.Nok.ToString();
                    result.Message = "Error, Index is set to readonly!";
                }
                else if (testResponse.Health == "red")
                {
                    result.Status = HeartbeatStatus.Nok.ToString();
                    result.Message = "Error, Index health-status is red!";
                }
                else if (testResponse.Status != "open")
                {
                    result.Status = HeartbeatStatus.Nok.ToString();
                    result.Message = $"Error, Index status is {testResponse.Status}";
                }
                else
                {
                    result.Status = HeartbeatStatus.Ok.ToString();
                    result.Message = $"Ok, Health: {testResponse.Health}, " +
                                     $"Status: {testResponse.Status}, " +
                                     $"IsReadOnly: {testResponse.IsReadOnly}, " +
                                     $"DocsCount: {testResponse.DocsCount}";
                }
            }
            catch (Exception ex)
            {
                result.Status = HeartbeatStatus.Nok.ToString();
                Log.Error(ex, "Error when getting Index-Health-Status");
                result.Message = "Error: Unknown Error when accessing Elastic (see Log)";
            }
            finally
            {
                watch.Stop();
                result.ExecutionTime = watch.ElapsedMilliseconds;
            }

            await context.RespondAsync(new TestElasticsearchResponse
            {
                MonitoringResult = result
            });
        }
    }
}