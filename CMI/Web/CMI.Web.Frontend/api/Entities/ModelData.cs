using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Templates;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Frontend.api.Entities
{
    public class ModelData : IModelData
    {
        public ModelData()
        {
            lock (lockObject)
            {
                Reset();
            }
        }

        public void Reset()
        {
            modelData = null;
            typesByNameCamelCased = new Dictionary<string, ModelType>();
        }

        public ModelType GetTypeByName(string typeName)
        {
            AssertInited();
            var name = typeName.ToLowerCamelCase();
            return typesByNameCamelCased.ContainsKey(name) ? typesByNameCamelCased[name] : null;
        }

        public ModelType GetEntityType(TreeRecord entity)
        {
            var type = GetTypeByName(BaseTypeName);

            if (entity?.Level != null)
            {
                if (GetTypeByName(entity.Level) != null)
                {
                    type = GetTypeByName(entity.Level) ?? type;
                }
                else if (!string.IsNullOrEmpty(entity.ExternalDisplayTemplateName))
                {
                    var formId = entity.ExternalDisplayTemplateName.Split(':')[0];
                    var templatetypeName = string.Format(TemplateBaseSubNamePattern, formId);
                    type = GetTypeByName(templatetypeName) ?? type;
                }
            }

            return type;
        }

        #region Constants & Properties

        public const string TypesKey = "Types";
        public const string TypesNameKey = "Name";
        public const string TypesSuperKey = "Super";

        public const string TypesMetaKey = "Meta";
        public const string MetaCategoryFieldsKey = "Fields";

        public const string BaseTypeName = "base";
        public const string TemplateBaseTypeName = "templateBase";
        public const string TemplateBaseSubNamePattern = "template_{0}";


        private readonly object lockObject = new object();
        private JObject modelData;

        public JObject Data
        {
            get
            {
                AssertInited();
                return modelData;
            }
        }

        private Dictionary<string, ModelType> typesByNameCamelCased;

        public IDictionary<string, ModelType> TypesByName
        {
            get
            {
                AssertInited();
                return typesByNameCamelCased;
            }
        }

        #endregion

        #region Private methods

        private void AssertInited()
        {
            if (modelData == null)
            {
                lock (lockObject)
                {
                    if (modelData != null)
                    {
                        return;
                    }

                    InitModelData();
                }
            }
        }

        private void InitModelData()
        {
            modelData = new JObject();

            // inject data
            var accessRoles = Reflective.GetConstants<string>(typeof(AccessRoles));
            var fieldTypes = Reflective.GetConstants<string>(typeof(ElasticFieldTypes));

            JsonHelper.AddOrSet(modelData, "accessRoles", JObject.FromObject(accessRoles));
            JsonHelper.AddOrSet(modelData, "fieldTypes", JObject.FromObject(fieldTypes));

            // types
            var types = JsonHelper.GetTokenValue<JObject>(modelData, TypesKey, true);
            if (types != null)
            {
                foreach (JProperty property in types.Children())
                {
                    var t = ModelType.CreateFrom(property);
                    typesByNameCamelCased[t.Name.ToLowerCamelCase()] = t;
                }
            }

            // templates
            foreach (var template in TemplateDefinitions.Templates)
            {
                var t = ModelType.CreateFrom(template);
                typesByNameCamelCased[t.Name.ToLowerCamelCase()] = t;
            }

            // setup Super
            foreach (var type in typesByNameCamelCased.Values)
            {
                var superName = (type.SuperName ?? string.Empty).ToLowerCamelCase();
                if (!string.IsNullOrEmpty(superName) && typesByNameCamelCased.ContainsKey(superName))
                {
                    type.Super = typesByNameCamelCased[superName];
                }
            }
        }

        #endregion
    }

    public class ModelType
    {
        public string Name { get; internal set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SuperName { get; internal set; }

        [JsonIgnore] public ModelType Super { get; set; }

        [JsonIgnore] public IList<ModelTypeMetaCategory> OwnMetaCategories { get; internal set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IList<ModelTypeMetaCategory> MetaCategories
        {
            get
            {
                var t = this;
                while (t != null && t.OwnMetaCategories == null)
                {
                    t = t.Super;
                }

                return t?.OwnMetaCategories;
            }
        }

        public static ModelType CreateFrom(JProperty typeProperty)
        {
            var t = new ModelType
            {
                Name = typeProperty.Name,
                SuperName = JsonHelper.FindTokenValue<string>(typeProperty, ModelData.TypesSuperKey, true),
                OwnMetaCategories = null
            };

            var categories = JsonHelper.FindToken(typeProperty.Value, ModelData.TypesMetaKey, true);
            if (categories != null)
            {
                foreach (JProperty categoryProperty in categories.Children())
                {
                    t.OwnMetaCategories = t.OwnMetaCategories ?? new List<ModelTypeMetaCategory>();
                    var category = ModelTypeMetaCategory.CreateFrom(categoryProperty);
                    t.OwnMetaCategories.Add(category);
                }
            }

            return t;
        }

        public static ModelType CreateFrom(Template template)
        {
            var t = new ModelType
            {
                Name = string.Format(ModelData.TemplateBaseSubNamePattern, template.FormId),
                SuperName = ModelData.TemplateBaseTypeName,
                OwnMetaCategories = null
            };

            var sections = template.Sections ?? new List<TemplateSection>();
            foreach (var section in sections)
            {
                t.OwnMetaCategories = t.OwnMetaCategories ?? new List<ModelTypeMetaCategory>();
                var category = ModelTypeMetaCategory.CreateFrom(section);
                t.OwnMetaCategories.Add(category);
            }

            return t;
        }

        public bool IsSubTypeOf(string superName)
        {
            var t = Super;
            while (t != null && t.Name != superName)
            {
                t = t.Super;
            }

            return t != null;
        }

        public bool IsSubTypeOf(ModelType superType)
        {
            return IsSubTypeOf(superType.Name);
        }
    }

    public class ModelTypeMetaCategory
    {
        public string Identifier { get; protected set; }

        public string Label { get; protected set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Labels { get; protected set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IList<ModelTypeField> Fields { get; protected set; }

        public static ModelTypeMetaCategory CreateFrom(JProperty property)
        {
            var m = new ModelTypeMetaCategory
            {
                Identifier = property.Name,
                Fields = null
            };

            var fields = JsonHelper.FindToken(property, ModelData.MetaCategoryFieldsKey);
            if (fields != null)
            {
                foreach (var field in fields.Children())
                {
                    var categoryField = ModelTypeField.CreateFrom(field);
                    if (categoryField != null)
                    {
                        m.Fields = m.Fields ?? new List<ModelTypeField>();
                        m.Fields.Add(categoryField);
                    }
                }
            }

            return m;
        }

        public static ModelTypeMetaCategory CreateFrom(TemplateSection section)
        {
            var m = new ModelTypeMetaCategory
            {
                Identifier = section.SectionId,
                Fields = null
            };

            if (section.SectionLabels != null && section.SectionLabels.Any())
            {
                m.Labels = section.SectionLabels;
            }

            var labels = m.Labels ?? new Dictionary<string, string>();
            m.Label = labels.ContainsKey(WebHelper.DefaultLanguage) ? labels[WebHelper.DefaultLanguage] : m.Identifier;

            var templateFields = section.Fields ?? new List<TemplateField>();
            foreach (var templateField in templateFields)
            {
                var categoryField = ModelTypeField.CreateFrom(templateField);
                if (categoryField != null)
                {
                    m.Fields = m.Fields ?? new List<ModelTypeField>();
                    m.Fields.Add(categoryField);
                }
            }

            return m;
        }
    }

    public class ModelTypeField
    {
        public const string KeyTag = "key";
        public const string NameTag = "name";

        public const string TypeTag = "type";
        public const string LabelTag = "label";
        public const string ValueTag = "value";
        public const string VisibilityTag = "visibility";

        // Key in index
        public string Key { get; protected set; }

        // Name for api
        public string Name { get; protected set; }

        public string Type { get; protected set; }
        public string Label { get; protected set; }
        public int Visibility { get; protected set; }

        public IDictionary<string, string> Labels { get; protected set; }

        public static ModelTypeField CreateFrom(string key, string name = null, JToken token = null)
        {
            var f = new ModelTypeField
            {
                Key = key
            };
            if (token != null)
            {
                f.Name = JsonHelper.FindTokenValue<string>(token, NameTag, true) ?? f.Name;
                f.Type = JsonHelper.FindTokenValue<string>(token, TypeTag, true);
                f.Label = JsonHelper.FindTokenValue<string>(token, LabelTag, true);
                f.Visibility = JsonHelper.FindTokenValue<int>(token, VisibilityTag, true);
            }

            if (f.Name == null)
            {
                f.Name = f.Key;
            }

            return f.Key != null ? f : null;
        }

        public static ModelTypeField CreateFrom(string key, JToken token = null)
        {
            return CreateFrom(key, null, token);
        }

        public static ModelTypeField CreateFrom(JToken token)
        {
            if (token.Type == JTokenType.String)
            {
                return CreateFrom(token.Value<string>(), null);
            }

            return CreateFrom(JsonHelper.FindTokenValue<string>(token, KeyTag, true), token);
        }

        public static ModelTypeField CreateFrom(TemplateField field)
        {
            var f = new ModelTypeField
            {
                Key = field.DbFieldName.ToLowerCamelCase(),
                Type = field.ElasticType,
                Label = field.FieldLabels.ContainsKey(WebHelper.DefaultLanguage) ? field.FieldLabels[WebHelper.DefaultLanguage] : field.DbFieldName,
                Visibility = field.Visibility ?? (int) DataElementVisibility.@internal,
                Labels = field.FieldLabels
            };

            f.Name = StringHelper.GetNormalizedKey(f.Key);

            return f.Key != null ? f : null;
        }

        public virtual JToken AsFieldValue(JToken value, string language)
        {
            if (string.IsNullOrEmpty(Type))
            {
                return value;
            }

            if (JsonHelper.IsNullOrEmpty(value))
            {
                return null;
            }

            var json = value as JObject ?? new JObject();

            if (json[TypeTag] == null)
            {
                json.Add(TypeTag, Type);
            }

            if (json[LabelTag] == null && !string.IsNullOrEmpty(Label))
            {
                var label = Label;
                if (!WebHelper.DefaultLanguage.Equals(language) && Labels != null && Labels.ContainsKey(language))
                {
                    label = Labels[language];
                }

                json.Add(LabelTag, label);
            }

            if (json[VisibilityTag] == null)
            {
                json.Add(VisibilityTag, Visibility);
            }

            if (value.Type != JTokenType.Object)
            {
                json.Add(ValueTag, value);
            }

            return json;
        }
    }
}