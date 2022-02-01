using System;
using CMI.Contract.Common.Extensions;
using CMI.Contract.Order;
using CMI.Manager.Order.Consumers;

namespace CMI.Manager.Order.Status
{
    internal static class EntscheidEinsichtsgesuchHinterlegenExtensions
    {
        public static void EntscheidGesuchHinterlegenInternal(this AuftragStatus auftragStatus, EntscheidGesuch entscheid, DateTime datumEntscheid,
            string interneBemerkung)
        {
            auftragStatus.Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Einsichtsgesuch
            });

            if (entscheid == EntscheidGesuch.NichtGeprueft)
            {
                throw new Exception("Die Funktion EntscheidGesuchHinterlegen verlangt, dass ein Entscheid getroffen wird.");
            }

            auftragStatus.Context.OrderItem.EntscheidGesuch = entscheid;
            auftragStatus.Context.OrderItem.DatumDesEntscheids = datumEntscheid;
            auftragStatus.Context.OrderItem.SachbearbeiterId = auftragStatus.Context.CurrentUser.Id;
            auftragStatus.Context.OrderItem.InternalComment = auftragStatus.Context.OrderItem.InternalComment.Prepend(interneBemerkung);

            auftragStatus.Context.OrderItem.Abschlussdatum = auftragStatus.Context.TransactionTimeStamp;
            auftragStatus.Context.SetNewStatus(AuftragStatusRepo.Abgeschlossen);

            if (!auftragStatus.Context.OrderItem.VeId.HasValue)
            {
                throw new Exception(
                    "Die Funktion EntscheidGesuchHinterlegen kann nur aufgerufen werden, wenn der Auftrag den Verweis auf eine Verzeichniseinheit enthält.");
            }

            UpdateIndivTokensHelper.RegisterActionForIndivTokensRefresh(auftragStatus);
        }
    }
}