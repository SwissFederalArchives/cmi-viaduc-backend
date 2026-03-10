using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Harvest.ActaPro.Mapping;
using CMI.Access.Harvest.ActaPro.Security;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using Serilog;

namespace CMI.Access.Harvest.ActaPro;

public class ActaProArchiveRecordBuilder : IArchiveRecordBuilder
{
    private readonly IAISDataProvider actaProDataProvider;
    private readonly ArchiveRecordMapper archiveRecordMapper;
    private readonly CachedLookupData lookupData;
    private readonly AccessTokenProvider accessTokenProvider;

    public ActaProArchiveRecordBuilder(IAISDataProvider actaProDataProvider, CachedLookupData lookupData)
    {
        this.actaProDataProvider = actaProDataProvider;
        this.lookupData = lookupData;
        archiveRecordMapper = new ArchiveRecordMapper();
        accessTokenProvider = new AccessTokenProvider();
    }

    public async Task<ArchiveRecord> Build(string archiveRecordId)
    {
        Log.Debug("Requesting document with ArchiveRecordId: {ArchiveRecordId}", archiveRecordId);

        var sw = new Stopwatch();
        sw.Start();

        try
        {
            var document = await actaProDataProvider.GetAisDataRecordAsync(archiveRecordId);
            if (document != null)
            {
                // Load the ancestors right away
                var ancestors = await actaProDataProvider.GetDocumentAncestorsDTO(document.DocKey);

                var childrenCount = await actaProDataProvider.GetDocumentChildrenCountAsync(document.DocKey);

                // Start creating the archive record
                var archiveRecord = new ArchiveRecord
                {
                    // The document key is the archive record ID, it may also have been read with the ScopeId, which is now no longer valid
                    ArchiveRecordId = document.DocKey,
                    Metadata = archiveRecordMapper.LoadMetadata(document, ancestors, childrenCount),
                    Security = LoadSecurityDetails(document, ancestors)
                };

                // The containers
                LoadContainers(document, archiveRecord);

                // The descriptors
                LoadDescriptors(document, archiveRecord);

                // The references
                await LoadVerweise(document, archiveRecord);

                // The display data
                archiveRecord.Display = LoadDisplayData(document, ancestors, archiveRecord.Metadata);

                // Teilbestand aggregation
                var path = ancestors.Document["path"].ToString();
                archiveRecord.Metadata.AggregationFields.AddRange(LoadFondsOverviewAggregation(path));

                // Do all the possible calculations
                DoCalculateFields(archiveRecord, document);

                return archiveRecord;
            }
        }
        catch (AisNotAvailableException ex)
        {
            Log.Error(ex, "ACTApro is not available");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{source} - HTTP request failed: {message} archiveRecordId: {archiveRecordId}", this, ex.Message, archiveRecordId);
        }
        finally
        {
            sw.Stop();
            Log.Information("Took {Time}ms to build ArchiveRecord for id {Id}", sw.ElapsedMilliseconds, archiveRecordId);
        }

        return null;
    }

    public async Task<ArchiveRecordSecurity> BuildSecurityTokens(string archiveRecordId)
    {
        var document = await actaProDataProvider.GetAisDataRecordAsync(archiveRecordId);
        if (document == null) return null;

        var ancestors = await actaProDataProvider.GetDocumentAncestorsDTO(document.DocKey);

        return this.LoadSecurityDetails(document, ancestors);
    }
    private void DoCalculateFields(ArchiveRecord archiveRecord, Document document)
    {

        // •	Stufe == «VZ» OR «Vor» OR «Dokum»
        // •	Anzahl Behältnis > 0 OR ID digitales Magazin != «»
        // •	Physische Benutzbarkeit == «Uneingeschränkt» 
        // As Containers or ID digitales Magazin can be linked only on the levels Dossier (VZ), Subdossier (Vor) and Dokument (Dokum)
        // We do not really need to check for the level.
        var actaProLevel = document != null && document.AdditionalProperties.ContainsKey("type") &&
            document.AdditionalProperties.TryGetValue("type", out var levelObject)
                ? levelObject.ToString()
                : "";

        archiveRecord.Display.CanBeOrdered = (
                                                 actaProLevel.Equals("Vz", StringComparison.InvariantCultureIgnoreCase) ||
                                                 actaProLevel.Equals("Vor", StringComparison.InvariantCultureIgnoreCase) ||
                                                 actaProLevel.Equals("Dokum", StringComparison.InvariantCultureIgnoreCase)
                                             ) &&
                                             (
                                                 archiveRecord.Metadata.Containers.NumberOfContainers > 0 ||
                                                 !string.IsNullOrEmpty(archiveRecord.Metadata.PrimaryDataLink)
                                             ) &&
                                             Convert.ToBoolean(archiveRecord.Metadata.Usage.PhysicalUsability?.Equals("Uneingeschränkt",
                                                 StringComparison.InvariantCultureIgnoreCase));
    }

