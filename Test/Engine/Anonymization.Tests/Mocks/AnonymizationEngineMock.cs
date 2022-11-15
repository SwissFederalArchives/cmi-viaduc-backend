using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CMI.Engine.Anonymization.Tests.Mocks
{
    public class AnonymizationEngineMock : AnonymizationEngine
    {
        private string returnValueFromServiceCall;

        public string ReturnValueFromServiceCall
        {
            get => returnValueFromServiceCall;
            set
            {
                returnValueFromServiceCall = value;
                AnonymTagWithBlockqute = ReplaceAnonymTagWithBlockquote(returnValueFromServiceCall);
            }
        }

        public string AnonymTagWithBlockqute { get; private set; }

        public AnonymizationEngineMock(HttpClient client) : base(client)
        {
            ReturnValueFromServiceCall = "Dies ist ein <anonym type='n'>geschwärzter</anonym> Text";
        }

        protected override Task<string> ExecuteHttpPostToService(AnonymizationRequest request)
        {
            var result = new AnonymizationResponse
            {
                AnonymizedValues = new Dictionary<string, string>()
            };
            foreach (var reqValue in request.Values)
            {
                if (!string.IsNullOrEmpty(reqValue.Value))
                {
                    result.AnonymizedValues.Add(reqValue.Key, ReturnValueFromServiceCall);
                }
            }
            return Task.FromResult(JsonConvert.SerializeObject(result));
        }
    }
}
