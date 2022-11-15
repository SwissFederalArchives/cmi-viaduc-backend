using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Common;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using CMI.Contract.Common.Extensions;
using CMI.Contract.Messaging;
using CMI.Engine.Anonymization;
using CMI.Manager.Index.Config;
using CMI.Manager.Index.Properties;
using CMI.Manager.Index.ValueExtractors;
using CMI.Utilities.Common.Helpers;
using MassTransit;
using Serilog;

namespace CMI.Manager.Index
{
    public class IndexManager : IIndexManager
    {
        private const string dossierLevelIdentifier = "Dossier";
        private readonly ISearchIndexDataAccess dbAccess;
        private readonly CustomFieldsConfiguration fieldsConfiguration;
        private readonly IAnonymizationEngine anomizationEngine;
        private readonly IManuelleKorrekturAccess dbManuelleKorrekturAccess;
        private readonly IAnonymizationReferenceEngine anonymizationReferenceEngine;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IndexManager" /> class.
        /// </summary>
        /// <param name="dbAccess">The database access object.</param>
        /// <param name="fieldsConfiguration">The fields configuration object.</param>
        public IndexManager(ISearchIndexDataAccess dbAccess, CustomFieldsConfiguration fieldsConfiguration, IAnonymizationEngine anomizationEngine, IManuelleKorrekturAccess dbManuelleKorrekturAccess, IAnonymizationReferenceEngine anonymizationReferenceEngine)
        {
            this.anomizationEngine = anomizationEngine;
            this.dbAccess = dbAccess;
            this.fieldsConfiguration = fieldsConfiguration;
            this.dbManuelleKorrekturAccess = dbManuelleKorrekturAccess;
            this.anonymizationReferenceEngine = anonymizationReferenceEngine;
        }

        /// <summary>
        ///  Updates an archive record in ElasticSearch
        /// </summary>
        /// <param name="elasticArchiveRecord"></param>
        /// <returns></returns>
        public ElasticArchiveRecord UpdateArchiveRecord(ElasticArchiveRecord elasticArchiveRecord)
        {
            // Save in elastic
            dbAccess.UpdateDocument(elasticArchiveRecord);
            return elasticArchiveRecord;
        }

        public void RemoveArchiveRecord(ConsumeContext<IRemoveArchiveRecord> removeContext)
        {
            dbAccess.RemoveDocument(removeContext.Message.ArchiveRecordId);
        }

        public ElasticArchiveRecord FindArchiveRecord(string archiveRecordId, bool includeFulltextContent, bool useUnanonymizedData)
        {
            var document = dbAccess.FindDocument(archiveRecordId, includeFulltextContent);
            if (useUnanonymizedData && document.IsAnonymized)
            {
                var dbRecord = dbAccess.FindDbDocument(archiveRecordId, includeFulltextContent);
                document.SetUnanonymizedValuesForAuthorizedUser(dbRecord);
            }

            return document;
        }

