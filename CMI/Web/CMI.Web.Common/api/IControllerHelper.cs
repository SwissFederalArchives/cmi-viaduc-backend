namespace CMI.Web.Common.api
{
    public interface IControllerHelper
    {
        string GetCurrentUserId();

        /// <summary>
        /// Identifizierte Benutzer sind solche mit QoA >= 40
        /// </summary>
        /// <returns></returns>
        bool IsIdentifiedUser();
        string GetFromClaim(string field);
        string GetMgntRoleFromClaim();
        bool HasClaims();
        string GetInitialRoleFromClaim();
        int GetQoAFromClaim();
    }
}