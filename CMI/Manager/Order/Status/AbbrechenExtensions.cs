using System;
using System.Dynamic;
using CMI.Contract.Order;
using CMI.Engine.MailTemplate;
using CMI.Manager.Order.Consumers;
using CMI.Manager.Order.Mails;

namespace CMI.Manager.Order.Status
{
    internal static class AbbrechenExtensions
    {
        public static void AbbrechenInternal(this AuftragStatus auftragStatus, Abbruchgrund abbruchgrund, string bemerkungZumDossier,
            string interneBemerkung)
        {
            if (abbruchgrund == Abbruchgrund.NichtGesetzt)
            {
                throw new Exception("Die Funktion Abbrechen verlangt, dass ein Abbruchgrund gesetzt wird.");
            }

            auftragStatus.Context.OrderItem.Abbruchgrund = abbruchgrund;
            auftragStatus.Context.OrderItem.InternalComment = auftragStatus.Context.OrderItem.InternalComment.Prepend(interneBemerkung);
            auftragStatus.Context.OrderItem.Comment = auftragStatus.Context.OrderItem.Comment.Append(bemerkungZumDossier);
            auftragStatus.Context.OrderItem.Abschlussdatum = auftragStatus.Context.TransactionTimeStamp;
            auftragStatus.Context.OrderItem.EntscheidGesuch = EntscheidGesuch.NichtGeprueft;
            auftragStatus.Context.OrderItem.DatumDesEntscheids = null;
            auftragStatus.Context.SetNewStatus(AuftragStatusRepo.Abgebrochen);

            var expando = CreateEmailData(auftragStatus);

            switch (abbruchgrund)
            {
                case Abbruchgrund.DigitalisierungNichtMoeglich:
                    auftragStatus.Context.MailPortfolio.AddFinishedMail<DigitalisierungNichtMoeglich>(expando);
                    break;

                case Abbruchgrund.DossierMomentanNichtVerfuegbar:
                    auftragStatus.Context.MailPortfolio.AddFinishedMail<DossierMomentanNichtVerfuegbar>(expando);
                    break;
            }

            UpdateIndivTokensHelper.RegisterActionForIndivTokensRefresh(auftragStatus);
        }

        private static ExpandoObject CreateEmailData(AuftragStatus auftragStatus)
        {
            var builder = new DataBuilder(auftragStatus.Context.Bus)
                .AddUser(auftragStatus.Context.CurrentUser.Id)
                .AddBesteller(auftragStatus.Context.Ordering.UserId)
                .AddBestellung(auftragStatus.Context.Ordering)
                .AddAuftrag(auftragStatus.Context.Ordering, auftragStatus.Context.OrderItem)
                .AddValue("Id", auftragStatus.Context.OrderItem.Id);

            var expando = builder.Create();

            expando.Sprachen = new[]
            {
                new Sprache("de"),
                new Sprache("fr"),
                new Sprache("it"),
                new Sprache("en")
            };

            return expando;
        }
    }
}