namespace CMI.Contract.Order
{
    public enum OrderStatesInternal
    {
        ImBestellkorb = 0,
        NeuEingegangen = 1,
        EinsichtsgesuchPruefen = 2,
        EinsichtsgesuchWeitergeleitet = 3,
        FreigabePruefen = 4,
        Abgebrochen = 5,
        FuerDigitalisierungBereit = 6,
        FuerAushebungBereit = 7,
        AushebungsauftragErstellt = 8,
        Ausgeliehen = 9,
        DigitalisierungAbgebrochen = 10,
        DigitalisierungExtern = 11,
        ZumReponierenBereit = 12,
        Abgeschlossen = 13
    }
}