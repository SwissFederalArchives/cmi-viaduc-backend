using System.Collections.Generic;

namespace CMI.Contract.Order
{
    public class MahnungVersendenRequest
    {
        /// <summary>
        ///     Die Liste der Aufträge die gemahnt werden soll.
        /// </summary>
        public List<int> OrderItemIds { get; set; }

        /// <summary>
        ///     Die Sprache die für die Mails verwendet werden soll.
        ///     Es gibt nur DE und FR
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        ///     Der Benutzer kann im UI wählen, ob es die erste oder zweite Mahnung ist.
        ///     Muss nicht übereinstimmen mit der Gesamtanzahl der Mahnungen die schon erfolgt sind.
        /// </summary>
        public int GewaehlteMahnungAnzahl { get; set; }

        /// <summary>
        ///     Benutzer der die Mahnung versendet
        /// </summary>
        public string UserId { get; set; }
    }

    public class MahnungVersendenResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}