    private ArchiveRecordDisplay LoadDisplayData(Document document, DocumentAncestorsDTO ancestors, ArchiveRecordMetadata metadata)
    {
        var retVal = new ArchiveRecordDisplay();

        // Set display template to the level value (dossier, document, etc.)
        retVal.DisplayTemplateName = metadata.DetailData.First(d => d.ElementName.Equals("STUFE")).ElementValue[0].TextValues[0].Value;

        // Load the node context
        retVal.ArchiveplanContext = ConvertToArchivplanContextItems(ancestors, document);

        // Redundant Information
        retVal.ParentArchiveRecordId = metadata.NodeInfo.ParentArchiveRecordId;

        return retVal;
    }


    private List<ArchiveplanContextItem> ConvertToArchivplanContextItems(DocumentAncestorsDTO ancestors, Document document)
    {
        var result = new List<ArchiveplanContextItem>();
        var parentSecurity = archiveRecordMapper.ExtractSecurityRelevantAttributes(ancestors);
        foreach (var ancestor in ancestors.Ancestors)
        {
            var archiveplanContextItem = archiveRecordMapper.MapAncestorRecord(ancestor);

            // Set the protected status to indicate if the item must/should be anonymized
            // If there are any field access tokens, it must be anonymized
            var security = parentSecurity.FirstOrDefault(f => f.DocKey == ancestor["id"].ToString());
            archiveplanContextItem.Protected = accessTokenProvider.GetFieldAccessTokens(security).Any();
            result.Add(archiveplanContextItem);
        }

        return result;
    }

