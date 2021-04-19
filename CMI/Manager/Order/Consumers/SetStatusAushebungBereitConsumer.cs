using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using CMI.Manager.Order.Status;

namespace CMI.Manager.Order.Consumers
{
    public class SetStatusAushebungBereitConsumer : SingleOrderItemRequestConsumer<SetStatusAushebungBereitRequest, SetStatusAushebungBereitResponse>
    {
        public SetStatusAushebungBereitConsumer(IOrderDataAccess dataAccess, StatusWechsler statuswechsler) : base(dataAccess, statuswechsler)
        {
        }

        public override async Task<SetStatusAushebungBereitResponse> CreateResponse(OrderItem orderItem, SetStatusAushebungBereitRequest request)
        {
            await statuswechsler.Execute(x => x.SetStatusAushebungBereit(), new[] {orderItem}, Users.Vecteur, DateTime.Now);
            return new SetStatusAushebungBereitResponse();
        }
    }
}