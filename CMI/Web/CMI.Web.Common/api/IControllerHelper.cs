namespace CMI.Web.Common.api
{
    public interface IControllerHelper
    {
        string GetCurrentUserId();
        bool IsKerberosAuthentication();
        bool IsSmartcartAuthentication();
        bool IsMTanAuthentication();

        /// <summary>
        ///     Kerberos-/Smartcard Anmeldung
        /// </summary>
        /// <returns></returns>
        bool IsInternalUser();

        string GetFromClaim(string field);
        string GetMgntRoleFromClaim();
        bool HasClaims();
    }
}