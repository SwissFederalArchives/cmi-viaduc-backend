using System;
using System.Diagnostics;
using System.IO;
using CMI.Utilities.FormTemplate.Helper.Properties;
using Devart.Data.Oracle;
using Newtonsoft.Json;

namespace CMI.Utilities.FormTemplate.Helper
{
    /// <summary>
    ///     Simple helper utility that generates the form template configuration file
    ///     from the data in the scopeArchiv database.
    /// </summary>
    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            using (var cn = new OracleConnection(Settings.Default.DefaultConnection))
            {
                cn.Open();
                SetSchema(Settings.Default.OracleSchemaName, cn);

                var helper = new FormTemplateHelper(cn);
                var templates = helper.GetFormTemplates();

                File.WriteAllText(".\\scopeArchivTemplates.json", JsonConvert.SerializeObject(templates, Formatting.Indented));
                Console.WriteLine(@"Saved data to file.");
                Console.ReadLine();
            }
        }

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
                Debug.WriteLine(ex.Message);
            }
        }
    }
}