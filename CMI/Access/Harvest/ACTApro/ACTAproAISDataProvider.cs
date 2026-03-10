using CMI.Contract.Harvest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMI.Access.Harvest.ActaPro.Mapping;

namespace CMI.Access.Harvest.ActaPro;

public class ActaProAISDataProvider : IAISDataProvider
{
    internal readonly IActaProClient actaProClient;

    public ActaProAISDataProvider(IActaProClient actaProClient)
    {
        this.actaProClient = actaProClient;
    }

    public Task InitiateFullResync()
    {
        return Task.CompletedTask;
    }

    public async Task<string> GetDbVersion()
    {
        var docParameters = new DocumentSearchParams
        {
            DocumentTypes = ["Arch"]
        };

        await actaProClient.SearchDocumentsAsync(0, 1, null, docParameters);

        return "ActaPro läuft ";
    }

    public async Task<DocumentAncestorsDTO> GetDocumentAncestorsDTO(string recordId)
    { /*
         Signatur
            Ar_Kuerz (Archiv)
            Te_Sign (Tektonik / Hauptabteilung)
            Bst_Signatur (Bestand, Teilbestand)
            Kl_Signatur (Klassifkation / Serie)
            Vz_OwnSign (Dossier, Subdossier, Dokument)
        Titel
            Ar_Name (Archiv)
            Te_Name (Tektonik / Hauptabteilung)
            Bst_Name (Bestand, Teilbestand)
            Kl_Name (Klassifkation / Serie)
            Vz_Titel (Dossier, Subdossier, Dokument)

         */

        var signaturFields = "Ar_Kuerz, Te_Sign, Bst_Signatur, Kl_Signatur, Vz_OwnSign";
        var titleFields = "Vz_Titel, Kl_Name, Bst_Name, Te_Name, Ar_Name";

        // Die zusätzlichen Felder sind teilweise für die Access-Token-Berechnung notwendig
        var additionalFields = "Laufzeit, Vz_ZugaenglichkeitBGA, Vz_Schutzfristkategorie, Vz_Schutzfristende, Zugaenglichkeit";
        var archiveRecordId = recordId;
        Log.Debug("Requesting document ancestors with archiveRecordId: {archiveRecordId}", archiveRecordId);

        var documentAncestorsAsync = await actaProClient.GetDocumentAncestorsAsync(archiveRecordId, $"{signaturFields}, {titleFields}, {additionalFields}", null);

        // Add also the document itself, as this is the easiest way to add the complete archiveplan context
        documentAncestorsAsync.Ancestors.Add(documentAncestorsAsync.Document);

        return documentAncestorsAsync;
    }

    public async Task<long> GetDocumentChildrenCountAsync(string archiveRecordId)
    {
        var result = await actaProClient.GetDocumentChildrenAsync(archiveRecordId, "id", string.Join(",", ActaProClientValues.AllVerzEinheitDocTypes), null, 0, 1, null);
        return result?.TotalElements ?? 0;
    }

    /// <summary>
    /// Returns the document keys of all direct children of the given archiveRecord
    /// </summary>
    /// <param name="archiveRecordId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<List<string>> GetDocumentChildrenAsync(string archiveRecordId)
    {
        var retVal = new List<string>();
        var pageIndex = 0;
        SearchResultPage result;

        do
        {
            result = await actaProClient.GetDocumentChildrenAsync(archiveRecordId, "id",
                string.Join(",", ActaProClientValues.AllVerzEinheitDocTypes), null,
                pageIndex,
                1000,
                null);
            retVal.AddRange(result.Content.Select(c => c["id"]));
            pageIndex++;

        } while (!result.Last);

        return retVal;
    }

    public async Task<List<VerzEinheitData>> GetArchiveRecordOrderDetailDataForContainer(string containerId)
    {
        var docParameters = new DocumentSearchParams
        {
            DocumentTypes = ["Vz", "Vor", "Dokum"],
            Fields = ["Vz_Titel", "Vz_OwnSign", "Laufzeit", "Vz_Aktenzeichen"]
        };

        docParameters.Filters.Add(new DocumentSearchFilter
        {
            Operator = DocumentSearchFilterOperator.Eq,
            FieldName = "Behaeltnis_Key",
            FieldValue = containerId
        });

        var pageIndex = 0;
        var retVal = new List<VerzEinheitData>();
        SearchResultPage result;

        do
        {
            result = await actaProClient.SearchDocumentsAsync(pageIndex, 1000, null, docParameters);
            retVal.AddRange(result.Content.Select(c =>
            {
                var item1 = MappingFunctions.GetFromBisDate(c["Laufzeit"]).Item1?.ToString("yyyy.MM.dd");
                var item2 = MappingFunctions.GetFromBisDate(c["Laufzeit"]).Item2?.ToString("yyyy.MM.dd");
                return new VerzEinheitData()
                {
                    Title = c.ContainsKey("Vz_Titel") ? c["Vz_Titel"] : "",
                    ReferenceCode = c.ContainsKey("Vz_Titel") ? c["Vz_OwnSign"] : "",
                    CreationPeriod = c.ContainsKey("Laufzeit") ? $"{item1}-{item2}" : "",
                    Aktenzeichen = c.ContainsKey("Vz_Aktenzeichen") ? c["Vz_Aktenzeichen"] : "",
                };
            }));
            pageIndex++;

        } while (!result.Last);

        return retVal;
    }

