using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using MassTransit;

namespace CMI.Manager.Order.Consumers
{
    public class RecalcIndivTokensConsumer : IConsumer<RecalcIndivTokens>
    {
        private readonly IOrderDataAccess dataAccess;

        public RecalcIndivTokensConsumer(IOrderDataAccess dataAccess)
        {
            this.dataAccess = dataAccess;
        }

        public async Task Consume(ConsumeContext<RecalcIndivTokens> context)
        {
            await UpdateIndivTokensHelper.SendToIndexManager(context.Message, dataAccess, context, context.SourceAddress);
        }
    }
}