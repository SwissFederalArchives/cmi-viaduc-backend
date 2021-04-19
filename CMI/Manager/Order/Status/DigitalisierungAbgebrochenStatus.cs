using System;
using CMI.Contract.Order;

namespace CMI.Manager.Order.Status
{
    public class DigitalisierungAbgebrochenStatus : AuftragStatus
    {
        private static readonly Lazy<DigitalisierungAbgebrochenStatus> lazy =
            new Lazy<DigitalisierungAbgebrochenStatus>(() => new DigitalisierungAbgebrochenStatus());

        private DigitalisierungAbgebrochenStatus()
        {
        }

        public static DigitalisierungAbgebrochenStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.DigitalisierungAbgebrochen;

        public override void Abbrechen(Abbruchgrund abbruchgrund, string bemerkungZumDossier, string interneBemerkung)
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Digitalisierungsauftrag
            });
            this.AbbrechenInternal(abbruchgrund, bemerkungZumDossier, interneBemerkung);
        }

        public override void Zuruecksetzen()
        {
            this.ZuruecksetzenInternal();
        }
    }
}