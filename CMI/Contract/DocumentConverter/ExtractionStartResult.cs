namespace CMI.Contract.DocumentConverter
{
    public class ExtractionStartResult : IErrorResult
    {
        public string Text { get; set; }
        public bool IsInvalid { get; set; }
        public string ErrorMessage { get; set; }
    }
}