namespace CMI.Web.Common.api;

public class ClaimValueNames
{
    public static string AuthenticationMethod => "/identity/claims/authenticationmethod";
    public static string HomeName => "/identity/claims/fp/homeName";
    public static string UserExtId => "/identity/claims/e-id/userExtId";
    public static string FamilyName => "/identity/claims/surname";
    public static string FirstName => "/identity/claims/givenname";
    public static string Email => "/identity/claims/emailaddress";
    public static string EIdProfileRole => "/identity/claims/e-id/profile/role";
    public static string Role => "/identity/claims/role";
}