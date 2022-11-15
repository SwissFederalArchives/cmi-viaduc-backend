using CMI.Contract.Order;

namespace CMI.Engine.MailTemplate
{
    public class BestellformularVe : Ve
    {
        public BestellformularVe(OrderItem orderItem)
        {
            Signatur = orderItem.Signatur;
            TeilBestand = orderItem.Bestand;
            Titel = orderItem.Dossiertitel;
            Id = null;
            IstFreiZugänglich = false;
            Entstehungszeitraum = orderItem.ZeitraumDossier;
            Aktenzeichen = orderItem.Aktenzeichen;
            Schutzfristkategorie = string.Empty;
            Schutzfristdauer = null;
            IdDir = false;
            ZustaendigeStelle = orderItem.ZustaendigeStelle ?? string.Empty;
            Behältnisse = new Behältnis[0];
            Ablieferung = orderItem.Ablieferung;
            BehältnisCodesText = orderItem.BehaeltnisNummer;
            Archivnummer = orderItem.ArchivNummer;
            Band = string.Empty;
            TitelTeilBestand = string.Empty;
        }

        public string Archivnummer { get; }


        public override string TeilBestand { get; }
        public override string TitelTeilBestand { get; }

        public override string Ablieferung { get; }
        public override string ZusaetzlicheInformationen { get; }

        public override string Signatur { get; }
        public override string Titel { get; }
        public override string Darin { get; }
        public override string Id { get; }
        public override bool IstFreiZugänglich { get; }
        public override string Level { get; }
        public override string Entstehungszeitraum { get; }
        public override string Aktenzeichen { get; }
        public override string Schutzfristkategorie { get; }
        public override int? Schutzfristdauer { get; }
        public override string Schutzfristende { get; }
        public override bool IdDir { get; }
        public override string ZustaendigeStelle { get; }
        public override Behältnis[] Behältnisse { get; }
        public override string Band { get; }

        public override string TitelBestand => string.Empty;
        public override string Bestand => string.Empty;
        public override string BehältnisCodesText { get; }
    }
}