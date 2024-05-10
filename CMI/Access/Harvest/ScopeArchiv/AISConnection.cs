using System;
using CMI.Access.Harvest.Properties;
using Devart.Data.Oracle;
using Serilog;

namespace CMI.Access.Harvest.ScopeArchiv
{
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

            var connectionString = $"User Id={userId};Password={password};Host={host};Port={port};Direct=true;Unicode=true;";
            connectionString += string.IsNullOrEmpty(sid.Trim()) ? $"Service Name={serviceName};" : $"SID={sid};";
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