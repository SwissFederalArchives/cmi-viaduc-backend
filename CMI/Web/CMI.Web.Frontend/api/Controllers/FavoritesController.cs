using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.Helpers;
using Newtonsoft.Json.Linq;
using WebGrease.Css.Extensions;

namespace CMI.Web.Frontend.api.Controllers
{
    [Authorize]
    [NoCache]
    public class FavoritesController : ApiFrontendControllerBase
    {
        private readonly IElasticService elasticService;
        private readonly FavoriteDataAccess sqlDataAccess = new FavoriteDataAccess(WebHelper.Settings["sqlConnectionString"]);
        private readonly VeExportRecordHelper veExportRecordHelper;

        public FavoritesController(IElasticService elasticService, VeExportRecordHelper veExportRecordHelper)
        {
            this.elasticService = elasticService;
            this.veExportRecordHelper  = veExportRecordHelper;
        }

        [HttpGet]
        public FavoriteList[] GetAllLists()
        {
            return sqlDataAccess.GetAllLists(ControllerHelper.GetCurrentUserId())
                .OrderBy(l => l.Name)
                .ToArray();
        }


        [HttpGet]
        public int GetFavoriteCount()
        {
            return sqlDataAccess.GetAllLists(ControllerHelper.GetCurrentUserId())
                .Sum(l => l.NumberOfItems);
        }

        [HttpGet]
        public JArray GetAllListsForUrl(string url)
        {
            var jls = new JArray();
            var ids = GetListContainingFavorite(url);

            sqlDataAccess.GetAllLists(ControllerHelper.GetCurrentUserId())
                .OrderBy(l => l.Name)
                .ForEach(l =>
                {
                    var jl = JObject.FromObject(l);
                    jl.Add("included", ids.Contains(l.Id));
                    jls.Add(jl);
                });

            return jls;
        }

        [HttpGet]
        public int[] GetListContainingFavorite(string url)
        {
            return sqlDataAccess.GetListContainingFavorite(ControllerHelper.GetCurrentUserId(), url).ToArray();
        }

        [HttpGet]
        public JArray GetAllListsForVe(string veId)
        {
            var jls = new JArray();
            var ids = GetListsContainingVe(veId);
            sqlDataAccess.GetAllLists(ControllerHelper.GetCurrentUserId())
                .OrderBy(l => l.Name)
                .ForEach(l =>
                {
                    var jl = JObject.FromObject(l);
                    jl.Add("included", ids.Contains(l.Id));
                    jls.Add(jl);
                });

            return jls;
        }

        [HttpGet]
        public FavoriteList GetList(int listId)
        {
            var list = sqlDataAccess.GetList(ControllerHelper.GetCurrentUserId(), listId);
            list.Items = GetFavoritesContainedOnList(listId);
            return list;
        }

        [HttpPost]
        public IHttpActionResult AddList(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return BadRequest($"missing parameter {nameof(listName)} ");
            }

            return Content(HttpStatusCode.Created, sqlDataAccess.AddList(ControllerHelper.GetCurrentUserId(), listName, null));
        }

        [HttpPost]
        public void RemoveList(int listId)
        {
            sqlDataAccess.RemoveList(ControllerHelper.GetCurrentUserId(), listId);
        }

        [HttpPost]
        public void RenameList(int listId, string newName)
        {
            sqlDataAccess.RenameList(ControllerHelper.GetCurrentUserId(), listId, newName);
        }

        [HttpGet]
        public IHttpActionResult ExportList(int listId)
        {
            var list = sqlDataAccess.GetList(ControllerHelper.GetCurrentUserId(), listId);
            list.Items = GetFavoritesContainedOnList(listId);
            var language = GetUserAccess(WebHelper.GetClientLanguage(Request)).Language;
            return ResponseMessage(veExportRecordHelper.CreateExcelFile(ConvertExportData(list.Items.Where(i => i is VeFavorite).ToList()), language,
                    FrontendSettingsViaduc.Instance.GetTranslation(language, "accountFavoritesDetailPageComponent.fileName") + $"{list.Name}.xlsx",
                FrontendSettingsViaduc.Instance.GetTranslation("en", "accountFavoritesDetailPageComponent.fileName") + $"{DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss")}.xlsx"));
        }

        [HttpPost]
        public IHttpActionResult AddSearchFavorite([FromUri] int listId, [FromBody] SearchFavorite favorite)
        {
            if (favorite == null)
            {
                return BadRequest($"missing parameter {nameof(favorite)}");
            }

            var favourite = sqlDataAccess.AddFavorite(ControllerHelper.GetCurrentUserId(), listId, favorite);
            return Content(HttpStatusCode.Created, favourite);
        }

