using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CMI.Contract.Common;
using Newtonsoft.Json;

namespace CMI.Access.Sql.Viaduc
{
    public class ApplicationRole
    {
        public int Id { get; set; }

        public string Identifier { get; set; }
        public string Name { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Description { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }
    }

    public class ApplicationRoleFeaturesInfo : ApplicationRole
    {
        public IList<ApplicationFeatureInfo> Features { get; } = new List<ApplicationFeatureInfo>();
    }

    public static class ApplicationRoleExtensions
    {
        public static T ToRole<T>(this SqlDataReader reader, int roleId = 0) where T : ApplicationRole, new()
        {
            var role = new T
            {
                Id = roleId > 0 ? roleId : Convert.ToInt32(reader["Id"])
            };

            reader.PopulateProperties(role);

            return role;
        }
    }
}