using System.Collections.Generic;
using System.Drawing;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;

namespace CMI.Contract.Messaging
{
    public class GetAllCollectionsRequest
    {
        public bool OnlyActive { get; set; }
    }

    public class GetAllCollectionsResponse
    {
        public List<CollectionListItemDto> Collections { get; set; }
    }

    public class GetCollectionRequest
    {
        public int CollectionId { get; set; }
    }

    public class GetCollectionResponse
    {
        public CollectionDto Collection { get; set; }
    }

    public class GetActiveCollectionsRequest
    {
        public int? ParentId { get; set; }
        public string Language { get; set; }
    }

    public class GetActiveCollectionsResponse
    {
        public List<CollectionListItemDto> Collections { get; set; }
    }

    public class GetCollectionsHeaderRequest
    {
        public string Language { get; set; }
    }

    public class GetCollectionsHeaderResponse
    {
        public string Header { get; set; }
    }

    public class InsertOrUpdateCollectionRequest
    {
        public CollectionDto Collection { get; set; }
        public string UserId { get; set; }
    }

    public class InsertOrUpdateCollectionResponse
    {
        public CollectionDto Collection { get; set; }
    }

    public class GetPossibleParentsRequest
    {
        public int CurrentCollectionId { get; set; }
    }

    public class GetPossibleParentsResponse
    {
        public List<DropDownListItem> Parents { get; set; }
    }

    public class GetImageRequest
    {
        public int CollectionId { get; set; }
        public bool Thumbnail { get; set; }
        public string ImageMimeType { get; set; }
        public Size ImageSize { get; set; }
    }

    public class GetImageResponse
    {
        public ImageInfo ImageInfo { get; set; }
    }

    public class DeleteCollectionRequest
    {
        public int CollectionId { get; set; }
    }

    public class DeleteCollectionResponse
    {
    }

    public class BatchDeleteCollectionRequest
    {
        public int[] CollectionIds { get; set; }
    }

    public class BatchDeleteCollectionResponse
    {
    }

    public class GetCollectionItemResultRequest
    {
        public int CollectionId { get; set; }
    }

    public class GetCollectionItemResultResponse
    {
        public CollectionItemResult Result { get; set; }
    }
}