        [HttpPost]
        public IHttpActionResult AddVeFavorite([FromUri] int listId, [FromBody] VeFavorite favorite)
        {
            if (favorite == null)
            {
                return BadRequest($"missing parameter {nameof(favorite)}");
            }

            var favourite = sqlDataAccess.AddFavorite(ControllerHelper.GetCurrentUserId(), listId, favorite);
            return Content(HttpStatusCode.Created, favourite);
        }

        [HttpPost]
        public void RemoveFavorite(int id, int listId)
        {
            sqlDataAccess.RemoveFavorite(ControllerHelper.GetCurrentUserId(), listId, id);
        }

        [HttpGet]
        public int[] GetListsContainingVe(string veId)
        {
            return sqlDataAccess.GetListsContainingVe(ControllerHelper.GetCurrentUserId(), veId).ToArray();
        }

        [HttpGet]
        public IEnumerable<IFavorite> GetFavoritesContainedOnList(int listId)
        {
            var uid = ControllerHelper.GetCurrentUserId();
            
            var favorites = sqlDataAccess.GetFavoritesContainedOnList(uid, listId).ToList();

            var veIds = favorites.OfType<VeFavorite>().Select(f => f.VeId).ToList();
            var access = GetUserAccess(WebHelper.GetClientLanguage(Request));

            var found = elasticService
                .QueryForIds<ElasticArchiveRecord>(veIds, access, new Paging {Take = ElasticService.ELASTIC_SEARCH_HIT_LIMIT, Skip = 0}).Entries;

            foreach (var f in favorites)
            {
                if (f is VeFavorite veFavorite)
                {
                    var elasticHit = found.FirstOrDefault(e => e?.Data?.ArchiveRecordId == veFavorite.VeId.ToString());
                    if (elasticHit == null)
                    {
                        continue;
                    }

                    veFavorite.CustomFields = elasticHit.Data?.CustomFields; 
                    veFavorite.WithinInfo = elasticHit.Data?.WithinInfo;
                    veFavorite.Title = elasticHit.Data?.Title;
                    veFavorite.Level = elasticHit.Data?.Level;
                    veFavorite.CreationPeriod = elasticHit.Data?.CreationPeriod?.Text;
                    veFavorite.ReferenceCode = elasticHit.Data?.ReferenceCode;
                    veFavorite.CanBeOrdered = elasticHit.Data?.CanBeOrdered ?? false;
                    veFavorite.HasPrimaryLink = !string.IsNullOrWhiteSpace(elasticHit.Data?.PrimaryDataLink);
                    veFavorite.CanBeDownloaded = veFavorite.HasPrimaryLink &&
                                                 access.HasAnyTokenFor(elasticHit.Data?.PrimaryDataDownloadAccessTokens);
                    veFavorite.ManifestLink = elasticHit.Data?.ManifestLink;
                    yield return veFavorite;
                }
                else
                {
                    yield return f;
                }
            }
        }

        [HttpGet]
        public PendingMigrationCheckResult CurrentUserHasPendingMigrations()
        {
            var user = ControllerHelper.UserDataAccess.GetUser(ControllerHelper.GetCurrentUserId());
            if (user.Access.RolePublicClient == AccessRoles.RoleOe3
                || user.Access.RolePublicClient == AccessRoles.RoleBVW
                || user.Access.RolePublicClient == AccessRoles.RoleAS
                || user.Access.RolePublicClient == AccessRoles.RoleBAR)
            {
                return sqlDataAccess.HasPendingMigrations(user.EmailAddress);
            }

            return new PendingMigrationCheckResult();
        }

        [HttpPost]
        public void MigrateFavorites(string source)
        {
            if (source.Equals("local", StringComparison.InvariantCultureIgnoreCase) ||
                source.Equals("public", StringComparison.InvariantCultureIgnoreCase))
            {
                var user = ControllerHelper.UserDataAccess.GetUser(ControllerHelper.GetCurrentUserId());
                if (sqlDataAccess.MigrateFavorites(user.Id, user.EmailAddress, source))
                {
                    return;
                }

                throw new InvalidOperationException("Unexpected error during migration. See server logs for details.");
            }
        }

        private List<VeExportRecord> ConvertExportData(List<IFavorite> searchResults)
        {
            return searchResults.Select(item => new VeExportRecord
            {
                // ReSharper disable PossibleNullReferenceException
                ReferenceCode = (item as VeFavorite).ReferenceCode,
                FileReference = VeExportRecordHelper.GetCustomField((item as VeFavorite).CustomFields, "aktenzeichen"),
                Title = item.Title,
                CreationPeriod = (item as VeFavorite).CreationPeriod,
                WithinInfo = (item as VeFavorite).WithinInfo,
                Level = (item as VeFavorite).Level,
                Accessibility = VeExportRecordHelper.GetCustomField((item as VeFavorite).CustomFields, "zugänglichkeitGemässBga")
                // ReSharper enable PossibleNullReferenceException
            }).ToList();
        }

    }
}