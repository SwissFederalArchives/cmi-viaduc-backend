using System.Collections.Generic;
using CMI.Contract.Common;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Frontend.api.Entities
{
    public interface IModelData
    {
        JObject Data { get; }
        IDictionary<string, ModelType> TypesByName { get; }
        void Reset();

        ModelType GetTypeByName(string typeName);
        ModelType GetEntityType(TreeRecord entity);
    }
}