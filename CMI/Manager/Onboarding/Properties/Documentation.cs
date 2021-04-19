using CMI.Utilities.Common;

namespace CMI.Manager.Onboarding.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<DbConnectionSetting>(x => x.ConnectionString, "DB-Connectionstring zur Viaduc DB");
            AddDescription<OnboardingPostbackSetting>(x => x.PostbackBaseUrl,
                "Diese URL wird mit 'api/v1/onboarding/postback' ergänzt. Swisscom ruft diese URL auf.");
        }
    }
}