using CMI.Contract.Monitoring;
using MassTransit;
using Serilog;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using CMI.Manager.Asset.Properties;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Asset.Consumers;

public class CantaloupeTestConsumer : IConsumer<CantaloupeTestRequest>
{
    private HttpClient client;

    public CantaloupeTestConsumer()
    {
        client = new HttpClient();
    }

    public async Task Consume(ConsumeContext<CantaloupeTestRequest> context)
    {
        using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
        {
            Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                nameof(AbbyyOcrTestRequest), context.ConversationId);
            client = new HttpClient { Timeout = TimeSpan.FromSeconds(context.Message.Timeout) };
            var response = await TestCantaloupeService();

            await context.RespondAsync(response);
        }
    }


    private async Task<CantaloupeTestResponse> TestCantaloupeService()
    {
        var result = new CantaloupeTestResponse();
        var uriBuilder = new UriBuilder(Settings.Default.CantaloupeUrl);
        try
        {
            var response = await client.GetAsync(uriBuilder.Uri);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
            {
                if (Regex.IsMatch(responseString, "<h1>.*?<\\/h1>"))
                {
                    var regexString = Regex.Match(responseString, "<h1>.*?<\\/h1>").Value;
                    var titel = Regex.Replace(regexString, @"<h1>|</h1>|</small>", "");
                    var version = Regex.Replace(titel, @"<small>", "Version: ");
                    result.CantaloupeResponse = version;
                }

                result.Ok = true;
            }
            else
            {
                result.Ok = false;
            }
        }
        catch (Exception ex)
        {
            result.Exception = ex;
            result.Ok = false;
        }

        result.CantaloupeResponse += $" Request uri: {uriBuilder}";

        return result;
    }

}