using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Common.Helpers
{
    public class JsonContent : HttpContent
    {
        private readonly Formatting formatting;
        private readonly JToken value;

        public JsonContent(JToken value, Formatting formatting = Formatting.None)
        {
            this.value = value;
            this.formatting = formatting;
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            if (value != null)
            {
                var jw = new JsonTextWriter(new StreamWriter(stream))
                {
                    Formatting = formatting
                };
                value.WriteTo(jw);
                jw.Flush();
            }

            return Task.FromResult<object>(null);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}