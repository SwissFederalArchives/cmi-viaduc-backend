using System;
using System.Net.Http;

namespace CMI.Web.Frontend.Helpers
{
    public interface IUsageAnalyzer
    {
        Threshold? GetExceededThreshold(string userId, HttpRequestMessage request);

        /// <param name="userId"></param>
        /// <param name="request"></param>
        /// <param name="usages">Anzahl Verwendungen</param>
        void UpdateUsageStatistic(string userId, HttpRequestMessage request, int usages);

        void Reset(string userId, HttpRequestMessage request);
        string GetText(TimeSpan span, string language);
    }
}