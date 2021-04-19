using CMI.Contract.Common;

namespace CMI.Manager.Index.ValueExtractors
{
    public class BoolExtractor : ExtractorBase<bool?>
    {
        protected override bool? GetValueInternal(DataElementElementValue value, DataElement dataElement)
        {
            bool? retVal = null;
            if (value != null)
            {
                retVal = value.BooleanValue;
            }

            return retVal;
        }
    }
}