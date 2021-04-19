namespace CMI.Contract.Management
{
    public interface INewsForOneLanguage
    {
        string FromDate { get; set; }
        string ToDate { get; set; }
        string Heading { get; set; }
        string Text { get; set; }
    }
}