using System.Collections.Generic;
using CMI.Utilities.Bus.Configuration.Properties;
using Newtonsoft.Json;

namespace CMI.Utilities.Bus.Configuration
{
    public static class PrefetchCountSettings
    {
        static PrefetchCountSettings()
        {
            if (!string.IsNullOrEmpty(Settings.Default.PrefetchCountSettings))
            {
                Items = JsonConvert.DeserializeObject<Dictionary<string, ushort>>(Settings.Default.PrefetchCountSettings);
            }
            else
            {
                Items = new Dictionary<string, ushort>();
            }
        }

        public static Dictionary<string, ushort> Items { get; set; }
    }
}