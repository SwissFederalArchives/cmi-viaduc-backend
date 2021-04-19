using System.Collections.Generic;
using CMI.Contract.Order;

namespace CMI.Web.Frontend.api.Dto
{
    public class OrderParams
    {
        public string UserId { get; set; }
        public OrderType Type { get; set; }
        public string Comment { get; set; }
        public int? ArtDerArbeit { get; set; }
        public string LesesaalDate { get; set; }

        /// <summary>
        ///     Wird gesetzt, wenn der Client sich für einige Aufträge entscheiden muss,
        ///     weil sein Kontingent sonst überschritten würde
        /// </summary>
        public List<int> OrderIdsToExclude { get; set; } = new List<int>();
    }
}