using System.Threading.Tasks;
using CMI.Contract.Monitoring;
using MassTransit;

namespace CMI.Manager.Asset.Consumers
{
    public class AbbyyOcrTestConsumer : IConsumer<AbbyyOcrTestRequest>
    {
        private readonly IOcrTester tester;

        public AbbyyOcrTestConsumer(IOcrTester tester)
        {
            this.tester = tester;
        }

        public async Task Consume(ConsumeContext<AbbyyOcrTestRequest> context)
        {
            var result = await tester.TestConversion();

            var response = new AbbyyOcrTestResponse
            {
                Error = result.Error,
                Success = result.Success
            };

            await context.RespondAsync(response);
        }
    }
}