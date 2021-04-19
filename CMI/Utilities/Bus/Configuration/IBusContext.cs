using MassTransit;

namespace CMI.Utilities.Bus.Configuration
{
    public interface IBusContext
    {
        ConsumeContext CallingConsumerContext { get; set; }
    }
}