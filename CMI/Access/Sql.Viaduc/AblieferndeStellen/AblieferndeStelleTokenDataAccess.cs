using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using CMI.Access.Sql.Viaduc.AblieferndeStellen.Dto;
using CMI.Utilities.Logging.Configurator;

namespace CMI.Access.Sql.Viaduc.AblieferndeStellen
{
    public class AblieferndeStelleTokenDataAccess : DataAccess, IAblieferndeStelleTokenDataAccess
    {
        private readonly string connectionString;

        public AblieferndeStelleTokenDataAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <inheritdoc />
        public IEnumerable<AmtTokenDto> GetAllTokens()
        {
            var amtList = GetAllAblieferndeStelleToToken().ToList();
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("SELECT TokenId, Token, Bezeichnung ");
                    query.Append("FROM AblieferndeStelleToken at ");

                    cmd.CommandText = query.ToString();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var amtToken = reader.ToAblieferStelleToken<AmtTokenDto>();
                            amtToken.AblieferndeStelleList = amtList.Where(amt => amt.TokenId.Equals(amtToken.TokenId))
                                .Cast<AblieferndeStelleDto>().ToList();
                            yield return amtToken;
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public AmtTokenDto GetToken(int tokenId)
        {
            if (tokenId == 0)
            {
                return null;
            }

            var amtList = GetAllAblieferndeStelleToToken(tokenId).ToList();
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("SELECT TokenId, Token, Bezeichnung ");
                    query.Append("FROM AblieferndeStelleToken at ");
                    query.Append("WHERE TokenId = @p1 ");

                    cmd.CommandText = query.ToString();

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = tokenId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.Int
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var amtToken = reader.ToAblieferStelleToken<AmtTokenDto>(tokenId);
                            amtToken.AblieferndeStelleList = amtList.Where(amt => amt.TokenId.Equals(amtToken.TokenId))
                                .Cast<AblieferndeStelleDto>().ToList();
                            return amtToken;
                        }
                    }
                }
            }

            return null;
        }


        /// <inheritdoc />
        public AmtTokenDto CreateToken(string token, string bezeichnung)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            if (string.IsNullOrEmpty(bezeichnung))
            {
                return null;
            }

            if (!TokenIsUnique(token))
            {
                return null;
            }

            var newToken = new AmtTokenDto
            {
                TokenId = 0,
                Token = token,
                Bezeichnung = bezeichnung
            };

            const string query =
                "INSERT INTO AblieferndeStelleToken (Token, Bezeichnung) OUTPUT INSERTED.TokenId VALUES (@p1, @p2)";

            var parameterList = new List<SqlParameter>
            {
                new SqlParameter
                {
                    Value = token,
                    ParameterName = "p1",
                    SqlDbType = SqlDbType.NVarChar
                },
                new SqlParameter
                {
                    Value = bezeichnung,
                    ParameterName = "p2",
                    SqlDbType = SqlDbType.VarChar
                }
            };

            var result = DataAccessExtensions.CreateNewItem(connectionString, query, parameterList);
            if (result == null)
            {
                return null;
            }

            newToken.TokenId = Convert.ToInt32(result);
            return newToken;
        }

        public void DeleteToken(int[] tokenIds)
        {
            if (tokenIds == null || tokenIds.Length == 0)
            {
                throw new BadRequestException("Es wurden keine tokenIds angegeben");
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                // Token darf nur gelöscht werden, wenn kein Amt mehr zugeordnet ist
                if (HasTokenAblieferndeStellen(cn, tokenIds))
                {
                    throw new BadRequestException("Es dürfen keine Tokens gelöscht werden, wenn noch ein Amt zugeordnet ist");
                }

                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("DELETE FROM AblieferndeStelleToken WHERE TokenId IN (");
                    query.Append(string.Join(", ", tokenIds));
                    query.Append(")");
                    cmd.CommandText = query.ToString();

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <inheritdoc />
        public void UpdateToken(int tokenId, string token, string bezeichnung)
        {
            if (tokenId == 0 || string.IsNullOrEmpty(token))
            {
                return;
            }

            if (string.IsNullOrEmpty(bezeichnung))
            {
                return;
            }

            if (!TokenIsUnique(token, tokenId))
            {
                return;
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText +=
                        "UPDATE AblieferndeStelleToken SET Token = @p1, Bezeichnung = @p2 WHERE TokenId = @p3 ";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = token,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = bezeichnung,
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.VarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = tokenId,
                        ParameterName = "p3",
                        SqlDbType = SqlDbType.Int
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private bool TokenIsUnique(string token, int? tokenId = null)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                using (var cmd = sqlConnection.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("SELECT COUNT(*) FROM AblieferndeStelleToken WHERE ");
                    query.Append("Token LIKE @p1 ");
                    if (tokenId.HasValue)
                    {
                        query.Append($"AND Token NOT LIKE (SELECT Token FROM AblieferndeStelleToken WHERE TokenId = {tokenId.Value}) ");
                    }

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = token,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.CommandText = query.ToString();

                    return Convert.ToInt32(cmd.ExecuteScalar()) == 0;
                }
            }
        }

        private bool HasTokenAblieferndeStellen(SqlConnection sqlConnection, int[] tokenIds)
        {
            if (sqlConnection == null)
            {
                throw new ArgumentNullException(nameof(sqlConnection));
            }

            if (tokenIds == null || tokenIds.Length == 0)
            {
                throw new ArgumentException(nameof(tokenIds));
            }

            using (var cmd = sqlConnection.CreateCommand())
            {
                var query = new StringBuilder();
                query.Append("SELECT COUNT(*) FROM AsTokenMapping WHERE TokenId IN (");
                query.Append(string.Join(", ", tokenIds));
                query.Append(")");
                cmd.CommandText = query.ToString();

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private IEnumerable<AsTokenMappingAmtDto> GetAllAblieferndeStelleToToken(int? tokenId = null)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("SELECT at.AblieferndeStelleId, at.Bezeichnung, at.Kuerzel, atm.TokenId ");
                    query.Append("FROM AsTokenMapping atm ");
                    query.Append(
                        "INNER JOIN AblieferndeStelle at ON at.AblieferndeStelleId = atm.AblieferndeStelleId ");

                    if (tokenId.HasValue)
                    {
                        query.Append("WHERE atm.TokenId = @p1 ");

                        cmd.Parameters.Add(new SqlParameter
                        {
                            Value = tokenId,
                            ParameterName = "p1",
                            SqlDbType = SqlDbType.Int
                        });
                    }

                    cmd.CommandText = query.ToString();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var asTokenMapping = reader.ToAblieferndeStelle<AsTokenMappingAmtDto>();
                            asTokenMapping.TokenId = Convert.ToInt32(reader["TokenId"]);
                            yield return asTokenMapping;
                        }
                    }
                }
            }
        }
    }
}