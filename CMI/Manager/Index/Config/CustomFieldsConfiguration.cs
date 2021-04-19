using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace CMI.Manager.Index.Config
{
    public class CustomFieldsConfiguration
    {
        public CustomFieldsConfiguration(string configurationFile)
        {
            if (File.Exists(configurationFile))
            {
                var json = File.ReadAllText(configurationFile);
                Fields = JsonConvert.DeserializeObject<List<FieldConfiguration>>(json);
            }
            else
            {
                throw new FileNotFoundException($"could not find the configuration file {configurationFile}.");
            }
        }

        public List<FieldConfiguration> Fields { get; }
    }
}