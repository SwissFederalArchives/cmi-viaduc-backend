using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using CMI.Access.Onboarding.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Access.Onboarding
{
    public class OnboardingConnector:IOnboardingConnector
    {
        private readonly IConnectorSettings settings;
        private readonly MemoryCache cache;

        private const string tokenCacheKey = "token";
        
        public OnboardingConnector(IConnectorSettings connectorSettings)
        {
            this.settings = connectorSettings;
            this.cache = new MemoryCache("TokenCache");
        }

        public async Task<string> GetAccessToken()
        {
            if(!cache.Contains(tokenCacheKey))
            {
                var credentials = new
                {
                    strategy = settings.Strategy,
                    clientId = settings.ClientId,
                    clientSecret = settings.ClientSecret,
                };

                var json = JObject.FromObject(credentials).ToString();
                var content = new StringContent(json, UnicodeEncoding.UTF8, "application/json");

                var client = HttpClientFactory.Create();
                HttpResponseMessage response = await client.PostAsync($"{settings.OnboardingBaseUrl}/api/v1/authentication", content);
                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();
                dynamic data = JObject.Parse(result);
                cache.Set(tokenCacheKey, data.accessToken, new CacheItemPolicy() { AbsoluteExpiration = DateTime.Now.AddMinutes(settings.TokenAbsoluteExpiration) });
            }

            return  $"{cache.Get(tokenCacheKey)}";
        }

        public async Task<Status> GetProcessById(string id)
        {
            var token = await GetAccessToken();
            var client = HttpClientFactory.Create();
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"{settings.OnboardingBaseUrl}/api/v1/fidentity/{id}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Status>(json);
        }

        public async Task<byte[]> GetDocumentByUri(string uri)
        {
            var token = await GetAccessToken();
            var client = HttpClientFactory.Create();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"{settings.OnboardingBaseUrl}{uri}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<string> StartProcess(string json)
        {
            var token = await GetAccessToken();

            var content = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
            var client = HttpClientFactory.Create();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync($"{settings.OnboardingBaseUrl}/api/v1/fidentity", content);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(result);
            
            return $"{settings.OnboardingBaseUrl}{data.processUrl}";
        }
    }
}