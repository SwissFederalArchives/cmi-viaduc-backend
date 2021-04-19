using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Serilog;

namespace CMI.Contract.Parameter.SaveParameter
{
    public class SaveParameterEventConsumer<T> : IConsumer<SaveParameterEvent> where T : ISetting
    {
        public ParameterHelper parameterHelper = new ParameterHelper();

        public async Task Consume(ConsumeContext<SaveParameterEvent> context)
        {
            Log.Information("Consumed SaveParameterEvent of Type {Type} from bus", typeof(T).FullName);

            if (typeof(T).FullName != GetClassNameFormFullName(context.Message.Parameter.Name))
            {
                Log.Warning($"No Parameter {context.Message.Parameter.Name} was found in {typeof(T).FullName}");
                return;
            }

            Log.Verbose("Parameter {ParamName} was found", context.Message.Parameter.Name);

            var response = new SaveParameterEventResponse
            {
                ErrorMessage = parameterHelper.SaveSetting<T>(context.Message.Parameter)
            };

            Log.Verbose("Consumed SaveParameterEvent of Type {Type} bus", typeof(T).FullName);
            await context.RespondAsync(response);
        }

        private string GetClassNameFormFullName(string fullName)
        {
            var parts = fullName.Split('.');
            return string.Join(".", parts.Take(parts.Length - 1));
        }
    }
}