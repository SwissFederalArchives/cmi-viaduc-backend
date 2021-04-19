using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace CMI.Access.Sql.Viaduc.File
{
    public class DownloadLogDataAccess : DataAccess, IDownloadLogDataAccess
    {
        private readonly string connectionString;

        public DownloadLogDataAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void LogTokenGeneration(string token, string userId, string userTokens, string signatur, string titel,
            string schutzfrist)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("INSERT INTO  DownloadLog");
                    query.Append(
                        "       (  Token,   UserId,   UserTokens, Vorgang,   Signatur,   Titel,   Schutzfrist,    DatumErstellungToken, DatumVorgang)");
                    query.Append(
                        "VALUES (@pToken, @pUserId, @pUserTokens, 'Token',    @pSignatur, @pTitel, @pSchutzfrist,  @pDatumErstellungToken, null        )");

                    cmd.CommandText = query.ToString();

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = token,
                        ParameterName = "pToken",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId,
                        ParameterName = "pUserId",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userTokens,
                        ParameterName = "pUserTokens",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = signatur,
                        ParameterName = "pSignatur",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = titel,
                        ParameterName = "pTitel",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = schutzfrist,
                        ParameterName = "pSchutzfrist",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = DateTime.Now,
                        ParameterName = "pDatumErstellungToken",
                        SqlDbType = SqlDbType.DateTime
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void LogVorgang(string token, string vorgang)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("UPDATE DownloadLog ");
                    query.Append("SET DatumVorgang=@pDatumVorgang,  Vorgang=@pVorgang ");
                    query.Append("WHERE Token = @pToken");

                    cmd.CommandText = query.ToString();

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = DateTime.Now,
                        ParameterName = "pDatumVorgang",
                        SqlDbType = SqlDbType.DateTime
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = vorgang,
                        ParameterName = "pVorgang",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = token,
                        ParameterName = "pToken",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}