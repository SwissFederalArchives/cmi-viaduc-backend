using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;

namespace CMI.Access.Sql.Viaduc
{
    public class UserUsageStatisticAccess
    {
        private readonly string connectionString;
        private readonly UsageType usageType;


        public UserUsageStatisticAccess(string connectionString, UsageType usageType)
        {
            this.connectionString = connectionString;
            this.usageType = usageType;
        }


        public UserUsageStatisticData GetUserUsage(string userId)
        {
            UserUsageStatisticData usage = null;

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT Usage FROM UsageStatistic{usageType} WHERE UserId = @userId";
                    cmd.AddParameter("userId", SqlDbType.NVarChar, userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var usageString = Convert.ToString(reader["Usage"]);
                            if (!string.IsNullOrEmpty(usageString))
                            {
                                usage = JsonConvert.DeserializeObject<UserUsageStatisticData>(usageString);
                            }
                        }
                    }
                }
            }

            return usage;
        }

        public void InsertOrUpdateUserUsage(string userId, UserUsageStatisticData usage)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        $"UPDATE UsageStatistic{usageType} SET Usage = @usage WHERE UserId = @userId;" +
                        "IF @@ROWCOUNT = 0 BEGIN" +
                        $"  INSERT INTO UsageStatistic{usageType} (UserId, Usage) VALUES (@userId, @usage);" +
                        "END";
                    cmd.AddParameter("userId", SqlDbType.NVarChar, userId);
                    var usageString = usage != null ? JsonConvert.SerializeObject(usage, Formatting.Indented) : string.Empty;
                    cmd.AddParameter("usage", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(usageString) ? (object) DBNull.Value : usageString);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }


    public class UsageBeginAndCount
    {
        public UsageBeginAndCount(DateTime time)
        {
            Begin = time;
        }

        public DateTime Begin { get; set; }
        public int Count { get; set; } = 0;
    }


    public class UserUsageStatisticData : Dictionary<int, UsageBeginAndCount>
    {
    }


    public enum UsageType
    {
        Display,
        Download
    }
}