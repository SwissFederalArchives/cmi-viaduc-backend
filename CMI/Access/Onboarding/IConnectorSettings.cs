namespace CMI.Access.Onboarding
{
    public interface IConnectorSettings
    {
        string Strategy { get;}
        string ClientId { get; }
        string ClientSecret { get; }
        string OnboardingBaseUrl { get; }
        int TokenAbsoluteExpiration { get; }
    }
}
