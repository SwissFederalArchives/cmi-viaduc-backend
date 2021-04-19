using System;
using CMI.Contract.Order;

namespace CMI.Manager.Order.Status
{
    public class FuerAushebungBereitStatus : AuftragStatus
    {
        private static readonly Lazy<FuerAushebungBereitStatus> lazy =
            new Lazy<FuerAushebungBereitStatus>(() => new FuerAushebungBereitStatus());

        private FuerAushebungBereitStatus()
        {
        }

        public static FuerAushebungBereitStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.FuerAushebungBereit;

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

        public override void AushebungsauftragDrucken()
        {
            Context.SetNewStatus(AuftragStatusRepo.AushebungsauftragErstellt);
        }
    }
}