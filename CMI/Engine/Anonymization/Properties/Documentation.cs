using CMI.Utilities.Common;

namespace CMI.Engine.Anonymization.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.AnonymizationAddress,
                "Adresse vom Anonymisierungsservice");
            AddDescription<Settings>(x => x.AnonymizationKey,
                "API-Key vom Anonymisierungsservice");
        }
    }
}
