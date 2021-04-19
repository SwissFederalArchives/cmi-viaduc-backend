using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using CMI.Contract.Common;

namespace CMI.Access.Sql.Viaduc
{
    public interface IApplicationRoleDataAccess
    {
        IEnumerable<ApplicationRole> GetRoles();

        IEnumerable<ApplicationRoleFeaturesInfo> GetFeaturesInfoForRoles(string sqlFilter = null, string sqlOrderBy = null, int offset = 0,
            int nuRows = 0);

        ApplicationRoleFeaturesInfo GetFeaturesInfoForRole(int roleId);

        bool InsertRoleFeature(IApplicationFeaturesAccess access, int roleId, ApplicationFeature feature);

        bool RemoveRoleFeature(IApplicationFeaturesAccess access, int roleId, ApplicationFeature feature);
    }

    public class ApplicationRoleDataAccess : DataAccess, IApplicationRoleDataAccess
    {
        private readonly string connectionString;

        public ApplicationRoleDataAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public IEnumerable<ApplicationRole> GetRoles()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM dbo.ApplicationRole";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return reader.ToRole<ApplicationRole>();
                        }
                    }
                }
            }
        }

        public IEnumerable<ApplicationRoleFeaturesInfo> GetFeaturesInfoForRoles(string sqlFilter = null, string sqlOrderBy = null, int offset = 0,
            int nuRows = 0)
        {
            const string prefix = "Feature_";
            var features = ApplicationFeatures.ApplicationFeaturesByIdentifier;
            using (var cn = new SqlConnection(connectionString))
            {
                var sqlFields = TAB + "r.Id";
                foreach (var feature in features)
                {
                    sqlFields += "," + EOL + TAB + "MAX(CASE WHEN (FeatureId = '" + feature.Key + "') THEN 1 ELSE 0 END) AS [" + prefix +
                                 feature.Key + "]";
                }

                sqlOrderBy = sqlOrderBy ?? "r.Id";

                var sql = @"
                    WITH roleFeatures AS (
	                    SELECT
                        " + sqlFields + @"
	                    FROM dbo.ApplicationRole r LEFT JOIN dbo.ApplicationRoleFeature rf ON (rf.RoleId = r.Id)
	                    " + (!string.IsNullOrEmpty(sqlFilter) ? "WHERE" + EOL + TAB + sqlFilter : string.Empty) + @"
                        GROUP BY r.Id
                    )
                    SELECT
	                    r.Identifier, r.Name, r.Description,
	                    rf.*
                    FROM
	                    ApplicationRole r INNER JOIN roleFeatures rf ON r.Id = rf.Id
                    ORDER BY
	                    " + sqlOrderBy + @"
                ";
                if (nuRows > 0)
                {
                    sql += $"OFFSET ({offset}) ROWS FETCH NEXT ({nuRows}) ROWS ONLY";
                }

                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = sql;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var info = reader.ToRole<ApplicationRoleFeaturesInfo>();
                            foreach (var feature in features)
                            {
                                if (Convert.ToInt32(reader[prefix + feature.Key]) > 0)
                                {
                                    info.Features.Add(feature.Value.ToInfo());
                                }
                            }

                            yield return info;
                        }
                    }
                }
            }
        }

        public ApplicationRoleFeaturesInfo GetFeaturesInfoForRole(int roleId)
        {
            return GetFeaturesInfoForRoles("(r.Id = '" + roleId + "')").FirstOrDefault();
        }

        public bool InsertRoleFeature(IApplicationFeaturesAccess access, int roleId, ApplicationFeature feature)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = ""
                                      + "IF NOT EXISTS(SELECT * FROM dbo.ApplicationRoleFeature WHERE (RoleId = @roleId) AND (FeatureId = @featureId)) BEGIN"
                                      + "  INSERT INTO dbo.ApplicationRoleFeature (RoleId,FeatureId) OUTPUT INSERTED.ID VALUES (@roleId,@featureId); "
                                      + "END ELSE BEGIN"
                                      + "  SELECT ID FROM dbo.ApplicationRoleFeature WHERE (RoleId = @roleId) AND (FeatureId = @featureId)"
                                      + "END";

                    cmd.AddParameter("roleId", SqlDbType.Int, roleId);
                    cmd.AddParameter("featureId", SqlDbType.NVarChar, feature.ToString());

                    var userRoleId = Convert.ToInt32(cmd.ExecuteScalar());

                    return userRoleId > 0;
                }
            }
        }

        public bool RemoveRoleFeature(IApplicationFeaturesAccess access, int roleId, ApplicationFeature feature)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM dbo.ApplicationRoleFeature WHERE (RoleId = @roleId) AND (FeatureId = @featureId);";
                    cmd.AddParameter("roleId", SqlDbType.Int, roleId);
                    cmd.AddParameter("featureId", SqlDbType.NVarChar, feature.ToString());
                    var nuRows = cmd.ExecuteNonQuery();
                    return nuRows == 1;
                }
            }
        }
    }
}