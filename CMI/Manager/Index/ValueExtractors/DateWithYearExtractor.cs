using CMI.Contract.Common;

namespace CMI.Manager.Index.ValueExtractors
{
    public class DateWithYearExtractor : ExtractorBase<ElasticDateWithYear>
    {
        protected override ElasticDateWithYear GetValueInternal(DataElementElementValue value, DataElement dataElement)
        {
            ElasticDateWithYear retVal = null;
            if (value != null)
            {
                retVal = new ElasticDateWithYear
                {
                    Date = value.DateValue,
                    Year = value.DateValue.Year
                };
            }

            return retVal;
        }
    }
}