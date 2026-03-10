using CMI.Utilities.Common;

namespace CMI.Utilities.ActaPro.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.MappingTableDirectory, "Der Dateipfad von der MappingTable.db");
        }
    }
}