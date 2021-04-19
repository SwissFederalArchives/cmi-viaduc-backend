using System;
using System.Collections.Generic;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Entities;

namespace CMI.Web.Frontend.Helpers
{
    public class FrontendTranslationHelper : TranslationHelper
    {
        protected override void Initialize()
        {
            base.Initialize();

            var vdInfo = Viaduc = new AppInfo(this, "viaducclient", "vd", "app;app/modules/client");
            vdInfo.AppDatas = new List<AppData>
            {
                new JsonAppData
                {
                    Info = "Public/Settings",
                    Root = FrontendSettingsViaduc.Instance.GetServerSettings(),
                    Mappings = new List<JsonDataMapping>
                    {
                        new JsonDataMapping
                        {
                            Select = "$.search.advancedSearchFields[*]",
                            Map = node => new Dictionary<string, string>
                            {
                                {
                                    "metadata.searchFields." + JsonHelper.GetTokenValue<string>(node, "key"),
                                    JsonHelper.GetTokenValue<string>(node, "displayName")
                                }
                            }
                        },
                        new JsonDataMapping
                        {
                            Select = "$.search.simpleSearchSortingFields[*]",
                            Map = node => new Dictionary<string, string>
                            {
                                {
                                    "metadata.sortFields." + StringHelper.AddToString(JsonHelper.GetTokenValue<string>(node, "orderBy"), ".",
                                        JsonHelper.GetTokenValue<string>(node, "sortOrder")),
                                    JsonHelper.GetTokenValue<string>(node, "displayName")
                                }
                            }
                        }
                    }
                },
                new MetaAppData(new ModelData())
                {
                    Info = "Model"
                }
            };
        }


        public class MetaAppData : AppData
        {
            private readonly IModelData model;

            public MetaAppData(IModelData model)
            {
                this.model = model;
            }

            protected TranslationHelper Helper { get; set; }

            public override void Process(TranslationHelper helper, ProcessResult result)
            {
                Helper = helper;
                foreach (var modelType in model.TypesByName)
                {
                    result.currentInfo = modelType.Key;
                    ProcessModelType(result, modelType.Value);
                }
            }

            protected void ProcessModelType(ProcessResult result, ModelType modelType)
            {
                var categories = modelType.OwnMetaCategories ?? new List<ModelTypeMetaCategory>();
                foreach (var category in categories)
                {
                    try
                    {
                        Helper.AddOrUpdateEntry(result, "metadata.title." + category.Identifier, category.Label);

                        var fields = category.Fields ?? new List<ModelTypeField>();

                        foreach (var field in fields)
                        {
                            try
                            {
                                Helper.AddOrUpdateEntry(result, "metadata.label." + field.Name, field.Label);
                            }
                            catch (Exception ex)
                            {
                                result.addInfo(string.Format("error for {0}.{1}: {2}", category.Identifier, field.Key, ex.Message));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.addInfo(string.Format("error for {0}: {1}", category.Identifier, ex.Message));
                    }
                }
            }
        }
    }
}