        // Returns all archive records for a given package, that is from dossier down to document
        // or in case of a document packageId all the items up to the dossier.
        public List<ElasticArchiveRecord> GetArchiveRecordsForPackage(string packageId)
        {
            var retVal = new List<ElasticArchiveRecord>();
            var entryItem = dbAccess.FindDocumentByPackageId(packageId);
            if (entryItem != null)
            {
                retVal.Add(entryItem);
                Log.Verbose("Added ordered item with id {ArchiveRecordId} to list", entryItem.ArchiveRecordId);
                retVal.AddRange(dbAccess.GetChildrenWithoutSecurity(entryItem.ArchiveRecordId, true));
                Log.Verbose("Added the children of the ordered item with id {ArchiveRecordId} to list. Found {Count} children",
                    entryItem.ArchiveRecordId, retVal.Count - 1);

                // Is the package we fetched already on dossier level?
                // If not, traverse up the hierarchie until we find the dossier level
                while (entryItem != null && entryItem.Level.Equals(dossierLevelIdentifier, StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    Log.Verbose("Ordered item is not dossier level. So we traverse up.");
                    if (!string.IsNullOrEmpty(entryItem.ParentArchiveRecordId))
                    {
                        entryItem = dbAccess.FindDocumentWithoutSecurity(entryItem.ParentArchiveRecordId, false);
                        if (entryItem != null)
                        {
                            Log.Verbose("Found parent item with id {ArchiveRecordId}. Adding to collection.", entryItem.ArchiveRecordId);
                            retVal.Insert(0, entryItem);
                        }
                    }
                }
            }

            return retVal;
        }

        public void UpdateTokens(string id, string[] primaryDataDownloadAccessTokens, string[] primaryDataFulltextAccessTokens,
            string[] metadataAccessTokens, string[] fieldAccessTokens)
        {
            dbAccess.UpdateTokens(id, primaryDataDownloadAccessTokens, primaryDataFulltextAccessTokens, metadataAccessTokens, fieldAccessTokens);
        }

        public async Task<ElasticArchiveDbRecord> AnonymizeArchiveRecordAsync(ElasticArchiveDbRecord elasticArchiveRecord)
        {
            return await anomizationEngine.AnonymizeArchiveRecordAsync(elasticArchiveRecord);
        }

        public ElasticArchiveRecord ConvertArchiveRecord(ArchiveRecord archiveRecord)
        {
            var levelIdentifier = Settings.Default.LevelAggregationIdentifier;

            // ReSharper disable once UseObjectOrCollectionInitializer
            // Helps in case of an exception. Line number point to exact location
            var elasticArchiveRecord = new ElasticArchiveRecord();
            elasticArchiveRecord.ArchiveRecordId = archiveRecord.ArchiveRecordId;
            elasticArchiveRecord.MetadataAccessTokens = archiveRecord.Security?.MetadataAccessToken;
            elasticArchiveRecord.FieldAccessTokens = archiveRecord.Security?.FieldAccessToken;
            elasticArchiveRecord.PrimaryDataDownloadAccessTokens = archiveRecord.Security?.PrimaryDataDownloadAccessToken;
            elasticArchiveRecord.PrimaryDataFulltextAccessTokens = archiveRecord.Security?.PrimaryDataFulltextAccessToken;
            elasticArchiveRecord.PrimaryDataLink = archiveRecord.Metadata.PrimaryDataLink;
            elasticArchiveRecord.HasImage = archiveRecord.Display.ContainsImages;
            elasticArchiveRecord.HasAudioVideo = archiveRecord.Display.ContainsMedia;
            elasticArchiveRecord.CanBeOrdered = archiveRecord.Display.CanBeOrdered;
            elasticArchiveRecord.TreePath = archiveRecord.Metadata.NodeInfo.Path;
            elasticArchiveRecord.TreeSequence = archiveRecord.Metadata.NodeInfo.Sequence;
            elasticArchiveRecord.TreeLevel = archiveRecord.Metadata.NodeInfo.Level;
            elasticArchiveRecord.IsLeaf = archiveRecord.Metadata.NodeInfo.IsLeaf;
            elasticArchiveRecord.IsRoot = archiveRecord.Metadata.NodeInfo.IsRoot;
            elasticArchiveRecord.ChildCount = archiveRecord.Metadata.NodeInfo.ChildCount;
            elasticArchiveRecord.AccessionDate = archiveRecord.Metadata.AccessionDate;
            elasticArchiveRecord.ProtectionEndDate = archiveRecord.Metadata.Usage.ProtectionEndDate.HasValue
                ? new ElasticDateWithYear
                {
                    Date = archiveRecord.Metadata.Usage.ProtectionEndDate.Value,
                    Year = archiveRecord.Metadata.Usage.ProtectionEndDate.Value.Year
                }
                : null;
            elasticArchiveRecord.ProtectionCategory = archiveRecord.Metadata.Usage.ProtectionCategory;
            elasticArchiveRecord.ProtectionDuration = archiveRecord.Metadata.Usage.ProtectionDuration;
            elasticArchiveRecord.Accessibility = archiveRecord.Metadata.Usage.Accessibility;
            elasticArchiveRecord.PhysicalUsability = archiveRecord.Metadata.Usage.PhysicalUsability;
            elasticArchiveRecord.Permission = archiveRecord.Metadata.Usage.Permission;
            elasticArchiveRecord.IsPhysicalyUsable = archiveRecord.Metadata.Usage.IsPhysicalyUsable;
            elasticArchiveRecord.ContainsPersonRelatedInformation = archiveRecord.Metadata.Usage.ContainsPersonRelatedData;
            elasticArchiveRecord.Places = null;
            elasticArchiveRecord.ParentContentInfos =
                archiveRecord.Display.ArchiveplanContext.Select(c => new ElasticParentContentInfo { Title = c.Title }).ToList();
            elasticArchiveRecord.ArchiveplanContext = archiveRecord.Display.ArchiveplanContext.Select(a => new ElasticArchiveplanContextItem
            {
                ArchiveRecordId = a.ArchiveRecordId,
                DateRangeText = a.DateRangeText,
                Level = a.Level,
                IconId = a.IconId,
                RefCode = a.RefCode,
                Title = a.Title,
                Protected = a.Protected
            })
                .ToList();
            elasticArchiveRecord.FirstChildArchiveRecordId = archiveRecord.Display.FirstChildArchiveRecordId;
            elasticArchiveRecord.PreviousArchiveRecordId = archiveRecord.Display.PreviousArchiveRecordId;
            elasticArchiveRecord.NextArchiveRecordId = archiveRecord.Display.NextArchiveRecordId;
            elasticArchiveRecord.ParentArchiveRecordId = archiveRecord.Metadata.NodeInfo.ParentArchiveRecordId;
            elasticArchiveRecord.ExternalDisplayTemplateName = archiveRecord.Display.ExternalDisplayTemplateName;
            elasticArchiveRecord.InternalDisplayTemplateName = archiveRecord.Display.InternalDisplayTemplateName;
            elasticArchiveRecord.LastSyncDate = DateTime.Now;
            elasticArchiveRecord.AggregationFields = new ElasticAggregationFields
            {
                Bestand = archiveRecord.Display.ArchiveplanContext
                    .LastOrDefault(a => a.Level.Equals(levelIdentifier, StringComparison.InvariantCultureIgnoreCase))?.Title,
                Ordnungskomponenten = archiveRecord.Metadata.AggregationFields.FirstOrDefault(a => a.AggregationName == "FondsOverview")?.Values,
                HasPrimaryData = !string.IsNullOrEmpty(archiveRecord.Metadata.PrimaryDataLink) || ContainsCustomFieldDigitaleVersion(elasticArchiveRecord.CustomFields)
            };
            // Only save references to Elastic that don't need to by anonymized, 
            // Problem is, that when two protected UoD have a reference on each other and then
            // a user gets permission to view UoD #1, then he automatically sees the Title of UoD #2 for which
            // he does not have a permission. As this can't be solved, we are not storing references to protected UoD
            elasticArchiveRecord.References = archiveRecord.Metadata.References
                .Where(r => r.Protected == false)
                .Select(s => new ElasticReference
                {
                    ArchiveRecordId = s.ArchiveRecordId,
                    ReferenceName = s.ReferenceName,
                    Role = s.Role,
                    Protected = s.Protected,
                }).ToList();
            elasticArchiveRecord.Containers = archiveRecord.Metadata.Containers.Container.Select(s => new ElasticContainer
            {
                ContainerLocation = s.ContainerLocation,
                ContainerType = s.ContainerType,
                IdName = s.IdName,
                ContainerCode = s.ContainerCode,
                ContainerCarrierMaterial = s.ContainerCarrierMaterial
            })
                .ToList();
            elasticArchiveRecord.Descriptors = archiveRecord.Metadata.Descriptors.Select(s => new ElasticDescriptor
            {
                Description = s.Description,
                Function = s.Function,
                IdName = s.IdName,
                OtherLanguageNames = s.OtherLanguageNames,
                Name = s.Name,
                SeeAlso = s.SeeAlso,
                Source = s.Source,
                Thesaurus = s.Thesaurus
            })
                .ToList();

            // If we receive data in the ElasticPrimaryData field, then we use this field. If not, then the PrimaryData field is used
            elasticArchiveRecord.PrimaryData = archiveRecord.ElasticPrimaryData != null && archiveRecord.ElasticPrimaryData.Any()
                ? archiveRecord.ElasticPrimaryData
                : archiveRecord.PrimaryData.ToElasticArchiveRecordPackage();

            TransferDataFromPropertyBag(elasticArchiveRecord, archiveRecord.Metadata.DetailData);

            // Add the creation period aggregation records
            // https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-bucket-histogram-aggregation.html
            // According to elastic documentation histograms are calculated with this formula
            // bucket_key = Math.floor((value - offset) / interval) * interval + offset
            CalculateCreationPeriodBuckets(elasticArchiveRecord);

            return elasticArchiveRecord;
        }

        /// <summary>
        /// Check if this record has a ManuelleKorrektur and apply those if required.
        /// Also update titles in Archivplans and ParentContentInfos of children and References 
        /// </summary>
        /// <param name="elasticArchiveRecord"></param>
        /// <returns>The eventually updated record</returns>
        public ElasticArchiveDbRecord SyncAnonymizedTextsWithRelatedRecords(ElasticArchiveDbRecord elasticArchiveRecord)
        {
            if (int.TryParse(elasticArchiveRecord.ArchiveRecordId, out int veId))
            {
                // Check if there exists a manual correction of anonymized texts
                var manuelleKorrektur = dbManuelleKorrekturAccess.GetManuelleKorrektur(mk => mk.VeId == veId).Result;
                if (manuelleKorrektur != null)
                {
                    var result = SyncRecordWithManuelleKorrektur(manuelleKorrektur, elasticArchiveRecord);
                    if (result.HasChanges)
                    {
                        dbManuelleKorrekturAccess.InsertOrUpdateManuelleKorrektur(manuelleKorrektur, "System");
                    }

                    elasticArchiveRecord.IsAnonymized = result.IsAnonymized;

                    if (result.CanSkipTitleUpdate)
                    {
                        return elasticArchiveRecord;
                    }
                }

                anonymizationReferenceEngine.UpdateSelf(elasticArchiveRecord);
                anonymizationReferenceEngine.UpdateDependentRecords(elasticArchiveRecord);
            }

            return elasticArchiveRecord;
        }

        public void UpdateDependentRecords(string archiveRecordId)
        {
            var dbRecord = dbAccess.FindDbDocument(archiveRecordId, false);
            anonymizationReferenceEngine.UpdateDependentRecords(dbRecord);
        }

        public void UpdateReferencesOfUnprotectedRecord(string archiveRecordId)
        {
            var dbRecord = dbAccess.FindDbDocument(archiveRecordId, false);
            anonymizationReferenceEngine.UpdateReferencesOfUnprotectedRecord(dbRecord);
        }

        public void DeletePossiblyExistingManuelleKorrektur(ElasticArchiveRecord elasticArchiveRecord)
        {
            // Es existieren nur FieldAccessTokens, wenn der Datensatz in Schutzfrist ist und gesperrt ist
            if (!elasticArchiveRecord.FieldAccessTokens.Any())
            {
                if (int.TryParse(elasticArchiveRecord.ArchiveRecordId, out int veId))
                {
                    var manuelleKorrektur = dbManuelleKorrekturAccess.GetManuelleKorrektur(mk => mk.VeId == veId).Result;
                    if (manuelleKorrektur != null)
                    {
                        dbManuelleKorrekturAccess.DeleteManuelleKorrektur(manuelleKorrektur.ManuelleKorrekturId);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the Period how create the Record
        /// </summary>
        /// <param name="elasticArchiveRecord"></param>
        public static void CalculateCreationPeriodBuckets(ElasticArchiveRecord elasticArchiveRecord)
        {
            if (elasticArchiveRecord.CreationPeriod != null && elasticArchiveRecord.CreationPeriod.Years.Any())
            {
                elasticArchiveRecord.AggregationFields.CreationPeriodYears001 =
                    elasticArchiveRecord.CreationPeriod.Years;
                elasticArchiveRecord.AggregationFields.CreationPeriodYears005 =
                    elasticArchiveRecord.CreationPeriod.Years.Select(year => (int) Math.Floor(year / 5m) * 5).Distinct().ToList();
                elasticArchiveRecord.AggregationFields.CreationPeriodYears010 =
                    elasticArchiveRecord.CreationPeriod.Years.Select(year => (int) Math.Floor(year / 10m) * 10).Distinct().ToList();
                elasticArchiveRecord.AggregationFields.CreationPeriodYears025 =
                    elasticArchiveRecord.CreationPeriod.Years.Select(year => (int) Math.Floor(year / 25m) * 25).Distinct().ToList();
                elasticArchiveRecord.AggregationFields.CreationPeriodYears100 =
                    elasticArchiveRecord.CreationPeriod.Years.Select(year => (int) Math.Floor(year / 100m) * 100).Distinct().ToList();
            }
        }

        public void TransferDataFromPropertyBag(ElasticArchiveRecord elasticArchiveRecord, List<DataElement> detailData)
        {
            var customFields = new ExpandoObject() as IDictionary<string, object>;

            var defaultProperties = elasticArchiveRecord.GetType().GetProperties();

            foreach (var fieldConfiguration in fieldsConfiguration.Fields)
            {
                try
                {
                    var value = GetValue(detailData, fieldConfiguration);

                    if ((fieldConfiguration.TargetField == "ReferenceCode" || fieldConfiguration.TargetField == "Title") &&
                        string.IsNullOrWhiteSpace(value as string))
                    {
                        value = "Keine Angabe";
                    }

                    if (value != null)
                    {
                        if (fieldConfiguration.IsDefaultField)
                        {
                            var prop = defaultProperties.FirstOrDefault(d =>
                                d.Name.ToLowerInvariant() == fieldConfiguration.TargetField.ToLowerInvariant());
                            if (prop != null)
                            {
                                prop.SetValue(elasticArchiveRecord, value);
                            }
                        }
                        else
                        {
                            // ToLowerCamelCase is important here, because when fetching the ElasticArchivRecord from 
                            // the elastic DB we have CamelCase style. If we do not do it here as well
                            // we have problems when anonymizing customFields
                            customFields.Add(fieldConfiguration.TargetField.ToLowerCamelCase(), value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }

            // Add the customer fields to the "main" record
            elasticArchiveRecord.CustomFields = customFields;
        }

        private object GetValue(List<DataElement> detailData, FieldConfiguration fieldConfiguration)
        {
            object retVal = null;
            try
            {
                switch (fieldConfiguration.Type)
                {
                    case ElasticFieldTypes.TypeString:
                        var textExtractor = new TextExtractor();
                        retVal = GetValue(detailData, fieldConfiguration, textExtractor);
                        break;
                    case ElasticFieldTypes.TypeTimePeriod:
                        var timePeriodExtractor = new TimePeriodExtractor();
                        retVal = GetValue(detailData, fieldConfiguration, timePeriodExtractor);
                        break;
                    case ElasticFieldTypes.TypeDateWithYear:
                        var dateWithYearExtractor = new DateWithYearExtractor();
                        retVal = GetValue(detailData, fieldConfiguration, dateWithYearExtractor);
                        break;
                    case ElasticFieldTypes.TypeBase64:
                        var elasticBase64Extractor = new Base64Extractor();
                        retVal = GetValue(detailData, fieldConfiguration, elasticBase64Extractor);
                        break;
                    case ElasticFieldTypes.TypeInt + "?":
                    case ElasticFieldTypes.TypeInt:
                        var intExtractor = new IntExtractor();
                        retVal = GetValue(detailData, fieldConfiguration, intExtractor);
                        break;
                    case ElasticFieldTypes.TypeBool + "?":
                    case ElasticFieldTypes.TypeBool:
                        var boolExtractor = new BoolExtractor();
                        retVal = GetValue(detailData, fieldConfiguration, boolExtractor);
                        break;
                    case ElasticFieldTypes.TypeFloat + "?":
                    case ElasticFieldTypes.TypeFloat:
                        var floatExtractor = new FloatExtractor();
                        retVal = GetValue(detailData, fieldConfiguration, floatExtractor);
                        break;
                    case ElasticFieldTypes.TypeHyperlink:
                        var hyperlinkExtractor = new HyperlinkExtractor();
                        retVal = GetValue(detailData, fieldConfiguration, hyperlinkExtractor);
                        break;
                    case ElasticFieldTypes.TypeEntityLink:
                        var entityLinkExtractor = new EntityLinkExtractor();
                        retVal = GetValue(detailData, fieldConfiguration, entityLinkExtractor);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                throw;
            }

            return retVal;
        }

        private static object GetValue<T>(List<DataElement> detailData, FieldConfiguration fieldConfiguration, ExtractorBase<T> extractor)
        {
            object retVal;
            // The scope type memo is special in that text can be spread over multiple fields in 4000 char chunks. Thus we use the simple GetValue method
            // that stiches the data together in one field.
            if (detailData.First().ElementType == DataElementElementType.memo)
            {
                retVal = extractor.GetValue(detailData, fieldConfiguration.ElementId);
            }
            else
            {
                retVal = fieldConfiguration.IsRepeatable
                    ? extractor.GetListValues(detailData, fieldConfiguration.ElementId)
                    : extractor.GetValue(detailData, fieldConfiguration.ElementId);
            }

            if (fieldConfiguration.IsDefaultField && retVal is List<string>)
            {
                retVal = string.Join(Environment.NewLine, (List<string>) retVal);
            }

            return retVal;
        }

        private static bool ContainsCustomFieldDigitaleVersion(dynamic customFields)
        {
            if (customFields is Dictionary<string, object> && ((IDictionary<string, object>) customFields).ContainsKey("digitaleVersion"))
            {
                var field =
                    ((IDictionary<string, object>) customFields).FirstOrDefault(k =>
                         k.Key.Equals("digitaleVersion", StringComparison.InvariantCultureIgnoreCase));
                if (field.Value != null && !string.IsNullOrWhiteSpace(field.Value.ToString()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the newly synced record has different texts than the existing corrections.
        /// 1. If the automatic anonymized text from the service has changed, copy the new value to the automatic field
        ///    and indicate that there are changes
        /// 2. If the original value from the AIS has changed, we can later skip the update of references in case of the title
        ///    and as well indicate that there are changes
        /// </summary>
        /// <param name="manuelleKorrektur"></param>
        /// <param name="archiveRecord"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private ManuelleKorrekturSyncResult SyncRecordWithManuelleKorrektur(ManuelleKorrekturDto manuelleKorrektur, ElasticArchiveDbRecord archiveRecord)
        {
            var result = new ManuelleKorrekturSyncResult
            {
                HasChanges = false,
                CanSkipTitleUpdate = false,
                IsAnonymized = archiveRecord.IsAnonymized

            };
            var isPublished = manuelleKorrektur.Anonymisierungsstatus == (int) AnonymisierungsStatusEnum.Published;
            foreach (var feld in manuelleKorrektur.ManuelleKorrekturFelder)
            {
                switch (feld.Feldname)
                {
                    case ManuelleKorrekturFelder.Titel:
                        // Check if the automatic synced text is different than before
                        // If yes, we need to sync the texts, but do not need to update the
                        if (feld.Automatisch != archiveRecord.Title)
                        {
                            result.HasChanges = true;
                            feld.Automatisch = archiveRecord.Title;
                        }

                        // Check if the original text from the db is different
                        // If yes, we have to mark the manual correction as "check required"
                        if (feld.Original != archiveRecord.UnanonymizedFields.Title)
                        {
                            result.CanSkipTitleUpdate = true;
                            result.HasChanges = true;
                            feld.Original = archiveRecord.UnanonymizedFields.Title;
                            SetCheckRequired(manuelleKorrektur, feld);
                        }
                        else switch (string.IsNullOrWhiteSpace(feld.Manuell))
                        {
                            case false when isPublished && archiveRecord.Title == feld.Manuell:
                                // Der gleiche Titel ist schon gesetzt
                                result.CanSkipTitleUpdate = true;
                                break;
                            case false when isPublished && archiveRecord.Title != feld.Manuell:
                                archiveRecord.Title = feld.Manuell;
                                break;
                        }
                        break;
                    case ManuelleKorrekturFelder.Darin:
                        if (feld.Automatisch != archiveRecord.WithinInfo)
                        {
                            result.HasChanges = true;
                            feld.Automatisch = archiveRecord.WithinInfo;
                        }

                        if (feld.Original != archiveRecord.UnanonymizedFields.WithinInfo)
                        {
                            result.HasChanges = true;
                            feld.Original = archiveRecord.UnanonymizedFields.WithinInfo;
                            SetCheckRequired(manuelleKorrektur, feld);
                        }
                        else if (!string.IsNullOrWhiteSpace(feld.Manuell) && isPublished)
                        {
                            archiveRecord.WithinInfo = feld.Manuell;
                        }
                        break;
                    case ManuelleKorrekturFelder.VerwandteVe:
                        if (feld.Automatisch != archiveRecord.VerwandteVe())
                        {
                            result.HasChanges = true;
                            feld.Automatisch = archiveRecord.VerwandteVe();
                        }

                        if (feld.Original != archiveRecord.UnanonymizedFields.VerwandteVe)
                        {
                            result.HasChanges = true;
                            feld.Original = archiveRecord.UnanonymizedFields.VerwandteVe;
                            SetCheckRequired(manuelleKorrektur, feld);
                        }
                        else if (!string.IsNullOrWhiteSpace(feld.Manuell) && isPublished)
                        {
                            archiveRecord.SetAnonymizeVerwandteVe(feld.Manuell);
                        }
                        break;
                    case ManuelleKorrekturFelder.ZusatzkomponenteZac1:
                        if (feld.Automatisch != archiveRecord.Zusatzmerkmal())
                        {
                            result.HasChanges = true;
                            feld.Automatisch = archiveRecord.Zusatzmerkmal();
                        }

                        if (feld.Original != archiveRecord.UnanonymizedFields.ZusatzkomponenteZac1)
                        {
                            result.HasChanges = true;
                            feld.Original = archiveRecord.UnanonymizedFields.ZusatzkomponenteZac1;
                            SetCheckRequired(manuelleKorrektur, feld);
                        }
                        else if (!string.IsNullOrWhiteSpace(feld.Manuell) && isPublished)
                        {
                            archiveRecord.SetAnonymizeZusatzmerkmal(feld.Manuell);
                        }
                        break;
                    case ManuelleKorrekturFelder.BemerkungZurVe:
                        if (feld.Automatisch != archiveRecord.ZusätzlicheInformationen())
                        {
                            result.HasChanges = true;
                            feld.Automatisch = archiveRecord.ZusätzlicheInformationen();
                        }

                        if (feld.Original != archiveRecord.UnanonymizedFields.BemerkungZurVe)
                        {
                            result.HasChanges = true;
                            feld.Original = archiveRecord.UnanonymizedFields.BemerkungZurVe;
                            SetCheckRequired(manuelleKorrektur, feld);
                        }
                        else if (!string.IsNullOrWhiteSpace(feld.Manuell) && isPublished)
                        {
                            archiveRecord.SetAnonymizeZusätzlicheInformationen(feld.Manuell);
                        }
                        break;
                    default:
                        throw new ArgumentException($"ManuelleKorrekturFelder {feld.Feldname} ist unbekannt");
                }
            }

            // Check if any of the fields are anonymized
            result.IsAnonymized = ContainsAnonymizationToken(archiveRecord.Title) ||
                                  ContainsAnonymizationToken(archiveRecord.WithinInfo) ||
                                  ContainsAnonymizationToken(archiveRecord.VerwandteVe()) ||
                                  ContainsAnonymizationToken(archiveRecord.Zusatzmerkmal()) ||
                                  ContainsAnonymizationToken(archiveRecord.ZusätzlicheInformationen());

            return result;
        }

        private bool ContainsAnonymizationToken(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            return input.Contains("███");
        }

        private static void SetCheckRequired(ManuelleKorrekturDto manuelleKorrektur, ManuelleKorrekturFeldDto feld)
        {
            manuelleKorrektur.Anonymisierungsstatus = (int) AnonymisierungsStatusEnum.CheckRequired;
            if (!string.IsNullOrWhiteSpace(feld.Manuell))
            {
                feld.Manuell = "=== Überprüfung erforderlich ===" + Environment.NewLine + feld.Manuell;
            }
        }
    }
}
