using System.Collections;
using System.Collections.Generic;

namespace CMI.Web.Frontend.api.Templates
{
    public class TemplateField
    {
        public string DbFieldName { get; set; }
        public string DbType { get; set; }
        public string ElasticType { get; set; }
        public int? Visibility { get; set; }

        public TemplateLabels FieldLabels { get; set; }

        public IList<TemplateSection> Sections { get; set; }

        public override bool Equals(object obj)
        {
            return ((TemplateField) obj)?.DbFieldName == DbFieldName;
        }

        public override int GetHashCode()
        {
            return DbFieldName.GetHashCode();
        }
    }
}