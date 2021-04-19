namespace CMI.Engine.MailTemplate
{
    public class Sprache
    {
        private readonly string sprachCode;

        public Sprache(string sprachCode)
        {
            this.sprachCode = sprachCode;
        }


        public bool IstDeutsch => sprachCode == "de";

        public bool IstFranzösisch => sprachCode == "fr";

        public bool IstItalienisch => sprachCode == "it";

        public bool IstEnglisch => sprachCode == "en";


        public bool IstDeutschOderEnglisch => sprachCode == "de" || sprachCode == "en";
    }
}