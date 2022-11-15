using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Harvest.ScopeArchiv.DataSets;
using CMI.Contract.Common;
using Serilog;

namespace CMI.Access.Harvest.ScopeArchiv
{
    /// <summary>
    ///     The archive record builder creates an <see cref="ArchiveRecord" /> from the metadata in the AIS.
    /// </summary>
    public class ArchiveRecordBuilder
    {
        private readonly ApplicationSettings applicationSettings;
        private readonly IAISDataProvider dataProvider;
        private readonly LanguageSettings languageSettings;
        private readonly CachedLookupData lookupData;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ArchiveRecordBuilder" /> class.
        /// </summary>
        /// <param name="dataProvider">The data provider.</param>
        /// <param name="languageSettings">The language settings.</param>
        /// <param name="applicationSettings">The application settings.</param>
        /// <param name="lookupData">The lookup data.</param>
        public ArchiveRecordBuilder(IAISDataProvider dataProvider, LanguageSettings languageSettings, ApplicationSettings applicationSettings,
            CachedLookupData lookupData)
        {
            this.dataProvider = dataProvider;
            this.languageSettings = languageSettings;
            this.applicationSettings = applicationSettings;
            this.lookupData = lookupData;
        }

        /// <summary>
        ///     Builds the specified archive record.
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <returns>ArchiveRecord if found, or null if no record with that id can be found in the database</returns>
        public virtual ArchiveRecord Build(string archiveRecordId)
        {
            var sw = new Stopwatch();
            sw.Start();

            // Get some base information about the record that is used in several places
            var recordRow = dataProvider.GetArchiveRecordRow(Convert.ToInt64(archiveRecordId));

            // If we don't receive a record, it does not exist.
            if (recordRow == null)
            {
                return null;
            }

            var ar = new ArchiveRecord
            {
                ArchiveRecordId = archiveRecordId,
                Metadata = LoadMetadata(archiveRecordId, recordRow),
                Security = LoadSecurityDetails(archiveRecordId)
            };
            ar.Display = LoadDisplayData(archiveRecordId, ar.Metadata, recordRow);
            sw.Stop();

            Log.Information("Took {Time}ms to build ArchiveRecord for id {Id}", sw.ElapsedMilliseconds, archiveRecordId);
            return ar;
        }

        /// <summary>
        ///     Loads the security details.
        /// </summary>
        /// <param name="recordId">The archive record identifier.</param>
        /// <returns>ArchiveRecordSecurity.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private ArchiveRecordSecurity LoadSecurityDetails(string recordId)
        {
            var security = new ArchiveRecordSecurity();
            try
            {
                var tMetadataSecurity = Task.Factory.StartNew(() =>
                {
                    var securityTokens = dataProvider.LoadMetadataSecurityTokens(Convert.ToInt64(recordId));
                    security.MetadataAccessToken = securityTokens;
                });

                var tFieldSecurity = Task.Factory.StartNew(() =>
                {
                    var fieldTokens = dataProvider.LoadFieldSecurityTokens(Convert.ToInt64(recordId));
                    security.FieldAccessToken = fieldTokens;
                });

                var tPrimaryDataSecurity = Task.Factory.StartNew(() =>
                {
                    var securityTokens = dataProvider.LoadPrimaryDataSecurityTokens(Convert.ToInt64(recordId));
                    if (securityTokens.DownloadAccessTokens.Any())
                    {
                        security.PrimaryDataDownloadAccessToken = securityTokens.DownloadAccessTokens;
                    }

                    if (securityTokens.FulltextAccessTokens.Any())
                    {
                        security.PrimaryDataFulltextAccessToken = securityTokens.FulltextAccessTokens;
                    }
                });


                Task.WaitAll(tMetadataSecurity, tPrimaryDataSecurity, tFieldSecurity);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load the security information for record {RecordId}", recordId);
                throw;
            }

            return security;
        }

