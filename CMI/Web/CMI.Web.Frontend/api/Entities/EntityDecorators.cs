using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace CMI.Web.Frontend.api.Entities
{
    public class EntityDecorator<T> where T : TreeRecord, new()
    {
        // Json output
        private const string ancestorsKey = "ancestors";
        private const string childrenKey = "children";
        private const string childrenPagingKey = "childrenPaging";

        // Custom fields
        private const string customFieldKey = "customFields";
        private const string customFieldPrefix = customFieldKey + ".";

        private readonly IElasticService elasticService;
        private readonly IElasticSettings elasticSettings;
        private readonly IEntityProvider entityProvider;
        private readonly IModelData modelData;

        public EntityDecorator(IElasticService elasticService, IElasticSettings elasticSettings, IEntityProvider entityProvider, IModelData modelData)
        {
            this.elasticService = elasticService;
            this.elasticSettings = elasticSettings;
            this.entityProvider = entityProvider;
            this.modelData = modelData;
        }

        public class GetAncestorsResult
        {
            public List<Entity<T>> Entities { get; set; }
            public int Depth { get; set; }
        }

        public async Task<GetAncestorsResult> GetAncestors(Entity<T> entity, UserAccess access)
        {
            var ancestors = new List<Entity<T>>();
            var entityId = entity.Data.ArchiveRecordId;

            var maxDepth = 0;
            var items = entity.Data.ArchiveplanContext;

            if (items != null)
            {
                var depth = 0;
                foreach (var contextItem in items)
                {
                    var id = int.TryParse(contextItem.ArchiveRecordId, out _) ? elasticService.ActaProMappingProvider.GetActaProId(contextItem.ArchiveRecordId) : contextItem.ArchiveRecordId;

                    if (entityId.Equals(contextItem.ArchiveRecordId))
                    {
                        continue;
                    }

                    // Falls keine ActaProId für die ScopeId gibt
                    if (string.IsNullOrEmpty(id))
                    {
                        id = contextItem.ArchiveRecordId;
                    }

                    Entity<T> item;
                    var isAnonymized = false;
                    // Aus Performance Gründen holen wir das Detailitem nur, wenn das Items grundsätzlich anonymisiert sein könnte
                    if (contextItem.Protected)
                    {
                        var record = (await elasticService.QueryForId<TreeRecord>(id, access)).Data.Items.FirstOrDefault()?.Data;
                        contextItem.Title = record?.Title;
                        isAnonymized = true;
                    }

                    item = new Entity<T>
                    {
                        Data = new T
                        {
                            ArchiveRecordId = id,
                            Title = contextItem.Title,
                            ReferenceCode = contextItem.RefCode,
                            IsAnonymized = isAnonymized,
                        }
                    };

                    var ancestorOptions = new EntityMetaOptions
                    {
                        SetDepth = depth
                    };

                    var context = await GetAsDecoratedContext(item, access, ancestorOptions);
                    item.Context = context;

                    ancestors.Add(item);
                    maxDepth = Math.Max(maxDepth, depth);
                    depth += 1;
                }
            }

            if (ancestors.Count > 0)
            {
                ancestors = ancestors.OrderBy(anc => anc.Depth).ToList();
            }

            return new GetAncestorsResult { Entities = ancestors, Depth = maxDepth };
        }


        public async Task<JObject> GetAsDecoratedContext(Entity<T> entity, UserAccess access, EntityMetaOptions options = null)
        {
            var hasContext = false;

            var context = new JObject();

            entity.MetaData = GetMetadata(entity.Data, access);
            options = options ?? EntityMetaOptions.DefaultOptions;
            var depth = options.SetDepth;

            // ancestors
            if (options.FetchAncestors)
            {
                var result = await GetAncestors(entity, access);
                var ancestors = result.Entities;
                depth = result.Depth;
                if (ancestors.Count > 0)
                {
                    hasContext = true;
                    context.Add(ancestorsKey, JArray.FromObject(ancestors));
                    depth += 1;
                }
            }

            // own depth
            entity.Depth = depth;
            depth += 1;

            // children
            if (options.FetchChildren)
            {
                var result = await GetChildren(entity.Data, depth, access, options?.ChildrenPaging);
                if (result.Items.Count > 0)
                {
                    hasContext = true;
                    JsonHelper.AddOrSet(context, childrenKey, JArray.FromObject(result.Items), true);
                    if (result.Paging != null)
                    {
                        JsonHelper.AddOrSet(context, childrenPagingKey, JObject.FromObject(result.Paging), true);
                    }
                }
            }

            return hasContext ? context : null;
        }


        public async Task<EntityResult<T>> GetChildren(T entity, int setDepth, UserAccess access, Paging paging)
        {
            paging ??= new Paging { OrderBy = "treeSequence", SortOrder = "Ascending" };

            var result = new EntityResult<T>
            {
                Items = new List<Entity<T>>(),
                Paging = paging
            };

            if (entity.IsLeaf)
            {
                return result;
            }

            var query = new ElasticQuery();
            var actaProId = entity.ArchiveRecordId;
            if (long.TryParse(entity.ArchiveRecordId, out var scopeId))
            {
                actaProId = elasticService.ActaProMappingProvider.GetActaProId(entity.ArchiveRecordId);
            }
            else
            {
                scopeId = elasticService.ActaProMappingProvider.GetScopeId(entity.ArchiveRecordId);
            }
            
            query.Query = new BoolQuery
            {
                Should = new List<Query>
                {
                    new TermsQuery(new Field(elasticSettings.ParentIdField.ToLowerCamelCase()), new TermsQueryField(new List<FieldValue>{actaProId})),
                    new TermsQuery(new Field(elasticSettings.ParentIdField.ToLowerCamelCase()), new TermsQueryField(new List<FieldValue>{scopeId}))
                },
                MustNot = new List<Query>()
                {
                    new TermsQuery(new Field(elasticSettings.IdField.ToLowerCamelCase()), new TermsQueryField(new List<FieldValue>{entity.ArchiveRecordId}))
                }
            };

            query.SearchParameters.Paging = paging;
            query.SearchParameters.Options = new SearchOptions { EnableAggregations = false, EnableExplanations = false, EnableHighlighting = false };

            var queryResult = await elasticService.RunQuery<T>(query, access);
            if (queryResult.Entries != null)
            {
                result.Items = await entityProvider.GetResultAsEntities(access, queryResult, new EntityMetaOptions
                {
                    SetDepth = setDepth
                });

                result.Paging.Total = queryResult.TotalNumberOfHits;
            }
            else
            {
                result.Paging.Total = 0;
            }

            return result;
        }



        private JObject GetMetadata(TreeRecord entity, UserAccess access)
        {
            if (entity?.Level == null)
            {
                return null;
            }

            var type = modelData.GetEntityType(entity);
            if (type == null)
            {
                Log.Information($"No type found for entiy level {entity?.Level} and template {entity.DisplayTemplateName}");
                return null;
            }

            var language = access.Language ?? WebHelper.DefaultLanguage;

            JObject metadata = null;
            var jsonEntity = JObject.FromObject(entity);
            var customFields = JsonHelper.GetTokenValue<JObject>(jsonEntity, customFieldKey, true) ?? new JObject();

            var categories = type.MetaCategories ?? new List<ModelTypeMetaCategory>();

            foreach (var category in categories)
            {
                var attributes = new JObject();

                // Nur wenn die Sektion (category) Felder hat und wir mindestens ein öffentliches Feld haben, oder der Benutzer ein BAR Benutzer ist, gehen wir überhaupt weiter.
                // (Fall abfangen, dass eine Kategorie nur interne Felder hat
                if (category?.Fields != null && (category.Fields.Any(f => f.Visibility == (int) DataElementVisibility.@public) ||
                                                 access.RolePublicClient == AccessRoles.RoleBAR))
                {
                    foreach (var field in category.Fields)
                    {
                        // Interne Felder sind nur für BAR Benutzer sichtbar
                        if (field.Visibility == (int) DataElementVisibility.@internal && access.RolePublicClient != AccessRoles.RoleBAR)
                        {
                            continue;
                        }

                        JToken token = null;
                        var name = field.Name.ToLowerCamelCase();
                        if (name.StartsWith(customFieldPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            var subKey = field.Key.Substring(customFieldPrefix.Length);
                            var subName = name.Substring(customFieldPrefix.Length);
                            name = subName.ToLowerCamelCase();
                            token = customFields.GetTokenByKey(subName, true) ?? customFields.GetTokenByKey(subKey, true);
                        }
                        else
                        {
                            token = jsonEntity.GetTokenByKey(name, true) ?? jsonEntity.GetTokenByKey(field.Key, true);
                        }

                        if (token != null)
                        {
                            var value = field.AsFieldValue(token, language);
                            if (value != null)
                            {
                                attributes.Add(name, value);
                            }
                        }
                    }
                }

                if (attributes.Children().Any())
                {
                    metadata = metadata ?? new JObject();

                    if (category.Labels != null && category.Labels.Any())
                    {
                        var title = category.Labels.ContainsKey(language) ? category.Labels[language] : category.Labels.Values.First();
                        JsonHelper.AddOrSet(attributes, "_title", title);
                    }

                    metadata.Add(category.Identifier.ToLowerCamelCase(), attributes);
                }
            }

            return metadata;
        }
    }
}