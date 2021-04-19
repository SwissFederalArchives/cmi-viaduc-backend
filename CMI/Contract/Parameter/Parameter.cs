namespace CMI.Contract.Parameter
{
    public class Parameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public object Value { get; set; }
        public object Default { get; set; }
        public string RegexValidation { get; set; }
        public string ErrrorMessage { get; set; }
        public bool Mandatory { get; set; } = false;
    }
}