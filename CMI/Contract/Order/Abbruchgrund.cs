namespace CMI.Contract.Order
{
    public enum Abbruchgrund
    {
        NichtGesetzt = 0,
        Storniert = 1,
        DossierMomentanNichtVerfuegbar = 2,
        DigitalisierungNichtMoeglich = 3,

        // Codes >= 100 bedeuten "Zurückgewiesen"
        ZurueckgewiesenEinsichtsbewilligungNoetig = 100,
        ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenInSchutzfrist = 101,
        ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenFreiBewilligung = 102,
        ZurueckgewiesenFormularbestellungNichtErlaubt = 103,
        ZurueckgewiesenDossierangabenUnzureichend = 104,
        ZurueckgewiesenTeilbewilligungVorhanden = 105
    }
}