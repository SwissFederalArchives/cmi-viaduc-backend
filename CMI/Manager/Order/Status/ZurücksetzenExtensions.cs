using CMI.Contract.Order;
using CMI.Manager.Order.Consumers;

namespace CMI.Manager.Order.Status
{
    internal static class ZurücksetzenExtensions
    {
        public static void ZuruecksetzenInternal(this AuftragStatus auftragStatus)
        {
            auftragStatus.Context.SetNewStatus(AuftragStatusRepo.NeuEingegangen);
            auftragStatus.Context.OrderItem.Ausgabedatum = null;
            auftragStatus.Context.OrderItem.Abschlussdatum = null;
            auftragStatus.Context.OrderItem.Abbruchgrund = Abbruchgrund.NichtGesetzt;
            auftragStatus.Context.OrderItem.EntscheidGesuch = EntscheidGesuch.NichtGeprueft;
            auftragStatus.Context.OrderItem.DatumDesEntscheids = null;
            auftragStatus.Context.OrderItem.HasAufbereitungsfehler = false;
            auftragStatus.Context.OrderItem.Ausleihdauer = 90;
            /* Einige Parameter werden im NeuEingegangenStatus gesetzt und hier nicht geleert, weil sie sonst wieder überschrieben werden */

            UpdateIndivTokensHelper.RegisterActionForIndivTokensRefresh(auftragStatus);

            if (auftragStatus.Context.OrderItem.VeId.HasValue)
            {
                UpdateIndivTokensHelper.RegisterActionForIndivTokensRefresh(auftragStatus);
            }
        }
    }
}