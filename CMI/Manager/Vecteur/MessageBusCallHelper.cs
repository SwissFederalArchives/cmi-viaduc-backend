using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;

namespace CMI.Manager.Vecteur
{
    public interface IMessageBusCallHelper
    {
        Task<OrderItem[]> FindOrderItems(int[] orderItemIds);
        Task<ElasticArchiveRecord> GetElasticArchiveRecord(string archiveRecordId);
    }

    public class MessageBusCallHelper : IMessageBusCallHelper
    {
        private readonly IBus bus;

        public MessageBusCallHelper(IBus bus)
        {
            this.bus = bus;
        }

        public async Task<OrderItem[]> FindOrderItems(int[] orderItemIds)
        {
            var requestClient = CreateRequestClient<FindOrderItemsRequest>(bus, BusConstants.OrderManagerFindOrderItemsRequestQueue);
            var result = await requestClient.GetResponse<FindOrderItemsResponse>(new FindOrderItemsRequest {OrderItemIds = orderItemIds});
            return result.Message.OrderItems;
        }

        public async Task<ElasticArchiveRecord> GetElasticArchiveRecord(string archiveRecordId)
        {
            var requestClient = CreateRequestClient<FindArchiveRecordRequest>(bus, BusConstants.IndexManagerFindArchiveRecordMessageQueue);
            var result = await requestClient.GetResponse<FindArchiveRecordResponse>(new FindArchiveRecordRequest {ArchiveRecordId = archiveRecordId});
            return result.Message.ElasticArchiveRecord;
        }

        private static IRequestClient<T1> CreateRequestClient<T1>(IBus busControl, string relativeUri) where T1 : class
        {
            var client = busControl.CreateRequestClient<T1>(new Uri(busControl.Address, relativeUri), TimeSpan.FromSeconds(10));
            return client;
        }
    }
}