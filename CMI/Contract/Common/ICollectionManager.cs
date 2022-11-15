using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using CMI.Contract.Common.Entities;

namespace CMI.Contract.Common
{
    public interface ICollectionManager
    {
        Task<List<CollectionListItemDto>> GetAllCollections(bool onlyActive = false);
        Task<CollectionDto> GetCollection(int collectionId);
        Task<List<CollectionListItemDto>> GetActiveCollections(int? parentId, string language);
        Task<CollectionDto> InsertOrUpdateCollection(CollectionDto collection, string userId);
        Task DeleteCollection(int collectionId);
        Task BatchDeleteCollection(int[] collectionIds);
        Task<List<DropDownListItem>> GetPossibleParents(int id);
        Task<ImageInfo> GetImage(int id, bool usePrecalculatedThumbnail, string mimeType = null, Size imageSize = new());
        Task<CollectionItemResult> GetCollectionItemResult(int collectionId);
        Task<string> GetCollectionsHeader(string language);
    }
}