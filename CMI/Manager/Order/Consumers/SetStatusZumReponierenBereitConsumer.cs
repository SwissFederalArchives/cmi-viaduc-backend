using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using CMI.Manager.Order.Status;
using MassTransit;

namespace CMI.Manager.Order.Consumers
{
    public class SetStatusZumReponierenBereitConsumer : IConsumer<SetStatusZumReponierenBereitRequest>
    {
        private readonly IOrderDataAccess dataAccess;
        private readonly StatusWechsler statuswechsler;
        private readonly IUserDataAccess userDataAccess;

        public SetStatusZumReponierenBereitConsumer(IOrderDataAccess dataAccess, IUserDataAccess userDataAccess, StatusWechsler statuswechsler)
        {
            this.dataAccess = dataAccess;
            this.userDataAccess = userDataAccess;
            this.statuswechsler = statuswechsler;
        }

        public async Task Consume(ConsumeContext<SetStatusZumReponierenBereitRequest> context)
        {
            var orderItems = await GetOrderItems(context.Message.OrderItemIds);
            var user = context.Message.UserId == Users.Vecteur.Id
                ? Users.Vecteur
                : userDataAccess.GetUser(context.Message.UserId);

            await statuswechsler.Execute(oi => oi.SetStatusZumReponierenBereit(), orderItems, user, DateTime.Now);
            await context.RespondAsync(new SetStatusZumReponierenBereitResponse());
        }

        private async Task<OrderItem[]> GetOrderItems(List<int> ids)
        {
            var list = new List<OrderItem>();

            foreach (var id in ids)
            {
                var item = await dataAccess.GetOrderItem(id);
                list.Add(item);
            }

            return list.ToArray();
        }
    }
}