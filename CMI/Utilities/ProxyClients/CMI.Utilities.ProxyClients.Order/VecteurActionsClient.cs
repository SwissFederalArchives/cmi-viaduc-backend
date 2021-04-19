using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Manager.Order.Status;
using MassTransit;

namespace CMI.Utilities.ProxyClients.Order
{
    public class VecteurActionsClient : IVecteurActions
    {
        private readonly IBus bus;

        public VecteurActionsClient(IBus bus)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public async Task SetStatusAushebungBereit(int auftragsId)
        {
            var client = GetRequestClient<SetStatusAushebungBereitRequest, SetStatusAushebungBereitResponse>(BusConstants
                .OrderManagerSetStatusAushebungBereitRequestQueue);
            var request = new SetStatusAushebungBereitRequest
            {
                OrderItemId = auftragsId
            };

            await client.Request(request);
        }

        public async Task SetStatusDigitalisierungExtern(int auftragsId)
        {
            var client = GetRequestClient<SetStatusDigitalisierungExternRequest, SetStatusDigitalisierungExternResponse>(BusConstants
                .OrderManagerSetStatusDigitalisierungExternRequestQueue);
            var request = new SetStatusDigitalisierungExternRequest
            {
                OrderItemId = auftragsId
            };

            await client.Request(request);
        }

        public async Task SetStatusDigitalisierungAbgebrochen(int auftragsId, string grund)
        {
            var client = GetRequestClient<SetStatusDigitalisierungAbgebrochenRequest, SetStatusDigitalisierungAbgebrochenResponse>(BusConstants
                .OrderManagerSetStatusDigitalisierungAbgebrochenRequestQueue);
            var request = new SetStatusDigitalisierungAbgebrochenRequest
            {
                OrderItemId = auftragsId,
                Grund = grund
            };

            await client.Request(request);
        }

        public async Task SetStatusZumReponierenBereit(int auftragId)
        {
            var client = GetRequestClient<SetStatusZumReponierenBereitRequest, SetStatusZumReponierenBereitResponse>(BusConstants
                .OrderManagerSetStatusZumReponierenBereitRequestQueue);
            var request = new SetStatusZumReponierenBereitRequest
            {
                OrderItemIds = new List<int> {auftragId},
                UserId = Users.Vecteur.Id
            };

            await client.Request(request);
        }

        private IRequestClient<T1, T2> GetRequestClient<T1, T2>(string serviceUrl) where T1 : class where T2 : class
        {
            var requestTimeout = TimeSpan.FromSeconds(30);


            return new MessageRequestClient<T1, T2>(bus, new Uri(bus.Address, serviceUrl), requestTimeout);
        }
    }
}