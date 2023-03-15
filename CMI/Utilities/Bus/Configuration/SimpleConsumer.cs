using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Utilities.Bus.Configuration
{
    /// <summary>
    ///     The simple consumer class allows to consume a message from the bus and passing the massage
    ///     to a matching method in the specified manager.
    ///     This works by naming conventions. The name of the method to call in the manager must
    ///     match the message type name stripped by the 'Request' suffix.
    ///     Example: A consumer of the message GetOrdersRequest will call the method GetOrders on the manager.
    /// 
    ///     The properties of the message must match the type and ORDER of the method call.
    /// 
    ///     The return value wraps the return value of a manager in a single property.
    ///     Therefore the Type R must contain 0 (for void) or exactly 1 property.
    /// </summary>
    /// <typeparam name="TRequest">The type of the class to consume</typeparam>
    /// <typeparam name="TResponse">The type of the return value</typeparam>
    /// <typeparam name="TManager">The type of the manager to call</typeparam>
    /// <seealso cref="MassTransit.IConsumer{T}" />
    // ReSharper disable InconsistentNaming
    public class SimpleConsumer<TRequest, TResponse, TManager> : IConsumer<TRequest>
        where TRequest : class
        where TResponse : class, new()
        where TManager : class
    {
        private readonly TManager manager;

        public SimpleConsumer(TManager manager)
        {
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<TRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);
                var message = context.Message;

                // If manager supports the IBusContex interface pass the context 
                var busContext = manager as IBusContext;
                if (busContext != null)
                {
                    busContext.CallingConsumerContext = context;
                }

                // Convention: The method name of the manager to call is the name of the type T
                //             without the Request suffix
                var messageTypeName = message.GetType().Name;
                if (!messageTypeName.EndsWith("Request"))
                {
                    throw new InvalidOperationException("The name of the message type must end with 'Request' and be like the method name to call!");
                }

                var methodName = messageTypeName.Substring(0, messageTypeName.Length - "Request".Length);

                var methods = manager.GetType().GetMethods();
                var method = methods.First(m => m.Name.Equals(methodName, StringComparison.InvariantCultureIgnoreCase));

                if (method == null)
                {
                    throw new InvalidOperationException($"No method with name {methodName} found on manager {manager.GetType().Name}");
                }

                object result;

                if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    result = await (dynamic) method.Invoke(manager, MessageToParams(message));
                }
                else
                {
                    result = method.Invoke(manager, MessageToParams(message));
                }

                var retVal = WrapResultInResponse(result);

                // The asset is not available
                await context.RespondAsync(retVal);
            }
        }

        private TResponse WrapResultInResponse(object result)
        {
            var retVal = new TResponse();
            var singleProp = retVal.GetType().GetProperties().FirstOrDefault();
            // If we don't have a single property the retval is void from the manager
            if (singleProp != null)
            {
                singleProp.SetValue(retVal, result);
            }

            return retVal;
        }

        private object[] MessageToParams(TRequest message)
        {
            var retVal = new List<object>();
            var props = message.GetType().GetProperties();
            foreach (var property in props)
            {
                retVal.Add(property.GetValue(message));
            }

            return retVal.ToArray();
        }
    }
}