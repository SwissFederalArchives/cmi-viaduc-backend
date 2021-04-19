using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties;
using Devart.Data.Oracle;
using Serilog;

namespace CMI.Utilities.DigitalRepository.CreateTestDataHelper
{
    internal class DbAccess
    {
        public static List<AipData> GetAipData()
        {
            var retVal = new List<AipData>();

            var cn = AISConnection.GetConnection();
            var sql = @"
                        select
                          v.vrzng_enht_id, v.vrzng_enht_titel, d.memo_txt
                        from
                          tbs_gsft_obj_dtl d, vws_vrzng_enht_haupt_sys v
                        where
                          d.gsft_obj_id      = v.vrzng_enht_id
                        and d.daten_elmnt_id = 10367
                        ";
            var da = new OracleDataAdapter(sql, cn);
            var ds = new DataSet();

            da.Fill(ds);

            var q = from r in ds.Tables[0].AsEnumerable()
                select new AipData
                {
                    Id = r.Field<double>("vrzng_enht_id").ToString(CultureInfo.InvariantCulture),
                    Title = r.Field<string>("vrzng_enht_titel"),
                    AipAtDossierId = r.Field<string>("memo_txt")
                };

            retVal.AddRange(q);
            cn.Close();

            return retVal;
        }
    }

    internal class AipData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string AipAtDossierId { get; set; }
    }

    /// <summary>
    ///     Provides the connection to the scopeArchiv database
    /// </summary>
    public static class AISConnection
    {
        public static OracleConnection GetConnection()
        {
            var host = Settings.Default.OracleHost;
            var userId = Settings.Default.OracleUser;
            var password = Settings.Default.OraclePassword;
            var port = Settings.Default.OraclePort;
            var sid = Settings.Default.OracleSID;
            var serviceName = Settings.Default.OracleServiceName;
            var schema = Settings.Default.OracleSchemaName;

            var connectionString = $"User Id={userId};Password={password};Host={host};Port={port};Direct=true;";
            connectionString += string.IsNullOrEmpty(sid) ? $"Service Name={serviceName};" : $"SID={sid};";
            var cn = new OracleConnection(connectionString);
            try
            {
                cn.Open();
                SetSchema(schema, cn);
                return cn;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open the connection");
                throw;
            }
        }

        /// <summary>
        ///     Sets the database schema.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <param name="cn">The connection.</param>
        /// <returns></returns>
        private static void SetSchema(string schema, OracleConnection cn)
        {
            try
            {
                using (var oracleCommand = new OracleCommand(string.Concat("ALTER SESSION SET CURRENT_SCHEMA = ", schema), cn))
                {
                    oracleCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to set the schema");
            }
        }
    }
}