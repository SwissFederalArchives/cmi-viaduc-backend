namespace CMI.Web.Common.Helpers
{
    public class ApiError
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string Identifier { get; set; }
    }
}