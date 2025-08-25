using CMI.Utilities.Transkribus.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CMI.Utilities.Transkribus
{
    public interface ITranskribusWorker
    {
        Task<bool> StartExtractText(string inputFolder, string outputFolder);

        Task<bool> StartClassifier(string fileFullname, string fileName, string packageId);

        Task<bool> Health();
    }
}