        /// <summary>
        ///     Loads the display data.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <param name="metadata">The already existing metadata.</param>
        /// <param name="recordRow">The record row with information about the archive record.</param>
        /// <returns>ArchiveRecordDisplay.</returns>
        private ArchiveRecordDisplay LoadDisplayData(string recordId, ArchiveRecordMetadata metadata, ArchiveRecordDataSet.ArchiveRecordRow recordRow)
        {
            var display = new ArchiveRecordDisplay
            {
                ExternalDisplayTemplateName = $"{recordRow.ANST_FRMLR_ID}: {recordRow.ANST_FRMLR_NM}",
                InternalDisplayTemplateName = $"{recordRow.BRBTG_FRMLR_ID}: {recordRow.BRBTG_FRMLR_NM}"
            };

            try
            {
                var tNodeContext = Task.Factory.StartNew(() =>
                {
                    var context = dataProvider.LoadNodeContext(Convert.ToInt64(recordId));
                    display.FirstChildArchiveRecordId = context.FirstChildArchiveRecordId;
                    display.NextArchiveRecordId = context.NextArchiveRecordId;
                    display.PreviousArchiveRecordId = context.PreviousArchiveRecordId;
                    display.ParentArchiveRecordId = context.ParentArchiveRecordId;
                });
                var tMetaInfo = Task.Factory.StartNew(() =>
                {
                    display.ContainsImages = metadata.DetailData.Any(d => d.ElementType == DataElementElementType.image);
                    display.ContainsMedia = metadata.DetailData.Any(d => d.ElementType == DataElementElementType.media);
                    // In scopeArchiv the Levels (Stufen) have an attribute called "Bestellbar". We now check this on the 
                    // Unit of Description (VRZNG_ENHT_BSTLG_IND) PLUS the additional rule, that there must be containers.
                    // PVW-1071: Nicht bestellbar, wenn physische Bestellbarkeit nicht gegeben ist, d.h. Bestellbarkeit != "Uneingeschränkt"
                    display.CanBeOrdered = metadata.Containers.NumberOfContainers > 0 && recordRow.VRZNG_ENHT_BSTLG_IND != 0 &&
                                           recordRow.VRZNG_ENHT_BNTZB_ID == 1;
                });
                var tArchiveplanContext = Task.Factory.StartNew(() =>
                {
                    display.ArchiveplanContext = LoadArchivePlanContext(Convert.ToInt64(recordId), metadata);
                });

                Task.WaitAll(tNodeContext, tMetaInfo, tArchiveplanContext);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load the display information for record {RecordId}", recordId);
                throw;
            }

            return display;
        }

        /// <summary>
        ///     Loads the metadata.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <param name="recordRow">The record row with base information about the archive record.</param>
        /// <returns>ArchiveRecordMetadata.</returns>
        public ArchiveRecordMetadata LoadMetadata(string recordId, ArchiveRecordDataSet.ArchiveRecordRow recordRow)
        {
            var retVal = new ArchiveRecordMetadata();

            try
            {
                var tDetailData = Task.Factory.StartNew(() =>
                {
                    retVal.DetailData = LoadDataElements(Convert.ToInt64(recordId));
                    // Get the accession year from the reserved data element with id 505
                    var accessionDataElement =
                        retVal.DetailData.FirstOrDefault(d => d.ElementId == ((int) ScopeArchivDatenElementId.AblieferungLink).ToString());
                    if (accessionDataElement != null && accessionDataElement.ElementValue.Any())
                    {
                        var textValue = accessionDataElement.ElementValue.First().TextValues.First().Value;
                        // the year is indicated in the first 4 digits
                        int year;
                        int.TryParse(textValue.Substring(0, 4), out year);
                        retVal.AccessionDate = year;
                    }

                    // Get the digital repository identifier
                    var repositoryDataElement =
                        retVal.DetailData.FirstOrDefault(d => d.ElementId == applicationSettings.DigitalRepositoryElementIdentifier);
                    if (repositoryDataElement != null && repositoryDataElement.ElementValue.Any())
                    {
                        var textValue = repositoryDataElement.ElementValue.First().TextValues.First().Value;
                        retVal.PrimaryDataLink = textValue;
                    }
                });
                var tArchiveData = Task.Factory.StartNew(() => { retVal.Usage = ExtractUsageData(recordRow); });
                var tNodeInfo = Task.Factory.StartNew(() => { retVal.NodeInfo = LoadNodeInfo(Convert.ToInt64(recordId)); });
                var tContainer = Task.Factory.StartNew(() => { retVal.Containers = LoadContainers(Convert.ToInt64(recordId)); });
                var tDescriptor = Task.Factory.StartNew(() => { retVal.Descriptors = LoadDescriptors(Convert.ToInt64(recordId)); });
                var tReference = Task.Factory.StartNew(() => { retVal.References = LoadReferences(Convert.ToInt64(recordId)); });
                var tAggregationData = Task.Factory.StartNew(() => { retVal.AggregationFields.AddRange(LoadAggregation(recordRow)); });

                Task.WaitAll(tDetailData, tArchiveData, tNodeInfo, tContainer, tDescriptor, tReference, tAggregationData);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load the metadata for record {RecordId}", recordId);
                throw;
            }

            return retVal;
        }

