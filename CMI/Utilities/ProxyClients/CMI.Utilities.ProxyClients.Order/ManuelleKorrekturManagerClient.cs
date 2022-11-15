using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using CMI.Contract.Messaging;
using MassTransit;

namespace CMI.Utilities.ProxyClients.Order
{
    public class ManuelleKorrekturManagerClient : IManuelleKorrekturManager
    {
        private readonly IBus bus;

        public ManuelleKorrekturManagerClient(IBus bus)
        {
            this.bus = bus;
        }

        public async Task<ManuelleKorrekturDetailItem> GetManuelleKorrektur(int manuelleKorrekturId)
        {
            var client = GetRequestClient<GetManuelleKorrekturRequest>();
            try
            {
                var result = await client.GetResponse<GetManuelleKorrekturResponse>(new GetManuelleKorrekturRequest
                {
                    ManuelleKorrekturId = manuelleKorrekturId
                });

                return result.Message.ManuelleKorrektur;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<ManuelleKorrekturDto> InsertOrUpdateManuelleKorrektur(ManuelleKorrekturDto value, string userId)
        {
            var client = GetRequestClient<InsertOrUpdateManuelleKorrekturRequest>();
            var result = await client.GetResponse<InsertOrUpdateManuelleKorrekturResponse>(new InsertOrUpdateManuelleKorrekturRequest
            {
                ManuelleKorrektur = value,
                UserId = userId
            });
            return result.Message.ManuelleKorrektur;
        }

        public async Task DeleteManuelleKorrektur(int manuelleKorrekturId)
        {
            var client = GetRequestClient<DeleteManuelleKorrekturRequest>();
            await client.GetResponse<DeleteManuelleKorrekturResponse>(new DeleteManuelleKorrekturRequest
            {
                ManuelleKorrekturId = manuelleKorrekturId
            });
        }

        public async Task BatchDeleteManuelleKorrektur(int[] manuelleKorrekturIds)
        {
            var client = GetRequestClient<BatchDeleteManuelleKorrekturRequest>();
            await client.GetResponse<BatchDeleteManuelleKorrekturResponse>(new BatchDeleteManuelleKorrekturRequest
            {
                ManuelleKorrekturIds = manuelleKorrekturIds
            });
        }

        public async Task<Dictionary<string, string>> BatchAddManuelleKorrektur(string[] identifiers, string userId)
        {
            var client = GetRequestClient<BatchAddManuelleKorrekturRequest>();
            var result = await client.GetResponse<BatchAddManuelleKorrekturResponse>(new BatchAddManuelleKorrekturRequest
            {
                Identifiers = identifiers,
                UserId = userId
            });

            return result.Message.Result;
        }

        public async Task<ManuelleKorrekturDto> PublizierenManuelleKorrektur(int manuelleKorrekturId, string userId)
        {
            var client = GetRequestClient<PublizierenManuelleKorrekturRequest>("", 300);

            var result = await client.GetResponse<PublizierenManuelleKorrekturResponse>(new PublizierenManuelleKorrekturRequest
                                                                    {
                                                                        Id = manuelleKorrekturId,
                                                                        UserId = userId
                                                                    });

            return result.Message.ManuelleKorrektur;
        }

        private IRequestClient<T1> GetRequestClient<T1>(string queueEndpoint = "", int requestTimeOutInSeconds = 0) where T1 : class
        {
            var serviceUrl = string.IsNullOrEmpty(queueEndpoint)
                ? string.Format(BusConstants.ViaducManagerRequestBase, typeof(T1).Name)
                : queueEndpoint;

#if DEBUG
            var requestTimeout = TimeSpan.FromSeconds(120);
#else
                var requestTimeout = TimeSpan.FromSeconds(60);
#endif

            if (requestTimeOutInSeconds > 0)
            {
                requestTimeout = TimeSpan.FromSeconds(requestTimeOutInSeconds);
            }

            return bus.CreateRequestClient<T1>(new Uri(bus.Address, serviceUrl), requestTimeout);
        }
    }
}
