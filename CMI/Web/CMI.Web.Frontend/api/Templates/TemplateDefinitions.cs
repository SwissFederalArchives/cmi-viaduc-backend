using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using Serilog;

namespace CMI.Web.Frontend.api.Templates
{
    public static class TemplateDefinitions
    {
        private const string TemplatesDefinitionFilename = "templates.json";
        private static List<Template> templates;

        private static Dictionary<string, Template> templatesById;

        private static readonly object lockObject = new object();

        private static readonly Regex KeyToBlankRegex = new Regex(@"([ :,;\.\-\/\?\!<>\\\*\|()\[\]{}""\x00-\x1f\x80-\x9f]+)");
        private static readonly Regex KeyReduceRegex = new Regex(@"([_]{2,})");

        public static List<Template> Templates
        {
            get
            {
                AssertInited();
                return templates;
            }
        }

        public static Dictionary<string, Template> TemplatesById
        {
            get
            {
                AssertInited();
                return templatesById;
            }
        }

        private static void AssertInited()
        {
            if (templatesById == null || !WebHelper.EnableModelDataCaching)
            {
                lock (lockObject)
                {
                    if (templates == null)
                    {
                        InitTemplates();
                    }
                }
            }
        }

        private static void InitTemplates()
        {
            templates = new List<Template>();
            templatesById = new Dictionary<string, Template>();
            try
            {
                var path = StringHelper.AddToString(DirectoryHelper.Instance.ConfigDirectory, @"\", TemplatesDefinitionFilename);
                if (!File.Exists(path))
                {
                    path = StringHelper.AddToString(DirectoryHelper.Instance.ClientConfigDirectory, @"\", TemplatesDefinitionFilename);
                }

                var templateDefinitionData = JsonHelper.GetJsonFromFile(path);
                var templateDefinitions = templateDefinitionData.ToObject<FromtemplateDefinitions>();

                templates = templateDefinitions.FormTemplates ?? templates;
                foreach (var template in templates)
                {
                    template.FormId = template.FormId.ToLowerCamelCase();
                    template.Sections = DecorateSections(template.Sections);
                    templatesById.Add(template.FormId, template);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "TemplateDefinitions.InitTemplates: failed to init templates");
            }
        }

        private static List<TemplateSection> DecorateSections(List<TemplateSection> sections)
        {
            if (sections == null || !sections.Any())
            {
                return sections;
            }

            var ids = new HashSet<string>();
            var index = 1;
            foreach (var section in sections)
            {
                var id = "section" + index;
                var labels = section.SectionLabels = SetupTemplateLabels(section.SectionLabels, index + ". Sektion");

                if (labels.ContainsKey(WebHelper.DefaultLanguage))
                {
                    var s = GetNormalizedTemplateId(labels[WebHelper.DefaultLanguage]);
                    if (!ids.Contains(s))
                    {
                        id = s;
                    }
                }

                section.SectionId = id;
                ids.Add(id);

                var fields = section.Fields ?? new List<TemplateField>();
                foreach (var field in fields)
                {
                    field.FieldLabels = SetupTemplateLabels(field.FieldLabels, field.DbFieldName);
                }

                section.SubSections = DecorateSections(section.SubSections);

                index += 1;
            }

            return sections;
        }

        private static TemplateLabels SetupTemplateLabels(TemplateLabels labels, string defaultValue)
        {
            labels = labels ?? new TemplateLabels();
            if (!labels.ContainsKey(WebHelper.DefaultLanguage))
            {
                labels.Add(WebHelper.DefaultLanguage, defaultValue);
            }

            var defaultLabel = labels[WebHelper.DefaultLanguage];
            var otherLanguages = WebHelper.SupportedLanguages.Where(language => !WebHelper.DefaultLanguage.Equals(language)).ToList();
            foreach (var language in otherLanguages)
            {
                if (!labels.ContainsKey(language))
                {
                    labels[language] = $"{language}:{defaultLabel}";
                }
            }

            return labels;
        }

        private static string GetNormalizedTemplateId(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                key = StringHelper.ReplaceDiacritics(key);
                key = KeyToBlankRegex.Replace(key, "_");
                key = KeyReduceRegex.Replace(key, "_");
                key = key.Trim('_');
                var parts = key.Split('_');
                key = string.Join("", parts.Select(s => s.Substring(0, 1).ToUpperInvariant() + s.Substring(1)));
                key = key.ToLowerCamelCase();
            }

            return key;
        }

        // Helper class to import templates.json
        private class FromtemplateDefinitions
        {
            public List<Template> FormTemplates { get; set; }
        }
    }
}