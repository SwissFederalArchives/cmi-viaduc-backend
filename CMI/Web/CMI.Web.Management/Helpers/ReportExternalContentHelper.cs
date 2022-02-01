using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;

namespace CMI.Web.Management.Helpers
{
    public class ReportExternalContentHelper : IReportExternalContentHelper
    {
        private readonly IRequestClient<SyncInfoForReportRequest> client;

        public ReportExternalContentHelper(IRequestClient<SyncInfoForReportRequest> client)
        {
            this.client = client;
        }

        public async Task<List<SyncInfoForReport>> GetSyncInfoForReport(int[] mutationsIds)
        {
            var response = (await client.GetResponse<SyncInfoForReportResponse>(new SyncInfoForReportRequest { MutationsIds = mutationsIds })).Message;

            try
            {
                if (response.Result != null)
                {
                    return response.Result.Records;
                }

                Log.Error("Error Message returned null");
                throw new InvalidOperationException("Error Message returned null");
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception in ReportExternalContentHelper.GetSyncInfoForReport");
                throw;
            }
        }

    }
}