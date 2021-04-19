using System.Linq;
using CMI.Contract.Common;

namespace CMI.Manager.Index.ValueExtractors
{
    public class FloatExtractor : ExtractorBase<ElasticFloat>
    {
        protected override ElasticFloat GetValueInternal(DataElementElementValue value, DataElement dataElement)
        {
            ElasticFloat retVal = null;
            if (value != null)
            {
                retVal = new ElasticFloat
                {
                    Value = value.FloatValue.Value,
                    DecimalPositions = value.FloatValue.DecimalPositions,
                    Text = value.TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value
                };
            }

            return retVal;
        }
    }
}