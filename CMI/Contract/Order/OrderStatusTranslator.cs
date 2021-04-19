using System;

namespace CMI.Contract.Order
{
    public class OrderStatusTranslator
    {
        public static ExternalStatus GetExternalStatus(OrderType orderType, OrderStatesInternal orderStatesInternal)
        {
            // Achtung, bei Änderungen muss die SQL View ebenfalls geändert werden: v_OrderingFlatItem
            switch (orderStatesInternal)
            {
                case OrderStatesInternal.ImBestellkorb:
                    return ExternalStatus.ImBestellkorb;

                case OrderStatesInternal.NeuEingegangen:
                case OrderStatesInternal.FreigabePruefen:
                case OrderStatesInternal.FuerDigitalisierungBereit:
                case OrderStatesInternal.FuerAushebungBereit:
                case OrderStatesInternal.AushebungsauftragErstellt:
                case OrderStatesInternal.EinsichtsgesuchPruefen:
                case OrderStatesInternal.EinsichtsgesuchWeitergeleitet:
                case OrderStatesInternal.DigitalisierungExtern:
                case OrderStatesInternal.DigitalisierungAbgebrochen:
                    return ExternalStatus.InBearbeitung;

                case OrderStatesInternal.Ausgeliehen:
                    return orderType == OrderType.Digitalisierungsauftrag
                        ? ExternalStatus.InBearbeitung
                        : ExternalStatus.Ausgeliehen;

                case OrderStatesInternal.Abgebrochen:
                    return ExternalStatus.Abgebrochen;

                case OrderStatesInternal.ZumReponierenBereit:
                case OrderStatesInternal.Abgeschlossen:
                    return ExternalStatus.Abgeschlossen;
                default:
                    throw new NotSupportedException("Der Externe Status kann nicht ermittelt werden");
            }
        }
    }

    public enum ExternalStatus
    {
        ImBestellkorb = 0,
        InBearbeitung = 1,
        Ausgeliehen = 2,
        Abgeschlossen = 3,
        Abgebrochen = 4
    }
}