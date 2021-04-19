using CMI.Utilities.Common;

namespace CMI.Contract.Parameter.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<ParameterSettings>(x => x.Path, "Lokaler Pfad zum Verzeichnis der Parameter eines Dienstes");
        }
    }
}