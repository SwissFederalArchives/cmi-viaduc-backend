using System.Collections.Generic;

namespace CMI.Web.Management.api.Data
{
    public class DigipoolPostData
    {
        public List<int> OrderItemIds { get; set; }
        public int DigitalisierungsKategorie { get; set; }
        public string TerminDigitalisierungDatum { get; set; }
        public string TerminDigitalisierungZeit { get; set; }
    }
}