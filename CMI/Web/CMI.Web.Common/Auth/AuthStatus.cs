namespace CMI.Web.Common.Auth
{
    public enum AuthStatus
    {
        Ok = 10,
        NeuerBenutzer = 20,
        KeineMTanAuthentication = 30,
        KeineKerberosAuthentication = 40,
        KeineRolleDefiniert = 50
    }
}