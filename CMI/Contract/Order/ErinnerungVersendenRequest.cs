using System.Collections.Generic;

namespace CMI.Contract.Order
{
    public class ErinnerungVersendenRequest
    {
        /// <summary>
        ///     Die Liste der Aufträge die gemahnt werden soll.
        /// </summary>
        public List<int> OrderItemIds { get; set; }
        /// <summary>
        ///     Benutzer der die Mahnung versendet
        /// </summary>
        public string UserId { get; set; }
    }


    public class ErinnerungVersendenResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}


