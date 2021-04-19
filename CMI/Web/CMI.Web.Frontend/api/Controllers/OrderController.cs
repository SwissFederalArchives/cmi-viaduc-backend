using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Order;
using CMI.Utilities.Common.Helpers;
using CMI.Utilities.Logging.Configurator;
using CMI.Utilities.ProxyClients.Order;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Dto;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using CMI.Web.Frontend.App_Start;
using CMI.Web.Frontend.Helpers;
using CMI.Web.Frontend.ParameterSettings;
using Ninject;

namespace CMI.Web.Frontend.api.Controllers
{
    [Authorize]
    public class OrderController : OrderControllerBase
    {
        private readonly OrderManagerClient client;
        private readonly DigitalisierungsbeschraenkungSettings digitalisierungsbeschraenkungSettings;
        private readonly IElasticService elasticService;
        private readonly IEntityProvider entityProvider;
        private readonly IKontrollstellenInformer kontrollstellenInformer;
        private readonly IUserDataAccess userDataAccess;

        public OrderController(OrderManagerClient client,
            IEntityProvider entityProvider,
            IKontrollstellenInformer kontrollstellenInformer,
            IElasticService elasticService,
            DigitalisierungsbeschraenkungSettings digitalisierungsbeschraenkungSettings,
            IUserDataAccess userDataAccess)
        {
            this.client = client;
            this.entityProvider = entityProvider;
            this.kontrollstellenInformer = kontrollstellenInformer;
            this.elasticService = elasticService;
            this.digitalisierungsbeschraenkungSettings = digitalisierungsbeschraenkungSettings;
            this.userDataAccess = userDataAccess;
        }

        [HttpPost]
        public async Task<IHttpActionResult> AddToBasket(int veId)
        {
            if (veId <= 0)
            {
                return BadRequest($"{nameof(veId)} must be greater than 0");
            }

            var userId = ControllerHelper.GetCurrentUserId();
            // Check VE is exists already in basket
            if (!await client.IsUniqueVeInBasket(veId, userId))
            {
                return Content(HttpStatusCode.OK, new OrderItemDto {VeId = veId});
            }

            var access = GetUserAccess(WebHelper.GetClientLanguage(Request));
            var entityResult = elasticService.QueryForId<ElasticArchiveRecord>(veId, access);
            var entity = entityResult.Response?.Hits?.FirstOrDefault()?.Source;

            if (entity == null)
            {
                return Content(HttpStatusCode.NotFound, $"No Ve found with id {veId}");
            }

            if (!entity.CanBeOrdered ||
                !(entity.Level == "Dossier" || entity.Level == "Subdossier" || entity.Level == "Dokument"))
            {
                return Content(HttpStatusCode.Conflict, "Ve can not be ordered according to index");
            }

            if (access.RolePublicClient == AccessRoles.RoleOe2 &&
                entity.MetadataAccessTokens.Contains($"EB_{access.UserId}"))
            {
                return Content(HttpStatusCode.Conflict, "This Ve cannot be ordered as Ö2 user. Ö3 role is required.");
            }

            var indexSnapShot = OrderHelper.GetOrderingIndexSnapshot(entity);

            var orderItemDb = await client.AddToBasket(indexSnapShot, userId);
            return Content(HttpStatusCode.Created, ConvertDbItemToDto(orderItemDb, OrderType.Bestellkorb, true));
        }

