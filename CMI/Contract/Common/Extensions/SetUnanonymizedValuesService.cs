using System.Collections.Generic;
using System.Dynamic;

namespace CMI.Contract.Common.Extensions
{
    public static class SetUnanonymizedValuesService
    {
        public static void SetUnanonymizedValuesForAuthorizedUser<T>(this T data, ElasticArchiveDbRecord dbRecord) where T : TreeRecord
        {
            // If data is empty and if the data is not anonymized at all, there is nothing to do
            // This prevents also that empty data is returned in case the UnanonymizedFields property
            // is not containing data. The title field is mandatory and thus should always contain a value
            if (data == null || data.IsAnonymized == false || string.IsNullOrEmpty(dbRecord.UnanonymizedFields.Title))
            {
                return;
            }

            data.Title = dbRecord.UnanonymizedFields.Title;
            data.ArchiveplanContext = dbRecord.UnanonymizedFields.ArchiveplanContext;

            if (data is SearchRecord searchRecord)
            {
                var clone = new ExpandoObject();
                searchRecord.WithinInfo = dbRecord.UnanonymizedFields.WithinInfo;

                foreach (var customField in searchRecord.CustomFields)
                {
                    if (customField.Key.ToString() == "verwandteVe")
                    {
                        ((IDictionary<string, object>) clone).Add("verwandteVe", dbRecord.UnanonymizedFields.VerwandteVe);
                    }
                    else if (customField.Key.ToString() == "zusatzkomponenteZac1")
                    {
                        ((IDictionary<string, object>) clone).Add("zusatzkomponenteZac1", dbRecord.UnanonymizedFields.ZusatzkomponenteZac1);
                    }
                    else if (customField.Key.ToString() == "bemerkungZurVe")
                    {
                        ((IDictionary<string, object>) clone).Add("bemerkungZurVe", dbRecord.UnanonymizedFields.BemerkungZurVe);
                    }
                    else
                    {
                        ((IDictionary<string, object>) clone).Add(customField.Key, customField.Value);
                    }
                }

                searchRecord.CustomFields = clone;
            }

            if (data is DetailRecord detailRecord)
            {
                detailRecord.ParentContentInfos = dbRecord.UnanonymizedFields.ParentContentInfos;
            }

            if (data is ElasticArchiveRecord elasticArchiveRecord)
            {
                elasticArchiveRecord.References = dbRecord.UnanonymizedFields.References;
            }
        }
    }
}
