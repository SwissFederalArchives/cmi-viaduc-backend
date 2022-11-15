namespace CMI.Engine.MailTemplate
{
    public abstract class Ve
    {
        public abstract string TeilBestand { get; }
        public abstract string TitelTeilBestand { get; }
        public abstract string Signatur { get; }
        public abstract string Titel { get; }
        public abstract string Darin { get; }
        public abstract string Id { get; }
        public bool IstFormularbestellung => string.IsNullOrWhiteSpace(Id);
        public abstract bool IstFreiZugänglich { get; }
        public abstract string Level { get; }
        public abstract string Entstehungszeitraum { get; }
        public abstract string Aktenzeichen { get; }
        public abstract string Schutzfristkategorie { get; }
        public abstract int? Schutzfristdauer { get; }
        public abstract string Schutzfristende { get; }
        public abstract bool IdDir { get; }
        public abstract string ZustaendigeStelle { get; }
        public abstract Behältnis[] Behältnisse { get; }
        public abstract string BehältnisCodesText { get; }
        public abstract string Band { get; }
        public abstract string TitelBestand { get; }
        public abstract string Bestand { get; }
        public abstract string Ablieferung { get; }
        public abstract string ZusaetzlicheInformationen { get; }
    }
}