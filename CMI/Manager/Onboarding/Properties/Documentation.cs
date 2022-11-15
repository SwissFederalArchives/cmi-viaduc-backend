using CMI.Utilities.Common;

namespace CMI.Manager.Onboarding.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<DbConnectionSetting>(x => x.ConnectionString, "DB-Connectionstring zur Viaduc DB");
            AddDescription<OnboardingSetting>(x => x.PostbackBaseUrl,"BaseUrl für  api/v1/onboarding/{event}'. Wird von Fidentity aufgerufen.");
            AddDescription<OnboardingSetting>(x => x.ConnectorBaseUrl, "Fidentity Base Url");
            AddDescription<OnboardingSetting>(x => x.ConnectorClientId, "Fidentity ClientId");
            AddDescription<OnboardingSetting>(x => x.ConnectorClientSecret, "Fidentity ClientSecret");
            AddDescription<OnboardingSetting>(x => x.ConnectorTokenExpiration, "Dauer (min) AccessToken wird im Cache gehalten");
            AddDescription<OnboardingSetting>(x => x.OnboardingCallbackErrorUrl, "Callback URL im Fehlerfall");
            AddDescription<OnboardingSetting>(x => x.OnboardingCallbackReviewUrl, "Callback URL bei einem manuellen Review");
            AddDescription<OnboardingSetting>(x => x.OnboardingCallbackSuccessUrl, "Callback URL im Erfogsfall");
            AddDescription<OnboardingSetting>(x => x.OnboardingCallbackWarnUrl, "Callback URL im Fall einer Warnung");
            AddDescription<OnboardingSetting>(x => x.TimerProcessDueTime, "Timerintervall für den Statusprozessor");
        }
    }
}
