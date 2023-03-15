using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Common.Gebrauchskopie;
using DotCMIS.Data.Extensions;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Access.Repository
{
    public class MetadataDataAccess : IMetadataDataAccess
    {
        /// <inheritdoc />
        /// <summary>
        ///     Gets a value for a property in the extended objects list.
        ///     The property can be nested over several levels, thus we can pass a
        ///     property path. For example: Arelda:datei/datei/originalName
        /// </summary>
        /// <param name="extensions">The lsit with the metadata extension items</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>System.String.</returns>
        public string GetExtendedPropertyValue(IList<ICmisExtensionElement> extensions, string propertyPath)
        {
            var elements = GetExtendedPropertyElements(extensions, propertyPath);
            if (elements.Any())
            {
                var element = elements.First();
                return GetElementValue(element, propertyPath.Split('/').Last());
            }

            return null;
        }

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
        public string GetExtendedPropertyBagValue(IList<ICmisExtensionElement> extensions, string propertyPath, string propertyName)
        {
            var elements = GetExtendedPropertyElements(extensions, propertyPath);
            foreach (var element in elements)
            {
                var attributValue = element.Attributes["name"];
                Log.Verbose("Found an name attribute value of a property bag: {attributes}", element.Attributes["name"]);
                if (attributValue.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return element.Value;
                }
            }

            return null;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Gets a collection of values for a property in the extended objects list.
        ///     The property can be nested over several levels, thus we can pass a
        ///     property path. For example: Arelda:datei/datei/originalName
        /// </summary>
        /// <param name="extensions">The extensions.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>IList&lt;System.String&gt;.</returns>
        public List<string> GetExtendedPropertyValues(IList<ICmisExtensionElement> extensions, string propertyPath)
        {
            var retVal = new List<string>();
            var elements = GetExtendedPropertyElements(extensions, propertyPath);

            foreach (var element in elements)
            {
                var value = GetElementValue(element, propertyPath.Split('/').Last());
                retVal.Add(value);
            }

            return retVal;
        }


        /// <summary>
        ///     Determines whether the cmis folder has a specific property in its extensions data.
        /// </summary>
        /// <param name="extensions">The extensions.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns><c>true</c> if the folder contains the property</returns>
        public bool HasProperty(IList<ICmisExtensionElement> extensions, string propertyPath)
        {
            var element = GetExtendedPropertyElements(extensions, propertyPath);
            return element != null;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Gets a historic data range for a given property in the metadata
        /// </summary>
        /// <param name="extensions">The metadata extensions.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>HistorischerZeitraum.</returns>
        public HistorischerZeitraum GetHistorischerZeitraum(IList<ICmisExtensionElement> extensions, string propertyPath)
        {
            var von = GetHistorischerZeitpunkt(extensions, $"{propertyPath}/von");
            var bis = GetHistorischerZeitpunkt(extensions, $"{propertyPath}/bis");

            if (von == null || bis == null)
            {
                return null;
            }

            return new HistorischerZeitraum
            {
                Von = von,
                Bis = bis
            };
        }

        /// <inheritdoc />
        /// <summary>
        ///     Gets a historic date for a given property in the metadata.
        /// </summary>
        /// <param name="extensions">The metadata extensions.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>HistorischerZeitpunkt.</returns>
        public HistorischerZeitpunkt GetHistorischerZeitpunkt(IList<ICmisExtensionElement> extensions, string propertyPath)
        {
            var caIndikator = bool.TryParse(GetExtendedPropertyValue(extensions, $"{propertyPath}/ca"), out var ca) && ca;
            var datum = GetExtendedPropertyValue(extensions, $"{propertyPath}/datum");

            // If datumstring is null or empty we do not have a historic timepoint
            if (string.IsNullOrEmpty(datum))
            {
                return null;
            }

            return new HistorischerZeitpunkt
            {
                Ca = caIndikator,
                Datum = datum
            };
        }

        private static string GetElementValue(ICmisExtensionElement element, string propertyName)
        {
            // the actual values can either be found on the value property, 
            // be stored in an attribute, or in a children collection
            if (!string.IsNullOrEmpty(element.Value))
            {
                return element.Value;
            }

            // the value must be taken from the attributes collection. The attribute name is 
            // separated from the property name with the @sign, e.g. datei@id --> get the id attribute of the datei property
            if (propertyName.Contains("@"))
            {
                var propertyNameParts = propertyName.Split('@');
                if (propertyNameParts.Length > 1 && element.Attributes.ContainsKey(propertyNameParts[1]))
                {
                    return element.Attributes[propertyNameParts[1]];
                }

                // trying to get property that does not exist
                if (propertyNameParts.Length > 1 && !element.Attributes.ContainsKey(propertyNameParts[1]))
                {
                    return string.Empty;
                }
            }


            // if we didn't find anything yet, the value must be in the childrens collection.
            if (element.Children != null && element.Children.Any())
            {
                return JsonConvert.SerializeObject(element.Children);
            }

            return null;
        }

        private IList<ICmisExtensionElement> GetExtendedPropertyElements(IList<ICmisExtensionElement> extensions, string propertyPath)
        {
            Log.Verbose("Trying to get property: {propertyPath}", propertyPath);
            IList<ICmisExtensionElement> retVal = new List<ICmisExtensionElement>();
            var metadata = extensions?.FirstOrDefault(e => e.Name == "e1:metadata");
            if (metadata != null)
            {
                var nestedPropertyNames = propertyPath.Split('/');
                var currentElement = metadata;
                IList<ICmisExtensionElement> elements = new List<ICmisExtensionElement>();
                foreach (var propertyName in nestedPropertyNames)
                {
                    var propertyNameParts = propertyName.Split('@');
                    elements = GetExtendedPropertyValueElements(currentElement, propertyNameParts[0]);

                    // if we didnt find an element, we can stop here
                    if (!elements.Any())
                    {
                        break;
                    }

                    // Only the last element in the hierarchiy can (could/should) be a collection
                    if (propertyName != nestedPropertyNames.Last())
                    {
                        currentElement = elements[0];
                    }
                }

                // Did we actually get a value element?
                if (elements.Any())
                {
                    retVal = elements;
                }
            }

            return retVal;
        }

        /// <summary>
        ///     Gets the extension parentElement of type "e1:value" of a parentElement with a specific name that
        ///     is found in the children collection of the passed parent.
        /// </summary>
        /// <param name="parentElement">The parent element that is to be searched.</param>
        /// <param name="propertyName">Name of the property to find.</param>
        /// <returns>ICmisExtensionElement.</returns>
        private IList<ICmisExtensionElement> GetExtendedPropertyValueElements(ICmisExtensionElement parentElement, string propertyName)
        {
            IList<ICmisExtensionElement> retVal = new List<ICmisExtensionElement>();
            try
            {
                foreach (var extensionElement in parentElement.Children)
                {
                    switch (extensionElement.Name.ToLowerInvariant())
                    {
                        case "e1:item":
                            // Spezialfall. Ein Element kann mehrere e1.item Kinderelemente enthalten.
                            // Der Name des Attributs ist im Kind-Element e1:name enthalten, 
                            // der Wert des Attributs im Kind-Element e1:value, wobei das e1:value
                            // Element den Wert entweder direkt im Value property hat, oder es ist ein 
                            // komplexes Element mit eigenen Kinder-Einträgen
                            if (extensionElement.Children.Any(c =>
                                c.Value != null && c.Value.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                var valueElements = extensionElement.Children
                                    .Where(f => f.Name.Equals("e1:value", StringComparison.InvariantCultureIgnoreCase)).ToList();
                                if (valueElements.Any())
                                {
                                    retVal = valueElements;
                                }
                            }

                            break;
                        default:
                            if (extensionElement.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase))
                            // Wir haben ein normales Property gefunden. Wir können das Element der Collection hinzufügen.
                            {
                                retVal.Add(extensionElement);
                            }

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unknown error while getting extended property elements. Error message is: {Message}", ex.Message);
            }

            return retVal;
        }
    }
}