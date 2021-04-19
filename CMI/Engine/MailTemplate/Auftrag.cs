using CMI.Contract.Order;

namespace CMI.Engine.MailTemplate
{
    public class Auftrag
    {
        private readonly OrderItem orderItem;

        public Auftrag(OrderItem orderItem, Ordering ordering, Ve bestellteVe, Ve auszuhebendeVe, Person besteller)
        {
            this.orderItem = orderItem;
            BestellteVe = bestellteVe;
            Bestellung = new Bestellung(ordering, besteller);
            AuszuhebendeVe = auszuhebendeVe;
        }

        public Bestellung Bestellung { get; }

        public Ve AuszuhebendeVe { get; }

        public string KategorieDigitalisierung => orderItem.DigitalisierungsKategorie == DigitalisierungsKategorie.Keine
            ? string.Empty
            : orderItem.DigitalisierungsKategorie.ToString();

        public string Bemerkungen => orderItem.Comment;

        public string InterneBemerkung => orderItem.InternalComment;

        public string Id => orderItem.Id.ToString();

        public string Bewilligungsdatum => orderItem.BewilligungsDatum?.ToString("dd.MM.yyyy") ?? string.Empty;

        public string Ausgabedatum => orderItem.Ausgabedatum?.ToString("dd.MM.yyyy") ?? string.Empty;

        public Ve BestellteVe { get; }
        public string ZugaenglichkeitGemaessBga => orderItem.ZugaenglichkeitGemaessBga;
    }
}