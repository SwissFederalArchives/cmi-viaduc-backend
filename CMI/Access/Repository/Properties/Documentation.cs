using CMI.Utilities.Common;

namespace CMI.Access.Repository.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.ConnectionMode, "Connection Mode zum DIR");
            AddDescription<Settings>(x => x.FixityAlgorithmRefElementName, "Algorithmus zum DIR");
            AddDescription<Settings>(x => x.FixityValueElementName, "Fixity Value fürs DIR");
            AddDescription<Settings>(x => x.RepositoryPassword, "Passwort für die Schnittstelle zum DIR");
            AddDescription<Settings>(x => x.RepositoryServiceUrl, "URL des DIR");
            AddDescription<Settings>(x => x.RepositoryUser, "Benutzer für die Schnittstelle zum DIR");
        }
    }
}