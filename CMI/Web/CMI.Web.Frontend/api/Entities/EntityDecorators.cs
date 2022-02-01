using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using Nest;
using Newtonsoft.Json.Linq;
using Serilog;

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

        public List<Entity<T>> GetAncestors(Entity<T> entity, UserAccess access, out int maxDepth)
        {
            var ancestors = new List<Entity<T>>();
            var entityId = entity.Data.ArchiveRecordId;

            maxDepth = 0;
            var items = entity.Data.ArchiveplanContext;

            if (items != null)
            {
                var depth = 0;
                foreach (var contextItem in items)
                {
                    var id = contextItem.ArchiveRecordId;
                    if (entityId.Equals(id))
                    {
                        continue;
                    }

                    var item = new Entity<T>
                    {
                        Data = new T
                        {
                            ArchiveRecordId = id,
                            Title = contextItem.Title,
                            ReferenceCode = contextItem.RefCode
                        }
                    };

                    var ancestorOptions = new EntityMetaOptions
                    {
                        SetDepth = depth
                    };

                    var context = GetAsDecoratedContext(item, access, ancestorOptions);
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

            return ancestors;
        }


        public JObject GetAsDecoratedContext(Entity<T> entity, UserAccess access, EntityMetaOptions options = null)
        {
            var hasContext = false;

            var context = new JObject();

            entity.MetaData = GetMetadata(entity.Data, access);
            options = options ?? EntityMetaOptions.DefaultOptions;
            var depth = options.SetDepth;

            // ancestors
            if (options.FetchAncestors)
            {
                var ancestors = GetAncestors(entity, access, out depth);
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
                var result = GetChildren(entity.Data, depth, access, options?.ChildrenPaging);
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

        public EntityResult<T> GetChildren(T entity, int setDepth, UserAccess access, Paging paging)
        {
            paging ??= new Paging {OrderBy = "treeSequence", SortOrder = "Ascending"};
            
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
            var id = entity.ArchiveRecordId;

            query.Query = new BoolQuery
            {
                Filter = new QueryContainer[]
                {
                    new TermQuery
                    {
                        Field = elasticSettings.ParentIdField,
                        Value = id
                    }
                },
                MustNot = new QueryContainer[]
                {
                    new TermQuery
                    {
                        Field = elasticSettings.IdField,
                        Value = id
                    }
                }
            };

            query.SearchParameters.Paging = paging;
            query.SearchParameters.Options = new SearchOptions {EnableAggregations = false, EnableExplanations = false, EnableHighlighting = false};

            var queryResult = elasticService.RunQuery<T>(query, access);
            if (queryResult.Entries != null)
            {
                result.Items = entityProvider.GetResultAsEntities(access, queryResult, new EntityMetaOptions
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
                Log.Information($"No type found for entiy level {entity?.Level} and external template {entity.ExternalDisplayTemplateName}");
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