    private List<AggregationField> LoadFondsOverviewAggregation(string hrchPfad)
    {
        var retVal = new List<AggregationField>();

        try
        {
            // Load the fonds aggregation 
            var fondsAggregation = new AggregationField
            {
                AggregationName = "FondsOverview",
                Values = GetFondLinkValues(hrchPfad)
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

    /// <summary>
    ///     Loads the security details.
    /// </summary>
    /// <param name="document">The archive record identifier.</param>
    /// <param name="ancestors"></param>
    /// <returns>ArchiveRecordSecurity.</returns>
    private ArchiveRecordSecurity LoadSecurityDetails(Document document, DocumentAncestorsDTO ancestors)
    {
        try
        {
            // Get the required attributes for the calculation
            var securityData = archiveRecordMapper.ExtractSecurityRelevantAttributes(document);
            var parentSecurityData = archiveRecordMapper.ExtractSecurityRelevantAttributes(ancestors);

            return new ArchiveRecordSecurity
            {
                // Sonst läuft die Anonymisierung 
                FieldAccessToken = accessTokenProvider.GetFieldAccessTokens(securityData),
                MetadataAccessToken = accessTokenProvider.GetMetadataAccessTokens(securityData),
                PrimaryDataFulltextAccessToken = accessTokenProvider.GetFulltextAccessTokens(securityData, parentSecurityData),
                PrimaryDataDownloadAccessToken = accessTokenProvider.GetDownloadAccessTokens(securityData)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load the security information for record {DocKey}", document.DocKey);
            throw;
        }
    }

    private void LoadContainers(Document document, ArchiveRecord archiveRecord)
    {
        if (document.Block.Fields.Any(a => a.Type.Equals("Behaeltnis_Gp")))
        {
            var behaeltnisGroups = document.Block.Fields.Where(a => a.Type.Equals("Behaeltnis_Gp"));
            foreach (var behaeltnisGroup in behaeltnisGroups)
            {
                archiveRecord.Metadata.Containers.Container.Add(MapContainer(behaeltnisGroup.Fields));
            }
        }

        archiveRecord.Metadata.Containers.NumberOfContainers = archiveRecord.Metadata.Containers.Container.Count;
    }

    private void LoadDescriptors(Document document, ArchiveRecord archiveRecord)
    {
        if (document.Block.Fields.Any(a => a.Type.Equals("Deskriptor_Gp")))
        {
            var descriptorGroups = document.Block.Fields.Where(a => a.Type.Equals("Deskriptor_Gp"));
            foreach (var descriptorGroup in descriptorGroups)
            {
                archiveRecord.Metadata.Descriptors.Add(MapDescriptor(descriptorGroup.Fields));
            }
        }
    }

    private async Task LoadVerweise(Document document, ArchiveRecord archiveRecord)
    {
        var verweise = document.Block.Fields.Where(f => f.Type.Equals("Verweis", StringComparison.InvariantCultureIgnoreCase));
        foreach (var verweis in verweise)
        {
            var verweisId = verweis.Fields.FirstOrDefault(f => f.Type.Equals("Vw_Key", StringComparison.InvariantCultureIgnoreCase))?.Value;
            if (!string.IsNullOrEmpty(verweisId))
            {
                var isProtected = await IsRecordProtected(verweisId);
                archiveRecord.Metadata.References.Add(
                    new ArchiveRecordMetadataReference
                    {
                        ArchiveRecordId = verweisId,
                        ReferenceName = verweis.Fields.FirstOrDefault(f => f.Type.Equals("Vw_Bez", StringComparison.InvariantCultureIgnoreCase))
                            ?.Value,
                        Role = verweis.Fields.FirstOrDefault(f => f.Type.Equals("Vw_Rolle", StringComparison.InvariantCultureIgnoreCase))?.Value,
                        Protected = isProtected
                    });
            }
        }

    }

    private async Task<bool> IsRecordProtected(string documentId)
    {
        try
        {
            var document = await actaProDataProvider.GetAisDataRecordAsync(documentId);
            var securityData = archiveRecordMapper.ExtractSecurityRelevantAttributes(document);
            var tokenProvider = new AccessTokenProvider();
            var isProtected = tokenProvider.GetFieldAccessTokens(securityData).Any();
            return isProtected;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unable to find record with id {documentId} when checking it's protected status.", documentId);
            return true;
        }
    }

    private ArchiveRecordMetadataContainersContainer MapContainer(ICollection<DocumentField> fields)
    {
        var id = fields.FirstOrDefault(f => f.Type.Equals("Behaeltnis_Key"))?.Value;
        return new ArchiveRecordMetadataContainersContainer
        {
            ContainerId = id,
            IdName = $"{id} {fields.FirstOrDefault(f => f.Type.Equals("Behaeltnis_Mg_Behaeltniscode"))?.Value}",
            ContainerCarrierMaterial = fields.FirstOrDefault(f => f.Type.Equals("Behaeltnis_Mg_Informationstraeger"))?.Value,
            ContainerCode = fields.FirstOrDefault(f => f.Type.Equals("Behaeltnis_Mg_Behaeltniscode"))?.Value,
            ContainerLocation = fields.FirstOrDefault(f => f.Type.Equals("Behaeltnis_Mg_Lagerungsorte"))?.Value,
            ContainerType = fields.FirstOrDefault(f => f.Type.Equals("Behaeltnis_Bhlgrp_Typ"))?.Value
        };
    }

    private Descriptor MapDescriptor(ICollection<DocumentField> fields)
    {
        var id = fields.FirstOrDefault(f => f.Type.Equals("Deskriptor_Key"))?.Value;
        return new Descriptor()
        {
            DescriptorId = id,
            IdName = $"{id} {fields.FirstOrDefault(f => f.Type.Equals("Deskriptor_Bez"))?.Value}",
            Name = fields.FirstOrDefault(f => f.Type.Equals("Deskriptor_Bez"))?.Value,
        };
    }
}
