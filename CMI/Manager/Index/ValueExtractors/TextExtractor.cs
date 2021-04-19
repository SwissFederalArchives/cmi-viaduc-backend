using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Common;

namespace CMI.Manager.Index.ValueExtractors
{
    public class TextExtractor : ExtractorBase<string>
    {
        /// <summary>
        ///     Overrides the default implementation of the abstract class.
        ///     Text values are special in the way, that long texts can be broken up in multiple
        ///     chunks of 4000 chars. Even though it is one value, the data is stored in
        ///     mulitple data elements.
        ///     Thus we are appending the values from each element into one return value.
        /// </summary>
        /// <param name="detailData">The detail data.</param>
        /// <param name="elementId">The element identifier.</param>
        /// <returns>T.</returns>
        public override string GetValue(List<DataElement> detailData, string elementId)
        {
            string retVal = null;
            var dataElement = detailData.FirstOrDefault(d => elementId.Equals(d.ElementId, StringComparison.OrdinalIgnoreCase));
            if (dataElement?.ElementValue != null)
            {
                foreach (var elementValue in dataElement.ElementValue.OrderBy(t => t.Sequence))
                {
                    retVal += GetValueInternal(elementValue, dataElement);
                }
            }

            return retVal;
        }

        protected override string GetValueInternal(DataElementElementValue value, DataElement dataElement)
        {
            return value.TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value;
        }
    }
}