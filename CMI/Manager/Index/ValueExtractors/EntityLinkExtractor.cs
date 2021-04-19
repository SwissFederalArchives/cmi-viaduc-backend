using CMI.Contract.Common;

namespace CMI.Manager.Index.ValueExtractors
{
    public class EntityLinkExtractor : ExtractorBase<ElasticEntityLink>
    {
        protected override ElasticEntityLink GetValueInternal(DataElementElementValue value, DataElement dataElement)
        {
            ElasticEntityLink retVal = null;
            if (value != null)
            {
                retVal = new ElasticEntityLink
                {
                    Value = value.EntityLink.Value,
                    EntityRecordId = value.EntityLink.EntityRecordId,
                    EntityType = value.EntityLink.EntityType
                };
            }

            return retVal;
        }
    }
}