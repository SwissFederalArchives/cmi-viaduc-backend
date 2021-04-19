namespace CMI.Contract.Messaging
{
    public class DoesExistInCacheResponse
    {
        public bool Exists { get; set; }
        public long FileSizeInBytes { get; set; }
    }
}