using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.Auth;
using MassTransit;
using Serilog;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    public class LogController : ApiManagementControllerBase
    {
        private readonly ExcelExportHelper exportHelper;
        private readonly IRequestClient<GetElasticLogRecordsRequest, GetElasticLogRecordsResponse> getLogClient;

        public LogController(IRequestClient<GetElasticLogRecordsRequest, GetElasticLogRecordsResponse> getLogClient,
            ExcelExportHelper exportHelper)
        {
            this.getLogClient = getLogClient;
            this.exportHelper = exportHelper;
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetElasticLogRecords(DateTime startDate, DateTime endDate)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationLoginformationenEinsehen);

            try
            {
                var response = await getLogClient.Request(new GetElasticLogRecordsRequest
                {
                    DataFilter = new LogDataFilter {StartDate = startDate, EndDate = endDate}
                });

                // Excel has a limit of 1'048'576 rows. We throw an error when more than 1 Mio is returned
                if (response.Result.TotalCount > 1000000)
                {
                    return BadRequest("Es wurden mehr als 1 Mio. Log-Einträge für den Zeitraum geliefert. Bitte wählen Sie einen kürzeren Zeitraum.");
                }

                var retVal = new HttpResponseMessage(HttpStatusCode.OK);
                var file = $"Viaduc-Log-Data-{DateTime.Now:s}.xlsx";
                var contentType = MimeMapping.GetMimeMapping(Path.GetExtension(file));

                using (var stream = exportHelper.ExportToExcel(response.Result.Records,
                    new ExcelColumnInfos
                    {
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.Id), MakeAutoWidth = true},
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.MachineName), MakeAutoWidth = true},
                        new ExcelColumnInfo
                            {ColumnName = nameof(ElasticLogRecord.Timestamp), MakeAutoWidth = true, FormatSpecification = "dd.mm.yyyy hh:mm:ss"},
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.ArchiveRecordId), MakeAutoWidth = true},
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.ConversationId), MakeAutoWidth = true},
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.Index), MakeAutoWidth = true},
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.Level), MakeAutoWidth = true},
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.MainAssembly), MakeAutoWidth = true},
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.ProcessId), MakeAutoWidth = true},
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.ThreadId), MakeAutoWidth = true},
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.Message), Width = 80},
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.Exception), Width = 80},
                        new ExcelColumnInfo {ColumnName = nameof(ElasticLogRecord.MessageTemplate), Hidden = true}
                    }))
                {
                    retVal.Content = new ByteArrayContent(stream.ToArray());
                    retVal.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                    retVal.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = file
                    };
                }

                return ResponseMessage(retVal);
            }
            catch (RequestTimeoutException ex)
            {
                Log.Error(ex, "Timeout while fetching the log records");
                return BadRequest(
                    "Timeout beim Holen der Log-Einträge. Der gewählte Zeitraum umfasst vermutlich zu viele Log-Einträge. Bitte wählen Sie einen kürzeren Zeitraum");
            }
            catch (RequestFaultException ex)
            {
                Log.Error(ex, "Rabbit Mq error while fetching the log records");
                return BadRequest(
                    "Fehler beim Holen der Log-Einträge. Der gewählte Zeitraum umfasst vermutlich zu viele Log-Einträge. Bitte wählen Sie einen kürzeren Zeitraum");
            }
            catch (OutOfMemoryException ex)
            {
                Log.Error(ex, "Out of memory while fetching the log records");
                return BadRequest("Out of Memory: Der gewählte Zeitraum umfasst zu viele Log-Einträge. Bitte wählen Sie einen kürzeren Zeitraum");
            }
        }
    }
}