        /// <summary>
        ///     Loads the archive plan context.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>List&lt;ArchiveplanContextItem&gt;.</returns>
        private List<ArchiveplanContextItem> LoadArchivePlanContext(long recordId, ArchiveRecordMetadata metadata)
        {
            var retVal = new List<ArchiveplanContextItem>();

            try
            {
                var path = metadata.NodeInfo.Path;
                // in scopeArchiv the path consists of the concatenated id's all all id's where
                // each id is padded to 10 digits
                var elements = Enumerable.Range(0, path.Length / 10).Select(i => Convert.ToInt64(path.Substring(i * 10, 10))).ToArray();

                var ds = dataProvider.LoadArchivePlanInfo(elements);

                foreach (var elementId in elements)
                {
                    var row = ds.ArchivePlanItem.FirstOrDefault(e => e.VRZNG_ENHT_ID == elementId);
                    if (row != null)
                    {
                        retVal.Add(new ArchiveplanContextItem
                        {
                            ArchiveRecordId = ((int) row.VRZNG_ENHT_ID).ToString(),
                            Level = row.ENTRG_TYP_NM,
                            DateRangeText = row.ZT_RAUM_TXT,
                            IconId = (int) row.ICON_ID,
                            RefCode = row.SGNTR_CD,
                            Title = row.VRZNG_ENHT_TITEL,
                            Protected = row.PRTCTD != 0
                        });
                    }
                    else
                        // Code should never run here. But during tests we had the situation where the archive plan path contained
                        // ids to records that were not in the database. These items we flag simply as unknown.
                    {
                        retVal.Add(new ArchiveplanContextItem
                        {
                            ArchiveRecordId = "-1",
                            Level = "?",
                            DateRangeText = "?",
                            IconId = -1,
                            RefCode = "?",
                            Title = "?"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load the archive plan context for record {RecordId}", recordId);
                throw;
            }

            return retVal;
        }

        /// <summary>
        ///     Loads the node information.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>NodeInfo.</returns>
        private NodeInfo LoadNodeInfo(long recordId)
        {
            var retVal = new NodeInfo();

            try
            {
                var ds = dataProvider.LoadNodeInfo(recordId);

                Debug.Assert(ds.NodeInfo.Count == 1, "Must receive exactly one record");
                var row = ds.NodeInfo.First();

                retVal.ChildCount = (int) row.ANZ_KNDR;
                retVal.IsLeaf = row.ANZ_KNDR == 0;
                retVal.IsRoot = row.IST_ROOT != 0;
                retVal.Level = row.HRCH_PFAD.Length / 10;
                retVal.ParentArchiveRecordId =
                    row.IsVATER_IDNull() ? "-1" : ((int) row.VATER_ID).ToString(); // By convention return -1 if no parent present
                retVal.Path = row.HRCH_PFAD;
                retVal.Sequence = (int) row.ZWEIG_POS;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load the node information for record {RecordId}", recordId);
                throw;
            }

            return retVal;
        }

        /// <summary>
        ///     Loads the references.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>List&lt;ArchiveRecordMetadataReference&gt;.</returns>
        private List<ArchiveRecordMetadataReference> LoadReferences(long recordId)
        {
            var retVal = new List<ArchiveRecordMetadataReference>();

            try
            {
                var ds = dataProvider.LoadReferences(recordId);
                foreach (var row in ds.References)
                {
                    retVal.Add(new ArchiveRecordMetadataReference
                    {
                        ArchiveRecordId = ((int)row.GSFT_OBJ_ID).ToString(),
                        ReferenceName = row.GSFT_OBJ_KURZ_NM,
                        Role = row.GSFT_OBJ_BZHNG_ROLLE_NM,
                        Protected = row.PRTCTD != 0
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load the references for record {RecordId}", recordId);
                throw;
            }

            return retVal;
        }

        /// <summary>
        ///     Loads the descriptors.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>List&lt;Descriptor&gt;.</returns>
        private List<Descriptor> LoadDescriptors(long recordId)
        {
            var retVal = new List<Descriptor>();

            try
            {
                var dsDescriptors = dataProvider.LoadDescriptors(recordId);
                foreach (var row in dsDescriptors.Descriptor)
                {
                    retVal.Add(new Descriptor
                    {
                        Name = row.DSKRP_NM,
                        Thesaurus = row.THSRS_NM,
                        Source = row.DSKRP_QLL_TXT,
                        SeeAlso = row.DSKRP_SIEHE_AUCH_LISTE.Split(';').Select(i => i.Trim()).ToList(),
                        Function = row.GSFT_OBJ_BZHNG_ROLLE_NM,
                        IdName = row.GSFT_OBJ_KURZ_NM,
                        Description = row.DSKRP_BSR,
                        OtherLanguageNames = row.DSKRP_FREMD_SPR_NM
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load the descriptors for record {RecordId}", recordId);
                throw;
            }

            return retVal;
        }

        /// <summary>
        ///     Loads the containers.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>ArchiveRecordMetadataContainers.</returns>
        private ArchiveRecordMetadataContainers LoadContainers(long recordId)
        {
            var retVal = new ArchiveRecordMetadataContainers();

            try
            {
                var dsContainers = dataProvider.LoadContainers(recordId);
                retVal.NumberOfContainers = dsContainers.StorageContainer.Count;

                foreach (var row in dsContainers.StorageContainer)
                {
                    retVal.Container.Add(new ArchiveRecordMetadataContainersContainer
                    {
                        ContainerLocation = row.BHLTN_DEF_STAND_ORT_CD,
                        ContainerType = row.BHLTN_TYP_NM,
                        IdName = row.GSFT_OBJ_KURZ_NM,
                        ContainerCode = row.BHLTN_CD,
                        ContainerCarrierMaterial = row.BHLTN_INFO_TRGR_NM
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load the containers for record {RecordId}", recordId);
                throw;
            }

            return retVal;
        }

        /// <summary>
        ///     Extracts the usage data from the archive record row.
        /// </summary>
        /// <param name="recordRow">The archive record row</param>
        /// <returns>ArchiveRecordMetadataUsage.</returns>
        private ArchiveRecordMetadataUsage ExtractUsageData(ArchiveRecordDataSet.ArchiveRecordRow recordRow)
        {
            var retVal = new ArchiveRecordMetadataUsage();
            try
            {
                if (recordRow != null)
                {
                    retVal.AlwaysVisibleOnline = recordRow.SUCH_FRGB_IND != 0;
                    retVal.IsPhysicalyUsable = recordRow.VRZNG_ENHT_BNTZB_ID == 1;
                    retVal.ContainsPersonRelatedData = recordRow.SCHTZ_PRSN_IND != 0;
                    retVal.ProtectionCategory = recordRow.VRZNG_ENHT_INHLT_NM;
                    retVal.ProtectionBaseDate = recordRow.VRZNG_ENHT_SCHTZ_FRIST_NM;
                    retVal.ProtectionDuration = (int) recordRow.SCHTZ_FRIST_DAUER;
                    retVal.ProtectionEndDate = recordRow.IsSCHTZ_FRIST_BIS_DTNull() ? null : recordRow.SCHTZ_FRIST_BIS_DT;
                    retVal.CannotFallBelow = recordRow.SCHTZ_FRIST_MIN_IND != 0;
                    retVal.AdjustedManually = recordRow.SCHTZ_FRIST_MTTN_IND != 0;
                    retVal.ProtectionNote = recordRow.SCHTZ_FRIST_NTZ;
                    retVal.Permission = recordRow.VRZNG_ENHT_BWLG_TYP_NM;
                    retVal.PhysicalUsability = recordRow.VRZNG_ENHT_BNTZB_NM;
                    retVal.Accessibility = recordRow.VRZNG_ENHT_ZGNGL_NM;
                    retVal.UsageNotes = recordRow.BNTZG_HNWS_TXT;
                    retVal.License = ArchiveRecordMetadataUsageLicense.Undefined; // property is eventually for later use. Just use undefined for now
                }
            }
            catch (Exception ex)
            {
                if (recordRow != null)
                {
                    Log.Error(ex, "Usage data load failed for record: {VRZNG_ENHT_ID}", recordRow.VRZNG_ENHT_ID);
                }

                throw;
            }

            return retVal;
        }

        /// <summary>
        ///     Loads the data elements.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>List&lt;DataElement&gt;.</returns>
        private List<DataElement> LoadDataElements(long recordId)
        {
            var retVal = new List<DataElement>();

            try
            {
                // Load all the data for building up the data elements collection
                var ds = dataProvider.LoadDetailData(recordId);

                var lastDatenElementId = 0;

                foreach (var row in ds.DetailData.OrderBy(d => d.DATEN_ELMNT_ID))
                {
                    if (lastDatenElementId != (int) row.DATEN_ELMNT_ID)
                    {
                        var element = new DataElement
                        {
                            // All the attributes
                            ElementId = ((int) row.DATEN_ELMNT_ID).ToString(),
                            ElementName = row.XML_CD,
                            EadCode = row.EAD_CD,
                            ElementType = MapperHelper.MapDataElementType((ScopeArchivDatenElementTyp) (int) row.DATEN_ELMNT_TYP_ID),
                            IncludeInFullTextIndex = (int) row.VOLL_TXT_SRCHBL_IND != 0,
                            Visibility = (int) row.ZGRF_BRTG_STUFE_ID == 2 ? DataElementVisibility.@public : DataElementVisibility.@internal
                        };
                        // Get the actual value(s) for the data element
                        element.ElementValue = GetValues(ds, (int) row.DATEN_ELMNT_ID, element.ElementType);
                        retVal.Add(element);

                        lastDatenElementId = (int) row.DATEN_ELMNT_ID;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load detail elements for record {RecordId}", recordId);
                throw;
            }


            return retVal;
        }

        /// <summary>
        ///     Helper method to return the values for a specifc data element.
        /// </summary>
        /// <param name="dataSet">The data set.</param>
        /// <param name="datenElementId">The daten element identifier.</param>
        /// <param name="elementType">Type of the data element.</param>
        /// <returns>List&lt;DataElementElementValue&gt;.</returns>
        private List<DataElementElementValue> GetValues(DetailDataDataSet dataSet, int datenElementId, DataElementElementType elementType)
        {
            var retVal = new List<DataElementElementValue>();
            var dataElementList = dataSet.DetailData.Where(d => d.DATEN_ELMNT_ID == datenElementId);
            foreach (var row in dataElementList)
            {
                DataElementElementValue value;

                // Create new value if it is not a memo field, or it is the first element in the sequenze
                if (elementType != DataElementElementType.memo || row.ELMNT_SQNZ_NR == 1)
                {
                    value = new DataElementElementValue();
                    retVal.Add(value);
                }
                else
                {
                    value = retVal.Last();
                }

                DataElementHelper.FillDataElementElementValue(elementType, row, value, languageSettings);
            }

            return retVal;
        }

        private List<AggregationField> LoadAggregation(ArchiveRecordDataSet.ArchiveRecordRow recordRow)
        {
            var retVal = new List<AggregationField>();

            try
            {
                // Load the fonds aggregation 
                var fondsAggregation = new AggregationField
                {
                    AggregationName = "FondsOverview",
                    Values = GetFondLinkValues(recordRow.HRCH_PFAD)
                };
                retVal.Add(fondsAggregation);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unexpected error while extracting aggregation values. Error message is {ex.Message}");
            }

            // Return the result
            return retVal;
        }


        /// <summary>
        ///     Gets the fond link values, by looking up matching
        ///     values in the fondsOverviewCache.
        /// </summary>
        /// <param name="hrchPfad">The hierarchy path.</param>
        /// <returns>System.String.</returns>
        private List<string> GetFondLinkValues(string hrchPfad)
        {
            if (string.IsNullOrEmpty(hrchPfad))
            {
                return new List<string>();
            }

            return lookupData.FondsOverview
                .Where(l => l.HierarchyPath.Length <= hrchPfad.Length && hrchPfad.Substring(0, l.HierarchyPath.Length)
                    .Equals(l.HierarchyPath, StringComparison.InvariantCultureIgnoreCase))
                .Select(i => i.LinkName)
                .ToList();
        }
    }
}