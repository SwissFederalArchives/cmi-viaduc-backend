using CMI.Manager.Order;

namespace CMI.Web.Common.api
{
    public abstract class OrderControllerBase : ApiControllerBase
    {
        protected readonly OrderManager orderManager;

        public OrderControllerBase(OrderManager orderManager)
        {
            this.orderManager = orderManager;
        }
    }
}