    public async Task<Document> GetAisDataRecordAsync(string documentId)
    {
        Log.Debug("Requesting document with archiveRecordId: {archiveRecordId}", documentId);

        try
        {
            if (int.TryParse(documentId, out _))
            {
                var readIdResult = await ReadActaProIdWithScopeIdFromActaProClient(documentId);
                if (string.IsNullOrWhiteSpace(readIdResult))
                {
                    var exception = new ApiException($"No ActaPro Id was found for the scopeId {documentId}", (int) HttpStatusCode.NotFound, string.Empty,
                        null, null);
                    throw exception;
                }

                documentId = readIdResult;
            }

            var result = await actaProClient.GetDocumentAsync(documentId, "json");
            return result;
        }
        catch (ApiException apiException)
        {
            if (apiException.StatusCode == (int) HttpStatusCode.NotFound)
            {
                Log.Error(apiException, "The ACTApro Record with id {documentId} could not be found", documentId);
            }
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while getting ACTApro Record with id {documentId}. Probably the ACTApro system is down.", documentId);
            throw new AisNotAvailableException();
        }
    }

    public async Task<List<FondLink>> LoadFondLinks()
    {
        var page = 0;
        var totalPages = 0;
        var found = new List<FondLink>();

        do
        {
            var result = await GetTeilbeständeAndBeständeWithFieldBestaendeuebersicht(page, 10000);

            totalPages = result.TotalPages;
            foreach (var dic in result.Content)
            {
                if (dic.ContainsKey("path") && dic.ContainsKey("Bestaendeuebersicht_Bez"))
                {
                    var links = dic["Bestaendeuebersicht_Bez"].Split(';');
                    foreach (var link in links)
                    {
                        var linkName = CleanFondName(link);

                        found.Add(new FondLink
                        {
                            HierarchyPath = dic["path"],
                            LinkName = linkName
                        });
                    }
                }
            }

            page++;

        } while (page < totalPages);

        return found;
    }
    private async Task<string> ReadActaProIdWithScopeIdFromActaProClient(string documentId)
    {
        var docParameters = new DocumentSearchParams
        {
            DocumentTypes = ActaProClientValues.AllVerzEinheitDocTypes
        };

        docParameters.Filters.Add(new DocumentSearchFilter
        {
            Operator = DocumentSearchFilterOperator.Eq,
            FieldName = "ScopeID",
            FieldValue = documentId
        });
        var actaProId = string.Empty;
        var searchResultPage = await actaProClient.SearchDocumentsAsync(0, 1, null, docParameters);
        if (searchResultPage.Content.Count == 1 && searchResultPage.Content.FirstOrDefault().ContainsKey("id"))
        {
            actaProId = searchResultPage.Content.FirstOrDefault()["id"];
        }

        return actaProId;
    }

    private async Task<SearchResultPage> GetTeilbeständeAndBeständeWithFieldBestaendeuebersicht(int page, int maxHits)
    {
        var docParameters = new DocumentSearchParams
        {
            DocumentTypes = ["Best", "TBest"]
        };

        docParameters.Fields = ["Bestaendeuebersicht_Bez"];
        var result = await actaProClient.SearchDocumentsAsync(page, maxHits, null, docParameters);

        return result;
    }

    private async Task<SearchResultPage> GetVEsAfterLastChangedDate(DateTime lastSyncDate, string[] docTypes, string status, int page, int maxHits)
    {
        var docParameters = new DocumentSearchParams
        {
            DocumentTypes = docTypes.ToList()
        };

        docParameters.Filters.Add(new DocumentSearchFilter
        {
            Operator = DocumentSearchFilterOperator.Ge,
            FieldName = "chdate",
            FieldValue = lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ssZ")

        });

        docParameters.Filters.Add(new DocumentSearchFilter
        {
            Operator = DocumentSearchFilterOperator.Eq,
            FieldName = "Status",
            FieldValue = status
        });

        var result = await actaProClient.SearchDocumentsAsync(page, maxHits, null, docParameters);

        return result;
    }

    private string CleanFondName(string text)
    {
        // Removes the prefix and the trailing text 
        // e.g. CH-BAR*/614 Münzwesen, Edelmetalle (Gliederungseinheit) ==> CH-BAR*/614 Münzwesen, Edelmetalle
        var pattern = @"(^.*\/)(.*)(\s+\(\w+\)\s*)+$";//  @"(^.*\*)(.*)(\s+\(\w+\)\s*)+$";
        var myRegex = new Regex(pattern, RegexOptions.IgnoreCase);
        return myRegex.Replace(text, "$2");
    }
}

