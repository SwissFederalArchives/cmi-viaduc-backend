using System;
using MassTransit;
using System.Threading.Tasks;
using Serilog;
using CMI.Contract.Monitoring;

namespace CMI.Manager.Index.Consumer
{
    public class AnonymizationTestConsumer : IConsumer<AnonymizationTestRequest>
    {
        private readonly IIndexManager indexManager;

        public AnonymizationTestConsumer(IIndexManager indexManager)
        {
            this.indexManager = indexManager;
        }

        /// <summary>
        ///     Consumes the specified message from the bus.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Task.</returns>
        public async Task Consume(ConsumeContext<AnonymizationTestRequest> context)
        {
            var response = new AnonymizationTestResponse() { IsSuccess = false};
            try
            {
                Log.Information($"Received Test Anonymization command with Text {context.Message?.Value} from the bus.");
                var record = await indexManager.AnonymizeArchiveRecordAsync(new Contract.Common.ElasticArchiveDbRecord() {Title = context.Message.Value});
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to anonymize Text {context.Message.Value}");
                response.Exception = ex;
                response.IsSuccess = false;
            }
            await context.RespondAsync(response);
        }
    }
}
