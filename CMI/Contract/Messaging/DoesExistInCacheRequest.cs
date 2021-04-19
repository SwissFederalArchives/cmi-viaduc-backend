using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class DoesExistInCacheRequest
    {
        public CacheRetentionCategory RetentionCategory { get; set; }
        public string Id { get; set; }
    }
}