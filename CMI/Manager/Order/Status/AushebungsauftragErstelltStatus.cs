using System;
using CMI.Contract.Order;

namespace CMI.Manager.Order.Status
{
    public class AushebungsauftragErstelltStatus : AuftragStatus
    {
        private static readonly Lazy<AushebungsauftragErstelltStatus> lazy =
            new Lazy<AushebungsauftragErstelltStatus>(() => new AushebungsauftragErstelltStatus());

        private AushebungsauftragErstelltStatus()
        {
        }

        public static AushebungsauftragErstelltStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.AushebungsauftragErstellt;

        public override void Abbrechen(Abbruchgrund abbruchgrund, string bemerkungZumDossier, string interneBemerkung)
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Verwaltungsausleihe,
                OrderType.Digitalisierungsauftrag,
                OrderType.Lesesaalausleihen
            });
            this.AbbrechenInternal(abbruchgrund, bemerkungZumDossier, interneBemerkung);
        }

        public override void Zuruecksetzen()
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Verwaltungsausleihe,
                OrderType.Lesesaalausleihen
            });
            this.ZuruecksetzenInternal();
        }

        public override void Ausleihen()
        {
            Context.SetNewStatus(AuftragStatusRepo.Ausgeliehen);
            Context.OrderItem.Ausgabedatum = Context.TransactionTimeStamp;
        }
    }
}