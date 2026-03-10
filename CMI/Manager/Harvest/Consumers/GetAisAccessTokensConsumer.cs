using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Harvest;
using MassTransit;
using Serilog;
using System.Collections.Generic;

namespace CMI.Manager.Harvest.Consumers
{
    public class GetAisAccessTokensConsumer : IConsumer<IGetSecurityTokens>
    {
        private readonly IHarvestManager harvestManager;

        public GetAisAccessTokensConsumer(IHarvestManager _harvestManager)
        {
            harvestManager = _harvestManager;
        }

        public async Task Consume(ConsumeContext<IGetSecurityTokens> context)
        {
            var archiveRecordId = context.Message.ArchiveRecordId;
            var calculated = new AccessTokens();

            try
            {
                var security = await harvestManager.GetAisAccessTokens(archiveRecordId);
                if (security != null)
                {
                    calculated.MetadataAccessTokens = string.Join(", ", security.MetadataAccessToken ?? new List<string>());

                    calculated.FulltextAccessTokens = string.Join(", ", security.PrimaryDataFulltextAccessToken ?? new List<string>());
                    calculated.DownloadAccessTokens = string.Join(", ", security.PrimaryDataDownloadAccessToken ?? new List<string>());
                    calculated.FieldAccessTokens = string.Join(", ", security.FieldAccessToken ?? new List<string>());
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not calculate access tokens for ArchiveRecordId {ArchiveRecordId}", archiveRecordId);
            }

            await context.RespondAsync(new GetSecurityTokensResponse
            {
                Calculated = calculated
            });
        }
    }
}
