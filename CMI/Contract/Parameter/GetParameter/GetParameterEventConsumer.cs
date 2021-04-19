using System.Threading.Tasks;
using MassTransit;
using Serilog;

namespace CMI.Contract.Parameter.GetParameter
{
    public class GetParameterEventConsumer<T> : IConsumer<GetParameterEvent> where T : ISetting
    {
        public ParameterHelper parameterHelper = new ParameterHelper();

        public async Task Consume(ConsumeContext<GetParameterEvent> context)
        {
            Log.Information("Consumed GetParameterEvent of Type {Type} bus", typeof(T).FullName);

            var response = new GetParameterEventResponse
            {
                Parameters = parameterHelper.GetSettingAsParamListFromFile<T>()
            };

            await context.RespondAsync(response);

            Log.Verbose($"Consumed GetParameterEvent of Type {typeof(T).FullName} bus");
        }
    }
}