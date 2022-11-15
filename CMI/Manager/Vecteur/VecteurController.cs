using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using CMI.Contract.Common;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Utilities.Template;
using MassTransit;
using NSwag.Annotations;
using Serilog;
using IBus = MassTransit.IBus;

namespace CMI.Manager.Vecteur
{
    public class VecteurController : ApiController
    {
        private const string faultedItemExceptionMessage = "Der angeforderte Auftrag ist fehlerhaft und kann nicht abgerufen werden.";
        private const string serviceNotAvailableExceptionMessage = "Der Service steht temporär nicht zur Verfügung.";
        private const string onlyFaultedItemsInDigipoolExceptionMessage = "Es liegen nur noch fehlerhafte Aufträge im Digipool.";
        private const string veNotFoundMessage = "Die angeforderte Verzeichnungseinheit wurde nicht gefunden.";
        private const string veHasNoProtectionEndDateMessage = "Die Verzeichnungseinheit hat kein Schutzfrist-Ende-Datum.";
        private readonly IBus bus;
        private readonly IDataBuilder dataBuilder;
        private readonly IDigitizationHelper digitizationHelper;
        private readonly IMailHelper mailHelper;
        private readonly IMessageBusCallHelper messageBusCallHelper;
        private readonly IPublicOrder orderManagerClient;
        private readonly IParameterHelper parameterHelper;

        private readonly IVecteurActions vecteurActionsClient;

        public VecteurController(IVecteurActions vecteurActionsClient,
            IPublicOrder orderManagerClient,
            IDigitizationHelper digitizationHelper,
            IMessageBusCallHelper messageBusCallHelper,
            IMailHelper mailHelper,
            IBus bus,
            IParameterHelper parameterHelper,
            IDataBuilder dataBuilder)
        {
            this.vecteurActionsClient = vecteurActionsClient ?? throw new ArgumentNullException(nameof(vecteurActionsClient));
            this.orderManagerClient = orderManagerClient ?? throw new ArgumentNullException(nameof(orderManagerClient));
            this.digitizationHelper = digitizationHelper ?? throw new ArgumentNullException(nameof(digitizationHelper));
            this.messageBusCallHelper = messageBusCallHelper ?? throw new ArgumentNullException(nameof(messageBusCallHelper));
            this.mailHelper = mailHelper ?? throw new ArgumentNullException(nameof(mailHelper));
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.parameterHelper = parameterHelper ?? throw new ArgumentNullException(nameof(parameterHelper));
            this.dataBuilder = dataBuilder ?? throw new ArgumentNullException(nameof(dataBuilder));
        }


