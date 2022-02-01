using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Common.Extensions;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Utilities.Common.Helpers;
using CMI.Utilities.Logging.Configurator;
using CMI.Utilities.Template;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.api.Data;
using CMI.Web.Management.Auth;
using CMI.Web.Management.ParameterSettings;
using MassTransit;
using Serilog;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    [NoCache]
    public class OrderController : ApiManagementControllerBase
    {
        private readonly IRequestClient<FindArchiveRecordRequest> findArchiveRecordClient;
        private readonly IMailHelper mailHelper;
        private readonly IBus bus;
        private readonly IPublicOrder orderManagerClient;
        private readonly IParameterHelper parameterHelper;


        public OrderController(IPublicOrder client,
            IRequestClient<FindArchiveRecordRequest> findArchiveRecordClient,
            IParameterHelper parameterHelper, IMailHelper mailHelper, IBus bus)
        {
            orderManagerClient = client;
            this.findArchiveRecordClient = findArchiveRecordClient;
            this.parameterHelper = parameterHelper;
            this.mailHelper = mailHelper;
            this.bus = bus;
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetAushebungsauftraegeHtml([FromUri] int[] orderItemIds)
        {
            var builder = new DataBuilder(bus);
            var access = ManagementControllerHelper.GetUserAccess();

            access.AssertFeatureOrThrow(ApplicationFeature.AuftragsuebersichtAuftraegeKannAushebungsauftraegeDrucken);

            var expando = builder
                .AddUser(access.UserId)
                .AddAuftraege(orderItemIds)
                .Create();

            var template = parameterHelper.GetSetting<AushebungsauftraegeTemplate>();
            string html = mailHelper.TransformToHtml(template.HtmlTemplate, expando);

            await orderManagerClient.AushebungsauftraegeDrucken(access.UserId, orderItemIds.ToList());

            return Ok(html);
        }

        [HttpGet]
        public IHttpActionResult GetVersandkontrolleHtml([FromUri] int[] orderItemIds)
        {
            try
            {
                var builder = new DataBuilder(bus);
                var access = ManagementControllerHelper.GetUserAccess();
                access.AssertFeatureOrThrow(ApplicationFeature.AuftragsuebersichtAuftraegeVersandkontrolleAusfuehren);
                var expando = builder
                    .AddUser(access.UserId)
                    .AddBestellerMitAuftraegen(orderItemIds)
                    .Create();

                var template = parameterHelper.GetSetting<VersandkontrolleTemplate>();
                string html = mailHelper.TransformToHtml(template.HtmlTemplate, expando);

                return Ok(html);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                Console.WriteLine(e);
                throw;
            }
        }


        [HttpGet]
        public async Task<DigipoolEntry[]> GetDigiPool(int numberOfEntries = int.MaxValue)
        {
            return await orderManagerClient.GetDigipool(numberOfEntries);
        }

        [HttpPost]
        public async Task<IHttpActionResult> UpdateDigiPool([FromBody] DigipoolPostData digipoolPost)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AuftragsuebersichtDigipoolPriorisierungAnpassenAusfuehren);
            if (digipoolPost == null)
            {
                return BadRequest("Keine Werte angegeben");
            }

            if (digipoolPost.OrderItemIds?.Count == 0)
            {
                return BadRequest("Keine OrderItemIds angegeben");
            }

            var terminDigitalisierung = digipoolPost.TerminDigitalisierungDatum.ParseDateTimeSwiss();
            if (terminDigitalisierung != null)
            {
                // Zeit setzen
                var terminDigitalisierungZeit = digipoolPost.TerminDigitalisierungZeit.ParseTimeSwiss();
                if (terminDigitalisierungZeit != null)
                {
                    terminDigitalisierung = terminDigitalisierung.Value.Date + terminDigitalisierungZeit.Value;
                }
            }

            await orderManagerClient.UpdateDigipool(digipoolPost.OrderItemIds, digipoolPost.DigitalisierungsKategorie, terminDigitalisierung);

            return Ok("success");
        }

        [HttpPost]
        public async Task<IHttpActionResult> ResetAufbereitungsfehler([FromBody] ZuruecksetzenParams zuruecksetzenPost)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AuftragsuebersichtDigipoolAufbereitungsfehlerZuruecksetzen);
            if (zuruecksetzenPost == null)
            {
                return BadRequest("Keine Werte angegeben");
            }

            if (zuruecksetzenPost.OrderItemIds?.Count == 0)
            {
                return BadRequest("Keine OrderItemIds angegeben");
            }

            await orderManagerClient.ResetAufbereitungsfehler(zuruecksetzenPost.OrderItemIds);
            return Ok("success");
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetOrderingDetailItem(int id)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtAuftraegeView))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            var item = await GetOrderingDetailItemInternal(id);

            return Ok(item);
        }

        [HttpGet]
        public async Task<IHttpActionResult> NichtSichtbarEinsehen(int id)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            var detail = await GetOrderingDetailItemInternal(id, true);
            if (detail?.Item == null)
            {
                return NotFound();
            }

            // we need the ordering type of the result to check, what feature the user must have
            if (detail.Item.OrderingType == (int) OrderType.Einsichtsgesuch)
            {
                access.AssertFeatureOrThrow(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheViewNichtSichtbar);
            }
            else
            {
                access.AssertFeatureOrThrow(ApplicationFeature.AuftragsuebersichtAuftraegeViewNichtSichtbar);
            }

            return Ok(detail);
        }

        private async Task<OrderingFlatDetailItem> GetOrderingDetailItemInternal(int id, bool nichtSichtbarEinsehen = false)
        {
            var ctx = new ViaducContext(WebHelper.Settings["sqlConnectionString"]);
            var flatItem = ctx.OrderingFlatItem.FirstOrDefault(i => i.ItemId == id);

            if (flatItem == null)
            {
                return null;
            }

            var item = new OrderingFlatDetailItem();
            item.FromFlatItem(flatItem);

            if (flatItem.VeId.HasValue)
            {
                var elasticItem = await GetElasticArchiveRecord(flatItem.VeId.Value.ToString());
                if (elasticItem != null)
                {
                    var ancestors = GetAncestors(elasticItem);
                    item.ArchivplanKontext = ancestors;

                    if (nichtSichtbarEinsehen)
                    {
                        var snapshot = OrderHelper.GetOrderingIndexSnapshot(elasticItem);
                        OrderHelper.ApplySnapshotToDetailItem(snapshot, item.Item);
                    }
                }
                else
                {
                    Log.Warning("elasticRecord nicht gefunden ");
                }

                var orderHistory = (await orderManagerClient.GetOrderingHistoryForVe(flatItem.VeId.Value)).ToList();
                item.OrderingHistory = orderHistory.Take(3);
                item.HasMoreOrderingHistory = orderHistory.Count > 3;
            }

            var statusHistory = await orderManagerClient.GetStatusHistoryForOrderItem(flatItem.ItemId);
            item.StatusHistory = statusHistory;
            return item;
        }


        private List<OrderingFlatItem> GetItemsToCheck(int[] orderItemids)
        {
            var ctx = new ViaducContext(WebHelper.Settings["sqlConnectionString"]);
            return ctx.OrderingFlatItem.Where(i => orderItemids.Contains(i.ItemId)).ToList();
        }

        private bool HasEinsichtsgesuche(IEnumerable<OrderingFlatItem> itemsToCheck)
        {
            return itemsToCheck.Any(i => i.OrderingType == (int) OrderType.Einsichtsgesuch);
        }

        private bool HasAuftraege(IEnumerable<OrderingFlatItem> itemsToCheck)
        {
            return itemsToCheck.Any(i => i.OrderingType != (int) OrderType.Einsichtsgesuch && i.OrderingType != (int) OrderType.Bestellkorb);
        }

        private void AssertUserHasFeatureForOrderingType(int[] orderItemIds,
            ManagementUserAccess access,
            ApplicationFeature? featureWhenHasAuftraege,
            ApplicationFeature? featureWhenHasEinsichtsgesuche)
        {
            var items = GetItemsToCheck(orderItemIds);
            if (featureWhenHasAuftraege.HasValue)
            {
                var hasAuftraege = HasAuftraege(items);
                if (hasAuftraege && !access.HasFeature(featureWhenHasAuftraege.Value))
                {
                    throw new ForbiddenException(
                        $"Sie benötigen das Feature {featureWhenHasAuftraege.Value.ToString()}, um diese Aktion auszuführen");
                }
            }

            if (featureWhenHasEinsichtsgesuche.HasValue)
            {
                var hasEinsichtsgesuche = HasEinsichtsgesuche(items);
                if (hasEinsichtsgesuche && !access.HasFeature(featureWhenHasEinsichtsgesuche.Value))
                {
                    throw new ForbiddenException(
                        $"Sie benötigen das Feature {featureWhenHasEinsichtsgesuche.Value.ToString()}, um diese Aktion auszuführen");
                }
            }
        }

        public List<TreeRecord> GetAncestors(ElasticArchiveRecord record)
        {
            var ancestors = new List<TreeRecord>();
            var items = record.ArchiveplanContext;

            if (items == null)
            {
                return ancestors;
            }

            foreach (var contextItem in items)
            {
                var item = new TreeRecord
                {
                    ArchiveRecordId = contextItem.ArchiveRecordId,
                    Title = contextItem.Title,
                    ReferenceCode = contextItem.RefCode
                };

                ancestors.Add(item);
            }

            return ancestors;
        }

        private async Task<ElasticArchiveRecord> GetElasticArchiveRecord(string archiveRecordId)
        {
            var result = await findArchiveRecordClient.GetResponse<FindArchiveRecordResponse>(new FindArchiveRecordRequest {ArchiveRecordId = archiveRecordId});
            return result.Message.ElasticArchiveRecord;
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetOrderingHistoryForVe(int id)
        {
            var orderHistory = (await orderManagerClient.GetOrderingHistoryForVe(id)).ToList();
            return Ok(orderHistory);
        }

        [HttpGet]
        public IHttpActionResult GetAuftragOrderingDetailFields()
        {
            var detailFields = GetOrderingFieldsInternal(ModulFunktionsblock.AuftragsuebersichAuftraege);
            return Ok(detailFields);
        }

        [HttpGet]
        public IHttpActionResult GetEinsichtsgesuchOrderingDetailFields()
        {
            var detailFields = GetOrderingFieldsInternal(ModulFunktionsblock.AuftragsuebersichEinsichtsgesuch);
            return Ok(detailFields);
        }

        private List<DetailField> GetOrderingFieldsInternal(ModulFunktionsblock modulFunktionsblock)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            var retFields = new List<DetailField>();

            var fields = typeof(OrderingFlatItem).GetProperties();
            foreach (var field in fields)
            {
                var isReadonly = true;
                IEditRequiresFeatureAttribute attr;
                switch (modulFunktionsblock)
                {
                    case ModulFunktionsblock.AuftragsuebersichAuftraege:
                        attr = field.GetCustomAttributes(true).OfType<EditAuftragRequiresFeatureAttribute>().FirstOrDefault();
                        break;
                    case ModulFunktionsblock.AuftragsuebersichEinsichtsgesuch:
                        attr = field.GetCustomAttributes(true).OfType<EditEinsichtsgesuchRequiresFeatureAttribute>().FirstOrDefault();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (attr != null)
                {
                    var features = attr.RequiredFeatures;
                    if (features.Any(f => access.HasFeature(f)))
                    {
                        isReadonly = false;
                    }
                }

                var name = field.Name;
                retFields.Add(new DetailField
                {
                    Name = name,
                    IsReadonly = isReadonly
                });
            }

            return retFields;
        }

        [HttpPost]
        public async Task<IHttpActionResult> AuftraegeEntscheidFreigabeHinterlegen([FromBody] EntscheidFreigabeHinterlegenParams p)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtAuftraegeKannEntscheidFreigabeHinterlegen))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            await orderManagerClient.EntscheidFreigabeHinterlegen(ControllerHelper.GetCurrentUserId(), p.OrderItemIds, p.Entscheid,
                p.DatumBewilligung, p.InterneBemerkung);
            return Content<object>(HttpStatusCode.NoContent, null);
        }

        [HttpPost]
        public async Task<IHttpActionResult> EinsichtsgesucheEntscheidGesuchHinterlegen([FromBody] EntscheidGesuchHinterlegenParams p)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheKannEntscheidGesuchHinterlegen))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            await orderManagerClient.EntscheidGesuchHinterlegen(ControllerHelper.GetCurrentUserId(), p.OrderItemIds, p.Entscheid, p.DatumEntscheid,
                p.InterneBemerkung);
            return Content<object>(HttpStatusCode.NoContent, null);
        }

        [HttpPost]
        public async Task<IHttpActionResult> AuftraegeAushebungsauftraegeDrucken([FromBody] AushebungsauftraegeDruckenParams p)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtAuftraegeKannAushebungsauftraegeDrucken))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            await orderManagerClient.AushebungsauftraegeDrucken(ControllerHelper.GetCurrentUserId(), p.OrderItemIds);
            return Content<object>(HttpStatusCode.NoContent, null);
        }

        [HttpPost]
        public async Task<IHttpActionResult> EinsichtsgesucheInVorlageExportieren([FromBody] InVorlageExportierenParams p)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheInVorlageExportieren))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            await orderManagerClient.InVorlageExportieren(ControllerHelper.GetCurrentUserId(), p.OrderItemIds, p.Vorlage, p.Sprache);
            return Content<object>(HttpStatusCode.NoContent, null);
        }

        [HttpPost]
        public async Task<IHttpActionResult> AuftraegeAbschliessen([FromBody] AbschliessenParams p)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtAuftraegeKannAbschliessen))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            await orderManagerClient.Abschliessen(ControllerHelper.GetCurrentUserId(), p.OrderItemIds);
            return Content<object>(HttpStatusCode.NoContent, null);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Zuruecksetzen([FromBody] ZuruecksetzenParams p)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            AssertUserHasFeatureForOrderingType(p.OrderItemIds.ToArray(), access,
                ApplicationFeature.AuftragsuebersichtAuftraegeKannZuruecksetzen,
                ApplicationFeature.AuftragsuebersichtEinsichtsgesucheKannZuruecksetzen);

            await orderManagerClient.Zuruecksetzen(ControllerHelper.GetCurrentUserId(), p.OrderItemIds);
            return Content<object>(HttpStatusCode.NoContent, null);
        }

        [HttpPost]
        public async Task<IHttpActionResult> ZumReponierenBereit([FromBody] AbbrechenParams p)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            AssertUserHasFeatureForOrderingType(p.OrderItemIds.ToArray(), access, ApplicationFeature.AuftragsuebersichtAuftraegeKannReponieren, null);

            await orderManagerClient.ZumReponierenBereit(ControllerHelper.GetCurrentUserId(), p.OrderItemIds);

            return Content<object>(HttpStatusCode.NoContent, null);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Abbrechen([FromBody] AbbrechenParams p)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            AssertUserHasFeatureForOrderingType(p.OrderItemIds.ToArray(), access,
                ApplicationFeature.AuftragsuebersichtAuftraegeKannAbbrechen,
                ApplicationFeature.AuftragsuebersichtEinsichtsgesucheKannAbbrechen);

            await orderManagerClient.Abbrechen(ControllerHelper.GetCurrentUserId(), p.OrderItemIds, p.Abbruchgrund, p.BemerkungZumDossier,
                p.InterneBemerkung);
            return Content<object>(HttpStatusCode.NoContent, null);
        }


        [HttpPost]
        public async Task<IHttpActionResult> AuftraegeAusleihen([FromBody] AuftraegeAusleihenParams p)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtAuftraegeKannAuftraegeAusleihen))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            await orderManagerClient.AuftraegeAusleihen(ControllerHelper.GetCurrentUserId(), p.OrderItemIds);
            return Content<object>(HttpStatusCode.NoContent, null);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DigitalisierungAusloesen([FromBody] DigitalisierungAusloesenParams p)
        {
            var access = ManagementControllerHelper.GetUserAccess();

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheDigitalisierungAusloesenAusfuehren))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            var items = GetItemsToCheck(p.OrderItemIds.ToArray());
            if (!HasEinsichtsgesuche(items))
            {
                return BadRequest("Funktion ist nur für Einsichtsgesuche gestattet");
            }

            var snapshots = new List<OrderingIndexSnapshot>();
            foreach (var item in items)
            {
                if (!item.VeId.HasValue)
                {
                    return BadRequest("Formularbestellungen sind in dieser Funktion nicht zulässig");
                }

                var elasticItem = await GetElasticArchiveRecord(item.VeId.Value.ToString());
                if (elasticItem == null)
                {
                    throw new Exception($"Ve with ID {item.VeId.Value} not found");
                }

                var snapshot = OrderHelper.GetOrderingIndexSnapshot(elasticItem);
                snapshots.Add(snapshot);
            }

            var statusUebergangSettings = parameterHelper.GetSetting<StatusUebergangSettings>();
            await orderManagerClient.DigitalisierungAusloesen(access.UserId, snapshots.ToArray(),
                statusUebergangSettings.ArtDerArbeitIdFuerDigitalisierungAusloesen);
            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]
        public async Task<IHttpActionResult> AuftragUpdateOrderItem([FromBody] OrderingFlatItem flatItem)
        {
            return await UpdateOrderItem(flatItem, ModulFunktionsblock.AuftragsuebersichAuftraege);
        }

        [HttpPost]
        public async Task<IHttpActionResult> EinsichtsgesuchUpdateOrderItem([FromBody] OrderingFlatItem flatItem)
        {
            return await UpdateOrderItem(flatItem, ModulFunktionsblock.AuftragsuebersichEinsichtsgesuch);
        }

        [HttpPost]
        public async Task<IHttpActionResult> ErinnerungVersenden([FromBody] ErinnerungVersendenPostData erinnerungVersendenPost)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AuftragsuebersichtAuftraegeErinnerungVersenden);
            if (erinnerungVersendenPost == null)
            {
                return BadRequest("Keine Werte angegeben");
            }

            if (erinnerungVersendenPost.OrderItemIds?.Count == 0)
            {
                return BadRequest("Keine OrderItemIds angegeben");
            }

            if (!CheckNurErinnerbareAuftraegeEnthalten(erinnerungVersendenPost.OrderItemIds))
            {
                return BadRequest(
                    "Es dürfen nur «Lesesaalausleihen» mit dem internen Status «Ausgeliehen» übergeben werden.");
            }
            var result = await orderManagerClient.ErinnerungVersenden(erinnerungVersendenPost.OrderItemIds, access.UserId);

            if (result.Success)
            {
                return Ok("success");
            }

            return BadRequest("");
        }

        [HttpPost]
        public async Task<IHttpActionResult> MahnungVersenden([FromBody] MahnungVersendenPostData mahnungVersendenPost)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AuftragsuebersichtAuftraegeMahnungVersenden);
            if (mahnungVersendenPost == null)
            {
                return BadRequest("Keine Werte angegeben");
            }

            if (mahnungVersendenPost.OrderItemIds?.Count == 0)
            {
                return BadRequest("Keine OrderItemIds angegeben");
            }

            if (!CheckNurMahnbareAuftraegeEnthalten(mahnungVersendenPost.OrderItemIds))
            {
                return BadRequest(
                    "Es dürfen nur «Verwaltungsausleihen» oder «Lesesaalausleihen» mit dem internen Status «Ausgeliehen» übergeben werden.");
            }

            var result = await orderManagerClient.MahnungVersenden(mahnungVersendenPost.OrderItemIds, mahnungVersendenPost.Language,
                mahnungVersendenPost.GewaehlteMahnungAnzahl, access.UserId);

            if (result.Success)
            {
                return Ok("success");
            }

            return BadRequest("");
        }

        private bool CheckNurMahnbareAuftraegeEnthalten(List<int> orderItemIds)
        {
            var ctx = new ViaducContext(WebHelper.Settings["sqlConnectionString"]);
            var items = ctx.OrderingFlatItem.Where(i => orderItemIds.Contains(i.ItemId) &&
                                                        (i.Status != (int) OrderStatesInternal.Ausgeliehen ||
                                                         !(i.OrderingType == (int) OrderType.Lesesaalausleihen ||
                                                           i.OrderingType == (int) OrderType.Verwaltungsausleihe))).ToList();
            return !items.Any();
        }

        private bool CheckNurErinnerbareAuftraegeEnthalten(List<int> orderItemIds)
        {
            var ctx = new ViaducContext(WebHelper.Settings["sqlConnectionString"]);
            var items = ctx.OrderingFlatItem.Where(i => orderItemIds.Contains(i.ItemId) &&
                                                        (i.Status != (int)OrderStatesInternal.Ausgeliehen ||
                                                         i.OrderingType != (int)OrderType.Lesesaalausleihen)).ToList();
            return !items.Any();
        }

        private async Task<IHttpActionResult> UpdateOrderItem([FromBody] OrderingFlatItem flatItem, ModulFunktionsblock modulFunktionsblock)
        {
            var existingOrdering = await orderManagerClient.GetOrdering(flatItem.OrderId);
            if (existingOrdering == null)
            {
                return BadRequest("Ordering does not exist in DB");
            }

            var existingItem = existingOrdering.Items.FirstOrDefault(i => i.Id == flatItem.ItemId);
            if (existingItem == null)
            {
                return BadRequest("OrderItem does not exist in DB");
            }

            var updateOrderingData = new UpdateOrderingData();
            var updateOrderItemData = new UpdateOrderItemData();

            // map flatitem back to to ordering & orderitem
            foreach (var field in GetOrderingFieldsInternal(modulFunktionsblock))
            {
                var flatItemProperty = typeof(OrderingFlatItem).GetProperty(field.Name);
                if (flatItemProperty == null)
                {
                    throw new ArgumentOutOfRangeException(field.Name);
                }

                var newVal = flatItemProperty.GetValue(flatItem);
                var originAttribute = flatItemProperty.GetCustomAttributes(true).OfType<OriginAttribute>().FirstOrDefault();

                if (string.IsNullOrWhiteSpace(originAttribute?.Column) || string.IsNullOrWhiteSpace(originAttribute.Table))
                {
                    continue; // field is not updateable, because it is missing the origin attribute
                }

                switch (originAttribute.Table)
                {
                    case nameof(Ordering):
                        EnsureUpdateIsGranted(newVal, existingOrdering, originAttribute.Column, field.IsReadonly);
                        Reflective.SetValue(updateOrderingData, originAttribute.Column, newVal);
                        break;
                    case nameof(OrderItem):
                        EnsureUpdateIsGranted(newVal, existingItem, originAttribute.Column, field.IsReadonly);
                        Reflective.SetValue(updateOrderItemData, originAttribute.Column, newVal);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(originAttribute.Table), $"Unknown table: {originAttribute.Table}");
                }
            }

            if (flatItem.Status == (int)OrderStatesInternal.Ausgeliehen && !existingItem.Ausleihdauer.Equals(flatItem.Ausleihdauer))
            {
                var user = ControllerHelper.UserDataAccess.GetUser(ControllerHelper.GetCurrentUserId());
                updateOrderItemData.InternalComment = updateOrderItemData.InternalComment.Prepend(
                    $"Erwartetes Rückgabedatum von {existingItem.Ausgabedatum.Value.AddDays(existingItem.Ausleihdauer):dd.MM.yyyy} " +
                    $"auf {flatItem.Ausgabedatum.Value.AddDays(flatItem.Ausleihdauer):dd.MM.yyyy} angepasst," +
                    $" {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}, {user.FirstName} {user.FamilyName}");
            }

            var data = new UpdateOrderDetailData
            {
                OrderItem = updateOrderItemData,
                Ordering = updateOrderingData
            };

            await orderManagerClient.UpdateOrderDetail(data);
            return StatusCode(HttpStatusCode.NoContent);
        }

        private void EnsureUpdateIsGranted<T>(object newVal, T existingObj, string existingPropertyName, bool isReadonly)
        {
            var oldValue = Reflective.GetValue<object>(existingObj, existingPropertyName);

            // if it's an enum, cast it to ints int representation
            if (oldValue?.GetType().IsEnum ?? false)
            {
                oldValue = (int) Enum.Parse(oldValue.GetType(), oldValue.ToString());
            }

            if (!Equals(newVal, oldValue) && isReadonly)
            {
                throw new ForbiddenException($"Update on field {existingPropertyName} is not allowed, but server received updated values");
            }
        }
    }

    public class DetailField
    {
        public string Name { get; set; }
        public bool IsReadonly { get; set; }
    }
}