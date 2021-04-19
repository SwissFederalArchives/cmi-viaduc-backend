using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Manager.Index.Config;
using CMI.Utilities.FormTemplate.Helper.Properties;
using CsvHelper;
using Devart.Data.Oracle;
using Newtonsoft.Json;

namespace CMI.Utilities.FormTemplate.Helper
{
    internal class FormTemplateHelper
    {
        private readonly OracleConnection cn;
        private readonly List<FieldConfiguration> fieldConfigurations;
        private readonly List<int> fieldsToExclude;
        private readonly List<string> supportedLanguages;
        private readonly List<Translation> translations;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FormTemplateHelper" /> class.
        /// </summary>
        /// <param name="cn">The cn.</param>
        public FormTemplateHelper(OracleConnection cn)
        {
            this.cn = cn;
            supportedLanguages = Settings.Default.SupportedLanguages.Split(',').Select(s => s.Trim()).ToList();
            translations = LoadTranslations();
            fieldsToExclude = LoadFieldsToExclude();

            var configurationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "customFieldsConfig.json");
            if (File.Exists(configurationFile))
            {
                var json = File.ReadAllText(configurationFile);
                fieldConfigurations = JsonConvert.DeserializeObject<List<FieldConfiguration>>(json);
            }
        }

        /// <summary>
        ///     Gets the form templates for all forms that are actively used in scopeArchiv.
        /// </summary>
        /// <returns>FormTemplateContainer.</returns>
        /// <exception cref="NotSupportedException">Fields that are not contained within a section is not supported in scopeArchiv</exception>
        public FormTemplateContainer GetFormTemplates()
        {
            Console.WriteLine(@"Get the data from the database...");
            var data = GetDataFromDatabase();

            // Group the flat data by form id
            // Remove all fields that should not be visible on the form on the public client (but that need to be synced)
            var groupedData = from d in data.Where(d => fieldsToExclude.All(f => f != d.DataElementId))
                orderby d.FormId
                group d by d.FormId
                into grp
                select new
                {
                    grp.Key,
                    grp
                };

            var retVal = new FormTemplateContainer();

            foreach (var grp in groupedData)
            {
                Console.WriteLine($@"Get template for form id {grp.Key}");
                var template = new FormTemplate {FormId = grp.Key};
                var sections = new List<Section>();

                // Hole alle Elemente der ersten Stufe. Auf der ersten Stufe ist die Länge der Sequenz Nummer 5 Zeichen
                foreach (var item in grp.grp.Where(i => i.DataElementSequenceNumber.Length == 5).OrderBy(i => i.DataElementSequenceNumber))
                {
                    if (item.DataElementType == ScopeArchivDatenElementTyp.Zwischentitel)
                    {
                        var section = GetSection(item, item.DataElementSequenceNumber, grp.grp);
                        if (section != null)
                        {
                            sections.Add(section);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("Fields that are not contained within a section is not supported in scopeArchiv");
                    }
                }

                // Eine einzige Detail-Section pro Formular erstellen
                var detailSection = new Section
                {
                    Fields = sections.SelectMany(s => s.Fields).ToList(),
                    SectionLabels = GetLabelsForAllLanguages("DETAILS"),
                    SubSections = new List<Section>()
                };

                template.Sections.Add(detailSection);
                retVal.FormTemplates.Add(template);
            }

            return retVal;
        }

        private Section GetSection(TemplateData sectionField, string sectionSequence, IGrouping<string, TemplateData> groupData)
        {
            Section retVal = null;

            // Get all the fields that belong to the current section
            var fields = groupData.Where(g =>
                    g.DataElementSequenceNumber.StartsWith(sectionSequence) && g.DataElementSequenceNumber.Length == sectionSequence.Length + 5)
                .OrderBy(o => o.DataElementSequenceNumber).ToList();

            if (fields.Any())
            {
                retVal = new Section
                {
                    // Set the labels for the new section
                    // In the db the section titles have numberings. These must be removed
                    SectionLabels = GetLabelsForAllLanguages(sectionField.XmlCd)
                };


                foreach (var field in fields)
                {
                    if (field.DataElementType == ScopeArchivDatenElementTyp.Zwischentitel)
                    {
                        retVal.SubSections.Add(GetSection(field, field.DataElementSequenceNumber, groupData));
                    }
                    else
                    {
                        var elasticFieldName = GetElasticFieldName(field.DataElementId);
                        if (!string.IsNullOrEmpty(elasticFieldName))
                        {
                            var newField = new FieldType
                            {
                                DbFieldName = elasticFieldName,
                                DbType = field.DataElementType.ToString(),
                                ElasticType = GetElasticTypeName(field.DataElementId),
                                Visibility = field.AccessLevel == 1 ? FieldTypeVisibility.@internal : FieldTypeVisibility.@public,
                                FieldLabels = GetLabelsForAllLanguages(field.XmlCd)
                            };
                            retVal.Fields.Add(newField);
                        }
                    }
                }
            }

            return retVal;
        }

        private string GetElasticFieldName(int dataElementId)
        {
            var elasticField = fieldConfigurations.FirstOrDefault(f => f.ElementId == dataElementId.ToString());
            if (elasticField == null)
            {
                Console.WriteLine($@"No field mapping found for dataelement id {dataElementId}");
                return null;
            }

            if (elasticField.IsDefaultField)
            {
                return elasticField.TargetField;
            }

            return $"CustomFields.{elasticField.TargetField}";
        }

        private string GetElasticTypeName(int dataElementId)
        {
            var elasticField = fieldConfigurations.FirstOrDefault(f => f.ElementId == dataElementId.ToString());
            if (elasticField == null)
            {
                Console.WriteLine($@"No type mapping found for dataelement id {dataElementId}");
                return null;
            }

            return elasticField.Type;
        }

        private Dictionary<string, string> GetLabelsForAllLanguages(string key)
        {
            var retVal = new Dictionary<string, string>();
            foreach (var language in supportedLanguages)
            {
                string text;
                var translation = translations.Find(t => t.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
                switch (language.ToLower())
                {
                    case "de":
                        text = translation.German;
                        break;
                    case "en":
                        text = translation.English;
                        break;
                    case "fr":
                        text = translation.French;
                        break;
                    case "it":
                        text = translation.Italian;
                        break;
                    default:
                        throw new InvalidEnumArgumentException();
                }

                retVal.Add(language, text);
            }

            return retVal;
        }

        private List<TemplateData> GetDataFromDatabase()
        {
            var sql = @"
                select
                  de.daten_elmnt_id, de.xml_cd, de.daten_elmnt_nm, de.zgrf_brtg_stufe_id ,
                  de.daten_elmnt_typ_id, frm.frmlr_nm, frm.frmlr_id, de.dzml_stln_anz, de.voll_txt_srchbl_ind
                  , pks_frmlr.get_entrg_pfad ( fe.frmlr_id , fe.daten_elmnt_id, fe.frmlr_entrg_id ) as
                  daten_elmnt_sqnz_nr
                from
                  tbs_daten_elmnt de, tbs_frmlr frm , tbs_frmlr_entrg fe
                where
                  fe.frmlr_id           = frm.frmlr_id
                and fe.daten_elmnt_id   = de.daten_elmnt_id
                and frm.gsft_obj_kls_id = 9
                and frm.frmlr_id       in
                  (
                    select distinct
                      frmlr_id
                    from
                      (
                        select distinct
                          anst_frmlr_id as frmlr_id
                        from
                          tbs_gsft_obj g
                        where
                          g.gsft_obj_kls_id = 9
                        union
                        select distinct
                          brbtg_frmlr_id as frmlr_id
                        from
                          tbs_gsft_obj g
                        where
                          g.gsft_obj_kls_id = 9
                      )
                  )
                order by
                  frmlr_nm, daten_elmnt_sqnz_nr                 
            ";

            var cmd = new OracleCommand(sql, cn);
            var da = new OracleDataAdapter(cmd);
            var ds = new DataSet();
            da.Fill(ds);

            var data = (from r in ds.Tables[0].AsEnumerable()
                select new TemplateData
                {
                    DataElementId = (int) r.Field<double>("daten_elmnt_id"),
                    DataElementName = r.Field<string>("daten_elmnt_nm"),
                    DataElementSequenceNumber = r.Field<string>("daten_elmnt_sqnz_nr"),
                    DataElementType = (ScopeArchivDatenElementTyp) r.Field<double>("daten_elmnt_typ_id"),
                    XmlCd = r.Field<string>("xml_cd"),
                    AccessLevel = (int) r.Field<double>("zgrf_brtg_stufe_id"),
                    DecimalDigits = r.Field<int>("dzml_stln_anz"),
                    FullTextSearchable = r.Field<int>("voll_txt_srchbl_ind") != 0,
                    FormName = r.Field<string>("frmlr_nm"),
                    FormId = ((int) r.Field<double>("frmlr_id")).ToString()
                }).ToList();
            return data;
        }

        private List<Translation> LoadTranslations()
        {
            var file = new FileInfo("FormFieldTranslations.csv");
            if (file.Exists)
            {
                var csv = new CsvReader(file.OpenText());
                csv.Configuration.Delimiter = ";";
                var records = csv.GetRecords<Translation>();
                return records.ToList();
            }

            throw new FileNotFoundException($"File {file.Name} not found");
        }

        private List<int> LoadFieldsToExclude()
        {
            var file = new FileInfo("FieldsToExclude.txt");
            if (file.Exists)
            {
                var lines = File.ReadAllLines(file.FullName);
                return lines.Select(i => Convert.ToInt32(i)).ToList();
            }

            throw new FileNotFoundException($"File {file.Name} not found");
        }
    }
}