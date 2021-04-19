using System;
using CMI.Contract.Order;

namespace CMI.Manager.Order.Status
{
    public class AbgebrochenStatus : AuftragStatus
    {
        private static readonly Lazy<AbgebrochenStatus> lazy =
            new Lazy<AbgebrochenStatus>(() => new AbgebrochenStatus());

        private AbgebrochenStatus()
        {
        }

        public static AbgebrochenStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.Abgebrochen;

        public override void Zuruecksetzen()
        {
            this.ZuruecksetzenInternal();
        }

        public override void EntscheidGesuchHinterlegen(EntscheidGesuch entscheid, DateTime datumEntscheid, string interneBemerkung)
        {
            this.EntscheidGesuchHinterlegenInternal(entscheid, datumEntscheid, interneBemerkung);
        }
    }
}