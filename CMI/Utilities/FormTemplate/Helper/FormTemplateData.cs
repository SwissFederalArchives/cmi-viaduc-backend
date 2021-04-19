using System.Collections.Generic;

namespace CMI.Utilities.FormTemplate.Helper
{
    public class FormTemplateContainer
    {
        public FormTemplateContainer()
        {
            FormTemplates = new List<FormTemplate>();
        }

        public List<FormTemplate> FormTemplates { get; set; }
    }

    public class FormTemplate
    {
        public FormTemplate()
        {
            Sections = new List<Section>();
        }

        public string FormId { get; set; }
        public List<Section> Sections { get; set; }
    }

    public class Section
    {
        public Section()
        {
            SubSections = new List<Section>();
            Fields = new List<FieldType>();
            SectionLabels = new Dictionary<string, string>();
        }

        public Dictionary<string, string> SectionLabels { get; set; }
        public List<FieldType> Fields { get; set; }
        public List<Section> SubSections { get; set; }
    }


    public class FieldType
    {
        public FieldType()
        {
            FieldLabels = new Dictionary<string, string>();
        }

        public string DbFieldName { get; set; }
        public string DbType { get; set; }
        public string ElasticType { get; set; }
        public FieldTypeVisibility Visibility { get; set; }
        public Dictionary<string, string> FieldLabels { get; set; }
    }

    public enum FieldTypeVisibility
    {
        /// <remarks />
        @public,

        /// <remarks />
        @internal
    }
}