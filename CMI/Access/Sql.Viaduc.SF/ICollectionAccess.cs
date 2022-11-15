using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;

namespace CMI.Access.Sql.Viaduc.EF
{
    public interface ICollectionAccess
    {
        Task<List<CollectionListItemDto>> GetAllCollections();
        Task<List<CollectionListItemDto>> GetActiveCollections();
        Task<List<CollectionListItemDto>> GetActiveCollections(int? parentId, string lang);
        Task<CollectionDto> GetCollection(int id);
        Task<List<DropDownListItem>> GetPossibleParents(int id);
        Task<ImageInfo> GetImage(int id, bool usePrecalculatedThumbnail, string mimeType, Size imageSize);
        Task<CollectionDto> InsertOrUpdateCollection(CollectionDto value, string userId);
        Task DeleteCollection(int id);
        Task BatchDeleteCollection(int[] collectionIds);
        Task<CollectionItemResult> GetCollectionItemResult(int collectionId);
    }
}
