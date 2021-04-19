using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common.api;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using NSwag.Annotations;
using Serilog;

namespace CMI.Web.Frontend.api.Controllers
{
    /// <summary>
    ///     Controller for external (public) API.
    ///     Be careful with changes
    ///     a.) breaking changes requires another api version
    ///     b.) don't remove existing versions
    /// </summary>
    [AllowAnonymous]
    [NoCache]
    [Description("Search access to the Swiss Federal Archives.")]
    public class ExternalController : ApiFrontendControllerBase
    {
        private readonly IEntityProvider entityProvider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExternalController" /> class.
        /// </summary>
        /// <param name="entityProvider">The entity provider.</param>
        public ExternalController(IEntityProvider entityProvider)
        {
            this.entityProvider = entityProvider;
        }

        /// <summary>
        ///     Used to fetch one record using its id.
        /// </summary>
        /// <param name="id">Id of the record to retrieve. Must be a positive value larger than 0.</param>
        /// <param name="skip">Defines how many child records should be skipped in the result.</param>
        /// <param name="take">Defines how many child records should be returned. The maximum number is 500.</param>
        /// <returns>Entity&lt;DetailRecord&gt;</returns>
        /// <remarks>
        ///     The result is a object of type Entity&lt;DetailRecord&gt;.
        /// 
        ///     This is a complex type where we want to elaborate on 4 special properties
        ///     + **highlight**: Information for highlighting. Is always null, as we are not searching.
        ///     + **explanation**: For debugging or analysis we could return detailed information
        ///     on how the score is calculated and the result was composed.
        ///     Is always null.
        ///     + **_metadata**: This property returns the metadata of the archive record in a organized way for
        ///     displaying the data on a web form or similar.
        ///     + **_context**: The context returns the ancestors and descendants of the requested archive record.
        ///     There could be possibly thousands of descendants/children to a records. That is why, we allow to pass
        ///     a *skip* and *take* parameter to limit the number of returned child records.
        /// 
        ///     The other properties in the object are directly relevant to the requested archive record.
        ///     ___
        /// 
        ///     ### Example calls (restful):
        ///     ``` http
        ///     GET https://www.recherche.bar.admin.ch/recherche/api/v1/entities/1
        ///     ```
        ///     or
        ///     ``` http
        ///     GET https://www.recherche.bar.admin.ch/recherche/api/v1/entities/3812261?skip=0&amp;take=10
        ///     ```
        ///     ___
        /// </remarks>
        [HttpGet]
        [Route("api/v1/entities/{id:int}")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(Entity<DetailRecord>), Description = "Returns the details of the record.")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, typeof(Exception),
            Description = "An internal server error is returned in case of a unknown error.")]
        [SwaggerResponse(HttpStatusCode.BadRequest, typeof(string),
            Description = "If the request is somehow malformed, the reason of the cause is returned.")]
        [SwaggerResponse(HttpStatusCode.NotFound, typeof(void), Description = "If no record was found.")]
        public IHttpActionResult GetEntity(int id, int? skip = null, int? take = null)
        {
            if (ControllerHelper.HasClaims())
            {
                return BadRequest("request was authorized, but this API only accepts unauthorized requests");
            }

            if (id <= 0)
            {
                return BadRequest("Id must be a positive integer");
            }

            if (take.HasValue && take > 500)
            {
                return BadRequest("The take parameter must be less or equal than 500");
            }

            try
            {
                var access = new UserAccess(ControllerHelper.GetCurrentUserId(), null, null, null, false);

                // The children should/must always be sorted by treeSequence. Thus we are not allowing a different sort order.
                var paging = new Paging {OrderBy = "treeSequence", SortOrder = "Ascending", Skip = skip, Take = take};
                var res = entityProvider.GetEntity<DetailRecord>(id, access, paging);

                if (res == null)
                {
                    return NotFound();
                }

                return Ok(res);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExternalController: Exception on processing request GetEntity");
                return InternalServerError();
            }
        }

        /// <summary>
        ///     Used to fetch multiple records
        /// </summary>
        /// <param name="ids">
        ///     The id numbers of the records to retrieve as a comma separated list. Each value must be a positive value larger
        ///     than 0.
        ///     The number of ids that can be passed must be less than 1000
        /// </param>
        /// <param name="skip">Defines how many child records should be skipped in the result.</param>
        /// <param name="take">Defines how many child records should be returned. The maximum number is 500.</param>
        /// <returns>EntityResult&lt;TreeRecord&gt;</returns>
        /// <remarks>
        ///     The result is a object of type Entity&lt;TreeRecord&gt;.
        /// 
        ///     This is a complex type where we have the properties
        ///     + **items**: A collection of Entity&lt;TreeRecord&gt;
        ///     + **paging**: Is always null, as there is no paging support for this operation.
        /// 
        ///     The individual items are of type Entity&lt;TreeRecord&gt;. Again we want to highlight 4 properties
        ///     + **highlight**: Information for highlighting. Is always null, as we are not searching.
        ///     + **explanation**: For debugging or analysis we could return detailed information
        ///     on how the score is calculated and the result was composed.
        ///     Is always null.
        ///     + **_metadata**: This property returns the metadata of the archive record in a organized way for
        ///     displaying the data on a web form or similar.
        ///     + **_context**: The context returns the ancestors and descendants of the requested archive record.
        ///     There could be possibly thousands of descendants/children to a records. That is why, we allow to pass
        ///     a *skip* and *take* parameter to limit the number of returned child records.
        /// 
        ///     The other properties in the object are directly relevant to the requested archive record.
        ///     ___
        /// 
        ///     ### Example calls (restful):
        ///     ``` http
        ///     GET https://www.recherche.bar.admin.ch/recherche/api/v1/entities?ids=1,412,42
        ///     ```
        ///     or
        ///     ``` http
        ///     GET https://www.recherche.bar.admin.ch/recherche/api/v1/entities?ids=123,455,312&amp;skip=0&amp;take=10
        ///     ```
        ///     ___
        /// </remarks>
        [Route("api/v1/entities")]
        [HttpGet]
        [SwaggerResponse(HttpStatusCode.OK, typeof(EntityResult<TreeRecord>), Description = "")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, typeof(Exception),
            Description = "An internal server error is returned in case of a unknown error.")]
        [SwaggerResponse(HttpStatusCode.BadRequest, typeof(string),
            Description = "If the request is somehow malformed, the reason of the cause is returned.")]
        public IHttpActionResult GetEntities(string ids, int? skip = null, int? take = null)
        {
            if (ControllerHelper.HasClaims())
            {
                return BadRequest("request was authorized, but this API only accepts unauthorized requests");
            }

            if (take.HasValue && take > 500)
            {
                return BadRequest("The take parameter must be less or equal than 500");
            }

            try
            {
                var idList = !string.IsNullOrEmpty(ids)
                    ? ids.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(i =>
                        {
                            var val = int.Parse(i);
                            if (val >= 0)
                            {
                                return val;
                            }

                            throw new ArgumentOutOfRangeException();
                        })
                        .Distinct()
                        .ToList()
                    : new List<int>();

                if (idList.Count > 1000)
                {
                    return BadRequest("A maximum of 1000 ids can be passed.");
                }

                try
                {
                    var access = new UserAccess(ControllerHelper.GetCurrentUserId(), null, null, null, false);

                    // The children should/must always be sorted by treeSequence. Thus we are not allowing a different sort order.
                    var paging = new Paging {OrderBy = "treeSequence", SortOrder = "Ascending", Skip = skip, Take = take};
                    var res = entityProvider.GetEntities<TreeRecord>(idList, access, paging);

                    return Ok(res);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "ExternalController: Exception on processing request GetEntities");
                    return InternalServerError();
                }
            }
            catch (Exception)
            {
                return BadRequest("Each Id must be a positive integer");
            }
        }

        /// <summary>
        ///     Provides search capabilities
        /// </summary>
        /// <param name="search">The search parameters as <see cref="SearchParameters" /></param>
        /// <remarks>
        ///     The result is a object of type SearchResult&lt;SearchRecord&gt;.
        /// 
        ///     This is a complex type where we have the properties
        ///     + **entities**: The detailed result from the search containing an items collection and paging information.
        ///     The paging information contains the number of total hits.
        ///     + **facets**: A collection with several facet filters
        ///     + **enableExplanations**: Whether or not the option was set in the search request.
        ///     + **requireCaptcha**: Whether or not subsequent searches require a captcha verification code.
        /// 
        ///     ___
        /// 
        ///     ### Example calls (restful):
        ///     ``` http
        ///     POST https://www.recherche.bar.admin.ch/recherche/api/v1/entities/search
        ///     {
        ///       "query": {
        ///         "searchGroups": [
        ///           {
        ///             "searchFields": [
        ///               {
        ///                 "value": "Pilot",
        ///                 "key": "allData"
        ///               },
        ///               {
        ///                 "key": "creationPeriod",
        ///                 "value": null
        ///               }
        ///             ],
        ///             "fieldOperator": 1
        ///           }
        ///         ],
        ///         "groupOperator": 1
        ///       },
        ///       "options": {
        ///         "enableHighlighting": true,
        ///         "enableAggregations": true
        ///       },
        ///       "paging": {
        ///         "skip": 0,
        ///         "take": 10,
        ///         "orderBy": "",
        ///         "sortOrder": ""
        ///       }
        ///     }
        /// 
        ///     POST https://www.recherche.bar.admin.ch/recherche/api/v1/entities/search
        ///     {
        ///       "query": {
        ///         "searchGroups": [
        ///           {
        ///             "searchFields": [
        ///               {
        ///                 "value": "Eisenbahn",
        ///                 "key": "allData"
        ///               },
        ///               {
        ///                 "key": "creationPeriod",
        ///                 "value": null
        ///               }
        ///             ],
        ///             "fieldOperator": 1
        ///           }
        ///         ],
        ///         "groupOperator": 1
        ///       },
        ///       "options": {
        ///         "enableHighlighting": true,
        ///         "enableAggregations": true
        ///       },
        ///       "paging": {
        ///         "skip": 0,
        ///         "take": 10,
        ///         "orderBy": "",
        ///         "sortOrder": ""
        ///       },
        ///       "facetsFilters": [
        ///         {
        ///           "filters": [
        ///             "customFields.zugänglichkeitGemässBga:\"Frei zugänglich\""
        ///           ],
        ///           "facet": "customFields.zugänglichkeitGemässBga"
        ///         },
        ///         {
        ///           "filters": [
        ///             "aggregationFields.ordnungskomponenten:\"513 Armeestab\""
        ///           ],
        ///           "facet": "aggregationFields.ordnungskomponenten"
        ///         }
        ///       ]
        ///     }
        /// 
        ///     POST https://www.recherche.bar.admin.ch/recherche/api/v1/entities/search
        ///     {
        ///       "query": {
        ///         "searchGroups": [
        ///           {
        ///             "searchFields": [
        ///               {
        ///                 "key": "title",
        ///                 "value": "Lastwagen"
        ///               },
        ///               {
        ///                 "key": "creationPeriod",
        ///                 "value": "1950-1990"
        ///               }
        ///             ],
        ///             "fieldOperator": 1
        ///           },
        ///           {
        ///             "searchFields": [
        ///               {
        ///                 "key": "withinInfo",
        ///                 "value": "Schwerverkehr"
        ///               }
        ///             ],
        ///             "fieldOperator": 3
        ///           }
        ///         ],
        ///         "groupOperator": 1
        ///       },
        ///       "options": {
        ///         "enableExplanations": false,
        ///         "enableHighlighting": true,
        ///         "enableAggregations": false
        ///       },
        ///       "paging": {
        ///         "skip": 0,
        ///         "orderBy": "",
        ///         "sortOrder": "",
        ///         "take": 10
        ///       }
        ///     }
        ///     ```
        ///     ## Search field key
        ///     Here the complete list of fields that can be searched.
        /// 
        ///     | SearchField                           | Description       |
        ///     |---------------------------------------|-------------------|
        ///     | allData                               | Searches in **all** metadata fields that the database knows about, and are applicable for search. (Numbers or Booleans are usually excluded) **AND** textual data of referenced digital files. If this field is searched, this has to be done with at least 2 none wildcard characters.|
        ///     | allMetaData                           | Searches in all metadata fields.  |
        ///     | allPrimaryData                        | Searches in the textual data of referenced digital files, e.g. primaryData. |
        ///     | title                                 | Searches the title field. |
        ///     | creationPeriod                        | Searches a data range. The record must have been created in this period. Use 1950-1960 or 24.12.1950-1960 or simply 1914. |
        ///     | withinInfo                            | Searches the Within Info field. (Darin-Vermerk) |
        ///     | referenceCode                         | Searches for the reference code. (Signatur) |
        ///     | customFields.form                     | Searches for the identification of special types. (medium) and forms of organisation of documents. |
        ///     | formerReferenceCode                   | Searches for the former reference code. (Frühere Signatur) |
        ///     | customFields.land                     | Searches for the country. |
        ///     | customFields.zusatzkomponenteZac1     | Searches in the supplementary component field. (Zusatzkomponente) |
        ///     | customFields.aktenzeichen             | Searches for the reference number or file number field. (Aktenzeichen) |
        ///     | customFields.früheresAktenzeichen     | Searches for the former reference number or former file number field. (Früheres Aktenzeichen) |
        ///     | customFields.zugänglichkeitGemässBga  | Searches for the accessibility according to BGA. Possible values are 'Frei Zugänglich', 'In Schutzfrist' and 'Prüfung nötig' |
        /// 
        ///     ## Search field value
        ///     For a search at least one none wildcard character (*, ?) is needed
        /// 
        ///     ## Operators
        ///     Individual search groups can be linked with the following operators
        ///     + And = 1,
        ///     + Or = 2
        /// 
        ///     Search fields can be linked with the operators
        ///     + And = 1,
        ///     + Or = 2,
        ///     + Not = 3
        /// 
        ///     ## Facets
        ///     The possible facets are obtained with the response. You can then activate the returned facets as filters by
        ///     specifying them in the request.
        ///     The second sample above shows how to make a search using facets.
        /// 
        ///     ## Paging
        ///     If the request contains a value for take, this value must be less or equal than 100.
        /// 
        /// </remarks>
        /// <returns>SearchResult&lt;SearchRecords&gt;</returns>
        [Route("api/v1/entities/search")]
        [HttpPost]
        [SwaggerResponse(HttpStatusCode.OK, typeof(SearchResult<SearchRecord>), Description = "")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, null, Description = "An internal server error is returned in case of a unknown error.")]
        [SwaggerResponse(HttpStatusCode.BadRequest, typeof(string),
            Description = "If the request is somehow malformed, the reason of the cause is returned.")]
        public IHttpActionResult Search([FromBody] SearchParameters search)
        {
            if (ControllerHelper.HasClaims())
            {
                return BadRequest("The request was authorized, but this API only accepts unauthorized requests");
            }

            try
            {
                var clientLanguage = WebHelper.GetClientLanguage(Request);

                var error = entityProvider.CheckSearchParameters(search, clientLanguage);
                if (!string.IsNullOrEmpty(error))
                {
                    return BadRequest(error);
                }

                var access = GetUserAccess(clientLanguage);

                var res = entityProvider.Search<SearchRecord>(search, access);
                var errorResult = res as ErrorSearchResult;
                if (errorResult == null)
                {
                    return Ok(res as SearchResult<SearchRecord>);
                }

                if (errorResult.Error.StatusCode == (int) HttpStatusCode.BadRequest) // Syntax Error
                {
                    return BadRequest("The search query had a syntax error or was invalid.");
                }

                return StatusCode(errorResult.Error?.StatusCode != 0
                    ? (HttpStatusCode) errorResult.Error.StatusCode
                    : HttpStatusCode.InternalServerError);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnknownElasticSearchFieldException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExternalController: Exception on processing request Search");
                return InternalServerError();
            }
        }
    }
}