using System.Collections.Generic;

namespace CMI.Web.Frontend.api.Templates
{
    public class Template
    {
        public string FormId { get; set; }

        public List<TemplateSection> Sections { get; set; }
    }
}