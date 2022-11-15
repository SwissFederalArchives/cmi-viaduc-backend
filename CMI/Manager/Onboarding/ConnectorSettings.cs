using CMI.Access.Onboarding;
using CMI.Manager.Onboarding.Properties;

namespace CMI.Manager.Onboarding
{
    public class ConnectorSettings : IConnectorSettings
    {
        public string Strategy => "strategy";
        public string ClientId => OnboardingSetting.Default.ConnectorClientId;
        public string ClientSecret => OnboardingSetting.Default.ConnectorClientSecret;
        public string OnboardingBaseUrl => OnboardingSetting.Default.ConnectorBaseUrl;
        public int TokenAbsoluteExpiration => int.Parse(OnboardingSetting.Default.ConnectorTokenExpiration);
    }
}