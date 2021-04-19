using System;
using CMI.Contract.Order;

namespace CMI.Manager.Order.Status
{
    public class EinsichtsgesuchWeitergeleitetStatus : AuftragStatus
    {
        private static readonly Lazy<EinsichtsgesuchWeitergeleitetStatus> lazy =
            new Lazy<EinsichtsgesuchWeitergeleitetStatus>(() => new EinsichtsgesuchWeitergeleitetStatus());

        private EinsichtsgesuchWeitergeleitetStatus()
        {
        }

        public static EinsichtsgesuchWeitergeleitetStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.EinsichtsgesuchWeitergeleitet;

        public override void EntscheidGesuchHinterlegen(EntscheidGesuch entscheid, DateTime datumEntscheid, string interneBemerkung)
        {
            this.EntscheidGesuchHinterlegenInternal(entscheid, datumEntscheid, interneBemerkung);
        }

        public override void Abbrechen(Abbruchgrund abbruchgrund, string bemerkungZumDossier, string interneBemerkung)
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Einsichtsgesuch
            });

            this.AbbrechenInternal(abbruchgrund, bemerkungZumDossier, interneBemerkung);
        }

        public override void Zuruecksetzen()
        {
            this.ZuruecksetzenInternal();
        }
    }
}