
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMI.Contract.Common;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Engine.Anonymization
{
    public class AnonymizationEngine : IAnonymizationEngine
    {
        private readonly HttpClient client;

        private const string findAnonymizationTagRegex = @"<anonym(.|\n|\r)*?<\/anonym>";
        private const string archiveplancontextKey = "archiveplanContext";
        private const string referenceKey = "reference";
        private const string titleKey = "Title";
        private const string withinInfoKey = "WithinInfo";
        private const string bemerkungZurVeKey = "BemerkungZurVe";
        private const string zusatzkomponenteZac1Key = "ZusatzkomponenteZac1";
        private const string verwandteVeKey = "VerwandteVe";

        public AnonymizationEngine(HttpClient client)
        {
            this.client = client;
        }

        public async Task<ElasticArchiveDbRecord> AnonymizeArchiveRecordAsync(ElasticArchiveDbRecord elasticArchiveDbRecord)
        {
            elasticArchiveDbRecord.IsAnonymized = false;
            var request = new AnonymizationRequest
            {
                Options = new  AnonymizeOptions
                {
                    Style = AnonymizeStyle.Tags
                },
                Values = new Dictionary<string, string>(),
                ReferenceCode = elasticArchiveDbRecord.ReferenceCode
            };

            // Title
            elasticArchiveDbRecord.UnanonymizedFields = new UnanonymizedFields() { Title = elasticArchiveDbRecord.Title };
            request.Values.Add(titleKey, elasticArchiveDbRecord.Title);
            
            // WithinInfo
            if (!string.IsNullOrWhiteSpace(elasticArchiveDbRecord.WithinInfo))
            {
                elasticArchiveDbRecord.UnanonymizedFields.WithinInfo = elasticArchiveDbRecord.WithinInfo;
                request.Values.Add(withinInfoKey, elasticArchiveDbRecord.WithinInfo);
            }

            var bemerkungZurVe = elasticArchiveDbRecord.ZusätzlicheInformationen();
            if (!string.IsNullOrWhiteSpace(bemerkungZurVe))
            {
                elasticArchiveDbRecord.UnanonymizedFields.BemerkungZurVe = bemerkungZurVe;
                request.Values.Add(bemerkungZurVeKey, bemerkungZurVe);
            }

            var zusatzkomponenteZac1 = elasticArchiveDbRecord.Zusatzmerkmal();
            if (!string.IsNullOrWhiteSpace(zusatzkomponenteZac1))
            {
                elasticArchiveDbRecord.UnanonymizedFields.ZusatzkomponenteZac1 = zusatzkomponenteZac1;
                request.Values.Add(zusatzkomponenteZac1Key, zusatzkomponenteZac1);
            }

            var verwandteVe = elasticArchiveDbRecord.VerwandteVe();
            if (!string.IsNullOrWhiteSpace(verwandteVe))
            {
                elasticArchiveDbRecord.UnanonymizedFields.VerwandteVe = verwandteVe;
                request.Values.Add(verwandteVeKey, verwandteVe);
            }

            // Archiveplan Context
            elasticArchiveDbRecord.UnanonymizedFields.ArchiveplanContext.Clear();
            elasticArchiveDbRecord.UnanonymizedFields.ArchiveplanContext.AddRange(CloneObject(elasticArchiveDbRecord.ArchiveplanContext));
            var textBuilder = new StringBuilder();
            foreach (var archiveplanContextItem in elasticArchiveDbRecord.ArchiveplanContext)
            {
                if (archiveplanContextItem.Protected)
                {
                    request.Values.Add($"{archiveplancontextKey}-{archiveplanContextItem.ArchiveRecordId}", archiveplanContextItem.Title);
                }

                textBuilder.AppendLine(
                    $"{archiveplanContextItem.RefCode} {archiveplanContextItem.Title} {archiveplanContextItem.DateRangeText} ({archiveplanContextItem.Level})");
            }

            request.Context = textBuilder.ToString();

            // References
            elasticArchiveDbRecord.UnanonymizedFields.References.Clear();
            elasticArchiveDbRecord.UnanonymizedFields.References.AddRange(CloneObject(elasticArchiveDbRecord.References));
            foreach (var reference in elasticArchiveDbRecord.References)
            {
                if (reference.Protected)
                {
                    request.Values.Add($"{referenceKey}-{reference.ArchiveRecordId}", reference.ReferenceName);
                }
            }

            var result = await AnonymizeTextAsync(request);
            SetAnonymizedTextInElasticRecord(elasticArchiveDbRecord, result);

            return elasticArchiveDbRecord;
        }

        #region protected Methods

        /// <summary>
        /// Call the Anonymize Service
        /// virtual for test purposes, mock override this method
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<string> ExecuteHttpPostToService(AnonymizationRequest request)
        {
            Log.Debug("Sending anonymization request. Payload is {request}", JsonConvert.SerializeObject(request));
            var response = await client.PostAsJsonAsync("anonymizeText", request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                {
                    var result = await response.Content.ReadAsStringAsync();
                    Log.Debug("Received anonymization result. {result}", JsonConvert.SerializeObject(result));
                    return result;
                }
                case HttpStatusCode.Unauthorized:
                    Log.Error("Unauthorized exception with anonymization service. Probably wrong or incorrect X-ApiKey set.");
                    throw new UnauthorizedAccessException("No access to anonymization service");
                case HttpStatusCode.BadRequest:
                    Log.Error("The message was not in the correct format. {Content}", await response.Content.ReadAsStringAsync());
                    throw new InvalidOperationException("The anonymization service could not be called due to a malformed exception");
                case HttpStatusCode.RequestEntityTooLarge:
                    Log.Error("The message was to big to be processed. Error is {Content}", await response.Content.ReadAsStringAsync());
                    throw new InvalidOperationException("The anonymization service could not be called due to a too large message");
                case HttpStatusCode.InternalServerError:
                default:
                    Log.Error("Some internal error occured in the anonymization service. Details: {Content}", await response.Content.ReadAsStringAsync());
                    throw new InvalidOperationException("The anonymization service reported an internal error. See log for more details.");
            }
        }
        protected static string ReplaceAnonymTagWithBlockquote(string anonymizeText)
        {
            return Regex.Replace(anonymizeText, findAnonymizationTagRegex, "███");
        }

        #endregion

        #region private methods

        private void SetAnonymizedTextInElasticRecord(ElasticArchiveDbRecord elasticArchiveDbRecord, Dictionary<string, string> result)
        {
            if (result != null)
            {
                foreach (var item in result)
                {
                    // Set flag that we have done our job
                    elasticArchiveDbRecord.IsAnonymized = elasticArchiveDbRecord.IsAnonymized || item.Value.Contains("███");
                    switch (item.Key)
                    {
                        case titleKey:
                            elasticArchiveDbRecord.Title = item.Value;
                            break;
                        case withinInfoKey:
                            elasticArchiveDbRecord.WithinInfo = item.Value;
                            break;
                        case bemerkungZurVeKey:
                            elasticArchiveDbRecord.SetAnonymizeZusätzlicheInformationen(item.Value);
                            break;
                        case verwandteVeKey:
                            elasticArchiveDbRecord.SetAnonymizeVerwandteVe(item.Value);
                            break;
                        case zusatzkomponenteZac1Key:
                            elasticArchiveDbRecord.SetAnonymizeZusatzmerkmal(item.Value);
                            break;
                        default:
                            SetSpecialTexts(item, elasticArchiveDbRecord);
                            break;
                    }
                }

                // Transfer the data to the Parent Content Infos
                elasticArchiveDbRecord.UnanonymizedFields.ParentContentInfos.Clear();
                elasticArchiveDbRecord.UnanonymizedFields.ParentContentInfos.AddRange(CloneObject(elasticArchiveDbRecord.ParentContentInfos));
                elasticArchiveDbRecord.ParentContentInfos =
                    elasticArchiveDbRecord.ArchiveplanContext.Select(c => new ElasticParentContentInfo { Title = c.Title }).ToList();

            }
        }

        private void SetSpecialTexts(KeyValuePair<string, string> item, ElasticArchiveDbRecord elasticArchiveDbRecord)
        {
            var identifiers = item.Key.Split('-');
            string key = identifiers[1];
            string fieldName = identifiers[0];

            if (fieldName.StartsWith(archiveplancontextKey))
            {
                elasticArchiveDbRecord.ArchiveplanContext.Single(a => a.ArchiveRecordId.Equals(key)).Title = item.Value;
            }
            else if (fieldName.StartsWith(referenceKey))
            {
                elasticArchiveDbRecord.References.Single(a => a.ArchiveRecordId.Equals(key)).ReferenceName = item.Value;
            }
        }

        private static bool TextContainsAnonymizationTag(string anonymizeText)
        {
            return Regex.IsMatch(anonymizeText, findAnonymizationTagRegex);
        }

        private T CloneObject<T>(T listOfObjects)
        {
            var str = JsonConvert.SerializeObject(listOfObjects);
            return JsonConvert.DeserializeObject<T>(str);
        }

        private async Task<Dictionary<string, string>> AnonymizeTextAsync(AnonymizationRequest request)
        {
            if (request.Values.Count == 0)
            {
                return null;
            }

            var result = await ExecuteHttpPostToService(request);
            var anonymizationResponse = JsonConvert.DeserializeObject<AnonymizationResponse>(result);
            var anonymizedDictionary = new  Dictionary<string, string>();
            foreach (var anonymizedText in anonymizationResponse.AnonymizedValues)
            {
                if (!string.IsNullOrEmpty(anonymizedText.Value) && TextContainsAnonymizationTag(anonymizedText.Value))
                {
                    anonymizedDictionary.Add(anonymizedText.Key, ReplaceAnonymTagWithBlockquote(anonymizedText.Value));
                }
                else
                {
                    anonymizedDictionary.Add(anonymizedText.Key, anonymizedText.Value);
                }
            }
          
            return anonymizedDictionary;
        }
        #endregion
    }
}
