using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Entities;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CMI.Web.Frontend.api.Providers
{
    public class EntityProvider : IEntityProvider
    {
        private readonly IElasticService elasticService;
        private readonly IElasticSettings elasticSettings;
        private readonly IModelData modelData;
        private readonly ITranslator translator;

        public EntityProvider(ITranslator translator, IElasticService elasticService, IElasticSettings elasticSettings, IModelData modelData)
        {
            this.translator = translator;
            this.elasticService = elasticService;
            this.elasticSettings = elasticSettings;
            this.modelData = modelData;
        }

        public async Task<string[]> GetArchivplanRootNodes(UserAccess access, string role, string language)
        {
            var entities = await elasticService.QueryForRootNodes<TreeRecord>(access);
            return entities.Entries.Select(e => e.Data.ArchiveRecordId).ToArray();
        }


        public async Task<string> GetArchivplanHtml(string id, UserAccess access, string role, string language)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var entities = await elasticService.QueryForId<TreeRecord>(id, access);
            var result = CreateHtml(entities?.Entries.Select(e => e.Data), role, language);
            Debug.WriteLine($"Get ArchivplanHtml for {id} {stopwatch.ElapsedMilliseconds}ms");
            return result;
        }

        public async Task<string> GetArchivplanChildrenHtml(string id, UserAccess access, string role, string language)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var entities = await elasticService.QueryForParentId(id, access);
            Debug.WriteLine($"Fetching child records took {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.Start();
            var html = CreateHtml(entities, role, language);
            Debug.WriteLine($"Creating html from records took {stopwatch.ElapsedMilliseconds}ms");
            return html;
        }

        public async Task<Entity<T>> GetEntity<T>(string id, UserAccess access, Paging paging = null) where T : TreeRecord, new()
        {
            var metaOptions = new EntityMetaOptions
            {
                FetchAncestors = true,
                FetchChildren = true,
                ChildrenPaging = paging
            };

            var found = await elasticService.QueryForId<T>(id, access);
            var result = found != null && found.Exception == null
                ? await CreateEntityResult(access, found, metaOptions)
                : null;

            return result;
        }


        public async Task<EntityResult<T>> GetEntities<T>(List<string> ids, UserAccess access, Paging paging = null) where T : TreeRecord, new()
        {
            var metaOptions = new EntityMetaOptions
            {
                FetchAncestors = false,
                FetchChildren = true,
                ChildrenPaging = paging
            };

            var found = await elasticService.QueryForIds<T>(ids, access, new Paging { Take = ElasticService.ELASTIC_SEARCH_HIT_LIMIT, Skip = 0 });
            var result = await CreateEntitiesResult(access, null, found, metaOptions);

            return result;
        }


        public async Task<ISearchResult> Search<T>(SearchParameters search, UserAccess access)
            where T : TreeRecord
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            if (search.Paging == null)
            {
                search.Paging = new Paging { Skip = 0, Take = 10 };
            }


            Log.Debug("Search: input={0}, query={1}",
                search,
                JsonConvert.SerializeObject(search.Query));

            var query = ElasticQueryBuilder.BuildElasticQuery(search, access);
            var found = await elasticService.RunQuery<T>(query, access);

            Log.Debug("Search.Entities: {0} ({1}ms)",
                found?.RequestInfo,
                found?.TimeInMilliseconds);

            Debug.WriteLine($"Search time: {found?.TimeInMilliseconds:N}ms");

            if (found?.Exception == null)
            {
                var result = CreateSearchResult(found, search.Paging);
                stopwatch.Stop();
                result.ExecutionTimeInMilliseconds = stopwatch.ElapsedMilliseconds;
                return result;
            }

            return CreateErrorResult(found);
        }


        public async Task<ISearchResult> SearchByReferenceCodeWithoutSecurity<T>(string signatur)
            where T : TreeRecord
        {
            Log.Debug($"CheckIsValidSignatur: Signatur:={signatur}");
            var searchSignatur = signatur;
            if (signatur.Contains("\""))
            {
                searchSignatur = signatur.Replace("\"", string.Empty);
            }

            var elasticQuery = new ElasticQuery();
            var query = ElasticQueryBuilder.CreateQueryForSignatur(searchSignatur);
            elasticQuery.Query = query;

            var found = await elasticService.RunQueryWithoutSecurityFilters<T>(elasticQuery);

            Log.Debug("Search.Entities: {0} ({1}ms)",
                found?.RequestInfo,
                found?.TimeInMilliseconds);

            if (found?.Exception == null)
            {
                return CreateSearchResult(found);
            }

            return CreateErrorResult(found);
        }


        public async Task<List<Entity<T>>> GetResultAsEntities<T>(UserAccess access, ElasticQueryResult<T> result, EntityMetaOptions options = null)
            where T : TreeRecord, new()
        {
            var entities = result?.Entries ?? new List<Entity<T>>();
            var decorator = new EntityDecorator<T>(elasticService, elasticSettings, this, modelData);
            foreach (var entity in entities)
            {
                entity.Context = await decorator.GetAsDecoratedContext(entity, access, options);
            }

            return entities;
        }

        public async Task<string[]> GetCountriesFromElastic()
        {
            try
            {
                var result = await elasticService.GetLaender();
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        ///     Falls searchParameters i.O. ist, gibt die Methode string.Empty zurück.
        ///     Andernfalls eine Fehlermeldung.
        ///     Nicht technische Fehlermeldungen werden übersetzt.
        /// </summary>
        public string CheckSearchParameters(SearchParameters searchParameters, string language)
        {
            if (searchParameters == null)
            {
                return "The search parameter could not be created from the body. Check if the json is correctly formed.";
            }

            if (searchParameters.Query == null)
            {
                return "Query is missing";
            }

            if (searchParameters.Paging?.Take > 100)
            {
                return "The take parameter must be less or equal than 100";
            }


            var foundQueryTerm = false;
            var containsAllField = false;

            foreach (var searchGroup in searchParameters.Query.SearchGroups.IfNullReturnEmpty())
            foreach (var searchField in searchGroup.SearchFields.IfNullReturnEmpty())
            {
                if (searchField.Key == "allData")
                {
                    containsAllField = true;
                }

                if (!string.IsNullOrEmpty(searchField.Value))
                {
                    var withoutWildcards = searchField.Value.Replace("*", string.Empty).Replace("?", string.Empty);

                    if (searchField.Key == "allData")
                    {
                        if (withoutWildcards.Length < 2)
                        {
                            return translator.GetTranslation(language, "search.termToShortForAll",
                                "Bitte geben Sie für eine Suche nach allem mindestens 2 Zeichen ein.");
                        }
                    }
                    else
                    {
                        if (withoutWildcards.Length < 1)
                        {
                            return translator.GetTranslation(language, "search.termToShort",
                                "Bitte geben Sie für eine Suche mindestens 1 Zeichen ein.");
                        }
                    }

                    foundQueryTerm = true;
                }
            }

            if (!foundQueryTerm)
            {
                if (containsAllField)
                {
                    return translator.GetTranslation(language, "search.termToShortForAll",
                        "Bitte geben Sie für eine Suche nach allem mindestens 2 Zeichen ein.");
                }

                return translator.GetTranslation(language, "search.termToShort", "Bitte geben Sie für eine Suche mindestens 1 Zeichen ein.");
            }

            return string.Empty;
        }

        private string CreateHtml(IEnumerable<TreeRecord> treeRecords, string role, string language)
        {
            // To reduce download size all unneeded whitespace is eliminated
            var s = new StringBuilder();

            foreach (var treeRecord in treeRecords)
            {
                var row = $@"<ul class=""row""><li class=""recordTitle""><ul id=""Node{WebUtility.HtmlEncode(treeRecord.ArchiveRecordId)}"" tabindex=""0""><li><a id=""{WebUtility.HtmlEncode(treeRecord.ArchiveRecordId)}"" {GetClass(treeRecord)} aria-label=""{GetTranslationTreeNode(language, "expandLink", "Link aufklappen")} {WebUtility.HtmlEncode(treeRecord.Title)}""></a></li><li><span aria-label=""{GetTranslationTreeNode(language, treeRecord.Level.ToLower(), "Typ " + treeRecord.Level)}""  class=""{GetIconName(treeRecord.Level)}""></span></li><li><a data-toggle=""tooltip"" title=""{WebUtility.HtmlEncode(treeRecord.Title)}"" tabindex=""-1"" href=""{GetDetailUrl(treeRecord, language)}""><b>{WebUtility.HtmlEncode(treeRecord.ReferenceCode)}</b>&nbsp;{AnonymizeText(treeRecord.Title,language)}&nbsp;</a></li><li><span>{WebUtility.HtmlEncode(GetCreationPeriod(treeRecord))}{AddEyeOffIcon(treeRecord, language, role)}</span></li></ul></li><li {GetAISKeys(treeRecord)} id =""children{WebUtility.HtmlEncode(treeRecord.ArchiveRecordId)}"" class=""tree-node-children""></li></ul>";
                s.Append(row);
            }
            // Müssen einen leeren Dummy Eintrag einfügen, ansonsten der Baum in Edge einen "null" Eintrag anzeigt. 
            // Fall sollte nicht mehr auftreten, da ChildCount nun nur noch die effektiv "sichtbaren" Kindern wiederspiegelt.
            if (s.Length == 0)
            {
                s.Append(@"<span style=""display:none""></span>");
            }

            return s.ToString();
        }

        private string GetAISKeys(TreeRecord treeRecord)
        {
            if (long.TryParse(treeRecord.ArchiveRecordId, out var scopeId))
            {
                return $"data-scopeKey=\"{scopeId}\" data-DocKey=\"{WebUtility.HtmlEncode(elasticService.ActaProMappingProvider.GetActaProId(treeRecord.ArchiveRecordId))}\"";
            }
            scopeId = elasticService.ActaProMappingProvider.GetScopeId(treeRecord.ArchiveRecordId);

            return $"data-scopeKey=\"{scopeId}\" data-DocKey=\"{WebUtility.HtmlEncode(treeRecord.ArchiveRecordId)}\"";
        }

        private string AddEyeOffIcon(TreeRecord treeRecord, string language, string role)
        {
            if (treeRecord.IsAnonymized && role.Equals(AccessRoles.RoleBAR, StringComparison.InvariantCultureIgnoreCase))
            {
                var tooltip = translator.GetTranslation(language, "simpleHit.anonymToolTip", "Für nicht berechtigte NutzerInnen anonymisiert.");
                return $@"&nbsp;<span data-toggle=""tooltip"" title=""{tooltip}"" class=""glyphicon glyphicon-eye-close text-danger""></span>";
            }

            return string.Empty;
        }

        private string AnonymizeText(string value, string language, string pattern = "███")
        {
            var tooltip = translator.GetTranslation(language, "anonymized.Tooltip", "Aus Datenschutzgründen anonymisiert.");

            var html = $"<span data-toggle=\"tooltip\" data-placement=\"bottom\" class=text-anonymized title=\"{tooltip}\">{pattern}</span>";
            return Regex.Replace(WebUtility.HtmlEncode(value), pattern, html);
        }

        private string GetTranslationTreeNode(string language, string element, string text)
        {
            return translator.GetTranslation(language, "archivplan.treeNode." + element, text);
        }

        private string GetClass(TreeRecord tree)
        {
            return tree.ChildCount > 0 ? "class=\"tree-collapse icon icon--before icon--greater\"" : string.Empty;
        }

        private string GetCreationPeriod(TreeRecord tree)
        {
            return tree.CreationPeriod?.Text != null ? $"({tree.CreationPeriod.Text})" : string.Empty;
        }

        private string GetDetailUrl(TreeRecord tree, string language)
        {
            switch (language.ToLower())
            {
                case "de": return "#/de/archiv/einheit/" + tree.ArchiveRecordId;
                case "fr": return "#/fr/archive/unite/" + tree.ArchiveRecordId;
                case "it": return "#/it/archivio/unita/" + tree.ArchiveRecordId;
                case "en": return "#/en/archive/unit/" + tree.ArchiveRecordId;
                default: return null;
            }
        }

        private string GetIconName(string type)
        {
            switch (type.ToLower())
            {
                case "dossier":
                    return "typeicon glyphicon glyphicon-folder-open";
                case "subdossier":
                    return "typeicon glyphicon glyphicon-folder-minus";
                case "dokument":
                    return "typeicon glyphicon glyphicon-article";
                case "teilserie":
                case "serie":
                    return "typeicon glyphicon glyphicon-sort";
                case "teilbestand":
                    return "typeicon glyphicon glyphicon-cube-empty";
                case "hauptabteilung":
                case "beständeserie":
                case "akzession":
                case "archiv":
                case "bestand":
                    return "typeicon glyphicon glyphicon-show-big-thumbnails";
                default:
                    return "typeicon";
            }
        }


        private static ErrorSearchResult CreateErrorResult<T>(ElasticQueryResult<T> found) where T : TreeRecord
        {
            var result = new ErrorSearchResult();
            var error = new ApiError
            {
                StatusCode = found.Status
            };
            result.Error = error;
            return result;
        }

        public async Task<Entity<T>> CreateEntityResult<T>(UserAccess access, ElasticQueryResult<T> found, EntityMetaOptions metaOptions)
            where T : TreeRecord, new()
        {
            var entity = await GetResultAsEntities(access, found, metaOptions);
            return entity?.FirstOrDefault();
        }

        public async Task<EntityResult<T>> CreateEntitiesResult<T>(UserAccess access, Paging paging, ElasticQueryResult<T> found, EntityMetaOptions metaOptions)
            where T : TreeRecord, new()
        {
            var result = new EntityResult<T>();
            if (found.Exception != null)
            {
                return result;
            }

            var entities = await GetResultAsEntities(access, found, metaOptions);
            result.Items = entities;

            if (paging != null)
            {
                result.Paging = paging;
            }

            return result;
        }

        public static SearchResult<T> CreateSearchResult<T>(ElasticQueryResult<T> found, Paging paging) where T : TreeRecord
        {
            var result = new SearchResult<T>();
            var entities = found.Entries ?? new List<Entity<T>>();

            if (paging != null)
            {
                paging.Total = found.TotalNumberOfHits;
            }

            var data = new EntityResult<T>
            {
                Items = entities,
                Paging = paging
            };

            result.Entities = data;
            result.EnableExplanations = found.EnableExplanations;
            result.Facets = found.Facets;
            result.SearchTimeInMilliseconds = found.TimeInMilliseconds;
            return result;
        }

        public static SearchResult<T> CreateSearchResult<T>(ElasticQueryResult<T> found)
            where T : TreeRecord
        {
            var result = new SearchResult<T>();
            var entities = found.Entries;
            var data = new EntityResult<T>
            {
                Items = entities
            };
            result.Entities = data;
            result.EnableExplanations = found.EnableExplanations;
            result.Facets = found.Facets;

            return result;
        }
    }
}