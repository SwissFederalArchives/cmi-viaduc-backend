using CMI.Utilities.Common;

namespace CMI.Manager.DataFeed.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.CheckQueueIntervalInSeconds, "Zeit für das Prüfen der Queue in Millisekunden");
            AddDescription<Settings>(x => x.RequeueJobIntervalInSeconds,
                "Wartezeit für das Wiederholen des Wiedereinstellen der Queue in Millisekunden");
            AddDescription<Settings>(x => x.MaxNumberOfRetries, "Maximale Anzahl Retries");
        }
    }
}