using CMI.Utilities.Common;

namespace CMI.Manager.Cache.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<CacheSettings>(x => x.BaseAddress, "Basis Adresse zum Cahce (Wird durch die Funktion  {MachineName} ermittelt)");
            AddDescription<CacheSettings>(x => x.BaseDirectory, "Lokales Verzeichnis des Cache");
            AddDescription<CacheSettings>(x => x.Port, "Port des Cache");
        }
    }
}