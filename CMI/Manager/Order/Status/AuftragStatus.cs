using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using CMI.Contract.Order;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Engine.MailTemplate;
using CMI.Manager.Order.Mails;
using CMI.Utilities.Logging.Configurator;

namespace CMI.Manager.Order.Status
{
    public abstract class AuftragStatus : IAuftragsAktionen
    {
        public StatuswechselContext Context => StatuswechselContext.Current;

        public abstract OrderStatesInternal OrderStateInternal { get; }


        public virtual void InVorlageExportieren(Vorlage vorlage, string sprache)
        {
            switch (vorlage)
            {
                case Vorlage.Musterbrief_EG_BGA_Art_09_11_12:
                    InVorlageExportieren<Musterbrief_EG_BGA_Art_09_11_12>(sprache);
                    break;
                case Vorlage.Musterbrief_EG_BGA_Art_15:
                    InVorlageExportieren<Musterbrief_EG_BGA_Art_15>(sprache);
                    break;
                case Vorlage.Musterbrief_EG_J:
                    InVorlageExportieren<Musterbrief_EG_J>(sprache);
                    break;
                case Vorlage.Weiterleitung_Entscheid_gemäss_Art_13_BGA:
                    InVorlageExportieren<Weiterleitung_Entscheid_gemaess_Art_13_BGA>(sprache);
                    break;
                case Vorlage.Weiterleitung_Entscheid_gemäss_Art_15_BGA:
                    InVorlageExportieren<Weiterleitung_Entscheid_gemaess_Art_15_BGA>(sprache);
                    break;
                default:
                    throw new ArgumentException("Invalid template name");
            }
        }

        public virtual void Abbrechen(Abbruchgrund abbruchgrund, string bemerkungZumDossier, string interneBemerkung)
        {
            ThrowReadableInvalidOperation();
        }

        public virtual void Zuruecksetzen()
        {
            ThrowReadableInvalidOperation();
        }

        public virtual void AuftragAusleihen()
        {
            ThrowReadableInvalidOperation();
        }

        public virtual void EntscheidFreigabeHinterlegen(ApproveStatus entscheid, DateTime? datumBewilligung,
            string interneBemerkung)
        {
            ThrowReadableInvalidOperation();
        }

        public virtual void EntscheidGesuchHinterlegen(EntscheidGesuch entscheid, DateTime datumEntscheid, string interneBemerkung)
        {
            ThrowReadableInvalidOperation();
        }

        public virtual void AushebungsauftragDrucken()
        {
            // Per Default nichts tun, auch keine Exception werfen.
            // Wird in Subklasse überschrieben für Statuswechsel.
        }

        public virtual void Ausleihen()
        {
            ThrowReadableInvalidOperation();
        }

        public virtual void Abschliessen()
        {
            ThrowReadableInvalidOperation();
        }

        public virtual void Bestellen()
        {
            ThrowReadableInvalidOperation();
        }

        public virtual void SetStatusAushebungBereit()
        {
            ThrowReadableInvalidOperation();
        }

        public virtual void SetStatusDigitalisierungExtern()
        {
            ThrowReadableInvalidOperation();
        }

        public virtual void SetStatusDigitalisierungAbgebrochen(string grund)
        {
            ThrowReadableInvalidOperation();
        }

        public virtual void SetStatusZumReponierenBereit()
        {
            ThrowReadableInvalidOperation();
        }


        protected void ThrowReadableInvalidOperation(
            [CallerMemberName] string memberName = "???",
            [CallerFilePath] string sourceFilePath = "???",
            [CallerLineNumber] int sourceLineNumber = -1)
        {
            throw new BadRequestException(
                $"Der Auftrag {Context.OrderItem.Id} ist im Status {GetFriendlyName()}. Die Funktion {memberName} kann daher nicht ausgeführt werden.");
        }

        public string GetFriendlyName()
        {
            return GetType()
                .Name
                .Replace("Status", "")
                .Replace("ae", "ä")
                .Replace("oe", "ö")
                .Replace("ue", "ü");
        }

        public virtual void EinsichtsgesuchAbschliessen()
        {
            ThrowReadableInvalidOperation();
        }

        private void InVorlageExportieren<T>(string sprache) where T : EmailTemplate, new()
        {
            dynamic emailExpando = Context.MailPortfolio.GetUnfinishedMailData<T>("InVorlageExportieren");
            var dataBuilder = new DataBuilder(Context.Bus, emailExpando ?? new ExpandoObject());

            if (emailExpando == null)
            {
                // Das EMail mit seine Grunddaten erstellen:
                emailExpando = dataBuilder
                    .AddSprache(sprache)
                    .AddUser(Context.CurrentUser.Id)
                    .AddBesteller(Context.Besteller.Id)
                    .AddBestellung(Context.Ordering)
                    .AddValue("HeutigesDatumPlus30Tage", DateTime.Now.AddDays(30).ToString("dd.MM.yyyy"))
                    .Create();
                Context.MailPortfolio.BeginUnfinishedMail<T>("InVorlageExportieren", emailExpando);
            }


            dataBuilder.AddAuftraege(new[] {Context.OrderItem.Id});

            var auftragsliste = (List<Auftrag>) emailExpando.Aufträge;

            emailExpando.Erfassungsdatum = string.Join(" / ", auftragsliste.Select(a => a.Bestellung.Erfassungsdatum).Distinct().OrderBy(e => e));
            emailExpando.BegründungEinsichtsgesuch = string.Join(" / ", auftragsliste.Select(a => a.Bestellung.BegründungEinsichtsgesuch).Distinct());
            emailExpando.ArtDerArbeit =
                new Stammdaten(auftragsliste.Select(a => a.Bestellung.ArtDerArbeitId).Where(e => e != null).Select(e => e.Value).Distinct(),
                    "ArtDerArbeit");
            emailExpando.SignaturUndTitelBestand = auftragsliste.First().BestellteVe.Bestand + ": " +
                                                   auftragsliste.First().BestellteVe.TitelBestand;
            emailExpando.TeilBestand = string.Join(" / ", auftragsliste.Select(a => a.BestellteVe.TeilBestand).Distinct().OrderBy(e => e));
            emailExpando.TitelUndSignaturTeilBestand = string.Join(" / ",
                auftragsliste.Select(a => $"{a.BestellteVe.TeilBestand} – {a.BestellteVe.TitelTeilBestand}").Distinct().OrderBy(e => e));
            emailExpando.FrühestesErfassungsdatumPlus30Tage = auftragsliste.Select(a => a.Bestellung.OrderDate).Where(a => a != null).Min()
                ?.AddDays(30).ToString("dd.MM.yyyy");
            emailExpando.BeginntEineSignaturMitJ1 = auftragsliste.Any(a => a.BestellteVe.Signatur.StartsWith("J1"));
            emailExpando.BeginntEineSignaturMitJ2 = auftragsliste.Any(a => a.BestellteVe.Signatur.StartsWith("J2"));
        }

        public virtual void OnStateEnter()
        {
        }

        public virtual void OnStateLeave()
        {
        }
    }
}