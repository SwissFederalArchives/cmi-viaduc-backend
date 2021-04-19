using CMI.Contract.Common;

namespace CMI.Manager.Index.ValueExtractors
{
    public class Base64Extractor : ExtractorBase<ElasticBase64>
    {
        protected override ElasticBase64 GetValueInternal(DataElementElementValue value, DataElement dataElement)
        {
            ElasticBase64 retVal = null;
            if (value != null)
            {
                retVal = new ElasticBase64
                {
                    Value = value.BlobValueBase64.Value,
                    MimeType = value.BlobValueBase64.MimeType
                };
            }

            return retVal;
        }
    }
}