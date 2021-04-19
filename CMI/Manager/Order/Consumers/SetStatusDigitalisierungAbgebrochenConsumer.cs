using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using CMI.Manager.Order.Status;

namespace CMI.Manager.Order.Consumers
{
    public class SetStatusDigitalisierungAbgebrochenConsumer : SingleOrderItemRequestConsumer<SetStatusDigitalisierungAbgebrochenRequest,
        SetStatusDigitalisierungAbgebrochenResponse>
    {
        public SetStatusDigitalisierungAbgebrochenConsumer(IOrderDataAccess dataAccess, StatusWechsler statuswechsler) : base(dataAccess,
            statuswechsler)
        {
        }

        public override async Task<SetStatusDigitalisierungAbgebrochenResponse> CreateResponse(OrderItem orderItem,
            SetStatusDigitalisierungAbgebrochenRequest Request)
        {
            await statuswechsler.Execute(oi => oi.SetStatusDigitalisierungAbgebrochen(Request.Grund), new[] {orderItem}, Users.Vecteur, DateTime.Now);
            return new SetStatusDigitalisierungAbgebrochenResponse();
        }
    }
}