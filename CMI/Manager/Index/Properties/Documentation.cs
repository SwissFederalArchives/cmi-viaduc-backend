using CMI.Utilities.Common;

namespace CMI.Manager.Index.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.LevelAggregationIdentifier, "Aggregationsebene für den Index");
        }
    }
}