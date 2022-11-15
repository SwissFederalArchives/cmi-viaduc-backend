using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc.EF.Helper;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using Serilog;

namespace CMI.Access.Sql.Viaduc.EF
{
    public class CollectionAccess : ICollectionAccess
    {
        private readonly ViaducDb dbContext;
        private readonly Size defaultTargetSize = new Size(800, 565);
        private readonly int maxThumbnailSize = 200;
        private readonly AccessHelper accessHelper;

        public CollectionAccess(ViaducDb dbContext, AccessHelper accessHelper)
        {
            this.accessHelper = accessHelper;
            DbInterception.Add(new ViaducDbCommandInterceptor());
            this.dbContext = dbContext;
        }

        public Task<List<CollectionListItemDto>> GetAllCollections()
        {
            return Task.FromResult(dbContext.CollectionList.ToDtosWithRelated(1).ToList());
        }

        public Task<List<CollectionListItemDto>> GetActiveCollections()
        {
            return Task.FromResult(dbContext.CollectionList.Where(c => c.ValidFrom <= DateTime.Now && c.ValidTo >= DateTime.Now)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Title)
                .ToDtosWithRelated(1)
                .ToList());
        }

        public Task<List<CollectionListItemDto>> GetActiveCollections(int? parentId, string lang)
        {
            var collections = dbContext.CollectionList.Where(c => c.ValidFrom <= DateTime.Now &&
            c.ValidTo >= DateTime.Now && c.Language == lang && (c.ParentId ?? 0) == (parentId ?? 0))
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Title)
                .ToDtosWithRelated(1)
                .ToList();
            return Task.FromResult(collections);
        }

        public Task<CollectionDto> GetCollection(int id)
        {
            var collection = dbContext.Collections.FirstOrDefault(c => c.CollectionId == id).ToDtoWithRelated(1);

            if (collection != null)
            {
                // Remove images from payload to reduce size. Not the best way to do it.
                // Probably it would be best to store the binaries in a separate table and fetch the images individually
                collection.ChildCollections.ForEach(c =>
                {
                    c.Image = null;
                    c.Thumbnail = null;
                });
                if (collection.Parent != null)
                {
                    collection.Parent.Image = null;
                    collection.Parent.Thumbnail = null;
                }
            }

            return Task.FromResult(collection);
        }

        public Task DeleteCollection(int id)
        {
            var collection = dbContext.Collections.FirstOrDefault(c => c.CollectionId == id);
            if (collection != null)
            {
                if (collection.ChildCollections.Any())
                {
                    throw new InvalidOperationException("Cannot delete a collection that has children");
                }

                dbContext.Collections.DeleteObject(collection);
                dbContext.SaveChanges();
            }

            return Task.CompletedTask;
        }

        public async Task BatchDeleteCollection(int[] collectionIds)
        {
            var itemsWithChildren = dbContext.Collections.Where(c => collectionIds.Contains(c.CollectionId) && c.ChildCollections.Any());
            if (itemsWithChildren.Any())
            {
                throw new InvalidOperationException("Cannot delete a collection that has children");
            }

            await dbContext.Collections.Where(c => collectionIds.Contains(c.CollectionId)).ForEachAsync(i => dbContext.Collections.DeleteObject(i));
            await dbContext.SaveChangesAsync();
        }

        public Task<CollectionDto> InsertOrUpdateCollection(CollectionDto value, string userId)
        {
            Collection item;

            if (value.CollectionId <= 0)
            {
                item = new Collection
                {
                    CreatedBy = accessHelper.GetUserNameFromId(userId),
                    CreatedOn = DateTime.Now,
                    ValidTo = new DateTime(9999, 12, 31),
                    ValidFrom = DateTime.Now
                };
                dbContext.Collections.AddObject(item);
            }
            else
            {
                item = dbContext.Collections.FirstOrDefault(c => c.CollectionId == value.CollectionId);
                if (item != null)
                {
                    item.ModifiedBy = accessHelper.GetUserNameFromId(userId);
                    item.ModifiedOn = DateTime.Now;
                }
            }

            if (item != null)
            {
                item.Language = value.Language;
                item.Description = value.Description;
                item.DescriptionShort = value.DescriptionShort;
                item.Link = value.Link;
                item.Image = FitImage(value.Image, ConvertImageMimeType(value.ImageMimeType), defaultTargetSize);
                item.ImageAltText = value.ImageAltText;
                item.ImageMimeType = value.ImageMimeType;
                item.ParentId = value.ParentId;
                item.CollectionTypeId = value.CollectionTypeId;
                item.Title = value.Title;
                item.SortOrder = value.SortOrder;
                item.Thumbnail = CreateThumbnailFromImage(item.Image);
                // Update collection path here if we have a valid id
                item.CollectionPath = value.CollectionId > 0 ? CreateCollectionPath(value.ToEntity()) : "temp";
                if (value.ValidFrom != DateTime.MinValue)
                {
                    item.ValidFrom = value.ValidFrom.ToLocalTime();
                }

                if (value.ValidTo != DateTime.MinValue)
                {
                    item.ValidTo = value.ValidTo.ToLocalTime();
                }

                dbContext.SaveChanges();

                // On insert, we can set the collection path only after the item has been saved
                if (value.CollectionId <= 0)
                {
                    item.CollectionPath = CreateCollectionPath(item);
                    dbContext.SaveChanges();
                }
            }

            return Task.FromResult(item.ToDtoWithRelated(1));
        }

        public Task<List<DropDownListItem>> GetPossibleParents(int id)
        {
            return Task.FromResult(dbContext.Collections.Where(c =>
                c.ValidFrom <= DateTime.Now && c.ValidTo >= DateTime.Now
                                            && c.CollectionTypeId == 1 && c.CollectionId != id
                                            && (c.ParentId != id || c.ParentId == null)).Select(c =>
                new DropDownListItem { Value = c.CollectionId, Text = c.Title }).ToList());
        }

        public Task<ImageInfo> GetImage(int id, bool usePrecalculatedThumbnail, string mimeType, Size imageSize)
        {
            var original = dbContext.Collections.Where(c => c.CollectionId == id).Select(c => usePrecalculatedThumbnail ?
                    new ImageInfo { Image = c.Thumbnail, MimeType = c.ImageMimeType } :
                    new ImageInfo { Image = c.Image, MimeType = string.IsNullOrEmpty(mimeType) ? c.ImageMimeType : mimeType })
                .FirstOrDefault();

            return Task.FromResult(new ImageInfo
            {
                Image = usePrecalculatedThumbnail ? original?.Image : FitImage(original?.Image, ConvertImageMimeType(mimeType), imageSize),
                MimeType = usePrecalculatedThumbnail ? original?.MimeType : string.IsNullOrEmpty(mimeType) ? original?.MimeType : mimeType
            });
        }

        public async Task<CollectionItemResult> GetCollectionItemResult(int collectionId)
        {
            var item = await GetCollection(collectionId);
            if (item != null)
            {
                var result = new CollectionItemResult
                {
                    Item = item,
                    Breadcrumb = GetBreadcrumb(item.CollectionPath)
                };
                return result;
            }

            return new CollectionItemResult {Item = null, Breadcrumb = null};
        }

        private Dictionary<int, string> GetBreadcrumb(string collectionPath)
        {
            var result = new Dictionary<int, string>();
            try
            {
                // Split the collection path to get the ids
                var partSize = 10;
                var ids = Enumerable.Range(0, (collectionPath.Length + partSize - 1) / partSize)
                            .Select(i => Convert.ToInt32(collectionPath.Substring(i * partSize, Math.Min(collectionPath.Length - i * partSize, partSize))));
                result = dbContext.Collections.Where(c => ids.Contains(c.CollectionId))
                    .ToDictionary(x => x.CollectionId, x => x.Title);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error building breadcrumb");
            }

            return result;
        }

        private string CreateCollectionPath(Collection item)
        {
            if (item != null)
            {
                var currentItemPath = item.CollectionId.ToString().PadLeft(10, '0');
                if (item.ParentId.HasValue)
                {
                    var parentPath = CreateCollectionPath(dbContext.Collections.FirstOrDefault(i => i.CollectionId == item.ParentId));
                    return parentPath + currentItemPath;
                }

                return currentItemPath;
            }

            return string.Empty;
        }

        private byte[] CreateThumbnailFromImage(byte[] imageValue)
        {
            if (imageValue == null)
            {
                return null;
            }

            var retVal = new MemoryStream();
            var callback = new Image.GetThumbnailImageAbort(() => true);
            using var image = Image.FromStream(new MemoryStream(imageValue));

            var height = image.Height;
            var width = image.Width;
            int destHeight;
            int destWidth;

            // we want max width 200 or max height 200
            if (width > height)
            {
                destWidth = maxThumbnailSize;
                destHeight = (int)(image.Height * ((double)maxThumbnailSize / image.Width));
            }
            else
            {
                destHeight = maxThumbnailSize;
                destWidth = (int)(image.Width * ((double)maxThumbnailSize / image.Height));
            }

            using var thumbnail = image.GetThumbnailImage(destWidth, destHeight, callback, new IntPtr());
            thumbnail.Save(retVal, image.RawFormat);
            retVal.Position = 0;
            return retVal.GetBuffer();
        }

        /// <summary>
        /// Fits an image into the passed width and height.
        /// </summary>
        private byte[] FitImage(byte[] imageValue, ImageFormat desiredFormat, Size targetSize)
        {
            if (imageValue == null)
            {
                return null;
            }

            using var image = Image.FromStream(new MemoryStream(imageValue));
            var height = image.Height;
            var width = image.Width;
            if (targetSize.Width == 0 || targetSize.Height == 0)
            {
                targetSize = new Size(width, height);
            }
            var aspectRatio = (float)targetSize.Width / targetSize.Height;
            var offsetX = 0f;
            var offsetY = 0f;

            // Get target rectangle
            if (width > height * aspectRatio)
            {
                offsetX = width - (height * aspectRatio);
            }
            if (height > width / aspectRatio)
            {
                offsetY = height - (width / aspectRatio);
            }

            Rectangle cropRect = new Rectangle(new Point((int)offsetX / 2, (int)offsetY / 2), new Size((int)(width - offsetX), (int)(height - offsetY)));
            var bitmap = new Bitmap(targetSize.Width, targetSize.Height);
            var retVal = new MemoryStream();
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(image, new Rectangle(new Point(0, 0), targetSize), cropRect, GraphicsUnit.Pixel);
                bitmap.Save(retVal, desiredFormat);
            }

            retVal.Position = 0;
            return retVal.GetBuffer();
        }

        private ImageFormat ConvertImageMimeType(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
            {
                return ImageFormat.Png;
            }

            switch (mimeType.Substring(mimeType.IndexOf('/') + 1).ToLower())
            {
                case "png":
                    return ImageFormat.Png;
                case "gif":
                    return ImageFormat.Gif;
                case "jpeg":
                case "jpg":
                    return ImageFormat.Jpeg;
                default:
                    return ImageFormat.Png;
            }
        }

    }
}