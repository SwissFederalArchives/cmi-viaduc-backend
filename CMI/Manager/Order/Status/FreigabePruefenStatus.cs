using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Common;
using CMI.Contract.Order;
using CMI.Engine.MailTemplate;
using CMI.Manager.Order.Consumers;
using CMI.Manager.Order.Mails;
using CMI.Utilities.Logging.Configurator;

namespace CMI.Manager.Order.Status
{
    public class FreigabePruefenStatus : AuftragStatus
    {
        private static readonly Lazy<FreigabePruefenStatus> lazy =
            new Lazy<FreigabePruefenStatus>(() => new FreigabePruefenStatus());

        private FreigabePruefenStatus()
        {
        }

        public static FreigabePruefenStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.FreigabePruefen;

        public override void Abbrechen(Abbruchgrund abbruchgrund, string bemerkungZumDossier, string interneBemerkung)
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Verwaltungsausleihe,
                OrderType.Lesesaalausleihen,
                OrderType.Digitalisierungsauftrag
            });
            this.AbbrechenInternal(abbruchgrund, bemerkungZumDossier, interneBemerkung);
        }

        public override void Zuruecksetzen()
        {
            this.ZuruecksetzenInternal();
        }

        public override void EntscheidFreigabeHinterlegen(ApproveStatus entscheid, DateTime? datumBewilligung, string interneBemerkung)
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Verwaltungsausleihe,
                OrderType.Lesesaalausleihen,
                OrderType.Digitalisierungsauftrag
            });

            if (entscheid == ApproveStatus.NichtGeprueft)
            {
                throw new BadRequestException("Die Funktion EntscheidFreigabeHinterlegen verlangt, dass ein Entscheid getroffen wird.");
            }

            if (entscheid == ApproveStatus.FreigegebenDurchSystem)
            {
                throw new BadRequestException(
                    $"Die Funktion EntscheidFreigabeHinterlegen kann nicht mit {nameof(ApproveStatus)} = {ApproveStatus.FreigegebenDurchSystem} aufgerufen werden.");
            }

            if (entscheid.VerlangtDatumsangabe() && !datumBewilligung.HasValue)
            {
                throw new BadRequestException(
                    $"Der Entscheid {entscheid.ToString()} verlangt die Angabe eines Datums. Es wurde aber kein Datum angegeben.");
            }

            if (!entscheid.VerlangtDatumsangabe() && datumBewilligung.HasValue)
            {
                throw new BadRequestException(
                    $"Der Entscheid {entscheid.ToString()} erlaubt nicht die Angabe eines Datums. Es wurde aber ein Datum angegeben.");
            }


            Context.SetApproveStatus(entscheid, Context.CurrentUser);
            Context.OrderItem.BewilligungsDatum = datumBewilligung;
            Context.OrderItem.InternalComment = interneBemerkung; // überschreiben, nicht anhängen 

            if (entscheid.IstFreigegeben())
            {
                if (VeLiegtDigitalVor(Context.OrderItem.VeId))
                {
                    this.AbschliessenInternal();
                }
                else
                {
                    switch (Context.Ordering.Type)
                    {
                        case OrderType.Digitalisierungsauftrag:
                            Context.SetNewStatus(AuftragStatusRepo.FuerDigitalisierungBereit);
                            break;

                        case OrderType.Verwaltungsausleihe:
                        case OrderType.Lesesaalausleihen:
                            Context.SetNewStatus(AuftragStatusRepo.FuerAushebungBereit);
                            break;
                    }
                }
            }
            else
            {
                Context.OrderItem.Abbruchgrund = Enum.TryParse(entscheid.ToString(), true, out Abbruchgrund abbruchgrund)
                    ? abbruchgrund
                    : Abbruchgrund.NichtGesetzt;
                Context.OrderItem.Abschlussdatum = Context.TransactionTimeStamp;
                Context.SetNewStatus(AuftragStatusRepo.Abgebrochen);
            }

            // Falls es der letzte Auftrag einer Bestellung war 
            if (Context.Ordering.Items.All(item => item.Status != OrderStatesInternal.FreigabePruefen))
            {
                PrepareMailFreigabeKomplett();
                PrepareMailMitteilungBestellungMitTeilbewilligung();
            }

            if (Context.OrderItem.VeId.HasValue)
            {
                UpdateIndivTokensHelper.RegisterActionForIndivTokensRefresh(this);
            }
        }


        private void PrepareMailMitteilungBestellungMitTeilbewilligung()
        {
            if (!Context.Ordering.Items.Any(i => i.ApproveStatus.VerlangtMitteilungBestellungMitTeilbewilligung()))
            {
                return; // Kein Mail versenden wenn keine Teilbewilligungen
            }

            var builder = new DataBuilder(Context.Bus)
                .AddUser(Context.CurrentUser.Id)
                .AddBesteller(Context.Ordering.UserId)
                .AddBestellung(Context.Ordering);

            // Für jeden möglichen ApproveStatus (XXX) folgendes generieren:
            // - eine eigene Liste (XXX), welche nur die Aufträge mit diesem Status enthält
            // - ein "Flag" (XXXVorhanden), welches anzeigt, ob die Liste Einträge hat.
            foreach (var enumValue in Enum.GetValues(typeof(ApproveStatus)))
            {
                var status = (ApproveStatus) enumValue;
                if (!status.VerlangtMitteilungBestellungMitTeilbewilligung())
                {
                    continue;
                }

                var orderItems = Context.Ordering.Items.Where(x => x.ApproveStatus == status).ToList();
                AddAuftraegeAndFlag(builder, orderItems, status.ToString());
            }

            Context.MailPortfolio.AddFinishedMail<MitteilungBestellungMitTeilbewilligung>(builder.Create());
        }

        private void PrepareMailFreigabeKomplett()
        {
            if (Context.Ordering.Items.All(item => FreigabeKomplettGehtAn(item) == MailTo.KeinMail))
            {
                return;
            }

            var builder = new DataBuilder(Context.Bus)
                .AddUser(Context.CurrentUser.Id)
                .AddBesteller(Context.Ordering.UserId)
                .AddBestellung(Context.Ordering);

            // Für jeden möglichen ApproveStatus (XXX) folgendes generieren:
            // - eine eigene Liste (XXX), welche nur die Aufträge mit diesem Status enthält
            // - ein "Flag" (XXXVorhanden), welches anzeigt, ob die Liste Einträge hat.
            foreach (var enumValue in Enum.GetValues(typeof(ApproveStatus)))
            {
                var status = (ApproveStatus) enumValue;
                var orderItems = Context.Ordering.Items.Where(x => x.ApproveStatus == status).ToList();
                AddAuftraegeAndFlag(builder, orderItems, status.ToString());
            }

            var orderItemsFreigegebenDigitaleVe = Context.Ordering.Items.Where(
                // ApproveStatus.FreigegebenDurchSystem bewusst weggelassen
                x => (x.ApproveStatus == ApproveStatus.FreigegebenAusserhalbSchutzfrist ||
                      x.ApproveStatus == ApproveStatus.FreigegebenInSchutzfrist) &&
                     VeLiegtDigitalVor(x.VeId)).ToList();

            var orderItemsFreigegebenInSchutzfristUndOe2 = orderItemsFreigegebenDigitaleVe.Where(
                x => x.ApproveStatus == ApproveStatus.FreigegebenInSchutzfrist &&
                     Context.Besteller.Access.RolePublicClient == AccessRoles.RoleOe2).ToList();
            AddAuftraegeAndFlag(builder, orderItemsFreigegebenInSchutzfristUndOe2, "FreigegebenDigitaleVe_FreigegebenInSchutzfristUndÖ2");

            var orderItemsFreigegebenAusserhalbSchutzfristOderNichtOe2 = orderItemsFreigegebenDigitaleVe.Where(
                x => x.ApproveStatus == ApproveStatus.FreigegebenAusserhalbSchutzfrist ||
                     Context.Besteller.Access.RolePublicClient != AccessRoles.RoleOe2).ToList();
            AddAuftraegeAndFlag(builder, orderItemsFreigegebenAusserhalbSchutzfristOderNichtOe2,
                "FreigegebenDigitaleVe_FreigegebenAusserhalbSchutzfristOderNichtÖ2");

            var expando = builder.Create();

            var empfaenger = Context.Ordering.Items.Max(item => FreigabeKomplettGehtAn(item));
            switch (empfaenger)
            {
                case MailTo.KeinMail:
                    return;
                case MailTo.Kunde:
                    expando.Weiterleitung = false;
                    expando.To = ((Person) expando.Besteller).EmailAddress;
                    expando.Sprachen = new[] {new Sprache(((Person) expando.Besteller).Sprache)};
                    break;
                case MailTo.FreigabeManager:
                    expando.Weiterleitung = true;
                    expando.To = ((Person) expando.User).EmailAddress;
                    expando.Sprachen = new[]
                    {
                        new Sprache("de"),
                        new Sprache("fr"),
                        new Sprache("it"),
                        new Sprache("en")
                    };

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var absender = Context.Ordering.Items.Max(item => FreigabeKomplettAbsender(item));
            switch (absender)
            {
                case MailFrom.KeinMail:
                    return;
                case MailFrom.Einsichtsgesuch:
                    expando.From = "einsichtsgesuch@bar.admin.ch";
                    break;
                case MailFrom.Bestellung:
                    expando.From = "bestellung@bar.admin.ch";
                    break;
                case MailFrom.DoNotReply:
                    expando.From = "do_not_reply@bar.admin.ch";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Context.MailPortfolio.AddFinishedMail<FreigabeKomplett>(builder.Create());
        }

        /// <summary>
        ///     Fügt beim Builder folgendes hinzu:
        ///     - eine eigene Liste (Bezeichnung vom Parameter propertyName), welche die Aufträge enthält
        ///     - ein "Flag" (Bezeichnung vom Parameter propertyName ergänzt um "Vorhanden"), welches anzeigt, ob die Liste
        ///     Einträge hat.
        /// </summary>
        private void AddAuftraegeAndFlag(IDataBuilder builder, List<OrderItem> orderItems, string propertyName)
        {
            builder.AddValue(propertyName + "Vorhanden", orderItems.Count > 0);
            builder.AddAuftraege(Context.Ordering, orderItems, propertyName);
        }

        private bool VeLiegtDigitalVor(int? veId)
        {
            // Für die Ermittlung von VeLiegtDigitalVor kann nicht OrderItem.IdentifikationDigitalesMagazin verwendet werden.
            // Darum werden Daten vom Elastic geholt.

            ElasticArchiveRecord archiveRecord = null;

            if (veId.HasValue)
            {
                archiveRecord = Context.IndexAccess.FindDocument(veId.Value.ToString(), false);
            }

            return !string.IsNullOrEmpty(archiveRecord?.PrimaryDataLink);
        }

        private MailTo FreigabeKomplettGehtAn(OrderItem orderItem)
        {
            switch (orderItem.ApproveStatus)
            {
                case ApproveStatus.NichtGeprueft:
                    return MailTo.KeinMail;
                case ApproveStatus.FreigegebenDurchSystem:
                    return MailTo.KeinMail;

                case ApproveStatus.FreigegebenAusserhalbSchutzfrist:
                case ApproveStatus.FreigegebenInSchutzfrist:
                    if (VeLiegtDigitalVor(orderItem.VeId))
                    {
                        return MailTo.Kunde;
                    }
                    else
                    {
                        return MailTo.KeinMail;
                    }

                case ApproveStatus.ZurueckgewiesenEinsichtsbewilligungNoetig:
                    return MailTo.Kunde;
                case ApproveStatus.ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenInSchutzfrist:
                    return MailTo.Kunde;
                case ApproveStatus.ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenFreiBewilligung:
                    return MailTo.Kunde;
                case ApproveStatus.ZurueckgewiesenFormularbestellungNichtErlaubt:
                    return MailTo.FreigabeManager;
                case ApproveStatus.ZurueckgewiesenDossierangabenUnzureichend:
                    return MailTo.FreigabeManager;
                case ApproveStatus.ZurueckgewiesenTeilbewilligungVorhanden:
                    return MailTo.Kunde;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderItem.ApproveStatus), orderItem.ApproveStatus, null);
            }
        }

        private MailFrom FreigabeKomplettAbsender(OrderItem orderItem)
        {
            switch (orderItem.ApproveStatus)
            {
                case ApproveStatus.NichtGeprueft:
                    return MailFrom.KeinMail;
                case ApproveStatus.FreigegebenDurchSystem:
                    return MailFrom.KeinMail;

                case ApproveStatus.FreigegebenAusserhalbSchutzfrist:
                case ApproveStatus.FreigegebenInSchutzfrist:
                    if (VeLiegtDigitalVor(orderItem.VeId))
                    {
                        return MailFrom.Bestellung;
                    }
                    else
                    {
                        return MailFrom.KeinMail;
                    }

                case ApproveStatus.ZurueckgewiesenEinsichtsbewilligungNoetig:
                    return MailFrom.Einsichtsgesuch;
                case ApproveStatus.ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenInSchutzfrist:
                    return MailFrom.Bestellung;
                case ApproveStatus.ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenFreiBewilligung:
                    return MailFrom.Bestellung;
                case ApproveStatus.ZurueckgewiesenFormularbestellungNichtErlaubt:
                    return MailFrom.DoNotReply;
                case ApproveStatus.ZurueckgewiesenDossierangabenUnzureichend:
                    return MailFrom.DoNotReply;
                case ApproveStatus.ZurueckgewiesenTeilbewilligungVorhanden:
                    return MailFrom.Einsichtsgesuch;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderItem.ApproveStatus), orderItem.ApproveStatus, null);
            }
        }


        private enum MailTo
        {
            KeinMail,
            Kunde,
            FreigabeManager
        }

        private enum MailFrom
        {
            KeinMail,
            Einsichtsgesuch,
            Bestellung,
            DoNotReply
        }
    }
}