        [HttpPost]
        public async Task<IHttpActionResult> AddUnknowToBasket(string signatur)
        {
            if (string.IsNullOrEmpty(signatur))
            {
                return BadRequest($"{nameof(signatur)}");
            }

            var searchResult =
                entityProvider.SearchByReferenceCodeWithoutSecurity<ElasticArchiveRecord>(signatur);
            var searchRecordResult = searchResult as SearchResult<ElasticArchiveRecord>;
            ElasticArchiveRecord entity = null;
            if (searchRecordResult?.Entities != null)
            {
                entity = searchRecordResult.Entities.Items[0].Data;
            }

            if (entity == null)
            {
                return Content(HttpStatusCode.NotFound, $"No VE found by signatur {signatur}");
            }

            if (!entity.CanBeOrdered ||
                !(entity.Level == "Dossier" || entity.Level == "Subdossier" || entity.Level == "Dokument"))
            {
                return Content(HttpStatusCode.Conflict, "Ve can not be ordered according to index");
            }

            var userId = ControllerHelper.GetCurrentUserId();
            // Check VE is exists already in basket
            if (int.TryParse(entity.ArchiveRecordId, out var veId) && !await client.IsUniqueVeInBasket(veId, userId))
            {
                return Content(HttpStatusCode.OK, new OrderItemDto {VeId = veId});
            }

            var settings = FrontendSettingsViaduc.Instance;
            var unknowText = settings.GetTranslation(WebHelper.GetClientLanguage(Request), "order.unknowtext",
                "[Nicht sichtbar]");
            var indexSnapShot = OrderHelper.GetOrderingIndexSnapshot(entity, unknowText);

            var orderItemDb = await client.AddToBasket(indexSnapShot, ControllerHelper.GetCurrentUserId());
            return Content(HttpStatusCode.Created, ConvertDbItemToDto(orderItemDb, OrderType.Bestellkorb, true));
        }

        [HttpPost]
        public async Task<IHttpActionResult> AddToBasket([FromBody] FormularBestellungParams param)
        {
            if (param == null)
            {
                return BadRequest($"{nameof(param)} must not be null");
            }

            // Special logic for period
            var zeitraum = FormatZeitraumAccordingToSipSpecification(Trim(param.Period));

            var orderItemDb = await client.AddToBasketCustom(Trim(param.Bestand), Trim(param.Ablieferung),
                Trim(param.BehaeltnisNr), Trim(param.ArchivNr), Trim(param.Aktenzeichen), Trim(param.Title), zeitraum,
                ControllerHelper.GetCurrentUserId());
            return Content(HttpStatusCode.Created, ConvertDbItemToDto(orderItemDb, OrderType.Bestellkorb, true));
        }

