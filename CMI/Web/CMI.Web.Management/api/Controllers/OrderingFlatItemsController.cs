using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using MassTransit;
using Microsoft.AspNet.OData.Query;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Web.Management.api.Controllers
{
    [NoCache]
    [Authorize]
    public class OrderingFlatItemsController : ODataManagementControllerBase
    {
        private readonly IRequestClient<FindArchiveRecordRequest> findArchiveRecordClient;

        public OrderingFlatItemsController(IRequestClient<FindArchiveRecordRequest> findArchiveRecordClient)
        {
            this.findArchiveRecordClient = findArchiveRecordClient;
        }

        public async Task<IHttpActionResult> Get(ODataQueryOptions<OrderingFlatItem> options)
        {
            var ctx = new ViaducContext(WebHelper.Settings["sqlConnectionString"]);
            var access = ManagementHelper.GetUserAccess();

            // Don't make the method Queryable but handle the settings internally
            // In that way we can apply the query parameters ourselves without
            // the controller method doing the work.
            // see https://learn.microsoft.com/en-us/aspnet/web-api/overview/odata-support-in-aspnet-web-api/supporting-odata-query-options#invoking-query-options-directly
            var settings = new ODataValidationSettings()
            {
                AllowedQueryOptions = AllowedQueryOptions.All, 
                AllowedArithmeticOperators = AllowedArithmeticOperators.All,
                AllowedFunctions = AllowedFunctions.AllFunctions, 
                AllowedLogicalOperators = AllowedLogicalOperators.All, 
                MaxNodeCount = 500
            };

            options.Validate(settings);

            var items = ctx.OrderingFlatItem.AsQueryable()
                .Where(o => o.Status != (int) OrderStatesInternal.ImBestellkorb);

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheView))
            {
                items = items.Where(i => i.OrderingType != (int) OrderType.Einsichtsgesuch);
            }

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtAuftraegeView))
            {
                items = items.Where(i => i.OrderingType != (int) OrderType.Digitalisierungsauftrag
                                         && i.OrderingType != (int) OrderType.Lesesaalausleihen
                                         && i.OrderingType != (int) OrderType.Verwaltungsausleihe);
            }

            // Let OData process the query (filters, pagination, etc.)
            // If this is not done here, the items queryable can contain thousands of records
            // that would be unanonymized before the filters and skip and take are applied.
            // With the ApplyTo all these is done here, so we only need to unanonymize the records that are returned.
            var appliedQuery = options.ApplyTo(items);

            // Convert `SelectSome<OrderingFlatItem>` objects to real `OrderingFlatItem` instances
            var finalResult = ConvertSelectSomeList(appliedQuery);

            Log.Debug("Fetched the following raw results from the database for the query {options} with the result of {result}",
                JsonConvert.SerializeObject(options.RawValues, Formatting.None),
                JsonConvert.SerializeObject(finalResult, Formatting.None));

            // Unanonymize only the returned results
            await UnanonymizeResult(finalResult.Where(t => t.Dossiertitel?.LastIndexOf("█", StringComparison.InvariantCultureIgnoreCase) > 0 ||
                                                           t.Darin?.LastIndexOf("█", StringComparison.InvariantCultureIgnoreCase) > 0 ||
                                                           t.ZusaetzlicheInformationen?.LastIndexOf("█", StringComparison.InvariantCultureIgnoreCase) > 0));

            Log.Debug("Returning the following processed results from the database: {result}", 
                JsonConvert.SerializeObject(finalResult, Formatting.None));

            return Ok(finalResult);
        }

        private List<OrderingFlatItem> ConvertSelectSomeList(IQueryable sourceList)
        {
            var serialized = JsonConvert.SerializeObject(sourceList);
            var list = JsonConvert.DeserializeObject<List<OrderingFlatItem>>(serialized);

            return list;
        }

        private async Task UnanonymizeResult(IEnumerable<OrderingFlatItem> anonymizedRecords)
        {
            var orderingFlatItems = anonymizedRecords.ToList();
            Log.Debug("Found {count} records that need to be anonymized", orderingFlatItems.Count());


            foreach (var anonymizedRecord in orderingFlatItems)
            {
                var result = await findArchiveRecordClient.GetResponse<FindArchiveRecordResponse>(
                    new FindArchiveRecordRequest
                    {
                        ArchiveRecordId = anonymizedRecord.VeId,
                        UseUnanonymizedData = UseUnanonymizedData.Yes
                    });

                if (result.Message.ElasticArchiveRecord != null)
                {
                    Log.Debug("Found elastic record with unanonymized data for record {veId} and the following data: {data}", anonymizedRecord.VeId, JsonConvert.SerializeObject(result.Message.ElasticArchiveRecord));
                    anonymizedRecord.Dossiertitel = result.Message.ElasticArchiveRecord.Title;
                    anonymizedRecord.Darin = result.Message.ElasticArchiveRecord.WithinInfo;
                    anonymizedRecord.ZusaetzlicheInformationen = result.Message.ElasticArchiveRecord.ZusätzlicheInformationen();
                }
                else
                {
                    Log.Debug("Did not find an elastic record with unanonymized data for record {veId}", anonymizedRecord.VeId);
                }
            }
        }
    }

}