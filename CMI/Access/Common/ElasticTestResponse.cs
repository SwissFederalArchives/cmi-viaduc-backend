namespace CMI.Access.Common
{
    public class ElasticTestResponse
    {
        public bool IsReadOnly { get; set; }
        public string DocsCount { get; set; }
        public string Health { get; set; }
        public string Status { get; set; }
    }
}