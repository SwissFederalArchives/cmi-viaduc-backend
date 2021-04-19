using CMI.Web.Common.Helpers;

namespace CMI.Web.Frontend.api.Configuration
{
    public class SearchFieldDefinition : SettingsEntry
    {
        public string Type { get; set; }
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string TranslatorType { get; set; }
    }
}