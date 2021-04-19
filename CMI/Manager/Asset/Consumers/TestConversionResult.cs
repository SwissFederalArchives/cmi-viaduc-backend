namespace CMI.Manager.Asset.Consumers
{
    public class TestConversionResult
    {
        public TestConversionResult(bool success, string error)
        {
            Success = success;
            Error = error;
        }

        public bool Success { get; set; }
        public string Error { get; set; }
    }
}