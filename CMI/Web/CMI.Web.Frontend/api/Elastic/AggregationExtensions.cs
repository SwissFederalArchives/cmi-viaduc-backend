using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nest;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Frontend.api.Elastic
{
    public static class AggregationExtensions
    {
        public static JObject CreateSerializableAggregations(this Dictionary<string, IAggregate> filteredAggregations)
        {
            var facette = new JObject();

            foreach (var aggregation in filteredAggregations)
            {
                Debug.Assert(aggregation.Value != null, "Aggregation value should not be null. (aggregation.Value != null)");

                if (aggregation.Value is SingleBucketAggregate sba)
                {
                    var aggregationLevel = new JObject();
                    var bucketContainer = new JObject();
                    aggregationLevel.Add("docCount", sba.DocCount);

                    foreach (var key in sba.Keys)
                    {
                        var ba = sba[key] as BucketAggregate;
                        var bucketLevel = CreateBucket(ba);

                        bucketContainer.Add(key, bucketLevel);
                    }

                    aggregationLevel.Add("aggregations", bucketContainer);
                    facette.Add(aggregation.Key, aggregationLevel);
                }
                else if (aggregation.Value is BucketAggregate bucketAggregate)
                {
                    var bucketLevel = CreateBucket(bucketAggregate);
                    facette.Add(aggregation.Key, bucketLevel);
                }
                else
                {
                    throw new InvalidOperationException($"Unhandled aggregation type {aggregation.Value.GetType().FullName}");
                }
            }

            return facette;
        }

        private static JObject CreateBucket(BucketAggregate bucketAggregate)
        {
            var bucketLevel = new JObject
            {
                {"docCount", bucketAggregate.DocCount},
                {"sumOtherDocCount", bucketAggregate.SumOtherDocCount},
                {"docCountErrorUpperBound", bucketAggregate.DocCountErrorUpperBound}
            };

            var items = CreateBucketItems(bucketAggregate);
            bucketLevel.Add("items", items);

            return bucketLevel;
        }

        private static JArray CreateBucketItems(BucketAggregate bucketAggregate)
        {
            var array = new JArray();
            foreach (var bucket1 in bucketAggregate.Items)
            {
                var keyedBucket = (KeyedBucket<object>) bucket1;
                var bucket = new JObject
                {
                    {"docCount", keyedBucket.DocCount},
                    {"docCountErrorUpperBound", keyedBucket.DocCountErrorUpperBound},
                    {"keyAsString", keyedBucket.KeyAsString},
                    {"key", keyedBucket.Key.ToString()}
                };
                array.Add(bucket);
            }

            return array;
        }
    }
}