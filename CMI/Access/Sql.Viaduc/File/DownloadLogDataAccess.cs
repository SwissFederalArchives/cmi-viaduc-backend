using System;
using System.ComponentModel;
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
            string schutzfrist, string zeitraum)
        {
            CreateLogEntry(token, userId, userTokens, signatur, titel, schutzfrist, zeitraum, "Download", null);
        }

        public void LogViewerClick(string token, string userId, string userTokens, string signatur, string titel,
           string schutzfrist, string zeitraum)
        {
            CreateLogEntry(token, userId, userTokens, signatur, titel, schutzfrist, zeitraum, "Viewer", DateTime.Now);
        }

        private void CreateLogEntry(string token, string userId, string userTokens, string signatur, string titel, string schutzfrist, string zeitraum, string vorgang, DateTime? datumVorgang)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("INSERT INTO  DownloadLog");

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
                        Value = vorgang,
                        ParameterName = "pVorgang",
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
                        Value = zeitraum,
                        ParameterName = "pZeitraum",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = DateTime.Now,
                        ParameterName = "pDatumErstellungToken",
                        SqlDbType = SqlDbType.DateTime
                    });
                    if (datumVorgang != null)
                    {
                        cmd.Parameters.Add(new SqlParameter
                        {
                            IsNullable = true,
                            Value = datumVorgang,
                            ParameterName = "pDatumVorgang",
                            SqlDbType = SqlDbType.DateTime
                        });
                        query.Append(
                            "       (  Token,   UserId,   UserTokens, Vorgang,   Signatur,   Titel,   Schutzfrist, Zeitraum,   DatumErstellungToken, DatumVorgang)");

                        query.Append(
                            "VALUES (@pToken, @pUserId, @pUserTokens, @pVorgang,    @pSignatur, @pTitel, @pSchutzfrist, @pZeitraum, @pDatumErstellungToken, @pDatumVorgang)");
                    }
                    else
                    {
                        query.Append(
                            "       (  Token,   UserId,   UserTokens, Vorgang,   Signatur,   Titel,   Schutzfrist, Zeitraum,   DatumErstellungToken, DatumVorgang)");

                        query.Append(
                            "VALUES (@pToken, @pUserId, @pUserTokens, @pVorgang,    @pSignatur, @pTitel, @pSchutzfrist, @pZeitraum, @pDatumErstellungToken, null)");
                    }

                    cmd.CommandText = query.ToString();
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