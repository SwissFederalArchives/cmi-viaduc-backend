using System.Threading.Tasks;
using MassTransit;

namespace CMI.Manager.Order
{
    internal interface ISendable
    {
        Task Send(IBus bus);
    }
}