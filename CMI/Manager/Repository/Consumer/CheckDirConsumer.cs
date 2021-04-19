using System.Threading.Tasks;
using CMI.Access.Repository;
using CMI.Contract.Monitoring;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Repository.Consumer
{
    public class CheckDirConsumer : IConsumer<DirCheckRequest>
    {
        private readonly IRepositoryConnectionFactory connectionFactory;


        public CheckDirConsumer(IRepositoryConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }


        public async Task Consume(ConsumeContext<DirCheckRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(DirCheckRequest),
                    context.ConversationId);

                var response = new DirCheckResponse();

                var session = connectionFactory.ConnectToFirstRepository();

                response.Ok = session != null;

                if (session != null)
                {
                    var repositoryInfo = session.RepositoryInfo;

                    response.RepositoryName = repositoryInfo.Name;
                    response.ProductVersion = repositoryInfo.ProductVersion;
                    response.ProductName = repositoryInfo.ProductName;
                }

                await context.RespondAsync(response);
            }
        }
    }
}