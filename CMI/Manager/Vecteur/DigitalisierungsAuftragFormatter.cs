using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using CMI.Contract.Common;

namespace CMI.Manager.Vecteur
{
    public class DigitalisierungsAuftragFormatter : BufferedMediaTypeFormatter
    {
        public DigitalisierungsAuftragFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xml"));
        }

        public override bool CanReadType(Type type)
        {
            return false;
        }

        public override bool CanWriteType(Type type)
        {
            if (type == typeof(DigitalisierungsAuftrag))
            {
                return true;
            }

            return false;
        }

        public override void WriteToStream(Type type, object value, Stream writeStream, HttpContent content)
        {
            using (var writer = new StreamWriter(writeStream))
            {
                var item = value as DigitalisierungsAuftrag;
                if (item == null)
                {
                    throw new InvalidOperationException("Cannot serialize type");
                }

                WriteItem(item, writer);
            }
        }

        private void WriteItem(DigitalisierungsAuftrag digitalisierungsAuftrag, StreamWriter writer)
        {
            var data = digitalisierungsAuftrag.Serialize();
            writer.WriteLine(data);
        }
    }
}