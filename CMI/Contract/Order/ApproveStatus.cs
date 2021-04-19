namespace CMI.Contract.Order
{
    public enum ApproveStatus
    {
        // 0 = Default
        NichtGeprueft = 0,

        // 0 < Codes < 100 bedeuten Freigegeben 
        FreigegebenDurchSystem = 1,
        FreigegebenAusserhalbSchutzfrist = 2,
        FreigegebenInSchutzfrist = 3,

        // Codes >= 100 bedeuten "Zurückgewiesen"
        ZurueckgewiesenEinsichtsbewilligungNoetig = 100,
        ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenInSchutzfrist = 101,
        ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenFreiBewilligung = 102,
        ZurueckgewiesenFormularbestellungNichtErlaubt = 103,
        ZurueckgewiesenDossierangabenUnzureichend = 104,
        ZurueckgewiesenTeilbewilligungVorhanden = 105
    }

    public static class ApproveStatusExtensions
    {
        public static bool IstFreigegeben(this ApproveStatus status)
        {
            return status > 0 && (int) status < 100;
        }

        public static bool VerlangtDatumsangabe(this ApproveStatus status)
        {
            return status == ApproveStatus.FreigegebenInSchutzfrist;
        }

        public static bool VerlangtMitteilungBestellungMitTeilbewilligung(this ApproveStatus approveStatus)
        {
            return approveStatus == ApproveStatus.ZurueckgewiesenTeilbewilligungVorhanden;
        }
    }
}