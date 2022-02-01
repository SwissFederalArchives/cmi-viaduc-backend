using System;
using System.Collections.Generic;
using CMI.Contract.Order;
using CMI.Engine.MailTemplate;
using CMI.Manager.Order.Mails;

namespace CMI.Manager.Order.Status
{
    public class EinsichtsgesuchPruefenStatus : AuftragStatus
    {
        private static readonly Lazy<EinsichtsgesuchPruefenStatus> lazy =
            new Lazy<EinsichtsgesuchPruefenStatus>(() => new EinsichtsgesuchPruefenStatus());

        private EinsichtsgesuchPruefenStatus()
        {
        }

        public static EinsichtsgesuchPruefenStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.EinsichtsgesuchPruefen;

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

        public override void InVorlageExportieren(Vorlage vorlage, string sprache)
        {
            base.InVorlageExportieren(vorlage, sprache);

            switch (vorlage)
            {
                // Achtung: Nur diese 3 Vorlagen bewirken eine Statusänderung 
                case Vorlage.Musterbrief_EG_BGA_Art_09_11_12:
                    Context.SetNewStatus(AuftragStatusRepo.EinsichtsgesuchWeitergeleitet);
                    break;
                case Vorlage.Musterbrief_EG_BGA_Art_15:
                    Context.SetNewStatus(AuftragStatusRepo.EinsichtsgesuchWeitergeleitet);
                    break;
                case Vorlage.Musterbrief_EG_J:
                    Context.SetNewStatus(AuftragStatusRepo.EinsichtsgesuchWeitergeleitet);
                    break;
                default:
                    return;
            }
        }

        public override void OnStateEnter()
        {
            AddOrderItemToNeueEinsichtsgesucheEMail();
        }

        public override void EntscheidGesuchHinterlegen(EntscheidGesuch entscheid, DateTime datumEntscheid, string interneBemerkung)
        {
            this.EntscheidGesuchHinterlegenInternal(entscheid, datumEntscheid, interneBemerkung);
        }

        private void AddOrderItemToNeueEinsichtsgesucheEMail()
        {
            dynamic emailExpando = Context.MailPortfolio.GetUnfinishedMailData<NeuesEinsichtsgesuch>("NeueEinsichtsgesuche");

            if (emailExpando == null)
            {
                // Das EMail mit seine Grunddaten erstellen:
                emailExpando = new DataBuilder(Context.Bus)
                    .AddUser(Context.Ordering.UserId)
                    .AddBestellung(Context.Ordering)
                    .AddVeList(new List<string>())
                    .AddValue("ArtDerArbeit", null)
                    .AddValue("Anzahl", 0)
                    .AddValue("HasOrganization", !string.IsNullOrEmpty(Context.CurrentUser.Organization))
                    .Create();

                Context.MailPortfolio.BeginUnfinishedMail<NeuesEinsichtsgesuch>("NeueEinsichtsgesuche", emailExpando);
            }

            int count = emailExpando.Anzahl;
            emailExpando.Anzahl = count + 1;
            if (Context.Ordering.ArtDerArbeit != null)
            {
                List<int> list = new List<int> { (int)Context.Ordering.ArtDerArbeit };
                emailExpando.ArtDerArbeit = new Stammdaten(list, "ArtDerArbeit");
            }

            // die Ve zum EMail hinzufügen:
            if (Context.OrderItem.VeId == null)
            {
                // Was, wenn es sich um eine Formularbestellung handelt?
            }
            else
            {
                var veRecord = Context.IndexAccess.FindDocument(Context.OrderItem.VeId.Value.ToString(), false);
                ((List<InElasticIndexierteVe>) emailExpando.VeList).Add(InElasticIndexierteVe.FromElasticArchiveRecord(veRecord));
            }
        }
    }
}