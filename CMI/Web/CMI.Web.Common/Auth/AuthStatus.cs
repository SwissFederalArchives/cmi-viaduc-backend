namespace CMI.Web.Common.Auth
{
    public enum AuthStatus
    {
        Ok = 10,
        NeuerBenutzer = 20,
        KeineMTanAuthentication = 30,
        KeineSmartcardAuthentication = 40,
        KeineRolleDefiniert = 50,
        ZuTieferQoAWert = 60,
        RequiresElevatedCheck = 1000
    }
}