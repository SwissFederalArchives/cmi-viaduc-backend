using System;
using CMI.Contract.Order;

namespace CMI.Manager.Order.Status
{
    public class ImBestellkorbStatus : AuftragStatus
    {
        private static readonly Lazy<ImBestellkorbStatus> lazy =
            new Lazy<ImBestellkorbStatus>(() => new ImBestellkorbStatus());

        private ImBestellkorbStatus()
        {
        }

        public static ImBestellkorbStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.ImBestellkorb;

        public override void Bestellen()
        {
            Context.SetNewStatus(AuftragStatusRepo.NeuEingegangen);
        }
    }
}