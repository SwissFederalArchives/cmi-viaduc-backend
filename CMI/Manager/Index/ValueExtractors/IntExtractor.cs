using CMI.Contract.Common;

namespace CMI.Manager.Index.ValueExtractors
{
    public class IntExtractor : ExtractorBase<int?>
    {
        protected override int? GetValueInternal(DataElementElementValue value, DataElement dataElement)
        {
            int? retVal = null;
            if (value != null)
            {
                switch (dataElement.ElementType)
                {
                    case DataElementElementType.integer:
                        retVal = value.IntValue;
                        break;
                    case DataElementElementType.timespan:
                        retVal = value.DurationInSeconds;
                        break;
                }
            }

            return retVal;
        }
    }
}