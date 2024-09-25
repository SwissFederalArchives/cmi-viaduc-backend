using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;

namespace CMI.Access.Sql.Viaduc
{
    public class DbUpgrader
    {
        private readonly string connectionString;
        private readonly Regex regex = new Regex(@"\r\n?\s*GO\s*\r\n?", RegexOptions.IgnoreCase);

        private readonly int sollVersion = 97;

        private int istVersion;


        public DbUpgrader(string connectionString)
        {
            this.connectionString = connectionString;
            istVersion = GetIstVersion();
        }

        private int GetIstVersion()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT count(*) FROM sysobjects WHERE xtype = 'U'";
                    var countTables = Convert.ToInt32(cmd.ExecuteScalar());

                    if (countTables == 0)
                    {
                        return 0;
                    }
                }

                using (var cmd2 = cn.CreateCommand())
                {
                    cmd2.CommandText = "SELECT DbVersion FROM Version";
                    return Convert.ToInt32(cmd2.ExecuteScalar());
                }
            }
        }

        public string Upgrade()
        {
            if (istVersion > sollVersion)
            {
                var message = "The DBVersion is " + istVersion +
                              ", which is too high for this program version, which expects DB-Version " +
                              sollVersion;

                Log.Error(message);
                throw new Exception(message);
            }

            var info = "" + istVersion;
            if (istVersion < sollVersion)
            {
                info = "" + istVersion + " -> " + sollVersion;

                while (istVersion < sollVersion)
                {
                    UpgradeToVersion(istVersion + 1);
                    istVersion++;
                }
            }

            return info;
        }

        private void UpgradeToVersion(int i)
        {
            var resourceName = string.Format("CMI.Access.Sql.Viaduc.SqlDbScripts.{0:D4}_TO_{1:D4}.sql", i - 1, i);
            var assembly = Assembly.GetExecutingAssembly();

            string sql;

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    var msg = $"Stream is null for resource '{resourceName}'.";
                    Log.Error(msg);
                    throw new Exception(msg);
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    sql = reader.ReadToEnd();
                }
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    foreach (var part in regex.Split(sql)
                        .Select(s => s.Trim())
                        .Where(sc => sc != string.Empty))
                    {
                        string runningScript;

                        if (part.Length >= 3 && part.EndsWith("go", StringComparison.OrdinalIgnoreCase) && char.IsWhiteSpace(part[part.Length - 3]))
                        {
                            runningScript = part.Substring(0, part.Length - 2).Trim();
                        }
                        else
                        {
                            runningScript = part;
                        }

                        cmd.CommandText = runningScript + Environment.NewLine;

                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Update to DB version {i} failed.\nException: {ex}\nExecuted command: {runningScript}");

                            using (var cmd2 = cn.CreateCommand())
                            {
                                cmd2.CommandText = "UPDATE VERSION SET DbVersion = 9999";
                                cmd2.ExecuteNonQuery();
                                istVersion = 9999;
                            }

                            throw;
                        }
                    }
                }

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE VERSION SET DbVersion = " + i;
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}