using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace CMI.Access.Sql.Viaduc.File
{
    public class DownloadTokenDataAccess : DataAccess, IDownloadTokenDataAccess
    {
        private readonly string connectionString;

        public DownloadTokenDataAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public string GetUserIdByToken(string token, int recordId, DownloadTokenType tokenType, string ipAdress)
        {
            if (string.IsNullOrEmpty(token))
            {
                return "";
            }

            if (recordId <= 0)
            {
                return "";
            }

            if (string.IsNullOrEmpty(ipAdress))
            {
                return "";
            }

            string foundUserId;

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("SELECT UserId ");
                    query.Append("FROM DownloadToken ");
                    query.Append("WHERE Token = @token AND IpAdress = @ipAddress AND recordId = @recordId AND tokenType = @tokenType");

                    cmd.CommandText = query.ToString();

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = token,
                        ParameterName = "token",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = ipAdress,
                        ParameterName = "ipAddress",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = recordId,
                        ParameterName = "recordId",
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = tokenType.ToString(),
                        ParameterName = "tokenType",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    foundUserId = cmd.ExecuteScalar() as string;
                }
            }

            return foundUserId;
        }

        public bool CheckTokenIsValidAndClean(string token, int recordId, DownloadTokenType tokenType, string ipAdress)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            if (recordId <= 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(ipAdress))
            {
                return false;
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                var timeNow = DateTime.Now;

                int foundToken;
                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("SELECT COUNT(*) ");
                    query.Append("FROM DownloadToken ");
                    query.Append(
                        "WHERE Token LIKE @token AND IpAdress LIKE @ipAddress AND ExpiryTime >= @expiryTime AND recordId = @recordId AND tokenType = @tokenType");

                    cmd.CommandText = query.ToString();

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = token,
                        ParameterName = "token",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = ipAdress,
                        ParameterName = "ipAddress",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = timeNow,
                        ParameterName = "expiryTime",
                        SqlDbType = SqlDbType.DateTime2
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = recordId,
                        ParameterName = "recordId",
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = tokenType.ToString(),
                        ParameterName = "tokenType",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    foundToken = Convert.ToInt32(cmd.ExecuteScalar());
                }

                return foundToken > 0;
            }
        }

        public void CleanUpOldToken(string token, int recordId, DownloadTokenType tokenType)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    // Alle nicht mehr gültigen Tokens aus der DB entfernen
                    var query = new StringBuilder();
                    query.Append(
                        "DELETE FROM DownloadToken WHERE ExpiryTime < @expiryTime OR (Token LIKE @token AND recordId = @recordId AND tokenType = @tokenType)");
                    cmd.CommandText = query.ToString();

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = DateTime.Now,
                        ParameterName = "expiryTime",
                        SqlDbType = SqlDbType.DateTime2
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = token,
                        ParameterName = "token",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = recordId,
                        ParameterName = "recordId",
                        SqlDbType = SqlDbType.Int
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = tokenType.ToString(),
                        ParameterName = "tokenType",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        bool IDownloadTokenDataAccess.CreateToken(string token, int recordId, DownloadTokenType tokenType, DateTime tokenExpiryTime, string ipAdress,
            string userId)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            if (recordId <= 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(ipAdress))
            {
                return false;
            }

            const string query =
                "INSERT INTO DownloadToken (Token, ExpiryTime, IpAdress, recordId, UserId, TokenType) VALUES (@token, @expiryTime, @ipAddress, @recordId, @userId, @tokenType)";

            var parameterList = new List<SqlParameter>
            {
                new SqlParameter
                {
                    Value = token,
                    ParameterName = "token",
                    SqlDbType = SqlDbType.NVarChar
                },
                new SqlParameter
                {
                    Value = tokenExpiryTime,
                    ParameterName = "expiryTime",
                    SqlDbType = SqlDbType.DateTime2
                },
                new SqlParameter
                {
                    Value = ipAdress,
                    ParameterName = "ipAddress",
                    SqlDbType = SqlDbType.NVarChar
                },
                new SqlParameter
                {
                    Value = recordId,
                    ParameterName = "recordId",
                    SqlDbType = SqlDbType.Int
                },
                new SqlParameter
                {
                    Value = userId,
                    ParameterName = "userId",
                    SqlDbType = SqlDbType.NVarChar
                },
                new SqlParameter
                {
                    Value = tokenType.ToString(),
                    ParameterName = "tokenType",
                    SqlDbType = SqlDbType.NVarChar
                }
            };

            return DataAccessExtensions.CreateNewItem(connectionString, query, parameterList) != null;
        }
    }
}