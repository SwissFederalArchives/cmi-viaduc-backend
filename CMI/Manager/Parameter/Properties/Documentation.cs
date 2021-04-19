using CMI.Utilities.Common;

namespace CMI.Manager.Parameter.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<WaitTimeSetting>(x => x.MillisekundenFirstAPICall, "Wartezeit des Parameterservice nach dem ersten API Aufruf");
            AddDescription<WaitTimeSetting>(x => x.MillisekundenInitial, "Wiederholinterwal des Parameterservice");
        }
    }
}