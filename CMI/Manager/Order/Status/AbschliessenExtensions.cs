namespace CMI.Manager.Order.Status
{
    internal static class AbschliessenExtensions
    {
        public static void AbschliessenInternal(this AuftragStatus auftragStatus)
        {
            auftragStatus.Context.SetNewStatus(AuftragStatusRepo.Abgeschlossen);
            auftragStatus.Context.OrderItem.Abschlussdatum = auftragStatus.Context.TransactionTimeStamp;
        }
    }
}