using System;
using System.Diagnostics;
using CMI.Contract.Order;
using CMI.Contract.Parameter;

namespace CMI.Manager.Order.Status
{
    public class FuerDigitalisierungBereitStatus : AuftragStatus
    {
        private static readonly Lazy<FuerDigitalisierungBereitStatus> lazy =
            new Lazy<FuerDigitalisierungBereitStatus>(() => new FuerDigitalisierungBereitStatus());

        private FuerDigitalisierungBereitStatus()
        {
        }

        public static FuerDigitalisierungBereitStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.FuerDigitalisierungBereit;


        public override void SetStatusAushebungBereit()
        {
            Context.ThrowIfAuftragstypIsNot(new[] {OrderType.Digitalisierungsauftrag});
            Context.ThrowIfUserIsNot(Users.Vecteur);

            Context.SetNewStatus(AuftragStatusRepo.FuerAushebungBereit);
        }

        public override void Abbrechen(Abbruchgrund abbruchgrund, string bemerkungZumDossier, string interneBemerkung)
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Digitalisierungsauftrag
            });
            this.AbbrechenInternal(abbruchgrund, bemerkungZumDossier, interneBemerkung);
        }

        public override void Zuruecksetzen()
        {
            this.ZuruecksetzenInternal();
        }

        public override void OnStateEnter()
        {
            switch (Context.Ordering.Type)
            {
                case OrderType.Digitalisierungsauftrag:
                    InitialiseDigitalisierungsTermin();
                    break;
            }
        }


        /// <summary>
        ///     Berechnet und setzt das Feld TerminDigitalisierung falls es noch leer ist.
        /// </summary>
        private void InitialiseDigitalisierungsTermin()
        {
            Debug.Assert(Context.Ordering.OrderDate != null, "Ordering.OrderDate != null");

            if (Context.OrderItem.TerminDigitalisierung != null)
            {
                return;
            }

            var parameterHelper = new ParameterHelper();
            var setting = parameterHelper.GetSetting<KontingentDigitalisierungsauftraegeSetting>();
            var digitalisierungsKategorie = Context.OrderItem.DigitalisierungsKategorie;

            string kontingentString;

            switch (digitalisierungsKategorie)
            {
                case DigitalisierungsKategorie.Intern:
                    kontingentString = setting.Intern;
                    break;
                case DigitalisierungsKategorie.Oeffentlichkeit:
                    kontingentString = setting.Oeffentlichkeit;
                    break;
                case DigitalisierungsKategorie.Gesuch:
                    kontingentString = setting.Gesuche;
                    break;
                case DigitalisierungsKategorie.Amt:
                    kontingentString = setting.Amt;
                    break;
                case DigitalisierungsKategorie.Forschungsgruppe:
                    kontingentString = setting.DDS;
                    break;
                default:
                    return;
            }

            var parser = new DigitalisierungsKontingentParser();
            var kontingent = parser.Parse(kontingentString);

            var orderDate = (DateTime) Context.Ordering.OrderDate;

            // ToDo: await correctly, when state machine is async
            var latestTermine = Context.OrderDataAccess.GetLatestDigitalisierungsTermine(Context.Besteller.Id,
                orderDate, digitalisierungsKategorie)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult(); ;

            var terminManager = new DigitalisierungsTerminManager(Context.OrderDataAccess);
            var newTermin = terminManager.GetNextPossibleTermin(orderDate, latestTermine, kontingent);

            Context.OrderItem.TerminDigitalisierung = newTermin;
            Context.OrderDataAccess.UpdateOrderItem(Context.OrderItem)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult(); // Update ist nötig, damit die Aufräge die in der selben Transaktion verarbeitet werden den richtigen Digitalisierungtermin erhalten
        }
    }
}