        [HttpGet]
        [Route("api/v1/getnextdigitalisierungsauftrag")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(DigitalisierungsAuftrag),
            Description =
                "Diese Funktion liefert die Auftragsdetails des nächsten zu verarbeitenden Auftrags. Im Normalfall wird der nächste Auftrag im XML Format gemäss Schema Digitalisierungsauftrag zurückgegeben und der HTTP Statuscode lautet 200 OK")]
        [SwaggerResponse(HttpStatusCode.NoContent, typeof(void), Description = "Wenn keine Digitalisierungsaufträge vorhanden sind.")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, typeof(void), Description = "Wenn der HTTP Header ein falsches oder keinen API-Key enthält.")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, typeof(Exception), Description = "Wenn ein interner Fehler auftritt.")]
        [SwaggerResponse(HttpStatusCode.ServiceUnavailable, typeof(string), Description = serviceNotAvailableExceptionMessage)]
        [SwaggerResponse(HttpStatusCode.Forbidden, typeof(string), Description = onlyFaultedItemsInDigipoolExceptionMessage)]
        [SwaggerResponse(HttpStatusCode.RequestEntityTooLarge, typeof(string), Description = faultedItemExceptionMessage)]
        public async Task<IHttpActionResult> GetNextDigitalisierungsauftrag()
        {
            DigipoolEntry digipoolEntry = null;
            if (!ApiKeyChecker.IsCorrect(Request))
            {
                return Unauthorized();
            }

            try
            {
                Log.Information("Getting next order from digipool");
                DigitalisierungsAuftrag auftrag;
                var digipoolEntryArray = await orderManagerClient.GetDigipool(1);

                if (digipoolEntryArray.Length == 0)
                {
                    return StatusCode(HttpStatusCode.NoContent);
                }

                digipoolEntry = digipoolEntryArray[0];

                // Hat der zurückgelieferte Eintrag einen Aufbereitungsfehler, so bedeutet dies,
                // dass nur noch Aufträge mit Aufbereitungsfehlern im Digipool liegen. 
                // Wir liefern einen spezifischen HTTP Code zurück um dies anzuzeigen
                if (digipoolEntry.HasAufbereitungsfehler)
                {
                    return Content(HttpStatusCode.Forbidden, onlyFaultedItemsInDigipoolExceptionMessage);
                }

                if (digipoolEntry.VeId.HasValue)
                {
                    // Hat die Bestellung eine VE-ID, so holen wir die Details zum Record aus Elastic und
                    // bereiten die Bestellung über den Dienst auf.
                    Log.Verbose($"Fetching ve {digipoolEntry.VeId} from elastic index");
                    var orderedItemRecord = await messageBusCallHelper.GetElasticArchiveRecord(digipoolEntry.VeId.ToString());

                    if (orderedItemRecord == null)
                    {
                        throw new Exception(veNotFoundMessage);
                    }

                    Log.Verbose($"Fetching full digitization order from AIS for {orderedItemRecord.GetAuszuhebendeArchiveRecordId()}");
                    auftrag = await digitizationHelper.GetDigitalisierungsAuftrag(orderedItemRecord.GetAuszuhebendeArchiveRecordId());

                    // Lade Dossier, falls bestellte Einheit nicht das Dossier ist
                    ElasticArchiveRecord dossierRecord;
                    dossierRecord = auftrag.Dossier.VerzEinheitId.ToString() != orderedItemRecord.ArchiveRecordId
                        ? await messageBusCallHelper.GetElasticArchiveRecord(auftrag.Dossier.VerzEinheitId.ToString())
                        : orderedItemRecord;

                    if (dossierRecord.ProtectionEndDate?.Date == null)
                    {
                        throw new Exception(veHasNoProtectionEndDateMessage);
                    }

                    // Schutzfrist für Bestellungen hängen vom Schutzfrist-Enddatum des Dossiers und dem Freigabestatus ab.
                    // Der Freigabestatus ist immer derjenige der bestellten Einheit.
                    if (dossierRecord.ProtectionEndDate.Date > DateTime.Today)
                    {
                        auftrag.Dossier.InSchutzfrist = digipoolEntry.ApproveStatus != ApproveStatus.FreigegebenAusserhalbSchutzfrist;
                    }
                    else
                    {
                        auftrag.Dossier.InSchutzfrist = digipoolEntry.ApproveStatus == ApproveStatus.FreigegebenInSchutzfrist;
                    }

                    // Übertrage den Wert des Schutzfristproperties des Dossiers, auf alle enthaltenen VE (rekursiv)
                    UpdateInSchutzfrist(auftrag.Dossier.UntergeordneteVerzEinheiten, auftrag.Dossier.InSchutzfrist);
                }
                else
                {
                    // Hat die Bestellung KEINE VE-ID, so handelt es sich um ein manuell hinzugefügtes Dossier.
                    // Wir erstellen manuell ein DigitalisierungsAuftrag mit den Angaben des Benutzers.
                    auftrag = await digitizationHelper.GetManualDigitalisierungsAuftrag(digipoolEntry);

                    // Schutzfrist für manuelle Bestellungen hängen nur vom Freigabestatus des Auftrags ab.
                    auftrag.Dossier.InSchutzfrist = digipoolEntry.ApproveStatus != ApproveStatus.FreigegebenAusserhalbSchutzfrist;
                }

                // Gemeinsame Daten
                auftrag.Auftragsdaten.AuftragsId = digipoolEntry.OrderItemId.ToString();
                auftrag.Auftragsdaten.BemerkungenBar = digipoolEntry.InternalComment + "";
                auftrag.Auftragsdaten.BemerkungenKunde = GetBemerkungKunde(digipoolEntry) + "";
                auftrag.Auftragsdaten.Bestelldatum = digipoolEntry.OrderDate;

                Log.Verbose("Update Benutzungskopie flag for order");
                await orderManagerClient.UpdateBenutzungskopie(digipoolEntry.OrderItemId, auftrag.Auftragsdaten.Benutzungskopie);

                var errorList = ValidateAuftrag(auftrag);

                if (errorList.Any())
                {
                    throw new XmlSchemaValidationException("The data is not valid according to the Auftragsdaten.xsd schema. " +
                                                           $"{Environment.NewLine}The errors found are:{Environment.NewLine}" +
                                                           $"{string.Join(Environment.NewLine, errorList)}");
                }

                Log.Information("Sucessfully fetched next order with id {AuftragsId} from digipool.", auftrag.Auftragsdaten.AuftragsId);
                return Ok(auftrag);
            }
            // Request timeout --> Manager, Index or External Content Service is not running
            catch (RequestTimeoutException e)
            {
                Log.Error(e, "RequestTimeout Exception");
                return Content(HttpStatusCode.ServiceUnavailable, serviceNotAvailableExceptionMessage);
            }
            // Something in the remote call failed.
            catch (RequestFaultException e)
            {
                Log.Error(e, "Remote call faulted");
                // if we have a remote sql db problem, then return a service unavailable
                if (e.Fault.Exceptions[0].ExceptionType == typeof(OrderDatabaseNotFoundOrNotRunningException).FullName)
                {
                    return Content(HttpStatusCode.ServiceUnavailable, $"{serviceNotAvailableExceptionMessage} (SQL Server not running.)");
                }

                // if we have a remote ais db problem, then return a service unavailable
                if (e.Fault.Exceptions[0].ExceptionType == typeof(AISDatabaseNotFoundOrNotRunningException).FullName)
                {
                    return Content(HttpStatusCode.ServiceUnavailable, $"{serviceNotAvailableExceptionMessage} (AIS database not running.");
                }

                // Any other problem, mark the order as faulted.
                if (digipoolEntry != null)
                {
                    return await ProcessFaultedItem(digipoolEntry,
                        $"{e.Fault.Exceptions[0].Message}{Environment.NewLine + Environment.NewLine}{e.Fault.Exceptions[0].StackTrace}");
                }

                // If there is an error, and we do not have a digipool entry, then return internal server error
                // This should not happen
                return InternalServerError(e);
            }
            catch (XmlSchemaValidationException e)
            {
                Log.Error(e, "Schema validation failed");
                return await ProcessNormalError(digipoolEntry, e);
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error while getting next order from digipool.");
                return await ProcessNormalError(digipoolEntry, e);
            }
        }

        [HttpGet]
        [Route("api/v1/getstatus/{auftragsid}")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(string), Description = "Liefert den aktuellen Status eines Digitalisierungsauftrags.")]
        [SwaggerResponse(HttpStatusCode.NotFound, typeof(string), Description = "Wenn die angegebene AuftragsId nicht existiert.")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, typeof(string), Description = "Wenn der HTTP Header ein falsches oder keinen API-Key enthält.")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, typeof(Exception), Description = "Wenn ein interner Fehler auftritt.")]
        public async Task<IHttpActionResult> GetStatus(int auftragsid)
        {
            Log.Information("Received GetStatus call for order with id {auftragsid}.", auftragsid);
            if (!ApiKeyChecker.IsCorrect(Request))
            {
                return Unauthorized();
            }

            try
            {
                var orderItems = await messageBusCallHelper.FindOrderItems(new[] {auftragsid});

                if (orderItems.Length == 0)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }

                Log.Information("Returned status for order with id {auftragsid}.", auftragsid);
                return Ok(orderItems[0].Status.ToString());
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error while getting status for order with id {auftragsid}", auftragsid);
                return InternalServerError(e);
            }
        }

        [HttpPost]
        [Route("api/v1/setstatusaushebungbereit/{auftragsid}")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(string), Description = "Der Status wurde wunschgemäss geändert.")]
        [SwaggerResponse(HttpStatusCode.NotFound, typeof(string), Description = "Wenn die angegebene AuftragsId nicht existiert.")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, typeof(string), Description = "Wenn der HTTP Header ein falsches oder keinen API-Key enthält.")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, typeof(Exception), Description = "Wenn ein interner Fehler auftritt.")]
        public async Task<IHttpActionResult> SetStatusAushebungBereit(int auftragsid)
        {
            Log.Information("Received SetStatusAushebungBereit call for order with id {auftragsid}.", auftragsid);
            if (!ApiKeyChecker.IsCorrect(Request))
            {
                return Unauthorized();
            }

            await vecteurActionsClient.SetStatusAushebungBereit(auftragsid);

            Log.Information("Successfully updated status to FuerAushebungBereit for order with id {auftragsid}.", auftragsid);
            return Ok("OK");
        }

        [HttpPost]
        [Route("api/v1/setstatusdigitalisierungabgebrochen/{auftragsid}")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(string), Description = "Der Status wurde wunschgemäss geändert.")]
        [SwaggerResponse(HttpStatusCode.NotFound, typeof(string), Description = "Wenn die angegebene AuftragsId nicht existiert.")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, typeof(string), Description = "Wenn der HTTP Header ein falsches oder keinen API-Key enthält.")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, typeof(Exception), Description = "Wenn ein interner Fehler auftritt.")]
        public async Task<IHttpActionResult> SetStatusDigitalisierungAbgebrochen(int auftragsid, string grund)
        {
            Log.Information("Received SetStatusDigitalisierungAbgebrochen call for order with id {auftragsid}.", auftragsid);
            if (!ApiKeyChecker.IsCorrect(Request))
            {
                return Unauthorized();
            }

            await vecteurActionsClient.SetStatusDigitalisierungAbgebrochen(auftragsid, grund);

            Log.Information("Successfully updated status to Abgebrochen for order with id {auftragsid}.", auftragsid);
            return Ok("OK");
        }

        [HttpPost]
        [Route("api/v1/setstatuszumreponierenbereit/{auftragsid}")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(string), Description = "Der Status wurde wunschgemäss geändert.")]
        [SwaggerResponse(HttpStatusCode.NotFound, typeof(string), Description = "Wenn die angegebene AuftragsId nicht existiert.")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, typeof(string), Description = "Wenn der HTTP Header ein falsches oder keinen API-Key enthält.")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, typeof(Exception), Description = "Wenn ein interner Fehler auftritt.")]
        public async Task<IHttpActionResult> SetStatusZumReponierenBereit(int auftragsid)
        {
            Log.Information("Received SetStatusZumReponierenBereit call for order with id {auftragsid}.", auftragsid);
            if (!ApiKeyChecker.IsCorrect(Request))
            {
                return Unauthorized();
            }

            await vecteurActionsClient.SetStatusZumReponierenBereit(auftragsid);

            Log.Information("Successfully updated status to ZumReponierenBereit for order with id {auftragsid}.", auftragsid);
            return Ok("OK");
        }

        [HttpPost]
        [Route("api/v1/setstatusdigitalisierungextern/{auftragsid}")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(string), Description = "Der Status wurde wunschgemäss geändert.")]
        [SwaggerResponse(HttpStatusCode.NotFound, typeof(string), Description = "Wenn die angegebene AuftragsId nicht existiert.")]
        [SwaggerResponse(HttpStatusCode.Unauthorized, typeof(string), Description = "Wenn der HTTP Header ein falsches oder keinen API-Key enthält.")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, typeof(Exception), Description = "Wenn ein interner Fehler auftritt.")]
        public async Task<IHttpActionResult> SetStatusDigitalisierungExtern(int auftragsid)
        {
            Log.Information("Received SetStatusDigitalisierungExtern call for order with id {auftragsid}.", auftragsid);
            if (!ApiKeyChecker.IsCorrect(Request))
            {
                return Unauthorized();
            }

            await vecteurActionsClient.SetStatusDigitalisierungExtern(auftragsid);

            Log.Information("Successfully updated status to StatusDigitalisierungExtern for order with id {auftragsid}.", auftragsid);
            return Ok("OK");
        }


        private string GetBemerkungKunde(DigipoolEntry digipoolEntry)
        {
            var list = new List<string>();

            if (!string.IsNullOrEmpty(digipoolEntry.OrderItemComment))
            {
                list.Add($"Aufrag: {digipoolEntry.OrderItemComment}");
            }

            if (!string.IsNullOrEmpty(digipoolEntry.OrderingComment))
            {
                list.Add($"Bestellung: {digipoolEntry.OrderingComment}");
            }

            return string.Join(" ", list);
        }

        /// <summary>
        ///     Überträgt den übergebenen Wert an das InSchutzfrist Property aller Verzeichnungseinheiten der übergebenen
        ///     Liste, und allfälligen Kindern.
        /// </summary>
        /// <param name="verzEinheiten">
        ///     Die Liste mit Verzeichnungseinheiten auf die der Wert auf das Property InSchutzfrist
        ///     übertragen werden soll.
        /// </param>
        /// <param name="inSchutzfrist">Der zu übertragende Wert.</param>
        private void UpdateInSchutzfrist(List<VerzEinheitType> verzEinheiten, bool inSchutzfrist)
        {
            if (verzEinheiten == null)
            {
                return;
            }

            foreach (var verzEinheit in verzEinheiten)
            {
                verzEinheit.InSchutzfrist = inSchutzfrist;
                UpdateInSchutzfrist(verzEinheit.UntergeordneteVerzEinheiten, inSchutzfrist);
            }
        }

        private async Task<IHttpActionResult> ProcessFaultedItem(DigipoolEntry digipoolEntry, string exceptionInfo)
        {
            await orderManagerClient.MarkOrderAsFaulted(digipoolEntry.OrderItemId);
            await SendDigipoolFailureMail(digipoolEntry.OrderItemId, exceptionInfo);
            return Content(HttpStatusCode.RequestEntityTooLarge, faultedItemExceptionMessage);
        }

        private async Task<IHttpActionResult> ProcessNormalError(DigipoolEntry digipoolEntry, Exception e)
        {
            // Any problem, mark the order as faulted.
            if (digipoolEntry != null)
            {
                return await ProcessFaultedItem(digipoolEntry, $"{e.Message}{Environment.NewLine + Environment.NewLine}{e.StackTrace}");
            }

            // If there is an error, and we do not have a digipool entry, then return internal server error
            // This should not happen
            return InternalServerError(e);
        }

        private async Task SendDigipoolFailureMail(int orderItemId, string exceptionInfo)
        {
            try
            {
                var template = parameterHelper.GetSetting<DigipoolAufbereitungFehlgeschlagen>();
                var dataContext = dataBuilder
                    .SetDataProtectionLevel(DataBuilderProtectionStatus.AllUnanonymized)
                    .AddAuftraege(new[] {orderItemId})
                    .AddValue("exceptionMessage", exceptionInfo)
                    .Create();

                await mailHelper.SendEmail(bus, template, dataContext);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while sending an EMail");
                throw;
            }
        }


        private List<string> ValidateAuftrag(DigitalisierungsAuftrag auftrag)
        {
            var data = auftrag.Serialize();

            var schemas = new XmlSchemaSet();
            string schema;
            var assembly = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CMI.Contract.Common.dll"));
            var resourceName = "CMI.Contract.Common.Digitalisierungsauftrag.xsd";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException()))
            {
                schema = reader.ReadToEnd();
            }

            schemas.Add("", XmlReader.Create(new StringReader(schema)));

            var xmlDoc = XDocument.Parse(data);
            var errors = new List<string>();
            xmlDoc.Validate(schemas, (o, e) => { errors.Add(e.Message); });

            return errors;
        }
    }
}