using System;
using CMI.Contract.Order;

namespace CMI.Engine.MailTemplate
{
    public class Bestellung
    {
        public Bestellung(Ordering ordering, Person besteller)
        {
            Ordering = ordering;
            Besteller = besteller;
        }

        public Bestellung(Ordering ordering)
        {
            Ordering = ordering;
        }


        /// <summary>Dieses Property ist nicht dazu gedacht, direkt in der Vorlage eingesetzt zu werden.</summary>
        internal Ordering Ordering { get; }

        public Person Besteller { get; }


        public string BegründungEinsichtsgesuch => Ordering?.BegruendungEinsichtsgesuch;

        public int? OrderId => Ordering?.Id;

        public string Auftragstyp => Ordering.Type.ToString();

        public string Erfassungsdatum => Ordering.OrderDate?.ToString("dd.MM.yyyy") ?? "";

        public string ErfassungsdatumMitUhrzeit => Ordering.OrderDate?.ToString("dd.MM.yyyy HH:mm") ?? "";

        public string LesesaalDatum
        {
            get
            {
                if (!Ordering.LesesaalDate.HasValue)
                {
                    return string.Empty;
                }

                return Ordering.LesesaalDate.Value == DateTime.MinValue ? string.Empty : Ordering.LesesaalDate.Value.ToString("dd.MM.yyyy");
            }
        }

        public string Bemerkungen => Ordering.Comment;
        public bool IstVerwaltungsausleihe => Ordering.Type == OrderType.Verwaltungsausleihe;
        public bool IstDigitalisierungsauftrag => Ordering.Type == OrderType.Digitalisierungsauftrag;
        public bool IstEinsichtsgesuch => Ordering.Type == OrderType.Einsichtsgesuch;
        public bool IstLesesaalbestellung => Ordering.Type == OrderType.Lesesaalausleihen && !Besteller.HatFlagBarInterneKonsultation;
        public bool IstBarInterneKonsultation => Ordering.Type == OrderType.Lesesaalausleihen && Besteller.HatFlagBarInterneKonsultation;
        public int? ArtDerArbeitId => Ordering.ArtDerArbeit;
        public DateTime? OrderDate => Ordering.OrderDate;

        public bool IstPersonenbezogeneNachforschung => Ordering.PersonenbezogeneNachforschung;
        public bool HatUnterlagenDieNutzerSelberBetreffen => Ordering.HasEigenePersonendaten;
    }
}