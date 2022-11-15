using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Manager.Viaduc.Properties;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Viaduc
{
    public class ReadUserInformationConsumer : IConsumer<ReadUserInformationRequest>
    {
        private UserDataAccess dataAccess;


        public async Task Consume(ConsumeContext<ReadUserInformationRequest> context)
        {
            if (dataAccess == null)
            {
                dataAccess = new UserDataAccess(DbConnectionSetting.Default.ConnectionString);
            }

            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(ReadUserInformationRequest), context.ConversationId);

                await context.RespondAsync(new ReadUserInformationResponse
                {
                    User = dataAccess.GetUser(context.Message.UserId)
                });

                Log.Information("Response sent.");
            }
        }
    }
}