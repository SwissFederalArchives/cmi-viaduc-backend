using CMI.Contract.Monitoring;
using CMI.Engine.Asset.Solr;
using MassTransit;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Asset.Consumers;

public class SolrTestConsumer : IConsumer<SolrTestRequest>
{
    private readonly SolrConnectionInfo solrConnectionInfo;

    private HttpClient client;

    public SolrTestConsumer(SolrConnectionInfo solrConnectionInfo)
    {
        this.solrConnectionInfo = solrConnectionInfo;
    }

    public async Task Consume(ConsumeContext<SolrTestRequest> context)
    {
        using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
        {
            Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                nameof(AbbyyOcrTestRequest), context.ConversationId);
            client = new HttpClient { Timeout = TimeSpan.FromSeconds(context.Message.Timeout) };
            var response = await TestSolrService();

            await context.RespondAsync(response);
        }
    }


    private async Task<SolrTestResponse> TestSolrService()
    {
        var result = new SolrTestResponse();
        var baseUri = $"{solrConnectionInfo.SolrUrl}{solrConnectionInfo.SolrCoreName}/admin/system";

        try
        {
            var uriBuilder = new UriBuilder(baseUri);
            var response = await client.GetAsync(uriBuilder.Uri);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
            {
                if (Regex.IsMatch(responseString, "\"solr-spec-version\":.*?,"))
                {
                    var regexString = Regex.Match(responseString, "\"solr-spec-version\":.*?,").Value;
                    var version = Regex.Replace(regexString, "\"solr-spec-version\":", "Solr Version: ");
                    version = Regex.Replace(version, "\"", "");

                    result.SolrResponse = version;
                }

                if (Regex.IsMatch(responseString, "\"start\":.*?,"))
                {
                    var regexString = Regex.Match(responseString, "\"start\":.*?,").Value;
                    var start = Regex.Replace(regexString, "\"start\":", " Start Zeit: ");
                    start = Regex.Replace(start, "\"", "");
                    result.SolrResponse += start;
                }

                result.Ok = true;
            }
            else
            {
                result.Ok = false;
                result.SolrResponse = responseString;
            }
        }
        catch (Exception ex)
        {
            result.Exception = ex;
            result.Ok = false;
        }

        result.SolrResponse += $" Request uri: {baseUri}";

        return result;
    }

}
