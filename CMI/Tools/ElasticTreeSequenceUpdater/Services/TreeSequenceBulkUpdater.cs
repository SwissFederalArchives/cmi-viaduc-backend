using CMI.Tools.ElasticTreeSequenceUpdater.Models;
using Elastic.Clients.Elasticsearch;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch.Core.Bulk;

namespace CMI.Tools.ElasticTreeSequenceUpdater.Services
{
    public class TreeSequenceBulkUpdater
    {
        private readonly ElasticsearchClient client;
        private readonly string indexName;

        public TreeSequenceBulkUpdater(ElasticsearchClient client, string indexName)
        {
            this.client = client;
            this.indexName = indexName;
        }

        /// <summary>
        /// Bulk update treeSequence für Records (erst ScopeId, dann ggf. ActaProId).
        /// </summary>
        public async Task BulkUpdateTreeSequenceAsync(IEnumerable<UpdateEntry> updates)
        {
            if (updates == null || !updates.Any())
            {
                Log.Warning("[BulkUpdater] No updates to process.");
                return;
            }

            // 🔹 Lookup für schnelles Matching vorbereiten
            var updateDict = updates
                .Where(u => !string.IsNullOrWhiteSpace(u.ScopeId) && !string.IsNullOrWhiteSpace(u.ActaProId))
                .ToDictionary(u => u.ScopeId, u => u);

            // Step 1: Update nach ScopeId
            var bulkRequest = new BulkRequest(indexName)
            {
                Operations = updates
                    .Where(u => !string.IsNullOrWhiteSpace(u.ScopeId))
                    .Select(u => (IBulkOperation) new BulkUpdateOperation<object, object>(u.ScopeId)
                    {
                        Doc = new { treeSequence = u.TreeSeq },
                        DocAsUpsert = false
                    })
                    .ToList()
            };

            var response = await client.BulkAsync(bulkRequest);

            var retryList = new ConcurrentBag<UpdateEntry>();

            if (response.Errors)
            {
                Parallel.ForEach(response.ItemsWithErrors, item =>
                {
                    if (!string.IsNullOrWhiteSpace(item.Error?.Reason) &&
                        item.Error.Reason.IndexOf("document missing", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (updateDict.TryGetValue(item.Id, out var failed))
                        {
                            retryList.Add(failed);
                        }
                    }
                    /*else if (!string.IsNullOrWhiteSpace(item.Error?.Reason))
                    {
                        Log.Error(" - Failed ID: {Id}, Error: {Error}", item.Id, item.Error?.Reason);
                    }*/
                });
            }

            Log.Information("[BulkUpdater] Successfully updated {Count} documents by ScopeId.",
                response.Items.Count(i => i.IsValid));

            // Step 2: Retry nach ActaProId
            if (!retryList.IsEmpty)
            {
                var retryRequest = new BulkRequest(indexName)
                {
                    Operations = retryList
                        .Select(u => (IBulkOperation) new BulkUpdateOperation<object, object>(u.ActaProId)
                        {
                            Doc = new { treeSequence = u.TreeSeq },
                            DocAsUpsert = false
                        })
                        .ToList()
                };

                var retryResponse = await client.BulkAsync(retryRequest);

                if (retryResponse.Errors)
                {
                    Parallel.ForEach(retryResponse.ItemsWithErrors, item =>
                    {
                        if (!string.IsNullOrWhiteSpace(item.Error?.Reason))
                        {
                            Log.Error(" - Retry failed ID: {Id}, Error: {Error}", item.Id, item.Error?.Reason);
                        }
                    });
                }

                Log.Information("[BulkUpdater] Successfully updated {Count} documents by ActaProId.",
                    retryResponse.Items.Count(i => i.IsValid));
            }
        }
    }
}
