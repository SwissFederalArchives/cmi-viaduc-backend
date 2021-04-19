using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using CMI.Manager.Order.Status;

namespace CMI.Manager.Order.Consumers
{
    public class SetStatusDigitalisierungExternConsumer : SingleOrderItemRequestConsumer<SetStatusDigitalisierungExternRequest,
        SetStatusDigitalisierungExternResponse>
    {
        public SetStatusDigitalisierungExternConsumer(IOrderDataAccess dataAccess, StatusWechsler statuswechsler) : base(dataAccess, statuswechsler)
        {
        }

        public override async Task<SetStatusDigitalisierungExternResponse> CreateResponse(OrderItem orderItem,
            SetStatusDigitalisierungExternRequest Request)
        {
            await statuswechsler.Execute(oi => oi.SetStatusDigitalisierungExtern(), new[] {orderItem}, Users.Vecteur, DateTime.Now);
            return new SetStatusDigitalisierungExternResponse();
        }
    }
}