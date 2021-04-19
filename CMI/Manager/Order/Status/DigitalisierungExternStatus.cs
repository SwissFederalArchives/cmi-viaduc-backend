using System;
using CMI.Contract.Order;

namespace CMI.Manager.Order.Status
{
    public class DigitalisierungExternStatus : AuftragStatus
    {
        private static readonly Lazy<DigitalisierungExternStatus> lazy =
            new Lazy<DigitalisierungExternStatus>(() => new DigitalisierungExternStatus());

        private DigitalisierungExternStatus()
        {
        }

        public static DigitalisierungExternStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.DigitalisierungExtern;

        public override void SetStatusDigitalisierungAbgebrochen(string grund)
        {
            Context.ThrowIfAuftragstypIsNot(new[] {OrderType.Digitalisierungsauftrag});
            Context.ThrowIfUserIsNot(Users.Vecteur);

            Context.SetNewStatus(AuftragStatusRepo.DigitalisierungAbgebrochen);
            Context.OrderItem.InternalComment = Context.OrderItem.InternalComment.Prepend("Abbruchgrund von Vecteur gemeldet: " + grund);
        }

        public override void SetStatusZumReponierenBereit()
        {
            Context.ThrowIfAuftragstypIsNot(new[] {OrderType.Digitalisierungsauftrag});
            Context.ThrowIfUserIsNot(Users.Vecteur);
            Context.SetNewStatus(AuftragStatusRepo.ZumReponierenBereit);
        }
    }
}