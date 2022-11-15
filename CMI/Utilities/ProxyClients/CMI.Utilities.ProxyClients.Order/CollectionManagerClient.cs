using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using CMI.Contract.Messaging;
using MassTransit;

namespace CMI.Utilities.ProxyClients.Order
{
    public class CollectionManagerClient : ICollectionManager
    {
        private readonly IBus bus;

        public CollectionManagerClient(IBus bus)
        {
            this.bus = bus;
        }

        public async Task<List<CollectionListItemDto>> GetAllCollections(bool onlyActive = false)
        {
            var client = GetRequestClient<GetAllCollectionsRequest>();
            var result = await client.GetResponse<GetAllCollectionsResponse>(new GetAllCollectionsRequest
            {
                OnlyActive = onlyActive
            });
            return result.Message.Collections;
        }

        public async Task<CollectionDto> GetCollection(int collectionId)
        {
            var client = GetRequestClient<GetCollectionRequest>();
            var result = await client.GetResponse<GetCollectionResponse>(new GetCollectionRequest
            {
                CollectionId = collectionId
            });
            return result.Message.Collection;
        }

        public async Task<List<CollectionListItemDto>> GetActiveCollections(int? parentId, string language)
        {
            var client = GetRequestClient<GetActiveCollectionsRequest>();
            var result = await client.GetResponse<GetActiveCollectionsResponse>(new GetActiveCollectionsRequest
            {
                ParentId = parentId,
                Language = language
            });
            return result.Message.Collections;
        }

        public async Task<CollectionDto> InsertOrUpdateCollection(CollectionDto collection, string userId)
        {
            var client = GetRequestClient<InsertOrUpdateCollectionRequest>();
            var result = await client.GetResponse<InsertOrUpdateCollectionResponse>(new InsertOrUpdateCollectionRequest
            {
                Collection = collection,
                UserId = userId
            });
            return result.Message.Collection;
        }

        public async Task DeleteCollection(int collectionId)
        {
            var client = GetRequestClient<DeleteCollectionRequest>();
            await client.GetResponse<DeleteCollectionResponse>(new DeleteCollectionRequest
            {
                CollectionId = collectionId
            });
        }

        public async Task BatchDeleteCollection(int[] collectionIds)
        {
            var client = GetRequestClient<BatchDeleteCollectionRequest>();
            await client.GetResponse<BatchDeleteCollectionResponse>(new BatchDeleteCollectionRequest
            {
                CollectionIds = collectionIds
            });
        }


        public async Task<List<DropDownListItem>> GetPossibleParents(int id)
        {
            var client = GetRequestClient<GetPossibleParentsRequest>();
            var result = await client.GetResponse<GetPossibleParentsResponse>(new GetPossibleParentsRequest
            {
                CurrentCollectionId = id
            });
            return result.Message.Parents;
        }

        public async Task<ImageInfo> GetImage(int id, bool usePrecalculatedThumbnail, string imageMimeType = null, Size imageSize = new())
        {
            var client = GetRequestClient<GetImageRequest>();
            var result = await client.GetResponse<GetImageResponse>(new GetImageRequest
            {
                CollectionId = id,
                Thumbnail = usePrecalculatedThumbnail,
                ImageMimeType = imageMimeType,
                ImageSize = imageSize
            });

            return result.Message.ImageInfo;
        }

        public async Task<CollectionItemResult> GetCollectionItemResult(int collectionId)
        {
            var client = GetRequestClient<GetCollectionItemResultRequest>();
            var result = await client.GetResponse<GetCollectionItemResultResponse>(new GetCollectionItemResultRequest
            {
                CollectionId = collectionId
            });

            return result.Message.Result;
        }

        public async Task<string> GetCollectionsHeader(string language)
        {
            var client = GetRequestClient<GetCollectionsHeaderRequest>();

            var result = await client.GetResponse<GetCollectionsHeaderResponse>(new GetCollectionsHeaderRequest
            {
               Language = language
            });
            return result.Message.Header;
        }

        private IRequestClient<T1> GetRequestClient<T1>(string queueEndpoint = "", int requestTimeOutInSeconds = 0) where T1 : class
        {
            var serviceUrl = string.IsNullOrEmpty(queueEndpoint)
                ? string.Format(BusConstants.ViaducManagerRequestBase, typeof(T1).Name)
                : queueEndpoint;

#if DEBUG
            var requestTimeout = TimeSpan.FromSeconds(120);
#else
                var requestTimeout = TimeSpan.FromSeconds(10);
#endif

            if (requestTimeOutInSeconds > 0)
            {
                requestTimeout = TimeSpan.FromSeconds(requestTimeOutInSeconds);
            }

            return bus.CreateRequestClient<T1>(new Uri(bus.Address, serviceUrl), requestTimeout);
        }
    }
}
