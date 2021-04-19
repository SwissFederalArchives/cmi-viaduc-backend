using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using CMI.Access.Sql.Viaduc.AblieferndeStellen.Dto;

namespace CMI.Access.Sql.Viaduc.AblieferndeStellen
{
    public class AblieferndeStelleDataAccess : DataAccess, IAblieferndeStelleDataAccess
    {
        private readonly string connectionString;

        public AblieferndeStelleDataAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }

        private string Sql => @"SELECT
                AblieferndeStelleId,
                Bezeichnung,
                Kuerzel,
                Kontrollstellen,
	            CreatedOn,
	            CreatedBy,
	            ModifiedOn,
	            ModifiedBy,
                (SELECT
                t.TokenId,
                t.Token,
                t.Bezeichnung
                    FROM AsTokenMapping m
                INNER
                    JOIN AblieferndeStelleToken t
                ON m.TokenId = t.TokenId
                WHERE m.AblieferndeStelleId = AblieferndeStelle.AblieferndeStelleId
                FOR xml PATH('AblieferndeStelleTokenDto'), ROOT('ArrayOfAblieferndeStelleTokenDto'), TYPE)
                AS AblieferndeStelleTokenList,
                (SELECT
                u.FamilyName,
                u.FirstName,
                u.Id
                    FROM ApplicationUserAblieferndeStelle m
                INNER JOIN ApplicationUser u
                ON m.UserId = U.ID
                WHERE m.AblieferndeStelleId = AblieferndeStelle.AblieferndeStelleId
                FOR xml PATH('UserInfo'), ROOT('ArrayOfUserInfo'), TYPE)
                AS ApplicationUserList

                FROM Ablieferndestelle";


        public IEnumerable<AblieferndeStelleDetailDto> GetAllAblieferndeStelle()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = Sql;

