using System.Text.RegularExpressions;

namespace CMI.Manager.Order
{
    public class DigitalisierungsKontingentParser
    {
        public DigitalisierungsKontingent Parse(string s)
        {
            s = Regex.Replace(s, @"\s+", " "); // Replace Whitespace
            var match = Regex.Match(s, KontingentDigitalisierungsauftraegeSetting.Regex);

            var anzahlAuftraege = match.Groups[1].Value.Trim();
            var inAnzahlTagen = match.Groups[3].Value.Trim();

            return new DigitalisierungsKontingent
            {
                AnzahlAuftraege = int.Parse(anzahlAuftraege),
                InAnzahlTagen = int.Parse(inAnzahlTagen)
            };
        }
    }
}