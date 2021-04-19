using System.Collections.Generic;

namespace CMI.Web.Frontend.api.Templates
{
    public class TemplateSection
    {
        public string SectionId { get; set; }

        public TemplateLabels SectionLabels { get; set; }

        public List<TemplateField> Fields { get; set; }

        public List<TemplateSection> SubSections { get; set; }
    }
}