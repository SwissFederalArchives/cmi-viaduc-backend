using System.Collections.Generic;
using CMI.Contract.Common.Gebrauchskopie;
using DotCMIS.Data.Extensions;

namespace CMI.Access.Repository
{
    public interface IMetadataDataAccess
    {
        /// <summary>
        ///     Gets a value for a property in the extended objects list.
        ///     The property can be nested over several levels, thus we can pass a
        ///     property path. For example: Arelda:datei/datei/originalName
        /// </summary>
        /// <param name="extensions">The list with the metadata extension items</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>System.String.</returns>
        string GetExtendedPropertyValue(IList<ICmisExtensionElement> extensions, string propertyPath);

        /// <summary>
        ///     Gets a value for a property in the extended objects list that is listed in a property like manner.<br />
        ///     The property path will return a list of objects whose attribute "name" will contain the property name that we are
        ///     looking for.
        ///     The actual value is the value of the found property.<br />In our case it is
        ///     zusatzDaten/merkmal[name="ReihenfolgeAnalogesDossier"]
        /// </summary>
        /// <param name="extensions">The list with the metadata extension items</param>
        /// <param name="propertyPath">The property path for the list of possible values</param>
        /// <param name="propertyName">
        ///     The name of the property we are looking for that is found in the attribute "name" of the
        ///     property.
        /// </param>
        string GetExtendedPropertyBagValue(IList<ICmisExtensionElement> extensions, string propertyPath, string propertyName);

        /// <summary>
        ///     Gets a collection of values for a property in the extended objects list.
        ///     The property can be nested over several levels, thus we can pass a
        ///     property path. For example: Arelda:datei/datei/originalName
        /// </summary>
        /// <param name="extensions">The extensions.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>IList&lt;System.String&gt;.</returns>
        List<string> GetExtendedPropertyValues(IList<ICmisExtensionElement> extensions, string propertyPath);

        /// <summary>
        ///     Determines whether the cmis folder has a specific property in its extensions data.
        /// </summary>
        /// <param name="extensions">The extensions.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns><c>true</c> if the folder contains the property</returns>
        bool HasProperty(IList<ICmisExtensionElement> extensions, string propertyPath);

        /// <summary>
        ///     Gets a historic data range for a given property in the metadata
        /// </summary>
        /// <param name="extensions">The metadata extensions.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>HistorischerZeitraum.</returns>
        HistorischerZeitraum GetHistorischerZeitraum(IList<ICmisExtensionElement> extensions, string propertyPath);

        /// <summary>
        ///     Gets a historic date for a given property in the metadata.
        /// </summary>
        /// <param name="extensions">The metadata extensions.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>HistorischerZeitpunkt.</returns>
        HistorischerZeitpunkt GetHistorischerZeitpunkt(IList<ICmisExtensionElement> extensions, string propertyPath);
    }
}