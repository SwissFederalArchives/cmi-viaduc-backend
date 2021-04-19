using CMI.Access.Harvest.ScopeArchiv;

namespace CMI.Utilities.FormTemplate.Helper
{
    public class TemplateData
    {
        public int DataElementId { get; set; }
        public string XmlCd { get; set; }
        public string DataElementName { get; set; }
        public int AccessLevel { get; set; }
        public ScopeArchivDatenElementTyp DataElementType { get; set; }
        public string FormName { get; set; }
        public string FormId { get; set; }
        public int DecimalDigits { get; set; }
        public bool FullTextSearchable { get; set; }
        public string DataElementSequenceNumber { get; set; }
    }
}