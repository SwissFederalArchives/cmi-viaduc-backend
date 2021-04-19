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
            var requestClient =
                CreateRequestClient<FindOrderItemsRequest, FindOrderItemsResponse>(bus, BusConstants.OrderManagerFindOrderItemsRequestQueue);
            var result = await requestClient.Request(new FindOrderItemsRequest {OrderItemIds = orderItemIds});
            return result.OrderItems;
        }

        public async Task<ElasticArchiveRecord> GetElasticArchiveRecord(string archiveRecordId)
        {
            var requestClient =
                CreateRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>(bus, BusConstants.IndexManagerFindArchiveRecordMessageQueue);
            var result = await requestClient.Request(new FindArchiveRecordRequest {ArchiveRecordId = archiveRecordId});
            return result.ElasticArchiveRecord;
        }

        private static IRequestClient<T1, T2> CreateRequestClient<T1, T2>(IBus busControl, string relativeUri) where T1 : class where T2 : class
        {
            var client = busControl.CreateRequestClient<T1, T2>(new Uri(busControl.Address, relativeUri), TimeSpan.FromSeconds(10));
            return client;
        }
    }
}