using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using CMI.Contract.Parameter;
using Nustache.Core;

namespace CMI.Manager.Viaduc
{
    public class CollectionManager : ICollectionManager
    {
        private readonly ICollectionAccess dbAccess;
        private readonly IParameterHelper parameterHelper;

        public CollectionManager(ICollectionAccess dbAccess,
            IParameterHelper parameterHelper)
        {
            this.dbAccess = dbAccess;
            this.parameterHelper = parameterHelper;
        }

        public Task<List<CollectionListItemDto>> GetAllCollections(bool onlyActive=false)
        {
            if (onlyActive)
            {
                return dbAccess.GetActiveCollections();
            } 

            return dbAccess.GetAllCollections();
        }

        public Task<CollectionDto> GetCollection(int collectionId)
        {
            return dbAccess.GetCollection(collectionId);
        }

        public Task<CollectionDto> InsertOrUpdateCollection(CollectionDto collection, string userId)
        {
            return dbAccess.InsertOrUpdateCollection(collection, userId);
        }

        public Task DeleteCollection(int collectionId)
        {
            return dbAccess.DeleteCollection(collectionId);
        }

        public Task BatchDeleteCollection(int[] collectionIds)
        {
            return dbAccess.BatchDeleteCollection(collectionIds);
        }

        public Task<List<DropDownListItem>> GetPossibleParents(int id)
        {
            return dbAccess.GetPossibleParents(id);
        }

        public Task<ImageInfo> GetImage(int id, bool usePrecalculatedThumbnail, string mimeType = null, Size imageSize = new())
        {
            return dbAccess.GetImage(id, usePrecalculatedThumbnail, mimeType, imageSize);
        }

        public Task<List<CollectionListItemDto>> GetActiveCollections(int? parentId, string language)
        {
            return dbAccess.GetActiveCollections(parentId, language);
        }

        public Task<CollectionItemResult> GetCollectionItemResult(int collectionId)
        {
            return dbAccess.GetCollectionItemResult(collectionId);
        }

        public Task<string> GetCollectionsHeader(string language)
        {
            var template = parameterHelper.GetSetting<CollectionSettings>();
            return Task.FromResult(Render.StringToString(template.CollectionHeader, CreateExpandoCreate(language)));
        }

        private static dynamic CreateExpandoCreate(string language)
        {
            dynamic expando = new ExpandoObject();
            expando.IstDeutsch = language == "de";
            expando.IstEnglisch = language == "en";
            expando.IstItalienisch = language == "it";
            expando.IstFranzösisch = language == "fr";
            return expando;
        }
    }
}