using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CMI.Contract.Common.Extensions;
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

            dynamic expando = CreateEmailData(auftragStatus);
            dynamic emailData = null;

            switch (abbruchgrund)
            {
                case Abbruchgrund.DigitalisierungNichtMoeglich:
                    emailData = auftragStatus.Context.MailPortfolio.GetUnfinishedMailData<DigitalisierungNichtMoeglich>(auftragStatus.Context.Besteller.Id);
                    if (emailData == null)
                    {
                        auftragStatus.Context.MailPortfolio.BeginUnfinishedMail<DigitalisierungNichtMoeglich>(auftragStatus.Context.Besteller.Id, expando);
                    }
                    break;

                case Abbruchgrund.DossierMomentanNichtVerfuegbar:
                    emailData = auftragStatus.Context.MailPortfolio.GetUnfinishedMailData<DossierMomentanNichtVerfuegbar>(auftragStatus.Context.Besteller.Id);
                    if (emailData == null)
                    {
                        auftragStatus.Context.MailPortfolio.BeginUnfinishedMail<DossierMomentanNichtVerfuegbar>(auftragStatus.Context.Besteller.Id, expando);
                    }
                    break;
            }

            if (emailData != null)
            {
                var builder = new DataBuilder(auftragStatus.Context.Bus, emailData);
                builder.SetDataProtectionLevel(DataBuilderProtectionStatus.AllUnanonymized);
                builder.AddAuftraege(new List<int> { auftragStatus.Context.OrderItem.Id });

                // After a new Auftrag has been set, we get the collection of the orderings and
                // get their dates. Finally we set a property that is called Auftragsdaten.
                List<Auftrag> aufträge = emailData.Aufträge;
                var bestellungen = aufträge.Select(a => a.Bestellung);
                var daten = bestellungen.Select(o => o.Erfassungsdatum).Distinct();
                
                builder.AddValue("Auftragsdaten", string.Join(" / ", daten));
            }


            UpdateIndivTokensHelper.RegisterActionForIndivTokensRefresh(auftragStatus);
        }

        private static ExpandoObject CreateEmailData(AuftragStatus auftragStatus)
        {
            var builder = new DataBuilder(auftragStatus.Context.Bus)
                .SetDataProtectionLevel(DataBuilderProtectionStatus.AllUnanonymized)
                .AddUser(auftragStatus.Context.CurrentUser.Id)
                .AddBesteller(auftragStatus.Context.Ordering.UserId)
                .AddAuftraege(new List<int>{ auftragStatus.Context.OrderItem.Id })
                .AddValue("Auftragsdaten", auftragStatus.Context.Ordering.OrderDate?.ToString("dd.MM.yyyy") ?? "");

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