using System;

namespace CMI.Web.Frontend.api
{
    public class KontingentResult
    {
        public int Bestellkontingent => Math.Max(Digitalisierungesbeschraenkung - AktiveDigitalisierungsauftraege, 0);
        public int Digitalisierungesbeschraenkung { get; set; }
        public int AktiveDigitalisierungsauftraege { get; set; }
    }
}