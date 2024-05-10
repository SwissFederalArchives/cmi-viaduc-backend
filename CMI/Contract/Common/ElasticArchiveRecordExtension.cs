using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using CMI.Contract.Common.Entities;
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
            var sr = entity as SearchRecord;
            return sr.HasCustomProperty(key);
        }

        public static bool HasCustomProperty<T>(this T entity, string key) where T : SearchRecord
        {
            // ignore case, because the customfields are lowercamelcase
            return entity?.CustomFields != null &&
                   ((IDictionary<string, object>) entity.CustomFields).Any(k => k.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool HasCustomPropertyWithValue<T>(this ElasticArchiveRecord entity, string key)
        {
            if (entity.HasCustomProperty(key))
            {
                 var entry = ((IDictionary<string, object>) entity.CustomFields).First(k => k.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));

                 if (entry.Value is T)
                 {
                     return entry.Value != null && !string.IsNullOrWhiteSpace(entry.Value.ToString());
                 }

                 if (entry.Value is List<T> innerList)
                 {
                     return innerList.Any();
                 }
            }

            return false;
        }

        public static void SetCustomProperty<T>(this T entity, string key, string value) where T : SearchRecord
        {
            if (entity.HasCustomProperty(key))
            { 
                ((IDictionary<string, object>) entity.CustomFields)[key] = value;
            }
            else
            {
                ((IDictionary<string, object>) entity.CustomFields).Add(key,value);
            }
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

        /// <summary>
        /// Feld verweist auf das CustomField "ZusatzkomponenteZac1"
        /// </summary>
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

        /// <summary>
        /// Feld verweist auf das CustomField "ZusatzkomponenteZac1"
        /// </summary>
        public static void SetAnonymizeZusatzmerkmal(this ElasticArchiveRecord record, string anonymizeText)
        {
            Log.Verbose("Setting property zusatzkomponenteZac1.");
            if (record.HasCustomProperty("zusatzkomponenteZac1"))
            {
                record.CustomFields.zusatzkomponenteZac1 = anonymizeText;
            }
            else
            {
                throw new InvalidOperationException("CustomFields Property zusatzkomponenteZac1 is not present");
            }
        }

        public static string VerwandteVe(this ElasticArchiveRecord record)
        {
            Log.Verbose("Getting property verwandteVe.");
            if (record.HasCustomProperty("verwandteVe"))
            {
                Log.Verbose("Property verwandteVe: {verwandteVe}",
                    JsonConvert.SerializeObject(record.CustomFields.verwandteVe));
                if (record.CustomFields.verwandteVe is string)
                {
                    return record.CustomFields.verwandteVe;
                }

                if (record.CustomFields.verwandteVe is List<object>)
                {
                    return string.Join(", ", record.CustomFields.verwandteVe);
                }
            }

            return null;
        }

        public static void SetAnonymizeVerwandteVe(this ElasticArchiveRecord record, string anonymizeText)
        {
            Log.Verbose("Setting property verwandteVe.");
            if (record.HasCustomProperty("verwandteVe"))
            {
                record.CustomFields.verwandteVe = anonymizeText;
            }
            else
            {
                throw new InvalidOperationException("CustomFields Property verwandteVe is not present");
            }
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

        /// <summary>
        /// Feld verweist auf das interne CustomField BemerkungZurVe
        /// </summary>
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

        /// <summary>
        /// Feld verweist auf das interne CustomField BemerkungZurVe
        /// </summary>
        public static void SetAnonymizeZusätzlicheInformationen(this ElasticArchiveRecord record, string anonymizeText)
        {
            Log.Verbose("Setting property bemerkungZurVe.");
            if (record.HasCustomProperty("bemerkungZurVe"))
            {
                record.CustomFields.bemerkungZurVe = anonymizeText;
            }
            else
            {
                throw new InvalidOperationException("CustomFields Property bemerkungZurVe is not present");
            }
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
              
                var level = ResourceManager.GetString(record.Level ?? "", cultureInfo) ;
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

        public static List<ArchiveRecordContextItem> GetArchivePlanContext(this ElasticArchiveRecord record)
        {
            return record.ArchiveplanContext.Select(contextItem => new ArchiveRecordContextItem
            {
                ArchiveRecordId = contextItem.ArchiveRecordId,
                Title = contextItem.Title,
                ReferenceCode = contextItem.RefCode,
                DateRangeText = contextItem.DateRangeText,
                Level = contextItem.Level,
                Protected = contextItem.Protected
            }).ToList();
        }

        public static List<ArchiveRecordContextItem> GetArchivePlanContext(this ElasticArchiveDbRecord record)
        {
            if (record.IsAnonymized)
            {
                return record.UnanonymizedFields.ArchiveplanContext.Select(contextItem => new ArchiveRecordContextItem
                {
                    ArchiveRecordId = contextItem.ArchiveRecordId,
                    Title = contextItem.Title,
                    ReferenceCode = contextItem.RefCode,
                    DateRangeText = contextItem.DateRangeText,
                    Level = contextItem.Level,
                    Protected = contextItem.Protected
                }).ToList();
            }

            return record.ArchiveplanContext.Select(contextItem => new ArchiveRecordContextItem
            {
                ArchiveRecordId = contextItem.ArchiveRecordId,
                Title = contextItem.Title,
                ReferenceCode = contextItem.RefCode,
                DateRangeText = contextItem.DateRangeText,
                Level = contextItem.Level,
                Protected = contextItem.Protected
            }).ToList();
        }


        public static ArchiveRecordContextItem ToArchiveRecordContextItem(this ElasticArchiveRecord record)
        {
            return new ArchiveRecordContextItem
            {
                ArchiveRecordId = record.ArchiveRecordId,
                Title = record.Title,
                ReferenceCode = record.ReferenceCode,
                DateRangeText = record.CreationPeriod.Text,
                Level = record.Level,
                Protected = !string.IsNullOrWhiteSpace(record.Permission)
            };
        }

        public static ManuelleKorrekturDto ToManuelleKorrektur(this ElasticArchiveDbRecord record)
        {
            List<ManuelleKorrekturFeldDto> manuelleKorrekturFelder;
            // Je nachdem ob der Record anonymisiert ist und die Felder UnanonymizedFields gefüllt sind, wird
            // der Datensatz für die manuelle Korrektur anders erstellt. 
            if (record.IsAnonymized)
            {
                manuelleKorrekturFelder = new List<ManuelleKorrekturFeldDto>
                {
                    new() {Feldname = ManuelleKorrekturFelder.Titel, Automatisch = record.Title, Original = record.UnanonymizedFields?.Title},
                    new() {Feldname = ManuelleKorrekturFelder.VerwandteVe, Automatisch = record.VerwandteVe(), Original = record.UnanonymizedFields?.VerwandteVe},
                    new() {Feldname = ManuelleKorrekturFelder.Darin, Automatisch = record.WithinInfo, Original = record.UnanonymizedFields?.WithinInfo},
                    new() {Feldname = ManuelleKorrekturFelder.ZusatzkomponenteZac1, Automatisch = record.Zusatzmerkmal(), Original = record.UnanonymizedFields?.ZusatzkomponenteZac1},
                    new() {Feldname = ManuelleKorrekturFelder.BemerkungZurVe, Automatisch = record.ZusätzlicheInformationen(),
                        Original = record.UnanonymizedFields?.BemerkungZurVe}
                };
            }
            else
            {
                manuelleKorrekturFelder = new List<ManuelleKorrekturFeldDto>
                {
                    new() {Feldname = ManuelleKorrekturFelder.Titel, Automatisch = record.Title, Original = record.Title},
                    new() {Feldname = ManuelleKorrekturFelder.VerwandteVe, Automatisch = record.VerwandteVe(), Original = record.VerwandteVe()},
                    new() {Feldname = ManuelleKorrekturFelder.Darin, Automatisch = record.WithinInfo, Original = record.WithinInfo},
                    new() {Feldname = ManuelleKorrekturFelder.ZusatzkomponenteZac1, Automatisch = record.Zusatzmerkmal(), Original = record.Zusatzmerkmal()},
                    new() {Feldname = ManuelleKorrekturFelder.BemerkungZurVe, Automatisch = record.ZusätzlicheInformationen(),
                        Original = record.ZusätzlicheInformationen()}
                };
            }

            return new ManuelleKorrekturDto(-1, Convert.ToInt32(record.ArchiveRecordId), record.ReferenceCode, record.ProtectionEndDate.Date,
                record.IsAnonymized ? record.UnanonymizedFields?.Title : record.Title, DateTime.Now, null, null, null, 
                0, string.Empty, record.Level, record.Aktenzeichen(), record.CreationPeriod.Text,
                record.HasCustomProperty("zugänglichkeitGemässBga") ? record.CustomFields.zugänglichkeitGemässBga : "",
                record.GetSchutzfristenVerzeichnung(), record.ZuständigeStelle(), record.Publikationsrechte(),
                record.IsAnonymized, manuelleKorrekturFelder, null);
        }
    }
}