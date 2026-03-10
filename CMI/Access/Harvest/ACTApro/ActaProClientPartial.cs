using Serilog;
using System.Net.Http;
using CMI.Access.Harvest.Properties;

namespace CMI.Access.Harvest.ActaPro
{
    public partial class ActaProClient
    {
        partial void Initialize()
        {
            BaseUrl = Settings.Default.ActaProEndpoint;
            ReadResponseAsString = true;
        }

        static partial void UpdateJsonSerializerSettings(Newtonsoft.Json.JsonSerializerSettings settings)
        {
            settings.DateFormatString = "dd.MM.yyyy HH:mm:ss:fff";
        }

        
        partial void ProcessResponse(HttpClient client, HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Read Dokument from ActaPro has an Error. StatusCode: {response.StatusCode}, LocalPath {response.RequestMessage.RequestUri.LocalPath}");
            }
        }
    }

    //  ToDo: Workaround 
    public partial class SortObject
    {
        [Newtonsoft.Json.JsonProperty("empty", Required = Newtonsoft.Json.Required.AllowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool Empty { get; set; }
        [Newtonsoft.Json.JsonProperty("unsorted", Required = Newtonsoft.Json.Required.AllowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool Unsorted { get; set; }
        [Newtonsoft.Json.JsonProperty("sorted", Required = Newtonsoft.Json.Required.AllowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool Sorted { get; set; }

    }
}
