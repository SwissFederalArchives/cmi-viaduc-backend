namespace CMI.Contract.DocumentConverter
{
    public class JobInitRequest
    {
        public string FileNameWithExtension { get; set; }
        public ProcessType RequestedProcessType { get; set; }
        public JobContext Context { get; set; }
    }
}
