namespace CMI.Manager.Index.Config
{
    public class FieldConfiguration
    {
        public string ElementId { get; set; }
        public string ElementName { get; set; }
        public string TargetField { get; set; }
        public bool IsDefaultField { get; set; }
        public string Type { get; set; }
        public bool IsRepeatable { get; set; }
    }
}