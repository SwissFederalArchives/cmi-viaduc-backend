using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Order;
using CMI.Manager.Order.Status;

namespace CMI.Manager.Order
{
    /// <summary>
    ///     Er möglicht den einfachen Zugriff auf die Instanzen von AuftragStatus
    /// </summary>
    public class AuftragStatusRepo
    {
        private static readonly Dictionary<OrderStatesInternal, AuftragStatus> auftragStatusByOrderStatesInternal =
            new Dictionary<OrderStatesInternal, AuftragStatus>();

        static AuftragStatusRepo()
        {
            var types = typeof(AuftragStatus).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(AuftragStatus)) && !t.IsAbstract)
                .ToArray();

            foreach (var t in types)
            {
                var propInfo = t.GetProperty("Instance");
                var status = (AuftragStatus) propInfo.GetValue(null);
                auftragStatusByOrderStatesInternal.Add(status.OrderStateInternal, status);
            }
        }

        public static ImBestellkorbStatus ImBestellkorb => ImBestellkorbStatus.Instance;
        public static NeuEingegangenStatus NeuEingegangen => NeuEingegangenStatus.Instance;
        public static EinsichtsgesuchPruefenStatus EinsichtsgesuchPruefen => EinsichtsgesuchPruefenStatus.Instance;
        public static EinsichtsgesuchWeitergeleitetStatus EinsichtsgesuchWeitergeleitet => EinsichtsgesuchWeitergeleitetStatus.Instance;
        public static FreigabePruefenStatus FreigabePruefen => FreigabePruefenStatus.Instance;
        public static AbgebrochenStatus Abgebrochen => AbgebrochenStatus.Instance;
        public static FuerDigitalisierungBereitStatus FuerDigitalisierungBereit => FuerDigitalisierungBereitStatus.Instance;
        public static FuerAushebungBereitStatus FuerAushebungBereit => FuerAushebungBereitStatus.Instance;
        public static AushebungsauftragErstelltStatus AushebungsauftragErstellt => AushebungsauftragErstelltStatus.Instance;
        public static AusgeliehenStatus Ausgeliehen => AusgeliehenStatus.Instance;
        public static DigitalisierungAbgebrochenStatus DigitalisierungAbgebrochen => DigitalisierungAbgebrochenStatus.Instance;
        public static DigitalisierungExternStatus DigitalisierungExtern => DigitalisierungExternStatus.Instance;
        public static ZumReponierenBereitStatus ZumReponierenBereit => ZumReponierenBereitStatus.Instance;
        public static AbgeschlossenStatus Abgeschlossen => AbgeschlossenStatus.Instance;

        public static AuftragStatus GetStatus(OrderStatesInternal orderItemStatus)
        {
            return auftragStatusByOrderStatesInternal[orderItemStatus];
        }
    }
}