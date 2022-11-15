using System;
using System.Dynamic;
using CMI.Contract.Common.Extensions;
using CMI.Contract.Order;
using CMI.Engine.MailTemplate;
using CMI.Manager.Order.Mails;

namespace CMI.Manager.Order.Status
{
    public class AusgeliehenStatus : AuftragStatus
    {
        private static readonly Lazy<AusgeliehenStatus> lazy =
            new Lazy<AusgeliehenStatus>(() => new AusgeliehenStatus());

        private AusgeliehenStatus()
        {
        }

        public static AusgeliehenStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.Ausgeliehen;

        public override void SetStatusDigitalisierungExtern()
        {
            Context.ThrowIfAuftragstypIsNot(new[] {OrderType.Digitalisierungsauftrag});
            Context.ThrowIfUserIsNot(Users.Vecteur);
            Context.MailPortfolio.AddFinishedMail<Verzoegerungsmeldung>(CreateEmailData());
            Context.SetNewStatus(AuftragStatusRepo.DigitalisierungExtern);
        }

        public override void SetStatusDigitalisierungAbgebrochen(string grund)
        {
            Context.ThrowIfAuftragstypIsNot(new[] {OrderType.Digitalisierungsauftrag});
            Context.ThrowIfUserIsNot(Users.Vecteur);
            Context.SetNewStatus(AuftragStatusRepo.DigitalisierungAbgebrochen);
            Context.OrderItem.InternalComment = Context.OrderItem.InternalComment.Prepend("Abbruchgrund von Vecteur gemeldet: " + grund);
        }

        public override void SetStatusZumReponierenBereit()
        {
            if (Context.CurrentUser == Users.Vecteur)
            {
                Context.ThrowIfAuftragstypIsNot(new[] {OrderType.Digitalisierungsauftrag});
            }
            else
            {
                Context.ThrowIfAuftragstypIsNot(new[] {OrderType.Verwaltungsausleihe, OrderType.Lesesaalausleihen});
            }

            Context.SetNewStatus(AuftragStatusRepo.ZumReponierenBereit);
        }

        public override void Abbrechen(Abbruchgrund abbruchgrund, string bemerkungZumDossier, string interneBemerkung)
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Verwaltungsausleihe,
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

        public override void Abschliessen()
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Lesesaalausleihen
            });

            this.AbschliessenInternal();
        }


        private ExpandoObject CreateEmailData()
        {
            var builder = new DataBuilder(Context.Bus)
                .SetDataProtectionLevel(DataBuilderProtectionStatus.AllUnanonymized)
                .AddBestellung(Context.Ordering)
                .AddBesteller(Context.Besteller.Id)
                .AddAuftraege(new[] {Context.OrderItem.Id});

            var expando = builder.Create();

            return expando;
        }
    }
}