                    return AblieferndeStelleDetailDto(cmd);
                }
            }
        }

        public AblieferndeStelleDetailDto GetAblieferndeStelle(int ablieferndeStelleId)
        {
            if (ablieferndeStelleId <= 0)
            {
                return null;
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $@"
                            {Sql}
                            WHERE AblieferndeStelleId = @p1";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = ablieferndeStelleId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.Int
                    });

                    return AblieferndeStelleDetailDto(cmd).FirstOrDefault();
                }
            }
        }

        /// <inheritdoc />
        public int CreateAblieferndeStelle(string bezeichnung, string kuerzel, List<int> tokenIdList, List<string> kontrollstelleList,
            string currentUserId)
        {
            if (kontrollstelleList == null)
            {
                kontrollstelleList = new List<string>();
            }

            const string query =
                "INSERT INTO AblieferndeStelle (Bezeichnung, Kuerzel, Kontrollstellen, CreatedBy, ModifiedBy) OUTPUT INSERTED.AblieferndeStelleId VALUES (@p1, @p2, @p3, (SELECT EmailAddress FROM ApplicationUser WHERE ID = @p4), (SELECT EmailAddress FROM ApplicationUser WHERE ID = @p4))";

            var parameterList = new List<SqlParameter>
            {
                new SqlParameter
                {
                    Value = bezeichnung,
                    ParameterName = "p1",
                    SqlDbType = SqlDbType.VarChar
                },
                new SqlParameter
                {
                    Value = kuerzel,
                    ParameterName = "p2",
                    SqlDbType = SqlDbType.VarChar
                },
                new SqlParameter
                {
                    Value = string.Join(",", kontrollstelleList),
                    ParameterName = "p3",
                    SqlDbType = SqlDbType.VarChar
                },
                new SqlParameter
                {
                    Value = currentUserId,
                    ParameterName = "p4",
                    SqlDbType = SqlDbType.NVarChar
                }
            };

            var result = DataAccessExtensions.CreateNewItem(connectionString, query, parameterList);

            var ablieferndeStelleId = Convert.ToInt32(result);

            if (tokenIdList == null)
            {
                return ablieferndeStelleId;
            }

            // Alle Tokens zuordnen
            var queryInsertToken = GetQueryForCleanAndInsertTokens(ablieferndeStelleId, tokenIdList);
            DataAccessExtensions.ExecuteQuery(connectionString, queryInsertToken);

            return ablieferndeStelleId;
        }

        /// <inheritdoc />
        public bool DeleteAblieferndeStelle(int[] ablieferndeStelleIds)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                // Amt darf nur gelöscht werden, wenn kein Benutzer mehr zugeordnet sind
                if (HasAblieferndeStelleUsers(cn, ablieferndeStelleIds))
                {
                    return false;
                }

                using (var cmd = cn.CreateCommand())
                {
                    // Alle zugeordneten Tokens entfernen
                    var query = new StringBuilder();
                    query.Append("DELETE FROM AsTokenMapping WHERE AblieferndeStelleId IN (");
                    query.Append(string.Join(", ", ablieferndeStelleIds));
                    query.Append(")");

                    cmd.CommandText += query.ToString();
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = cn.CreateCommand())
                {
                    // Amt entfernen
                    var query = new StringBuilder();
                    query.Append("DELETE FROM AblieferndeStelle WHERE AblieferndeStelleId IN (");
                    query.Append(string.Join(", ", ablieferndeStelleIds));
                    query.Append(")");

                    cmd.CommandText += query.ToString();
                    cmd.ExecuteNonQuery();
                }
            }

            return true;
        }

        /// <inheritdoc />
        public void UpdateAblieferndeStelle(int ablieferndeStelleId, string bezeichnung, string kuerzel, List<int> tokenIdList,
            List<string> kontrollstelleList, string currentUserId)
        {
            if (ablieferndeStelleId == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(bezeichnung) || string.IsNullOrEmpty(kuerzel))
            {
                return;
            }

            if (kontrollstelleList == null)
            {
                kontrollstelleList = new List<string>();
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText +=
                        "UPDATE AblieferndeStelle SET Bezeichnung = @p1, Kuerzel = @p2, Kontrollstellen = @p3, ModifiedBy = (SELECT EmailAddress FROM ApplicationUser WHERE ID = @p4), ModifiedOn = sysdatetime() WHERE AblieferndeStelleId = @p5";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = bezeichnung,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = kuerzel,
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.VarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = string.Join(",", kontrollstelleList),
                        ParameterName = "p3",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = currentUserId,
                        ParameterName = "p4",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = ablieferndeStelleId,
                        ParameterName = "p5",
                        SqlDbType = SqlDbType.Int
                    });

                    cmd.ExecuteNonQuery();
                }
            }

            // Alle Tokens neu zuordnen
            var queryInsertToken = GetQueryForCleanAndInsertTokens(ablieferndeStelleId, tokenIdList);
            DataAccessExtensions.ExecuteQuery(connectionString, queryInsertToken);
        }


        private static AblieferndeStelleDetailDto[] AblieferndeStelleDetailDto(SqlCommand cmd)
        {
            var tmp = new List<AblieferndeStelleDetailDto>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tmp.Add(reader.ToAblieferndeStelle<AblieferndeStelleDetailDto>());
                }
            }

            return tmp.ToArray();
        }

        private bool HasAblieferndeStelleUsers(SqlConnection sqlConnection, int[] ablieferndeStelleIds)
        {
            if (sqlConnection == null)
            {
                throw new ArgumentNullException(nameof(sqlConnection));
            }

            if (ablieferndeStelleIds == null || ablieferndeStelleIds.Length == 0)
            {
                throw new ArgumentException(nameof(ablieferndeStelleIds));
            }

            using (var cmd = sqlConnection.CreateCommand())
            {
                var query = new StringBuilder();
                query.Append("SELECT COUNT(*) FROM ApplicationUserAblieferndeStelle WHERE AblieferndeStelleId IN (");
                query.Append(string.Join(", ", ablieferndeStelleIds));
                query.Append(")");
                cmd.CommandText = query.ToString();

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private string GetQueryForCleanAndInsertTokens(int ablieferndeStelleId, List<int> tokenIdList)
        {
            if (ablieferndeStelleId <= 0)
            {
                return string.Empty;
            }

            if (tokenIdList == null)
            {
                return string.Empty;
            }

            tokenIdList = tokenIdList.Count > 0 ? tokenIdList.Distinct().ToList() : new List<int>();

            var queryInsertToken = new StringBuilder();
            queryInsertToken.Append($"DELETE FROM AsTokenMapping WHERE AblieferndeStelleId = {ablieferndeStelleId} ");

            foreach (var tokenId in tokenIdList)
            {
                queryInsertToken.Append($"IF EXISTS ( SELECT 1 FROM AblieferndeStelleToken WHERE TokenId = {tokenId} ) ");
                queryInsertToken.Append("BEGIN ");
                queryInsertToken.Append("  INSERT INTO AsTokenMapping (AblieferndeStelleId,  TokenId) VALUES  ");
                queryInsertToken.Append($"  ({ablieferndeStelleId}, {tokenId}) ");
                queryInsertToken.Append("END ");
            }

            return queryInsertToken.ToString();
        }
    }
}