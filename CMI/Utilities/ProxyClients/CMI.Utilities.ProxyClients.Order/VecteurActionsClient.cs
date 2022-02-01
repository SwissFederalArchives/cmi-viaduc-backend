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
            var client = GetRequestClient<SetStatusAushebungBereitRequest>(BusConstants
                .OrderManagerSetStatusAushebungBereitRequestQueue);
            var request = new SetStatusAushebungBereitRequest
            {
                OrderItemId = auftragsId
            };

            await client.GetResponse<SetStatusAushebungBereitResponse>(request);
        }

        public async Task SetStatusDigitalisierungExtern(int auftragsId)
        {
            var client = GetRequestClient<SetStatusDigitalisierungExternRequest>(BusConstants
                .OrderManagerSetStatusDigitalisierungExternRequestQueue);
            var request = new SetStatusDigitalisierungExternRequest
            {
                OrderItemId = auftragsId
            };

            await client.GetResponse<SetStatusDigitalisierungExternResponse>(request);
        }

        public async Task SetStatusDigitalisierungAbgebrochen(int auftragsId, string grund)
        {
            var client = GetRequestClient<SetStatusDigitalisierungAbgebrochenRequest>(BusConstants
                .OrderManagerSetStatusDigitalisierungAbgebrochenRequestQueue);
            var request = new SetStatusDigitalisierungAbgebrochenRequest
            {
                OrderItemId = auftragsId,
                Grund = grund
            };

            await client.GetResponse<SetStatusDigitalisierungAbgebrochenResponse>(request);
        }

        public async Task SetStatusZumReponierenBereit(int auftragId)
        {
            var client = GetRequestClient<SetStatusZumReponierenBereitRequest>(BusConstants
                .OrderManagerSetStatusZumReponierenBereitRequestQueue);
            var request = new SetStatusZumReponierenBereitRequest
            {
                OrderItemIds = new List<int> {auftragId},
                UserId = Users.Vecteur.Id
            };

            await client.GetResponse<SetStatusZumReponierenBereitResponse>(request);
        }

        private IRequestClient<T1> GetRequestClient<T1>(string serviceUrl) where T1 : class
        {
            var requestTimeout = TimeSpan.FromSeconds(30);
            return bus.CreateRequestClient<T1>(new Uri(bus.Address, serviceUrl), requestTimeout);
        }
    }
}