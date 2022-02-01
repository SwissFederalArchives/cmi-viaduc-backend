using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Order;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.Auth;
using CMI.Web.Management.Helpers;
using MassTransit;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    public class ReportController : ApiManagementControllerBase
    {
        private readonly ExcelExportHelper exportHelper;
        private readonly IReportExternalContentHelper externalContentClient;
        private readonly IPublicOrder orderManagerClient;

        public ReportController(IPublicOrder client, ExcelExportHelper exportHelper, IReportExternalContentHelper externalContentClient)
        {
            this.orderManagerClient = client;
            this.exportHelper = exportHelper;
            this.externalContentClient = externalContentClient;
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetPrimaerdatenRecords(DateTime startDate, DateTime endDate)
        {
            Log.Information("Start GetPrimaerdatenRecords");
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.ReportingStatisticsReportsEinsehen);
            try
            {
                var rawResult =
                    await this.orderManagerClient.GetPrimaerdatenReportRecords(new LogDataFilter {StartDate = startDate, EndDate = endDate});
                var mutationIds = rawResult.Where(r => r.MutationsId.HasValue)
                    .Select(r => r.MutationsId.Value).ToArray();
                List<SyncInfoForReport> externalContent = null;
                if (mutationIds.Length > 0)
                {
                    Log.Information("Has mutationIds read by GetSyncInfoForReport");
                    externalContent = await externalContentClient.GetSyncInfoForReport(mutationIds);
                }
                else if (rawResult.Count == 0)
                {
                    throw new Exception("Keine Daten gefunden, Zeitraum vergrössern ");
                }
                Log.Information("Has records count {Count} read", rawResult.Count);

                var response = ConvertToPrimaerdatenRecord(rawResult, externalContent);

                var retVal = CreateExcelFile(response);

                return ResponseMessage(retVal);
            }
            catch (RequestTimeoutException ex)
            {
                Log.Error(ex, ex.Message);
                return BadRequest(
                    "Timeout beim Holen der Daten-Einträge. Der gewählte Zeitraum umfasst vermutlich zu viele Daten-Einträge. Bitte wählen Sie einen kürzeren Zeitraum");
            }
            catch (RequestFaultException ex)
            {
                Log.Error(ex, "Rabbit Mq error while fetching the log records");
                return BadRequest(
                    "Fehler beim Holen der Daten-Einträge. Der gewählte Zeitraum umfasst vermutlich zu viele Daten-Einträge. Bitte wählen Sie einen kürzeren Zeitraum");
            }
            catch (OutOfMemoryException ex)
            {
                Log.Error(ex, ex.Message);
                return BadRequest("Out of Memory: Der gewählte Zeitraum umfasst zu viele Daten-Einträge. Bitte wählen Sie einen kürzeren Zeitraum");
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        private HttpResponseMessage CreateExcelFile(List<PrimaerdatenReportRecord> response)
        {
            var retVal = new HttpResponseMessage(HttpStatusCode.OK);
            var file = $"Viaduc-Primaerdaten-{DateTime.Now:s}.xlsx";
            var contentType = MimeMapping.GetMimeMapping(Path.GetExtension(file));
            var format = new DateTimeFormat("dd.MM.yyyy HH:mm:ss");

            using (var stream = exportHelper.ExportToExcel(response, new ExcelColumnInfos
            {
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.AufbereitungsArt), ColumnHeader = "Auftragstyp", MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.OrderId), ColumnHeader = "Auftrags-ID", MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.VeId), ColumnHeader = "VE-ID", MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.MutationsId), ColumnHeader = "Mutations-ID", MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.PrimaerdatenAuftragId), ColumnHeader = "Primärdaten Auftrag-ID", MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.Size),ColumnHeader = "Grösse SIP in MB",  MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.FileCount), ColumnHeader = "Anzahl Files im SIP", MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.FileFormats), ColumnHeader = "Dateiformat im SIP", MakeAutoWidth = true},
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.Source), 
                    ColumnHeader = "Quelle: digitale Ablieferung oder Digitalisierung durch Vecteur?", 
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo {
                    ColumnName = nameof(PrimaerdatenReportRecord.NeuEingegangen),
                    ColumnHeader = "Zeitstempel Status 'Neu Eingegangen'",
                    FormatSpecification = format.FormatString, MakeAutoWidth = true
                },
                new ExcelColumnInfo {
                    ColumnName = nameof(PrimaerdatenReportRecord.FreigabePrüfen),
                    ColumnHeader = "Zeitstempel Status 'Freigabe prüfen'",
                    FormatSpecification = format.FormatString, MakeAutoWidth = true 
                },
                new ExcelColumnInfo {
                    ColumnName = nameof(PrimaerdatenReportRecord.FürDigitalisierungBereit),
                    ColumnHeader = "Zeitstempel Status 'Für Digitalisierung bereit'", 
                    FormatSpecification = format.FormatString, MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerManuelleFreigabe),
                    ColumnHeader = "Dauer manuelle Freigabe durch BAR-MA  in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                    {ColumnName = nameof(PrimaerdatenReportRecord.FürAushebungBereit), ColumnHeader = "Zeitstempel Status 'Für Aushebung bereit'",
                        FormatSpecification = format.FormatString, MakeAutoWidth = true},
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerAuftragsabrufVecteur),
                    ColumnHeader = "Dauer Auftragsabruf durch Vecteur  in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                    {ColumnName = nameof(PrimaerdatenReportRecord.Ausgeliehen), ColumnHeader = "Zeitstempel Status 'Ausgeliehen'",
                        FormatSpecification = format.FormatString, MakeAutoWidth = true},
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerAusleiheLogistik),
                    ColumnHeader = "Dauer Ausleihe durch Logistik in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo {
                        ColumnName = nameof(PrimaerdatenReportRecord.ZumReponierenBereit),
                        ColumnHeader = "Zeitstempel Status 'Zum Reponieren bereit'",
                        FormatSpecification = format.FormatString, MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerDigitalisierungVecteur),
                    ColumnHeader = "Dauer Digitalisierung durch Vecteur inkl. Ingest DIR in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.PrimaryDataLinkCreationDate),
                    ColumnHeader = "Zeitstempel Status 'Identifikation digitales Magazin' in AIS",
                    FormatSpecification = format.FormatString, MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerUpdateAIPAdresseAIS),
                    ColumnHeader = "Dauer Update AIP-Adresse in AIS in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.StartFirstSynchronizationAttempt),
                    ColumnHeader = "Zeitstempel Start erster Synchronisierungsversuch (Status 1)",
                    FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerStartSynchronisierungWebOZ),
                    ColumnHeader = "Dauer Start Synchronisierung durch WebOZ in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.StartLastSynchronizationAttempt),
                    ColumnHeader = "Zeitstempel Start letzter Synchronisierungsversuch bevor Synchronisierung erfolgreich war (Status 2)",
                    FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.CompletionLastSynchronizationAttempt),
                    ColumnHeader = "Zeitstempel Abschluss Synchronisierung (Status 2)",
                    FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.CountSynchronizationAttempts),
                    ColumnHeader = "Anzahl notwendiger Synchronisierungsversuche bis Status 2",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerErfolgreicherSyncVersuch),
                    ColumnHeader = "Dauer erfolgreicher Sync-Versuch in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerAlleSyncVersuche),
                    ColumnHeader = "Dauer alle Sync-Versuche  in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerZumReponierenBereitSyncCompleted),
                    ColumnHeader = "Dauer von 'Zum Reponieren bereit' / 'Ingest completed DIR' bis Sync completed Viaduc  in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.ClickButtonPrepareDigitalCopy),
                    ColumnHeader = "Zeitstempel Klick Button 'Digitalisat aufbereiten'",
                    FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.EstimatedPreparationTimeVeAccordingDetailPage),
                    ColumnHeader = "Geschätzte Aufbereitungszeit der VE gem. Anzeige auf Detailseite in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.StartFirstPreparationAttempt),
                    ColumnHeader = "Zeitstempel Start erster Aufbereitungsversuch Gebrauchskopie",
                    FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.StartLastPreparationAttempt),
                    ColumnHeader = "Zeitstempel Start letzter Aufbereitungsversuch bevor Aufbereitung erfolgreich war",
                    FormatSpecification = format.FormatString, MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.CompletionLastPreparationAttempt),
                    ColumnHeader = "Zeitstempel Abschluss Aufbereitung Gebrauchskopie",
                    FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.CountPreparationAttempts),
                    ColumnHeader = "Aufbereitung erfolgreich",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerErfolgreicherAufbereitungsversuch),
                    ColumnHeader = "Dauer erfolgreicher Aufbereitungsversuch in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerAllAufbereitungsversuch),
                    ColumnHeader = "Dauer alle Aufbereitungsversuche in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerSyncCompletedAufbereitungErfolgreich),
                    ColumnHeader = "Dauer von 'Sync completed Viaduc' bis zu 'Aufbereitung erfolgreich' in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.StorageUseCopyCache),
                    ColumnHeader = "Zeitstempel Speicherung Gebrauchskopie im Cache",
                    FormatSpecification = format.FormatString, MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerAufbereitungErfolgreichSpeicherungGebrauchskopieCache),
                    ColumnHeader = "Dauer von 'Aufbereitung erfolgreich' bis Speicherung Gebrauchskopie im Cache in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.ShippingMailReadForDownload),
                    ColumnHeader = "Zeitstempel Versand Mail 'Fur Download bereit' an User",
                    FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.DauerAufbereitungErfolgreichMailVersandt),
                    ColumnHeader = "Dauer von 'Aufbereitung erfolgreich' bis Mail versandt in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.EingangBestellungVersandEMailZumDownloadBereit),
                    ColumnHeader = "Eingang Bestellung bis Versand der E-Mail 'Zum Download bereit' in [h]:mm",
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.KlickButtonDigitalisatAufbereitenVersandEMailZumDownloadBereit),
                    ColumnHeader = "Klick Button 'Digitalisat aufbereiten' bis Versand E-Mail 'Zum Download bereit'  in [h]:mm",
                    MakeAutoWidth = true
                }
            }))
                retVal.Content = new ByteArrayContent(stream.ToArray());
            retVal.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            retVal.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = file
            };
            return retVal;
        }

        private static void SetMetaData(PrimaerdatenAufbereitungItem item, PrimaerdatenReportRecord record)
        {
            var list = JsonConvert.DeserializeObject<List<ElasticArchiveRecordPackage>>(item.PackageMetadata);
            var metaData = list.FirstOrDefault();

            if (metaData != null)
            {
                var items = metaData.Items.Where(i => i.Type.Equals(ElasticRepositoryObjectType.File)).ToList().Select(i => Path.GetExtension(i.Name)).Distinct().ToArray();
                // ReSharper disable once PossibleLossOfFraction
                record.Size = metaData.SizeInBytes / (1024 * 1024);
                record.FileCount = metaData.FileCount;
                record.FileFormats = string.Join(",", items);
                var timeSpan = metaData.FulltextExtractionDuration + metaData.RepositoryExtractionDuration;
                record.EstimatedPreparationTimeVeAccordingDetailPage = FormatTimeSpan(timeSpan);
            }
        }

        private static void SetPrimaerdaten(PrimaerdatenReportRecord record, PrimaerdatenAufbereitungItem item)
        {
            // Primaerdaten
            record.AufbereitungsArt = item.AufbereitungsArt == "Sync" ? "Synchronisation" : "Aufbereitung";
            record.OrderId = item.OrderItemId;
            record.VeId = item.VeId;
            record.MutationsId = item.MutationsId;
            record.PrimaerdatenAuftragId = item.PrimaerdatenAuftragId;
            record.Source = item.Quelle;
            record.NeuEingegangen = item.NeuEingegangen;
            record.FreigabePrüfen = item.FreigabePruefen;
            record.FürDigitalisierungBereit = item.FuerDigitalisierungBereit;
            record.FürAushebungBereit = item.FuerAushebungBereit;
            record.Ausgeliehen = item.Ausgeliegen;
            record.ZumReponierenBereit = item.ZumReponierenBereit;
            record.ClickButtonPrepareDigitalCopy = item.Registriert;
            record.StartFirstPreparationAttempt = item.ErsterAufbereitungsversuch;
            record.StartLastPreparationAttempt = item.LetzterAufbereitungsversuch;
            record.CompletionLastPreparationAttempt = item.AuftragErledigt;
            record.ShippingMailReadForDownload = item.AuftragErledigt; 
            record.CountPreparationAttempts = item.AnzahlVersucheDownload;
            record.StorageUseCopyCache = item.ImCacheAbgelegt;
        }

        private List<PrimaerdatenReportRecord> ConvertToPrimaerdatenRecord(List<PrimaerdatenAufbereitungItem> rawResult, List<SyncInfoForReport> syncInfoForReport)
        {
            List<PrimaerdatenReportRecord> result = new List<PrimaerdatenReportRecord>();
            
            foreach (var item in rawResult)
            {
                var record = new PrimaerdatenReportRecord();
                if (syncInfoForReport != null && item.MutationsId.HasValue)
                {
                    var infoForReport = syncInfoForReport.FirstOrDefault(mut => mut.MutationId == item.MutationsId.Value);
                   if (infoForReport != null)
                   {
                       record.PrimaryDataLinkCreationDate = infoForReport.ErstellungsdatumPrimaerdatenVerbindung;
                       record.StartFirstSynchronizationAttempt = infoForReport.StartErsterSynchronisierungsversuch;
                       record.CompletionLastSynchronizationAttempt = infoForReport.AbschlussSynchronisierung;
                       record.StartLastSynchronizationAttempt = infoForReport.StartLetzterSynchronisierungsversuch;
                       record.CountSynchronizationAttempts = infoForReport.AnzahlNotwendigerSynchronisierungsversuche;
                   }
                }

                SetMetaData(item, record);
                SetPrimaerdaten(record, item);
                SetCalculatedValues(record, item);
                result.Add(record);
            }

            return result;
        }

        private void SetCalculatedValues(PrimaerdatenReportRecord @record, PrimaerdatenAufbereitungItem item)
        {
            var timeSpan = item.FreigabePruefen.HasValue & item.FuerDigitalisierungBereit.HasValue
                ? item.FuerDigitalisierungBereit.Value.Subtract(item.FreigabePruefen.Value)
                : (TimeSpan?)null;
            record.DauerManuelleFreigabe = FormatTimeSpan(timeSpan);

            timeSpan = item.FuerAushebungBereit.HasValue & item.FuerDigitalisierungBereit.HasValue ?
                    item.FuerAushebungBereit.Value.Subtract(item.FuerDigitalisierungBereit.Value)
                    : (TimeSpan?)null;
            record.DauerAuftragsabrufVecteur = FormatTimeSpan(timeSpan);

            timeSpan = item.FuerAushebungBereit.HasValue & item.Ausgeliegen.HasValue ?
            item.Ausgeliegen.Value.Subtract(item.FuerAushebungBereit.Value)
            : (TimeSpan?)null;
            record.DauerAusleiheLogistik = FormatTimeSpan(timeSpan);

            timeSpan = item.ZumReponierenBereit.HasValue & item.Ausgeliegen.HasValue ?
                item.ZumReponierenBereit.Value.Subtract(item.Ausgeliegen.Value)
                : (TimeSpan?)null;
            record.DauerDigitalisierungVecteur = FormatTimeSpan(timeSpan);

            timeSpan = item.ZumReponierenBereit.HasValue & record.PrimaryDataLinkCreationDate.HasValue ?
                record.PrimaryDataLinkCreationDate.Value.Subtract(item.ZumReponierenBereit.Value)
                : (TimeSpan?)null;
            record.DauerUpdateAIPAdresseAIS = FormatTimeSpan(timeSpan);

            timeSpan = record.StartFirstSynchronizationAttempt.HasValue & record.PrimaryDataLinkCreationDate.HasValue ?
                record.StartFirstSynchronizationAttempt.Value.Subtract(record.PrimaryDataLinkCreationDate.Value)
                : (TimeSpan?)null;
            record.DauerStartSynchronisierungWebOZ = FormatTimeSpan(timeSpan);

            timeSpan = record.CompletionLastSynchronizationAttempt.HasValue & record.StartLastSynchronizationAttempt.HasValue ?
                record.CompletionLastSynchronizationAttempt.Value.Subtract(record.StartLastSynchronizationAttempt.Value)
                : (TimeSpan?)null;
            record.DauerErfolgreicherSyncVersuch = FormatTimeSpan(timeSpan);

            timeSpan = record.CompletionLastSynchronizationAttempt.HasValue & record.StartFirstSynchronizationAttempt.HasValue ?
                record.CompletionLastSynchronizationAttempt.Value.Subtract(record.StartFirstSynchronizationAttempt.Value)
                : (TimeSpan?)null;
            record.DauerAlleSyncVersuche = FormatTimeSpan(timeSpan);

            timeSpan = record.CompletionLastSynchronizationAttempt.HasValue & item.ZumReponierenBereit.HasValue ?
                record.CompletionLastSynchronizationAttempt.Value.Subtract(item.ZumReponierenBereit.Value)
                : (TimeSpan?)null;
            record.DauerZumReponierenBereitSyncCompleted = FormatTimeSpan(timeSpan);

            timeSpan = record.CompletionLastPreparationAttempt.HasValue & record.StartLastPreparationAttempt.HasValue ?
                record.CompletionLastPreparationAttempt.Value.Subtract(record.StartLastPreparationAttempt.Value)
                : (TimeSpan?)null;
            record.DauerErfolgreicherAufbereitungsversuch = FormatTimeSpan(timeSpan);

            timeSpan = record.CompletionLastPreparationAttempt.HasValue & record.StartFirstPreparationAttempt.HasValue ?
                record.CompletionLastPreparationAttempt.Value.Subtract(record.StartFirstPreparationAttempt.Value)
                : (TimeSpan?)null;
            record.DauerAllAufbereitungsversuch = FormatTimeSpan(timeSpan);

            timeSpan = record.CompletionLastPreparationAttempt.HasValue & record.CompletionLastSynchronizationAttempt.HasValue ?
                record.CompletionLastPreparationAttempt.Value.Subtract(record.CompletionLastSynchronizationAttempt.Value)
                : (TimeSpan?)null;
            record.DauerSyncCompletedAufbereitungErfolgreich = FormatTimeSpan(timeSpan);

            timeSpan = record.StorageUseCopyCache.HasValue && record.CompletionLastPreparationAttempt.HasValue ?
                record.StorageUseCopyCache.Value.Subtract(record.CompletionLastPreparationAttempt.Value)
                : (TimeSpan?)null;
            record.DauerAufbereitungErfolgreichSpeicherungGebrauchskopieCache = FormatTimeSpan(timeSpan);

            timeSpan = record.ShippingMailReadForDownload.HasValue && record.CompletionLastPreparationAttempt.HasValue
                ? record.ShippingMailReadForDownload.Value.Subtract(record.CompletionLastPreparationAttempt.Value)
                : (TimeSpan?)null;
            record.DauerAufbereitungErfolgreichMailVersandt = FormatTimeSpan(timeSpan);

            timeSpan = record.ShippingMailReadForDownload.HasValue && item.NeuEingegangen.HasValue ?
                record.ShippingMailReadForDownload.Value.Subtract(item.NeuEingegangen.Value)
                : (TimeSpan?)null;
            record.EingangBestellungVersandEMailZumDownloadBereit = FormatTimeSpan(timeSpan);

            timeSpan = record.ShippingMailReadForDownload.HasValue && record.ClickButtonPrepareDigitalCopy.HasValue ?
                record.ShippingMailReadForDownload.Value.Subtract(record.ClickButtonPrepareDigitalCopy.Value)
                : (TimeSpan?)null;
            record.KlickButtonDigitalisatAufbereitenVersandEMailZumDownloadBereit = FormatTimeSpan(timeSpan);

        }

        private static string FormatTimeSpan(TimeSpan? timeSpan)
        {
            var result = string.Empty;
            if (timeSpan.HasValue)
            {
                result = timeSpan.Value.TotalSeconds >= 0 ?
                    $"{Math.Floor(timeSpan.Value.TotalHours)}:{timeSpan.Value.Minutes.ToString().PadLeft(2, '0')}"
                    : $"-{Math.Floor(Math.Abs(timeSpan.Value.TotalHours))}:{Math.Abs(timeSpan.Value.Minutes).ToString().PadLeft(2, '0')}";
                if (result == "-0:00")
                {
                    result = "0:00";
                }
            }
            return result;
        }
    }
}
