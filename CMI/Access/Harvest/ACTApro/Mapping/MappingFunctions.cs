using CMI.Contract.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Serilog;

namespace CMI.Access.Harvest.ActaPro.Mapping
{
    public class MappingFunctions
    {

        public static string GetFieldValue(Document document, string typeName)
        {
            var value = document.Block.Fields
                .FirstOrDefault(f => f.Type.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))?.Value;
            return value;
        }

        public static string GetFieldValue(ICollection<DocumentField> documentFields, string typeName)
        {
            var value = documentFields
                .FirstOrDefault(f => f.Type.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))?.Value;
            return value;
        }

        public static string GetFieldValue(ICollection<DocumentField> documentFields, string groupName, string fieldName)
        {
            var groups = documentFields.Where(f => f.Type.Equals(groupName, StringComparison.InvariantCultureIgnoreCase));
            var groupValues = string.Empty;
            foreach (var group in groups)
            {
                if (!string.IsNullOrEmpty(groupValues))
                {
                    groupValues += Environment.NewLine;
                }
                var value = group.Fields.FirstOrDefault(f => f.Type == fieldName)?.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    groupValues += value;
                }
            }

            return groupValues;
        }


        public static string GetGroupBezValue(ICollection<DocumentField> documentFields, string groupName)
        {
            var groups = documentFields.Where(f => f.Type.Equals(groupName + "_GP", StringComparison.InvariantCultureIgnoreCase));
            var groupValues = string.Empty;
            foreach (var group in groups)
            {
                if (!string.IsNullOrEmpty(groupValues))
                {
                    groupValues += Environment.NewLine;
                }
                groupValues += group.Fields.First(f => f.Type == groupName + "_Bez").Value;
            }

            return groupValues;
        }

        /// <summary>
        /// Formatiert eine Laufzeit die im Format "18130101 19711231" geliefert wird
        /// in eine lesbare verständliche Form. Im genannten Beispiel als 1813-1971
        /// Es werden nur die Jahre angegeben. Ist das Von und Bis Jahr dasselbe wird nur eine Jahreszahl zurückgegeben
        /// </summary>
        /// <param name="laufzeitText"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static string FormatLaufzeitText(string laufzeitText)
        {
            if (string.IsNullOrEmpty(laufzeitText))
            {
                return string.Empty;
            }

            if (laufzeitText.Contains(';'))
            {
                laufzeitText = RestructureLaufzeitText(laufzeitText);
            }

            var parts = laufzeitText.Split(' ');

            if (laufzeitText.Length != 17 || parts.Length != 2)
            {
                throw new ArgumentException($"The given time range text '{laufzeitText}' doesn't have the expected format");
            }

            try
            {
                var fromYear = Convert.ToInt32(parts[0].Substring(0, 4));
                var toYear = Convert.ToInt32(parts[1].Substring(0, 4));


                if (fromYear == toYear)
                {
                    return fromYear.ToString();
                }

                return $"{fromYear}-{toYear}";

            }
            catch (Exception)
            {
                throw new ArgumentException($"The given time range text '{laufzeitText}' doesn't have the expected format");
            }
        }

        /// <summary>
        /// Returns the from date in the first tuple, the to date in the second one.
        /// It parses the input string that must be in the format: "yyyyMMdd yyyyMMdd"
        /// </summary>
        /// <param name="laufzeitText"></param>
        /// <returns>A tuple with the from and to date</returns>
        /// <exception cref="ArgumentException">Throws an exception if the input format is not as expected</exception>
        public static Tuple<DateTime?, DateTime?> GetFromBisDate(string laufzeitText)
        {
            // Laufzeit Text is formated as this: 20200101 20200101
            //                                    14240101 20231231
            // Sometimes there are values like:   17120101 17121231; 18090101 18091231
            // In that case we take the from date from the first range, end the end date from the second
            if (string.IsNullOrEmpty(laufzeitText))
            {
                return new Tuple<DateTime?, DateTime?>(null, null);
            }

            if (laufzeitText.Contains(';'))
            {
                laufzeitText = RestructureLaufzeitText(laufzeitText);
            }


            var parts = laufzeitText.Split(' ');

            if (laufzeitText.Length != 17 || parts.Length != 2)
            {
                throw new ArgumentException($"The given time range text '{laufzeitText}' doesn't have the expected format");
            }

            try
            {
                var fromYear = Convert.ToInt32(parts[0].Substring(0, 4));
                var toYear = Convert.ToInt32(parts[1].Substring(0, 4));
                var fromMonth = Convert.ToInt32(parts[0].Substring(4, 2));
                var toMonth = Convert.ToInt32(parts[1].Substring(4, 2));
                var fromDay = Convert.ToInt32(parts[0].Substring(6, 2));
                var toDay = Convert.ToInt32(parts[1].Substring(6, 2));

                return new Tuple<DateTime?, DateTime?>(
                    new DateTime(fromYear, fromMonth, fromDay),
                    new DateTime(toYear, toMonth, toDay));
            }
            catch (Exception)
            {
                throw new ArgumentException($"The given time range text '{laufzeitText}' doesn't have the expected format");
            }
        }

        public static string RestructureLaufzeitText(string laufzeitText)
        {
            var lowerParts = new List<string>();
            var higherParts = new List<string>();
            var rangeParts = laufzeitText.Split(';').Select(s => s.Trim());
            foreach (var rangePart in rangeParts)
            {
                var innerParts = rangePart.Split(' ');
                if (innerParts.Length != 2)
                {
                    throw new ArgumentException($"The given inner time range text '{rangePart}' doesn't have the expected format");
                }
                lowerParts.Add(innerParts[0]);
                higherParts.Add(innerParts[1]);
            }

            // Reset the laufzeit
            laufzeitText = $"{lowerParts.First()} {higherParts.Last()}";
            return laufzeitText;
        }

        public static DataElement MapDateTimeElement(ICollection<DocumentField> fields, string fieldName, string elementName)
        {

            var format = "dd.MM.yyyy";
            var field = fields.FirstOrDefault(f => f.Type.Equals(fieldName));
            if (field != null)
            {
                var timeRange = field?.Timerange?.Values.FirstOrDefault();
                if (timeRange != null)
                {
                    try
                    {
                        var dataElement = new DataElement
                        {
                            // ElementId = GetElementId(field.Type),
                            ElementName = elementName,
                            ElementType = DataElementElementType.dateRange,
                            ElementValue =
                            [
                                new DataElementElementValue
                        {
                            DateRange = new DateRange
                            {
                                FromDate = DateTime.ParseExact(timeRange.Min, format, new DateTimeFormatInfo()),
                                ToDate = DateTime.ParseExact(timeRange.Max, format, new DateTimeFormatInfo()),
                                // If we have a min value date, then the operator is 'before'
                                // if we have a max value date, then the operator is 'after'
                                // if the min and max is the same, then it is 'exact'
                                // otherwise it is 'fromTo'
                                DateOperator = DateTime.ParseExact(timeRange.Min, format, new DateTimeFormatInfo()) == DateTime.MinValue.Date ? DateRangeDateOperator.before :
                                    DateTime.ParseExact(timeRange.Max, format, new DateTimeFormatInfo()) == DateTime.MaxValue.Date ? DateRangeDateOperator.after :
                                    timeRange.Min == timeRange.Max ? DateRangeDateOperator.exact : DateRangeDateOperator.fromTo,
                                From = timeRange.Min,
                                To = timeRange.Max,
                                FromApproxIndicator = false,
                                ToApproxIndicator = false
                            },
                            TextValues =
                            [
                                new DataElementElementValueTextValue
                                {
                                    Value = field?.Value,
                                    IsDefaultLang = true,
                                    Lang = "de-CH"
                                }
                            ]
                        }
                            ]
                        };
                        return dataElement;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unexpected error while parsing this date value: {0}", timeRange.Value);
                        throw;
                    }
                }
            }
            return null;
        }

        public static DataElement CreateIntegerElement(ICollection<DocumentField> fields, string groupName, string fieldName, string elementName)
        {
            var group = fields.FirstOrDefault(f => f.Type.Equals(groupName, StringComparison.InvariantCultureIgnoreCase));
            if (group != null)
            {
                var field = group.Fields.FirstOrDefault(f => f.Type.Equals(fieldName));

                if (field != null && int.TryParse(field.Value, out var fieldValue))
                {
                    return new DataElement
                    {
                        ElementName = elementName,
                        ElementType = DataElementElementType.integer,
                        ElementValue = [new DataElementElementValue { IntValue = fieldValue }]
                    };
                }
            }

            return null;
        }

        public static DataElement CreateBoolElement(ICollection<DocumentField> fields, string groupName, string fieldName, string elementName)
        {
            var group = fields.FirstOrDefault(f => f.Type.Equals(groupName, StringComparison.InvariantCultureIgnoreCase));
            if (group != null)
            {
                var field = group.Fields.FirstOrDefault(f => f.Type.Equals(fieldName));

                if (field != null)
                {
                    return new DataElement
                    {
                        ElementName = elementName,
                        ElementType = DataElementElementType.boolean,
                        ElementValue = [new DataElementElementValue { BooleanValue = field.Value != "0" }]
                    };
                }
            }

            return null;
        }

        public static DataElement CreateVerknüpfungElement(ICollection<DocumentField> fields, string fieldGroupName, string fieldTextName, string fieldKeyName, string elementName, string entityType)
        {
            DataElement retVal = new DataElement
            {
                ElementName = elementName,
                ElementType = DataElementElementType.entityLink
            };

            var groupFields = fields.Where(f => f.Type.Equals(fieldGroupName, StringComparison.InvariantCultureIgnoreCase)).ToList();
            foreach (var groupField in groupFields)
            {
                var fieldValue = groupField.Fields.FirstOrDefault(gf => gf.Type.Equals(fieldTextName, StringComparison.InvariantCultureIgnoreCase))?.Value;
                var fieldLink = groupField.Fields.FirstOrDefault(gf => gf.Type.Equals(fieldKeyName, StringComparison.InvariantCultureIgnoreCase))?.Value;

                if (!string.IsNullOrEmpty(fieldValue) && !string.IsNullOrEmpty(fieldLink))
                {
                    retVal.ElementValue.Add(new DataElementElementValue
                    {
                        TextValues =
                    [
                        new DataElementElementValueTextValue
                        {
                            Value = fieldValue,
                            IsDefaultLang = true,
                            Lang = "de-CH"
                        }
                    ],
                        EntityLink = new DataElementElementValueEntityLink
                        {
                            Value = fieldValue,
                            EntityRecordId = fieldLink,
                            EntityType = entityType
                        }
                    });

                }
            }

            return retVal;
        }

        public static DataElement CreateVerknüpfungElement(Dictionary<string, string> fields, string elementName, string entityType)
        {
            DataElement retVal = new DataElement
            {
                ElementName = elementName,
                ElementType = DataElementElementType.entityLink
            };

            foreach (var field in fields)
            {
                var fieldValue = field.Value;
                var fieldLink = field.Key;

                if (!string.IsNullOrEmpty(fieldValue) && !string.IsNullOrEmpty(fieldLink))
                {
                    retVal.ElementValue.Add(new DataElementElementValue
                    {
                        TextValues =
                        [
                            new DataElementElementValueTextValue
                            {
                                Value = fieldValue,
                                IsDefaultLang = true,
                                Lang = "de-CH"
                            }
                        ],
                        EntityLink = new DataElementElementValueEntityLink
                        {
                            Value = fieldValue,
                            EntityRecordId = fieldLink,
                            EntityType = entityType
                        }
                    });

                }
            }

            return retVal;
        }

        public static DataElement CreateTextElement(string fieldValue, string elementName)
        {
            if (string.IsNullOrEmpty(fieldValue))
            {
                return new DataElement
                {
                    ElementName = elementName,
                    ElementType = DataElementElementType.text
                };
            }

            return new DataElement
            {
                ElementName = elementName,
                ElementType = DataElementElementType.text,
                ElementValue =
                [
                    new DataElementElementValue
                    {
                        TextValues =
                        [
                            new DataElementElementValueTextValue
                            {
                                Value = fieldValue,
                                IsDefaultLang = true,
                                Lang = "de-CH"
                            }
                        ]
                    }
                ]
            };
        }

        public static DataElement CreateTextElement(List<string> fieldValues, string elementName)
        {
            var retVal = new DataElement
            {
                ElementName = elementName,
                ElementType = DataElementElementType.text
            };

            foreach (var fieldValue in fieldValues)
            {
                retVal.ElementValue.Add(new DataElementElementValue
                {
                    TextValues =
                    [
                        new DataElementElementValueTextValue
                        {
                            Value = fieldValue,
                            IsDefaultLang = true,
                            Lang = "de-CH"
                        }
                    ]
                });
            }

            return retVal;
        }

        public static DataElement MapGroupFieldLevelDependent(ICollection<DocumentField> fields, string levelValue, string fieldName, string elementName)
        {
            var fieldValues = new List<string>();

            switch (levelValue.ToLower())
            {
                case "vz":
                case "dokum":
                case "vor":
                    fieldValues = GetGroupOrFieldValues(fields, fieldName, "Vz");
                    break;


                case "klas": // Klassifkation / Serie
                    fieldValues = GetGroupOrFieldValues(fields, fieldName, "Kl");
                    break;

                case "tbest":
                case "best": //Bestand, Teilbestand
                    fieldValues = GetGroupOrFieldValues(fields, fieldName, "Bst");
                    break;

                case "tekt": //Tektonik / Hauptabteilung
                    fieldValues = GetGroupOrFieldValues(fields, fieldName, "Te");
                    break;
                case "arch": //Archiv
                    fieldValues = GetGroupOrFieldValues(fields, fieldName, "Ar");
                    break;
                default:
                    throw new ArgumentException($"Level is not supported: {levelValue}");
            }

            return MappingFunctions.CreateTextElement(fieldValues, elementName);
        }

        private static List<string> GetGroupOrFieldValues(ICollection<DocumentField> fields, string fieldName, string levelDominator)
        {
            var retVal = new List<string>();
            var groupFields = fields.Where(f => f.Type.Equals($"{levelDominator}_{fieldName}_Gp", StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (groupFields.Any())
            {
                foreach (var groupField in groupFields)
                {
                    foreach (var field in groupField.Fields)
                    {
                        retVal.Add(field.Value);
                    }
                }
            }
            else
            {
                retVal.Add(fields.FirstOrDefault(f => f.Type.Equals($"{levelDominator}_{fieldName}", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString());
            }

            return retVal;
        }

        public static DataElement MapGroupField(ICollection<DocumentField> fields, string fieldName, string elementName)
        {
            var fieldValues = new List<string>();
            var groupFields = fields.Where(f => f.Type.Equals($"{fieldName}_Gp", StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (groupFields.Any())
            {
                foreach (var groupField in groupFields)
                {
                    foreach (var field in groupField.Fields)
                    {
                        fieldValues.Add(field.Value);
                    }
                }
            }
            else
            {
                fieldValues.Add(fields.FirstOrDefault(f => f.Type.Equals($"{fieldName}", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString());
            }

            return MappingFunctions.CreateTextElement(fieldValues, elementName);
        }

        public static DataElement CreateHyperlinkElement(ICollection<DocumentField> fields, string fieldGroupName, string fieldTextName, string fieldUrlName, string elementName)
        {
            DataElement retVal = new DataElement
            {
                ElementName = elementName,
                ElementType = DataElementElementType.hyperlink
            };

            var groupFields = fields.Where(f => f.Type.Equals(fieldGroupName, StringComparison.InvariantCultureIgnoreCase)).ToList();
            foreach (var groupField in groupFields)
            {
                var fieldValue = groupField.Fields.FirstOrDefault(gf => gf.Type.Equals(fieldTextName, StringComparison.InvariantCultureIgnoreCase))?.Value;
                var fieldLink = groupField.Fields.FirstOrDefault(gf => gf.Type.Equals(fieldUrlName, StringComparison.InvariantCultureIgnoreCase))?.Value;

                if (!string.IsNullOrEmpty(fieldValue) && !string.IsNullOrEmpty(fieldLink))
                {
                    retVal.ElementValue.Add(new DataElementElementValue
                    {
                        TextValues =
                        [
                            new DataElementElementValueTextValue
                            {
                                Value = fieldValue,
                                IsDefaultLang = true,
                                Lang = "de-CH"
                            }
                        ],
                        Link = new DataElementElementValueLink()
                        {
                            Href = fieldLink,
                            Value = fieldValue,
                        }
                    });
                }
            }

            return retVal;
        }

    }
}
