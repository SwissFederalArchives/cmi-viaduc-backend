using System;
using DotCMIS.Data.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Manager.Repository.Tests
{
    public class ExtensionElementConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ICmisExtensionElement);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            return jo.ToObject<CmisExtensionElement>(serializer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}