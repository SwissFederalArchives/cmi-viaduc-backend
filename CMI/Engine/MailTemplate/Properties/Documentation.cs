using CMI.Utilities.Common;

namespace CMI.Engine.MailTemplate.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.ManagementClientUrl,
                "Management-Client URL, ohne führendes Schema (https://) zu erfassen");
            AddDescription<Settings>(x => x.PublicClientUrl,
                "Public-Client URL, ohne führendes Schema (https://) zu erfassen");
        }
    }
}