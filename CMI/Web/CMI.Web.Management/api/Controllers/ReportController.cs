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
using MassTransit;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    public class ReportController : ApiManagementControllerBase
    {
        private readonly ExcelExportHelper exportHelper;
        private readonly IPublicOrder orderManagerClient;

        public ReportController(IPublicOrder client, ExcelExportHelper exportHelper)
        {
            this.orderManagerClient = client;
            this.exportHelper = exportHelper;
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
               
                Log.Information("Has records count {Count} read", rawResult.Count);
                List<PrimaerdatenAufbereitungItem> rawData = rawResult.GroupBy(
                        i => i.OrderItemId)
                    .Where(g => g.Count() == 1 && g.Key != null).SelectMany(element => element).ToList();

                rawData.AddRange(rawResult
                    .GroupBy(i => i.OrderItemId).Where(g =>
                        g.Count() > 1 && g.Key != null).Select(entry => 
                        entry.FirstOrDefault(e => e.AuftragErledigt.HasValue )));

                if (rawData.Count == 0)
                {
                    throw new Exception("Keine Daten gefunden, Zeitraum vergrössern ");
                }

                Log.Information("Has records count {Count} read", rawData.Count);
                var response = ConvertToPrimaerdatenRecord(rawData);

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
               new() {ColumnName = nameof(PrimaerdatenReportRecord.OrderId), ColumnHeader = "Auftrags-ID", MakeAutoWidth = true},
               new() {ColumnName = nameof(PrimaerdatenReportRecord.VeId), ColumnHeader = "ID-VE", MakeAutoWidth = true},
               new() {ColumnName = nameof(PrimaerdatenReportRecord.Signatur), ColumnHeader = "Signatur", MakeAutoWidth = true},
               new() {ColumnName = nameof(PrimaerdatenReportRecord.Size),ColumnHeader = "Grösse SIP in MB",  MakeAutoWidth = true},
               new() {ColumnName = nameof(PrimaerdatenReportRecord.FileCount), ColumnHeader = "Anzahl Files im SIP", MakeAutoWidth = true},
               new() {ColumnName = nameof(PrimaerdatenReportRecord.DigitalisierungsKategorie), ColumnHeader = "Kategorie Digipool", MakeAutoWidth = true},
               new() {
                   ColumnName = nameof(PrimaerdatenReportRecord.NeuEingegangen),
                   ColumnHeader = "Zeitstempel Status 'Neu eingegangen'",
                   FormatSpecification = format.FormatString, MakeAutoWidth = true
               },
               new() {
                   ColumnName = nameof(PrimaerdatenReportRecord.Ausgeliehen),
                   ColumnHeader = "Zeitstempel Status 'Ausgeliehen'",
                   FormatSpecification = format.FormatString, MakeAutoWidth = true},
               new() {
                   ColumnName = nameof(PrimaerdatenReportRecord.ZumReponierenBereit),
                   ColumnHeader = "Zeitstempel Status 'Zum Reponieren bereit'",
                   FormatSpecification = format.FormatString, MakeAutoWidth = true
               },
               new() {
                   ColumnName = nameof(PrimaerdatenReportRecord.Mail),
                   ColumnHeader = "Zeitstempel Versand Mail 'Für Download bereit' an User",
                   FormatSpecification = format.FormatString,
                   MakeAutoWidth = true
                },
               new() {ColumnName = nameof(PrimaerdatenReportRecord.WartezeitDigitalisierung),ColumnHeader = "Von 'Neu eingegangen' bis 'Ausgeliehen' in [h]:mm",  MakeAutoWidth = true},
               new() {ColumnName = nameof(PrimaerdatenReportRecord.DauerDigitalisierung),ColumnHeader = "Von 'Ausgeliehen' bis 'Zum Reponieren bereit' in [h]:mm",  MakeAutoWidth = true},
               new() {ColumnName = nameof(PrimaerdatenReportRecord.DigitalisierungTotal),ColumnHeader = "Von 'Neu Eingegangen' bis 'Zum Reponieren bereit' in [h]:mm",  MakeAutoWidth = true},
               new() {ColumnName = nameof(PrimaerdatenReportRecord.DauerAufbereitung),ColumnHeader = "Von 'Zum Reponieren bereit' bis Mailversand in [h]:mm",  MakeAutoWidth = true},
               new() {ColumnName = nameof(PrimaerdatenReportRecord.TotalWartezeitNutzer),ColumnHeader = "Von 'Neu Eingegangen' bis Mailversand in [h]:mm",  MakeAutoWidth = true}
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
            try
            {
                var list = JsonConvert.DeserializeObject<List<ElasticArchiveRecordPackage>>(item.PackageMetadata);
                if (list != null)
                {
                    var metaData = list.FirstOrDefault();

                    if (metaData != null)
                    {
                        record.Size = metaData.SizeInBytes / (1024 * 1024);
                        record.FileCount = metaData.FileCount;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("ReportController, VeId {VeId}, PrimaerdatenAuftragId {PrimaerdatenAuftragId}: an error occurred while deserializing meta data: {Message}", item.VeId, item.PrimaerdatenAuftragId, ex.Message);
            }
        }

        private static void SetPrimaerdaten(PrimaerdatenReportRecord record, PrimaerdatenAufbereitungItem item)
        {
            // Primaerdaten
            record.OrderId = item.OrderItemId;
            record.VeId = item.VeId;
            record.Signatur = item.Signatur;
            record.NeuEingegangen = item.NeuEingegangen;
            record.Ausgeliehen = item.Ausgeliehen;
            record.ZumReponierenBereit = item.ZumReponierenBereit;
            if (item.DigitalisierungsKategorieId.HasValue)
            {
                record.DigitalisierungsKategorie = DigitalisierungsKategorieId((DigitalisierungsKategorie)item.DigitalisierungsKategorieId.Value);
            }
            record.Mail = item.Abgeschlossen ; 
        }

        private static string DigitalisierungsKategorieId(DigitalisierungsKategorie itemDigitalisierungsKategorieId)
        {
            return itemDigitalisierungsKategorieId.ToString();
        }

        private List<PrimaerdatenReportRecord> ConvertToPrimaerdatenRecord(List<PrimaerdatenAufbereitungItem> rawResult)
        {
            List<PrimaerdatenReportRecord> result = new List<PrimaerdatenReportRecord>();
            
            foreach (var item in rawResult)
            {
                var record = new PrimaerdatenReportRecord();
                SetMetaData(item, record);
                SetPrimaerdaten(record, item);
                SetCalculatedValues(record, item);
                result.Add(record);
            }

            return result;
        }

        private void SetCalculatedValues(PrimaerdatenReportRecord @record, PrimaerdatenAufbereitungItem item)
        {
            var timeSpan = item.Ausgeliehen.HasValue & item.NeuEingegangen.HasValue
                ? item.Ausgeliehen.Value.Subtract(item.NeuEingegangen.Value)
                : (TimeSpan?)null;
            record.WartezeitDigitalisierung = FormatTimeSpan(timeSpan);

            timeSpan = item.Ausgeliehen.HasValue & item.ZumReponierenBereit.HasValue
                ? item.ZumReponierenBereit.Value.Subtract(item.Ausgeliehen.Value)
                : (TimeSpan?)null;
            record.DauerDigitalisierung = FormatTimeSpan(timeSpan);

            timeSpan = item.NeuEingegangen.HasValue & item.ZumReponierenBereit.HasValue ?
                item.ZumReponierenBereit.Value.Subtract(item.NeuEingegangen.Value)
                : (TimeSpan?)null;
            record.DigitalisierungTotal = FormatTimeSpan(timeSpan);

            timeSpan = item.ZumReponierenBereit.HasValue & item.Abgeschlossen.HasValue ?
                item.Abgeschlossen.Value.Subtract(item.ZumReponierenBereit.Value)
                : (TimeSpan?)null;
            record.DauerAufbereitung = FormatTimeSpan(timeSpan);

            timeSpan = item.NeuEingegangen.HasValue & item.Abgeschlossen.HasValue ?
                item.Abgeschlossen.Value.Subtract(item.NeuEingegangen.Value)
                : (TimeSpan?)null;
            record.TotalWartezeitNutzer = FormatTimeSpan(timeSpan);
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
