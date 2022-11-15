using System;
using CMI.Tools.AnonymizeServiceMock.Properties;
using Microsoft.Owin.Hosting;

namespace CMI.Tools.AnonymizeServiceMock
{
    public class AnonymizeService
    {
        private readonly IDisposable webApp;

        public AnonymizeService()
        {
            var url = AnonymizeSettings.Default.Address; 
           webApp = WebApp.Start<Startup>(url);
           Console.WriteLine($"Service läuft auf {url}", url);
        }
        
        public void Start()
        {
            ApiKeyChecker.Key = AnonymizeSettings.Default.ApiKey;
        }
        
        public void Stop()
        {
            webApp.Dispose();
        }
    }
}
