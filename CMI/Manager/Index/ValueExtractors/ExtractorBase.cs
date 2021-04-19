using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Common;

namespace CMI.Manager.Index.ValueExtractors
{
    /// <summary>
    ///     Abstract class for extracting values from a list of <see cref="DataElement" />.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ExtractorBase<T>
    {
        /// <summary>
        ///     Returns a list of values for the given elementId from the list of data elements.
        ///     Returns an empty collection if the element cannot be found in the collection.
        /// </summary>
        /// <param name="detailData">The detail data.</param>
        /// <param name="elementId">The element identifier.</param>
        /// <returns>List&lt;T&gt;.</returns>
        public List<T> GetListValues(List<DataElement> detailData, string elementId)
        {
            var retVal = new List<T>();
            var dataElement = detailData.FirstOrDefault(d => elementId.Equals(d.ElementId, StringComparison.OrdinalIgnoreCase));
            if (dataElement != null)
            {
                foreach (var elementValue in dataElement.ElementValue.OrderBy(t => t.Sequence))
                {
                    retVal.Add(GetValueInternal(elementValue, dataElement));
                }
            }

            return retVal;
        }

        /// <summary>
        ///     Returns a single value for the given elementId from the list of data elements,
        ///     even if the given elementId would contain a collection of values.
        ///     Returns null, if the data element cannont be found, or does not contain a value.
        /// </summary>
        /// <param name="detailData">The detail data.</param>
        /// <param name="elementId">The element identifier.</param>
        /// <returns>T.</returns>
        public virtual T GetValue(List<DataElement> detailData, string elementId)
        {
            var dataElement = detailData.FirstOrDefault(d => elementId.Equals(d.ElementId, StringComparison.OrdinalIgnoreCase));
            if (dataElement == null)
            {
                return default;
            }

            var value = dataElement.ElementValue.FirstOrDefault();
            var retVal = GetValueInternal(value, dataElement);
            return retVal;
        }

        /// <summary>
        ///     Gets the value for a given type.
        ///     Must be overriden in inherited classes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="dataElement">The data element.</param>
        /// <returns>T.</returns>
        protected abstract T GetValueInternal(DataElementElementValue value, DataElement dataElement);
    }
}