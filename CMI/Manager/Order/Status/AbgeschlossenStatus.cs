using System;
using CMI.Contract.Order;

namespace CMI.Manager.Order.Status
{
    public class AbgeschlossenStatus : AuftragStatus
    {
        private static readonly Lazy<AbgeschlossenStatus> lazy =
            new Lazy<AbgeschlossenStatus>(() => new AbgeschlossenStatus());

        private AbgeschlossenStatus()
        {
        }

        public static AbgeschlossenStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.Abgeschlossen;

        public override void Abbrechen(Abbruchgrund abbruchgrund, string bemerkungZumDossier, string interneBemerkung)
        {
            this.AbbrechenInternal(abbruchgrund, bemerkungZumDossier, interneBemerkung);
        }

        public override void Zuruecksetzen()
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Digitalisierungsauftrag,
                OrderType.Lesesaalausleihen,
                OrderType.Verwaltungsausleihe
            });

            this.ZuruecksetzenInternal();
        }

        public override void EntscheidGesuchHinterlegen(EntscheidGesuch entscheid, DateTime datumEntscheid, string interneBemerkung)
        {
            this.EntscheidGesuchHinterlegenInternal(entscheid, datumEntscheid, interneBemerkung);
        }
    }
}