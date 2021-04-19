using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Order;

namespace CMI.Manager.Order.Status
{
    public class NeuEingegangenStatus : AuftragStatus
    {
        private static readonly Lazy<NeuEingegangenStatus> lazy =
            new Lazy<NeuEingegangenStatus>(() => new NeuEingegangenStatus());

        private NeuEingegangenStatus()
        {
        }

        public static NeuEingegangenStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.NeuEingegangen;

        public override void OnStateEnter()
        {
            switch (Context.Ordering.Type)
            {
                case OrderType.Einsichtsgesuch:
                    Context.SetNewStatus(AuftragStatusRepo.EinsichtsgesuchPruefen, Users.System);
                    Context.SetApproveStatus(ApproveStatus.NichtGeprueft, Users.System);
                    break;

                case OrderType.Digitalisierungsauftrag:
                    InitializeDigitalisierungsKategorie();
                    AutomatischOderManuellPruefenSetzen(AuftragStatusRepo.FuerDigitalisierungBereit);
                    break;

                case OrderType.Lesesaalausleihen:
                    AutomatischOderManuellPruefenSetzen(AuftragStatusRepo.FuerAushebungBereit);
                    break;

                case OrderType.Verwaltungsausleihe:
                    AutomatischOderManuellPruefungSetzenVerwaltungsausleihe();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AutomatischOderManuellPruefungSetzenVerwaltungsausleihe()
        {
            // Bei Verwaltungsausleihen soll das System nur dann eine automatische Freigabe erteilen, wenn der 
            // Benutzer ein passendes AS_XXX Token hat. Alle anderen Token werden nicht beachtet!
            if (Context.Besteller.Access.RolePublicClient == AccessRoles.RoleAS && Context.OrderItem.VeId.HasValue)
            {
                var veRecord = Context.IndexAccess.FindDocument(Context.OrderItem.VeId.Value.ToString(), false);
                if (veRecord != null)
                {
                    if (Context.Besteller.Access.HasAsTokenFor(veRecord.PrimaryDataDownloadAccessTokens)) // nur AS_XXX Tokens sind hier gültig
                    {
                        Context.SetNewStatus(AuftragStatusRepo.FuerAushebungBereit, Users.System);
                        Context.SetApproveStatus(ApproveStatus.FreigegebenDurchSystem, Users.System);
                        return;
                    }
                }
            }

            // Verwaltungsausleihen von BVW-Benutzer müssen immer den Status "Freigabe Prüfen" haben 
            Context.SetNewStatus(AuftragStatusRepo.FreigabePruefen, Users.System);
            Context.SetApproveStatus(ApproveStatus.NichtGeprueft, Users.System);
        }

        private void AutomatischOderManuellPruefenSetzen(AuftragStatus zielStatusWennAutomatischeFreigabeMoeglich)
        {
            // https://devblogs.microsoft.com/pfxteam/should-i-expose-synchronous-wrappers-for-asynchronous-methods/
            // ToDo: await correctly, when state-machine is async
            var kannAutomatischFreigeben = KannAutomatischFreigeben(Context.OrderItem, Context.Besteller)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult(); ;

            if (kannAutomatischFreigeben)
            {
                Context.SetNewStatus(zielStatusWennAutomatischeFreigabeMoeglich, Users.System);
                Context.SetApproveStatus(ApproveStatus.FreigegebenDurchSystem, Users.System);
            }
            else
            {
                Context.SetNewStatus(AuftragStatusRepo.FreigabePruefen, Users.System);
                Context.SetApproveStatus(ApproveStatus.NichtGeprueft, Users.System);
            }
        }

        /// <summary>
        ///     Setzt das Feld DigitalisierungsKategorie falls es noch leer ist
        /// </summary>
        private void InitializeDigitalisierungsKategorie()
        {
            var orderItem = Context.OrderItem;

            if (orderItem.DigitalisierungsKategorie != DigitalisierungsKategorie.Keine)
            {
                return;
            }

            switch (Context.Besteller.Access.RolePublicClient.GetRolePublicClientEnum())
            {
                case AccessRolesEnum.Ö2:
                    orderItem.DigitalisierungsKategorie = DigitalisierungsKategorie.Oeffentlichkeit;
                    break;

                case AccessRolesEnum.Ö3:
                    orderItem.DigitalisierungsKategorie =
                        Context.Besteller.ResearcherGroup
                            ? DigitalisierungsKategorie.Forschungsgruppe
                            : DigitalisierungsKategorie.Oeffentlichkeit;
                    break;

                case AccessRolesEnum.BVW:
                case AccessRolesEnum.AS:
                    orderItem.DigitalisierungsKategorie = DigitalisierungsKategorie.Amt;
                    break;

                case AccessRolesEnum.BAR:
                    orderItem.DigitalisierungsKategorie = DigitalisierungsKategorie.Intern;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(null,
                        "Angegeben Rolle wurde für die 'InitializeDigitalisierungsKategorie' nicht definiert");
            }
        }

        private async Task<bool> KannAutomatischFreigeben(OrderItem currentOrderItem, User besteller)
        {
            // Nur Bestellungen die mit einer VE in der Datenbank verknüpft sind, könn(t)en automatsich 
            // freigegeben werden.
            if (!currentOrderItem.VeId.HasValue)
            {
                return false;
            }

            // Prüfen ob gültiger Record von Elasic geliefert wurde.
            var veRecord = Context.IndexAccess.FindDocument(currentOrderItem.VeId.Value.ToString(), false);
            if (veRecord == null || veRecord.ArchiveRecordId != currentOrderItem.VeId.Value.ToString())
            {
                return false;
            }

            if (besteller.Access.HasNonIndividualTokenFor(veRecord.PrimaryDataDownloadAccessTokens))
            {
                return true;
            }

            var indivTokens = await Context.OrderDataAccess.GetIndividualAccessTokens(currentOrderItem.VeId.Value, currentOrderItem.Id);
            return besteller.Access.HasAnyTokenFor(indivTokens.PrimaryDataDownloadAccessTokens);
        }
    }
}