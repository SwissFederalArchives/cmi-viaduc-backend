using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Resources;
using CMI.Contract.Common.Properties;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Contract.Common
{
    public static class ElasticArchiveRecordExtension
    {
        private static ResourceManager resourceManager;
        private static ResourceManager ResourceManager
        {
            get { return resourceManager ?? (resourceManager = new ResourceManager(typeof(Resources))); }
        }

        public static string GetAuszuhebendeArchiveRecordId(this ElasticArchiveRecord elasticArchiveRecord)
        {
            return elasticArchiveRecord.ArchiveplanContext.FirstOrDefault(apc => apc.Level == "Dossier")?.ArchiveRecordId;
        }

        public static string GetSchutzfristenVerzeichnung(this ElasticArchiveRecord entity)
        {
            var anhang3 = entity.HasCustomProperty("Anhang3") && entity.CustomFields.anhang3
                ? "/ Anhang 3"
                : "";

            var katAutomatisierung = entity.HasCustomProperty("KategorieDia") && entity.CustomFields.kategorieDia == 2
                ? "/ Schutzfristverzeichnung validiert"
                : "";

            var optional = $"{anhang3} {katAutomatisierung}".Trim();
            return
                $"SF-Kat: {entity.ProtectionCategory} / SF-Dauer: {entity.ProtectionDuration} / SF-Ende: {entity.ProtectionEndDate?.Date.ToString("dd.MM.yyy") ?? "-"} {optional}"
                    .Trim();
        }

        public static T GetCustomValueOrDefault<T>(this ElasticArchiveRecord entity, string key)
        {
            if (!entity.HasCustomProperty(key))
            {
                return default;
            }

            var kv = ((IDictionary<string, object>) entity.CustomFields).FirstOrDefault(k =>
                k.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
            return (T) kv.Value;
        }

        public static bool HasCustomProperty(this ElasticArchiveRecord entity, string key)
        {
            // ignore case, because the customfields are lowercamelcase
            return entity?.CustomFields != null &&
                   ((IDictionary<string, object>) entity.CustomFields).Any(k => k.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
        }

        public static string Aktenzeichen(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property aktenzeichen.");
            if (record.HasCustomProperty("aktenzeichen"))
            {
                Log.Verbose("Property aktenzeichen: {aktenzeichen}", JsonConvert.SerializeObject(record.CustomFields.aktenzeichen));
                if (record.CustomFields.aktenzeichen is string)
                {
                    return record.CustomFields.aktenzeichen;
                }

                if (record.CustomFields.aktenzeichen is List<object>)
                {
                    return string.Join(", ", record.CustomFields.aktenzeichen);
                }
            }

            return null;
        }

        public static string Zusatzmerkmal(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property zusatzkomponenteZac1.");
            if (record.HasCustomProperty("zusatzkomponenteZac1"))
            {
                Log.Verbose("Property zusatzkomponenteZac1: {zusatzkomponenteZac1}",
                    JsonConvert.SerializeObject(record.CustomFields.zusatzkomponenteZac1));
                if (record.CustomFields.zusatzkomponenteZac1 is string)
                {
                    return record.CustomFields.zusatzkomponenteZac1;
                }

                if (record.CustomFields.zusatzkomponenteZac1 is List<object>)
                {
                    return string.Join(", ", record.CustomFields.zusatzkomponenteZac1);
                }
            }

            return null;
        }

        public static string Form(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property form.");
            if (record.HasCustomProperty("form"))
            {
                Log.Verbose("Property form: {form}", JsonConvert.SerializeObject(record.CustomFields.form));
                if (record.CustomFields.form is string)
                {
                    return record.CustomFields.form;
                }

                if (record.CustomFields.form is List<object>)
                {
                    return string.Join(", ", record.CustomFields.form);
                }
            }

            return null;
        }

        public static string EntstehungszeitraumAnmerkung(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property entstehungszeitraumAnmerkung.");
            if (record.HasCustomProperty("entstehungszeitraumAnmerkung"))
            {
                Log.Verbose("Property entstehungszeitraumAnmerkung: {entstehungszeitraumAnmerkung}",
                    JsonConvert.SerializeObject(record.CustomFields.entstehungszeitraumAnmerkung));
                if (record.CustomFields.entstehungszeitraumAnmerkung is string)
                {
                    return record.CustomFields.entstehungszeitraumAnmerkung;
                }

                if (record.CustomFields.entstehungszeitraumAnmerkung is List<object>)
                {
                    return string.Join(", ", record.CustomFields.entstehungszeitraumAnmerkung);
                }
            }

            return null;
        }

        public static string Publikationsrechte(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property publikationsrechte.");
            if (record.HasCustomProperty("publikationsrechte"))
            {
                Log.Verbose("Property publikationsrechte: {publikationsrechte}", JsonConvert.SerializeObject(record.CustomFields.publikationsrechte));
                if (record.CustomFields.publikationsrechte is string)
                {
                    return record.CustomFields.publikationsrechte;
                }

                if (record.CustomFields.publikationsrechte is List<object>)
                {
                    return string.Join(", ", record.CustomFields.publikationsrechte);
                }
            }

            return null;
        }

        public static string ZuständigeStelle(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property zuständigeStelle.");
            if (record.HasCustomProperty("zuständigeStelle"))
            {
                Log.Verbose("Property zuständigeStelle: {zuständigeStelle}", JsonConvert.SerializeObject(record.CustomFields.zuständigeStelle));
                if (record.CustomFields.zuständigeStelle is string)
                {
                    return record.CustomFields.zuständigeStelle;
                }

                if (record.CustomFields.zuständigeStelle is List<object>)
                {
                    return string.Join(", ", record.CustomFields.zuständigeStelle);
                }
            }

            return null;
        }

        public static string ZusätzlicheInformationen(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property bemerkungZurVe.");
            if (record.HasCustomProperty("bemerkungZurVe"))
            {
                Log.Verbose("Property bemerkungZurVe: {bemerkungZurVe}", JsonConvert.SerializeObject(record.CustomFields.bemerkungZurVe));
                if (record.CustomFields.bemerkungZurVe is string)
                {
                    return record.CustomFields.bemerkungZurVe;
                }

                if (record.CustomFields.bemerkungZurVe is List<object>)
                {
                    return string.Join(", ", record.CustomFields.bemerkungZurVe);
                }
            }

            return null;
        }

        public static string Land(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property land.");
            if (record.HasCustomProperty("land"))
            {
                Log.Verbose("Property land: {land}", JsonConvert.SerializeObject(record.CustomFields.land));
                if (record.CustomFields.land is string)
                {
                    return record.CustomFields.land;
                }

                if (record.CustomFields.land is List<object>)
                {
                    return string.Join(", ", record.CustomFields.land);
                }
            }

            return null;
        }

        public static string FrüheresAktenzeichen(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property früheresAktenzeichen.");
            if (record.HasCustomProperty("früheresAktenzeichen"))
            {
                Log.Verbose("Property früheresAktenzeichen: {früheresAktenzeichen}",
                    JsonConvert.SerializeObject(record.CustomFields.früheresAktenzeichen));
                if (record.CustomFields.früheresAktenzeichen is string)
                {
                    return record.CustomFields.früheresAktenzeichen;
                }

                if (record.CustomFields.früheresAktenzeichen is List<object>)
                {
                    return string.Join(", ", record.CustomFields.früheresAktenzeichen);
                }
            }

            return null;
        }

        public static string Thema(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property thema.");
            if (record.HasCustomProperty("thema"))
            {
                Log.Verbose("Property thema: {thema}", JsonConvert.SerializeObject(record.CustomFields.thema));
                if (record.CustomFields.thema is string)
                {
                    return record.CustomFields.thema;
                }

                if (record.CustomFields.thema is List<object>)
                {
                    return string.Join(", ", record.CustomFields.thema);
                }
            }

            return null;
        }

        public static string Format(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property format.");
            if (record.HasCustomProperty("format"))
            {
                Log.Verbose("Property format: {format}", JsonConvert.SerializeObject(record.CustomFields.format));
                if (record.CustomFields.format is string)
                {
                    return record.CustomFields.format;
                }

                if (record.CustomFields.format is List<object>)
                {
                    return string.Join(", ", record.CustomFields.format);
                }
            }

            return null;
        }

        public static string Urheber(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property urheber.");
            if (record.HasCustomProperty("urheber"))
            {
                Log.Verbose("Property urheber: {urheber}", JsonConvert.SerializeObject(record.CustomFields.urheber));
                if (record.CustomFields.urheber is string)
                {
                    return record.CustomFields.urheber;
                }

                if (record.CustomFields.urheber is List<object>)
                {
                    return string.Join(", ", record.CustomFields.urheber);
                }
            }

            return null;
        }

        public static string Verleger(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property verleger.");
            if (record.HasCustomProperty("verleger"))
            {
                Log.Verbose("Property verleger: {verleger}", JsonConvert.SerializeObject(record.CustomFields.verleger));
                if (record.CustomFields.verleger is string)
                {
                    return record.CustomFields.verleger;
                }

                if (record.CustomFields.verleger is List<object>)
                {
                    return string.Join(", ", record.CustomFields.verleger);
                }
            }

            return null;
        }

        public static string Abdeckung(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property abdeckung.");
            if (record.HasCustomProperty("abdeckung"))
            {
                Log.Verbose("Property abdeckung: {abdeckung}", JsonConvert.SerializeObject(record.CustomFields.abdeckung));
                if (record.CustomFields.abdeckung is string)
                {
                    return record.CustomFields.abdeckung;
                }

                if (record.CustomFields.abdeckung is List<object>)
                {
                    return string.Join(", ", record.CustomFields.abdeckung);
                }
            }

            return null;
        }

        public static string Ablieferung(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property veAblieferungLink.");
            if (record.HasCustomProperty("veAblieferungLink"))
            {
                Log.Verbose("Property veAblieferungLink: {veAblieferungLink}", JsonConvert.SerializeObject(record.CustomFields.veAblieferungLink));
                var veAblieferungLink = record.CustomFields.veAblieferungLink;

                if (veAblieferungLink is List<object>)
                {
                    var ablieferung = new List<string>();
                    foreach (var elasticEntityLink in veAblieferungLink)
                    {
                        if (HasProperty(elasticEntityLink, "value"))
                        {
                            ablieferung.Add(elasticEntityLink.value);
                        }
                    }

                    return string.Join(", ", ablieferung);
                }

                if (HasProperty(veAblieferungLink, "value"))
                {
                    return veAblieferungLink.value;
                }
            }

            return null;
        }

        /// <summary>
        /// Translates the field Levels and the customer field "Accessibility according to BGA".
        /// Was made for the task PVW-789
        /// </summary>
        /// <param name="record">the to translate record</param>
        /// <param name="language">language abbreviation e.g. "en"</param>
        public static void Translate(this TreeRecord record, string language)
        {
            try
            {
                var cultureInfo = new CultureInfo(language);
                if (record is SearchRecord searchRecord)
                {
                    searchRecord.TranslateCustomFieldZugaenglichkeitGemässBga(cultureInfo);
                }
                
                var level = ResourceManager.GetString(record.Level, cultureInfo) ;
                if (!string.IsNullOrEmpty(level))
                {
                    record.Level = level;
                } 
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while translating the record");
                throw;
            }
        }

        private static void TranslateCustomFieldZugaenglichkeitGemässBga(this SearchRecord record, CultureInfo cultureInfo)
        {
            dynamic customFields = record.CustomFields;
            var fields = (IDictionary<string, object>)customFields;

            if (fields.ContainsKey("zugänglichkeitGemässBga"))
            {
                var value = fields["zugänglichkeitGemässBga"].ToString();
                fields.Remove("zugänglichkeitGemässBga");
                var result = ResourceManager.GetString(value, cultureInfo); 
                ((IDictionary<string, object>)customFields).Add("zugänglichkeitGemässBga", result != string.Empty ? result : value);
            }
        }

        private static bool HasProperty(dynamic expandoObject, string propertyName)
        {
            return ((IDictionary<string, object>) expandoObject).ContainsKey(propertyName);
        }
    }
}