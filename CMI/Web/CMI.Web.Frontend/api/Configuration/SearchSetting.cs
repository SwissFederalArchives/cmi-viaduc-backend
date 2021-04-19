using CMI.Web.Common.Helpers;

namespace CMI.Web.Frontend.api.Configuration
{
    public class SearchSetting : SettingsEntry
    {
        public SearchFieldDefinition[] AdvancedSearchFields { get; set; }
    }
}