        [HttpPost]
        public async Task RemoveFromBasket(int orderItemId)
        {
            await client.RemoveFromBasket(orderItemId, ControllerHelper.GetCurrentUserId());
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetKontingent(string forUserId = "")
        {
            var access = GetCurrentUserAccess();
            var role = access.RolePublicClient.GetRolePublicClientEnum();

            User user;
            if (!string.IsNullOrWhiteSpace(forUserId) && role == AccessRolesEnum.BAR)
                // BAR User bestellt im Namen eines anderen Benutzer, Kontingent für Empfänger zurückgeben
            {
                user = userDataAccess.GetUser(forUserId);
            }
            else
            {
                user = userDataAccess.GetUser(access.UserId);
            }

            if (user == null)
            {
                return Content(HttpStatusCode.NotFound, $"User with id {forUserId} not found");
            }

            var bestimmer = new KontingentBestimmer(digitalisierungsbeschraenkungSettings);
            var userOrderings = await client.GetOrderings(user.Id);
            var result = bestimmer.BestimmeKontingent(userOrderings, role, user);

            return Ok(result);
        }

        [HttpPost]
        public async Task UpdateComment(int orderItemId, string comment)
        {
            await client.UpdateComment(orderItemId, comment, ControllerHelper.GetCurrentUserId());
        }

        [HttpPost]
        public async Task<IHttpActionResult> UpdateBewilligungsDatum(int orderItemId, string bewilligung)
        {
            if (string.IsNullOrWhiteSpace(bewilligung))
            {
                return BadRequest($"missing parameter '{nameof(bewilligung)}'!");
            }

            var bewilligungsDatum = bewilligung.ParseDateTimeSwiss();
            await client.UpdateBewilligungsDatum(orderItemId, bewilligungsDatum, ControllerHelper.GetCurrentUserId());
            return
                Ok(bewilligungsDatum); // Hack: Angularbug, HttpClient verträgt aktuell keine leeren OK Responses, https://github.com/angular/angular/issues/18680            
        }

        [HttpPost]
        public async Task<IHttpActionResult> UpdateReason(int orderItemId, int? reason, bool hasPersonendaten)
        {
            if (hasPersonendaten && (!reason.HasValue || reason.Value == 0))
            {
                return BadRequest("orderitem hat Personendaten, aber es wurde kein Grund angegeben");
            }

            await client.UpdateReason(orderItemId, reason, hasPersonendaten, ControllerHelper.GetCurrentUserId());

            return Content<object>(HttpStatusCode.NoContent, null);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Order([FromBody] OrderParams orderParams)
        {
            // Diese Prüfung soll immer für den aktuellen Benutzer (also nicht für den Besteller) ablaufen gemäss Telefonat mit Marlies Hertig 
            var basket = await GetBasket();
            if (basket == null || basket.Count(i => !i.EinsichtsbewilligungNotwendig) == 0)
            {
                throw new BadRequestException("Order not is allowed, because there are no items in basket");
            }

            var userAccess = GetUserAccess();

            var bestellerId = !string.IsNullOrWhiteSpace(orderParams.UserId)
                ? orderParams.UserId
                : ControllerHelper.GetCurrentUserId();
            var bestellungIstFuerAndererBenutzer = ControllerHelper.GetCurrentUserId() != bestellerId;
            if (bestellungIstFuerAndererBenutzer)
            {
                if (GetCurrentUserAccess().RolePublicClient != AccessRoles.RoleBAR)
                {
                    throw new BadRequestException(
                        "Es wird versucht, für einen anderen Benutzer als den aktuellen Benutzer zu bestellen. Dazu fehlt aber die Berechtigung.");
                }

                orderParams.Comment = string.Join(" ", orderParams.Comment,
                    FrontendSettingsViaduc.Instance.GetTranslation(WebHelper.GetClientLanguage(Request),
                        "order.frombar", "(Diese Bestellung wurde durch das Bundesarchiv ausgelöst.)"));
            }

            var orderItemIdsToExclude = basket
                .Where(basketItem => basketItem.EinsichtsbewilligungNotwendig
                                     || orderParams.Type == OrderType.Digitalisierungsauftrag &&
                                     orderParams.OrderIdsToExclude != null && orderParams.OrderIdsToExclude.Contains(basketItem.Id))
                .Select(item => item.Id)
                .ToList();

            DateTime? leseSaalDateAsDateTime = null;
            switch (orderParams.Type)
            {
                case OrderType.Verwaltungsausleihe:
                    ValidateVerwaltungsausleiheBestellung(userAccess);
                    break;
                case OrderType.Digitalisierungsauftrag:
                    await ValidateDigitalisierungsauftragBestellung(bestellerId, bestellungIstFuerAndererBenutzer,
                        userAccess, basket, orderItemIdsToExclude);
                    break;
                case OrderType.Lesesaalausleihen:
                    leseSaalDateAsDateTime = orderParams.LesesaalDate.ParseDateTimeSwiss();
                    ValidateLesesaalBestellung(leseSaalDateAsDateTime);
                    break;
                default:
                    throw new BadRequestException(
                        $"Bestelltyp {orderParams.Type.ToString()} ist hier nicht unterstützt.");
            }

            if (userAccess.RolePublicClient == AccessRoles.RoleAS)
            {
                var settings = NinjectWebCommon.Kernel.Get<VerwaltungsausleiheSettings>();
                orderParams.ArtDerArbeit = (int) settings.ArtDerArbeitFuerAmtsBestellung;
            }

            var creationRequest = new OrderCreationRequest
            {
                OrderItemIdsToExclude = orderItemIdsToExclude,
                Type = orderParams.Type,
                Comment = orderParams.Comment,
                ArtDerArbeit = orderParams.ArtDerArbeit,
                LesesaalDate = leseSaalDateAsDateTime,
                CurrentUserId = ControllerHelper.GetCurrentUserId(),
                BestellerId = bestellerId
            };

            var veInfoList = basket.Where(item => item.VeId.HasValue && !orderItemIdsToExclude.Contains(item.Id))
                .Select(item => new VeInfo((int) item.VeId, item.Reason)).ToList();

            await kontrollstellenInformer.InformIfNecessary(userAccess, veInfoList);
            await client.CreateOrderFromBasket(creationRequest);

            return Content<object>(HttpStatusCode.NoContent, null);
        }

        private static void ValidateVerwaltungsausleiheBestellung(UserAccess userAccess)
        {
            if (userAccess.RolePublicClient != AccessRoles.RoleBVW &&
                userAccess.RolePublicClient != AccessRoles.RoleAS)
            {
                throw new BadRequestException("Verwaltungsausleihen sind nur möglich für AS oder BVW Benutzer");
            }
        }

        private static void ValidateLesesaalBestellung(DateTime? leseSaalDateAsDateTime)
        {
            var settings = NinjectWebCommon.Kernel.Get<ManagementClientSettings>();
            var valid = leseSaalDateAsDateTime.HasValue &&
                        (settings.OpeningDaysLesesaal?.Split(new[] {";"}, StringSplitOptions.RemoveEmptyEntries)
                            .Contains(leseSaalDateAsDateTime.Value.ToString("dd.MM.yyyy")) ?? false);

            if (!valid)
            {
                throw new BadRequestException(
                    $"The date {leseSaalDateAsDateTime} is not valid, because it is not specified in the opening days");
            }
        }

        private async Task ValidateDigitalisierungsauftragBestellung(string bestellerId,
            bool bestellungIstFuerAndererBenutzer,
            UserAccess userAccess, OrderItemDto[] basket, List<int> orderItemIdsToExclude)
        {
            var kontingentBestimmer = new KontingentBestimmer(digitalisierungsbeschraenkungSettings);
            var orderings = await client.GetOrderings(bestellerId);
            var role = bestellungIstFuerAndererBenutzer
                ? AccessRolesEnum.BAR
                : userAccess.RolePublicClient.GetRolePublicClientEnum();

            var user = userDataAccess.GetUser(bestellerId);

            var result = kontingentBestimmer.BestimmeKontingent(orderings, role, user);

            // User should usually not see these validation-messages, as it is already checked on client (so no translation here)
            if (result.Bestellkontingent <= 0)
            {
                throw new BadRequestException(
                    $"Das Bestellkontingent ist überschritten. Sie haben bereits {result.AktiveDigitalisierungsauftraege} aktive Aufträge.");
            }

            var anzahlgewaehlteAuftraege = basket.Select(i => i.Id).Except(orderItemIdsToExclude).Count();
            if (anzahlgewaehlteAuftraege == 0)
            {
                throw new BadRequestException("Es wurden keine Aufträge für die Bestellung ausgewählt.");
            }

            if (result.Bestellkontingent < anzahlgewaehlteAuftraege)
            {
                throw new BadRequestException(
                    $"Das Bestellkontingent wird überschritten. Sie haben {anzahlgewaehlteAuftraege} Aufträge ausgewählt, dürfen aber nur {result.Bestellkontingent} Aufträge bestellen.");
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> OrderEinsichtsgesuch(
            [FromBody] OrderEinsichtsgesuchParams orderEinsichtsgesuchParams)
        {
            if (orderEinsichtsgesuchParams == null)
            {
                throw new BadRequestException("Missing request-parameter");
            }

            if (orderEinsichtsgesuchParams.Type != OrderType.Einsichtsgesuch)
            {
                throw new BadRequestException(
                    $"Order type must be 'Einsichtsgesuch' but was '{orderEinsichtsgesuchParams.Type}'");
            }

            var basket = await GetBasket();

            if (basket == null || basket.Count(i => i.EinsichtsbewilligungNotwendig) == 0)
            {
                throw new BadRequestException("Order not is allowed, because there are no items in basket");
            }

            var itemsToExclude = basket
                .Where(item => !item.EinsichtsbewilligungNotwendig)
                .Select(item => item.Id)
                .ToList();

            var userAccess = GetUserAccess();
            if (userAccess.RolePublicClient == AccessRoles.RoleAS)
            {
                var settings = NinjectWebCommon.Kernel.Get<VerwaltungsausleiheSettings>();
                orderEinsichtsgesuchParams.ArtDerArbeit = (int) settings.ArtDerArbeitFuerAmtsBestellung;
            }

            var creationRequest = new OrderCreationRequest
            {
                OrderItemIdsToExclude = itemsToExclude,
                Type = orderEinsichtsgesuchParams.Type,
                Comment = orderEinsichtsgesuchParams.Comment,
                ArtDerArbeit = orderEinsichtsgesuchParams.ArtDerArbeit,
                LesesaalDate = DateTime.MinValue,
                BegruendungEinsichtsgesuch = orderEinsichtsgesuchParams.BegruendungEinsichtsgesuch,
                CurrentUserId = ControllerHelper.GetCurrentUserId(),
                BestellerId = ControllerHelper.GetCurrentUserId(),
                PersonenbezogeneNachforschung = orderEinsichtsgesuchParams.PersonenbezogeneNachforschung,
                HasEigenePersonendaten = orderEinsichtsgesuchParams.HasEigenePersonendaten
            };

            await client.CreateOrderFromBasket(creationRequest);

            return Content<object>(HttpStatusCode.NoContent, null);
        }

        [HttpGet]
        public async Task<OrderItemDto[]> GetBasket()
        {
            var orderItemsDb = (await client.GetBasket(ControllerHelper.GetCurrentUserId())).ToList();

            return ConvertDbItemsToDto(orderItemsDb, OrderType.Bestellkorb, true);
        }

        [HttpGet]
        public async Task<OrderingDto[]> GetOrderings()
        {
            var orderingsDb = (await client.GetOrderings(ControllerHelper.GetCurrentUserId())).ToList();
            var orderings = new List<OrderingDto>();
            foreach (var ordering in orderingsDb)
            {
                var newOrdering = new OrderingDto
                {
                    Id = ordering.Id,
                    ArtDerArbeit = ordering.ArtDerArbeit,
                    Type = ordering.Type,
                    Comment = ordering.Comment,
                    OrderDate = ordering.OrderDate,
                    LesesaalDate = ordering.LesesaalDate,
                    UserId = ordering.UserId,
                    BegruendungEinsichtsgesuch = ordering.BegruendungEinsichtsgesuch,
                    PersonenbezogeneNachforschung = ordering.PersonenbezogeneNachforschung,
                    HasEigenePersonendaten = ordering.HasEigenePersonendaten,
                    RolePublicClient = ordering.RolePublicClient,
                    Items = ConvertDbItemsToDto(ordering.Items.ToList(), ordering.Type, false)
                };
                orderings.Add(newOrdering);
            }

            return orderings.OrderByDescending(o => o.OrderDate).ToArray();
        }

        private OrderItemDto ConvertDbItemToDto(OrderItem orderItemDb, OrderType orderType, bool needsSecurityInfo)
        {
            return ConvertDbItemsToDto(new List<OrderItem> {orderItemDb}, orderType, needsSecurityInfo)
                .FirstOrDefault();
        }

        /// <summary>
        ///     Converts the database order items to an OrderItemDto array.
        ///     The OrderItemDto objects are returned to the calling UI.
        ///     For performance reasons security info is loaded when needed.
        /// </summary>
        /// <param name="orderItemsDb">The order items as fetched from the database.</param>
        /// <param name="orderType">Type of the order.</param>
        /// <param name="needsSecurityInfo">
        ///     if set to <c>true</c> the properties <c>EinsichtsbewilligungNotwendig</c> and <c>CouldNeedAReason</c> are
        ///     correctly filled using information from the elastic index.
        /// </param>
        /// <returns>OrderItemDto[].</returns>
        private OrderItemDto[] ConvertDbItemsToDto(List<OrderItem> orderItemsDb, OrderType orderType,
            bool needsSecurityInfo)
        {
            var orderItemsRet = new List<OrderItemDto>();

            foreach (var itemDb in orderItemsDb)
            {
                orderItemsRet.Add(new OrderItemDto
                {
                    ReferenceCode = itemDb.Signatur,
                    Title = itemDb.Dossiertitel,
                    Period = itemDb.ZeitraumDossier,
                    VeId = itemDb.VeId,
                    Id = itemDb.Id,
                    Comment = itemDb.Comment,
                    BewilligungsDatum = itemDb.BewilligungsDatum,
                    HasPersonendaten = itemDb.HasPersonendaten,
                    Hierarchiestufe = itemDb.Hierarchiestufe,
                    ZusaetzlicheInformationen = itemDb.ZusaetzlicheInformationen,
                    Darin = itemDb.Darin,
                    Signatur = itemDb.Signatur,
                    ZustaendigeStelle = itemDb.ZustaendigeStelle,
                    Publikationsrechte = itemDb.Publikationsrechte,
                    Behaeltnistyp = itemDb.Behaeltnistyp,
                    IdentifikationDigitalesMagazin = itemDb.IdentifikationDigitalesMagazin,
                    ZugaenglichkeitGemaessBga = itemDb.ZugaenglichkeitGemaessBga,
                    Standort = itemDb.ZugaenglichkeitGemaessBga,
                    Schutzfristverzeichnung = itemDb.Schutzfristverzeichnung,
                    ExternalStatus = OrderStatusTranslator.GetExternalStatus(orderType, itemDb.Status),
                    Reason = itemDb.Reason,
                    Aktenzeichen = itemDb.Aktenzeichen,
                    DigitalisierungsKategorie = itemDb.DigitalisierungsKategorie,
                    TerminDigitalisierung = itemDb.TerminDigitalisierung,
                    EntscheidGesuch = itemDb.EntscheidGesuch,
                    DatumDesEntscheids = itemDb.DatumDesEntscheids,
                    Abbruchgrund = itemDb.Abbruchgrund
                });
            }

            // Fügt sicherheitsrelevante Informationen zum order item hinzu
            if (needsSecurityInfo)
            {
                var veIdList = orderItemsDb.Where(item => item.VeId != null).Select(item => (int) item.VeId).ToList();

                UserAccess access = null;
                List<Entity<ElasticArchiveRecord>> orderItemsElastic = null;

                if (veIdList.Count > 0)
                {
                    access = GetUserAccess(WebHelper.GetClientLanguage(Request));
                    // Need to call without security, as the list of manual added basket items could contain items that are not visible to the user
                    // but depending on the data we need to fill the properties EinsichtsbewilligungNotwendig and CouldNeedAReason
                    orderItemsElastic = elasticService.QueryForIdsWithoutSecurityFilter<ElasticArchiveRecord>(veIdList,
                        new Paging {Take = ElasticService.ELASTIC_SEARCH_HIT_LIMIT, Skip = 0}).Entries;
                }

                if (orderItemsElastic != null)
                {
                    foreach (var itemDto in orderItemsRet.Where(i => i.VeId.HasValue))
                    {
                        var elasticData = orderItemsElastic
                            .FirstOrDefault(item => itemDto.VeId.ToString() == item.Data.ArchiveRecordId)?.Data;
                        if (elasticData != null)
                        {
                            itemDto.EinsichtsbewilligungNotwendig = IsEinsichtsbewilligungNotwendig(elasticData, access,
                                itemDto.BewilligungsDatum.HasValue);
                            itemDto.CouldNeedAReason = CouldNeedAReason(elasticData, access);
                        }
                    }
                }
            }

            return orderItemsRet.ToArray();
        }

        private string Trim(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            return text.Trim();
        }

        private UserAccess GetCurrentUserAccess()
        {
            return GetUserAccess(WebHelper.GetClientLanguage(Request));
        }

        /// <summary>
        ///     Der Zeitraum wird vom Client in einem der folgenden Formate geliefert
        ///     JJJJ
        ///     JJJJ-JJJJ
        ///     TT.MM.JJJJ
        ///     TT.MM.JJJJ-TT.MM.JJJJ
        ///     respektive Variationen davon.
        ///     Wir prüfen nicht mehr, ob diese Formate korrekt sind, sondern behandeln nur die Spezialfälle ab. Dies sind:
        ///     - Nur Angabe Jahr oder Datum
        ///     - Leere Angabe
        /// </summary>
        /// <param name="zeitraum"></param>
        /// <returns></returns>
        private string FormatZeitraumAccordingToSipSpecification(string zeitraum)
        {
            if (string.IsNullOrEmpty(zeitraum))
            {
                return "keine Angabe"; // Wird nicht übersetzt
            }

            // Wird nur das Jahr oder nur ein Datum angegeben, dann muss daraus ein Zeitraum gemacht werden
            // 1950         --> 1950-1950
            // 10.12.2015   --> 10.12.2015-10.12.2015
            if (zeitraum.IndexOf("-", StringComparison.Ordinal) < 0)
            {
                return $"{zeitraum}-{zeitraum}";
            }

            return zeitraum;
        }
    }
}