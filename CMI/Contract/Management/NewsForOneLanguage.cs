namespace CMI.Contract.Management
{
    public class NewsForOneLanguage : INewsForOneLanguage
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Heading { get; set; }
        public string Text { get; set; }
    }
}