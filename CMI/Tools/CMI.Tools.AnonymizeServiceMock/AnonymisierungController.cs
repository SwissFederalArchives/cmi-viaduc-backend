using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Http;
using Swashbuckle.Swagger.Annotations;

namespace CMI.Tools.AnonymizeServiceMock
{
    public class AnonymisierungController : ApiController
    {
        private readonly WordList wordList;

        public AnonymisierungController(WordList wordList)
        {
            this.wordList = wordList;
        }

        /// <summary>
        /// Anonymisiert die Werte in der übergebenen Values Struktur
        /// </summary>
        /// <param name="payload">Der zu anonymisierende Text</param>
        /// <response code="200">Text was anonymized successfully</response>
        /// <response code="400">invalid input, object invalid</response>
        /// <response code="401">Unauthorized - API key is missing or invalid</response>
        /// <response code="413">Payload Too Large - Zu bearbeitende Daten sind zu gross</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="503">ServiceNotAvailable - Die Schnittstelle selber, oder ein Subsystem ist aktuell nicht verfügbar. Die Schnittstelle kann beliebig weiter aufgerufen werden und wird wie-der antworten, sobald die Systeme online sein.</response>
        [HttpPost]
        [Route("api/v1/anonymizeText")]
        [SwaggerResponse(HttpStatusCode.OK, "Die Daten wurden erfolgreich anonymisiert.", typeof(AnonymizationResponse))]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "Wenn der HTTP Header einen falschen oder keinen API-Key enthält.", typeof(string))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "invalid input, object invalid.", typeof(string))]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "Wenn ein interner Fehler auftritt.", typeof(Exception))]
        [SwaggerResponse(HttpStatusCode.RequestEntityTooLarge,"Payload Too Large - Zu bearbeitende Daten sind zu gross.", typeof(string))]
        public IHttpActionResult AnonymizeText([FromBody] AnonymizationRequest payload)
        {
            try
            {
                if (!ApiKeyChecker.IsCorrect(Request))
                {
                    return Unauthorized();
                }

                if (payload == null || payload.Values.All(v => string.IsNullOrEmpty(v.Value)))
                {
                    return BadRequest("At least one value must be passed that is not empty.");
                }

                if (payload.Values.Any(v => v.Value.Length > 200000))
                {
                    return Content(HttpStatusCode.RequestEntityTooLarge, "Payload Too Large - Text value may not exceed 200000 characters.");
                }

                var respose = new AnonymizationResponse
                {
                    AnonymizedValues = new Dictionary<string, string>()
                };

                foreach (var item in payload.Values)
                {
                    respose.AnonymizedValues.Add(item.Key, Anonymize(item.Value));
                }
                return Ok(respose);
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        private string Anonymize(string text)
        {
            foreach (var word in wordList.WordsToBeAnonymized)
            {
                text = Regex.Replace(text, $@"(?!<anonym.*?>)({word})(?!<\/anonym>)", @"<anonym type=""n"">$1</anonym>");
            }

            return text;
        }
    }
}

