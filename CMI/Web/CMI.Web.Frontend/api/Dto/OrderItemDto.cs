using System;
using CMI.Contract.Order;

namespace CMI.Web.Frontend.api.Dto
{
    public class OrderItemDto
    {
        public string ReferenceCode { get; set; }
        public string Title { get; set; }

        public string Period { get; set; }
        public string Comment { get; set; }


        /// <summary>
        ///     EinsichtsbewilligungNotwendig ist nur relevant für OrderItems aus dem Bestellkorb.
        ///     Zeigt anhand der Access Tokens ob eine Einsichtsbewilligung notwendig ist.
        /// </summary>
        /// <value><c>true</c> if [einsichtsbewilligung notwendig]; otherwise, <c>false</c>.</value>
        public bool EinsichtsbewilligungNotwendig { get; set; }

        /// <summary>
        ///     Zeigt an, ob eine Begründung für die Einsichtsbewilligung notwendig ist.
        /// </summary>
        /// <value><c>true</c> if [could need a reason]; otherwise, <c>false</c>.</value>
        public bool CouldNeedAReason { get; set; }

        public DateTime? BewilligungsDatum { get; set; }

        public int? VeId { get; set; }
        public int Id { get; set; }
        public int OrderId { get; set; }
        public bool HasPersonendaten { get; set; }
        public string Standort { get; set; }
        public string Signatur { get; set; }
        public string Darin { get; set; }
        public string ZusaetzlicheInformationen { get; set; }
        public string Hierarchiestufe { get; set; }
        public string Schutzfristverzeichnung { get; set; }
        public string ZugaenglichkeitGemaessBga { get; set; }
        public string Publikationsrechte { get; set; }
        public string Behaeltnistyp { get; set; }
        public string ZustaendigeStelle { get; set; }
        public string IdentifikationDigitalesMagazin { get; set; }
        public ExternalStatus ExternalStatus { get; set; }
        public int? Reason { get; set; }
        public string Aktenzeichen { get; set; }
        public DigitalisierungsKategorie DigitalisierungsKategorie { get; set; }
        public DateTime? TerminDigitalisierung { get; set; }
        public EntscheidGesuch EntscheidGesuch { get; set; }
        public DateTime? DatumDesEntscheids { get; set; }
        public Abbruchgrund Abbruchgrund { get; set; }
    }
}