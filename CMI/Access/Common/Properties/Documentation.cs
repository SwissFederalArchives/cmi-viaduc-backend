using CMI.Utilities.Common;

namespace CMI.Access.Common.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.ElasticSearchUrl, "URL zum Elastic Search");
            AddDescription<Settings>(x => x.ElasticSearchUsername, "Username für Zugriff auf Elastic Search");
            AddDescription<Settings>(x => x.ElasticSearchPWD, "Passwort für den Zugriff auf Elastic Search");
        }
    }
}