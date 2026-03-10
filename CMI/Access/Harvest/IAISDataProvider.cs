using CMI.Access.Harvest.ActaPro;
using CMI.Contract.Harvest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMI.Access.Harvest;

public interface IAISDataProvider
{
    Task<string> GetDbVersion();
    Task InitiateFullResync();
    Task<Document> GetAisDataRecordAsync(string documentId);
    Task<DocumentAncestorsDTO> GetDocumentAncestorsDTO(string archiveRecordId);
    Task<long> GetDocumentChildrenCountAsync(string archiveRecordId);
    Task<List<FondLink>> LoadFondLinks();
    Task<List<string>> GetDocumentChildrenAsync(string archiveRecordId);
    Task<List<VerzEinheitData>> GetArchiveRecordOrderDetailDataForContainer(string containerId);
}