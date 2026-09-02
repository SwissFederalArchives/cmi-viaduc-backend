using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Clients.Elasticsearch.Aggregations;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Frontend.api.Elastic
{
    public static class AggregationExtensions
    {
        public static JObject CreateSerializableAggregations(this Dictionary<string, AggregateDictionary> filteredAggregations)
        {
            var facette = new JObject();

            foreach (var aggregation in filteredAggregations)
            {
                Debug.Assert(aggregation.Value != null, "Aggregation value should not be null. (aggregation.Value != null)");

                foreach (var aggregate in aggregation.Value.Values)
                {
                    //  SingleBucketAggregate
                    if (aggregate is FilterAggregate sba)
                    {
                        var aggregationLevel = new JObject();
                        var bucketContainer = new JObject();
                        aggregationLevel.Add("docCount", sba.DocCount);
                        

                        foreach (var key in sba.Aggregations.Keys)
                        {
                            var ba = sba.Aggregations[key] as BucketMetricValueAggregate;
                          

                            //bucketContainer.Add(key, bucketLevel);
                        }

                        aggregationLevel.Add("aggregations", bucketContainer);
                        facette.Add(aggregation.Key, aggregationLevel);
                    }

                    else if (aggregate is StringTermsAggregate stringTerms)
                    {
                        var bucketLevel = CreateBucket(stringTerms);
                        facette.Add(aggregation.Key, bucketLevel);
                    }
                    else if (aggregate is LongTermsAggregate bucketAggregate)
                    {
                         var bucketLevel = CreateBucket(bucketAggregate);
                        facette.Add(aggregation.Key, bucketLevel);
                    }
                    else if (aggregate is DateRangeAggregate dateRangeAggregate)
                    {
                         var bucketLevel = CreateBucket(dateRangeAggregate);
                         facette.Add(aggregation.Key, bucketLevel);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unhandled aggregation type {aggregation.Value.GetType().FullName}");
                    }
                }
            }

            return facette;
        }

        private static JObject CreateBucket(StringTermsAggregate bucketAggregate)
        {
            var bucketLevel = new JObject
            {
                {"docCount", bucketAggregate.Buckets.Count},
                {"sumOtherDocCount", bucketAggregate.SumOtherDocCount},
                {"docCountErrorUpperBound", bucketAggregate.DocCountErrorUpperBound}
            };

            var items = CreateBucketItems(bucketAggregate.Buckets);
            bucketLevel.Add("items", items);

            return bucketLevel;
        }

        private static JObject CreateBucket(LongTermsAggregate bucketAggregate)
        {
            var bucketLevel = new JObject
            {
                {"docCount", bucketAggregate.Buckets.Count},
                {"sumOtherDocCount", bucketAggregate.SumOtherDocCount},
                {"docCountErrorUpperBound", bucketAggregate.DocCountErrorUpperBound}
            };

            var items = CreateBucketItems(bucketAggregate.Buckets);
            bucketLevel.Add("items", items);

            return bucketLevel;
        }


        private static JObject CreateBucket(DateRangeAggregate bucketAggregate)
        {
            var bucketLevel = new JObject
            {
                {"docCount", bucketAggregate.Buckets.Count},
                {"sumOtherDocCount", 0},
                {"docCountErrorUpperBound", 0}
            };

            var items = CreateBucketItems(bucketAggregate.Buckets);
            bucketLevel.Add("items", items);

            return bucketLevel;
        }
        

        private static JArray CreateBucketItems(IReadOnlyCollection<StringTermsBucket> buckets)
        {
            var array = new JArray();
            foreach (var bucket1 in buckets)
            {
                var keyedBucket = bucket1.Key;
                var bucket = new JObject
                {
                    {"docCount", bucket1.DocCount},
                    {"docCountErrorUpperBound", bucket1.DocCountErrorUpperBound},
                    {"keyAsString", keyedBucket.ToString()},
                    {"key", bucket1.Key.ToString()}
                };
                array.Add(bucket);
            }

            return array;
        }


        private static JArray CreateBucketItems(IReadOnlyCollection<LongTermsBucket> buckets)
        {
            var array = new JArray();
            foreach (var bucket1 in buckets)
            {
                var keyedBucket = bucket1.KeyAsString;
                var bucket = new JObject
                {
                    {"docCount", bucket1.DocCount},
                    {"docCountErrorUpperBound", bucket1.DocCountErrorUpperBound},
                    {"keyAsString", keyedBucket},
                    {"key", bucket1.Key.ToString()}
                };
                array.Add(bucket);
            }

            return array;
        }


        private static JArray CreateBucketItems(IReadOnlyCollection<RangeBucket> buckets)
        {
            var array = new JArray();
            foreach (var bucket1 in buckets)
            {
                var keyedBucket = bucket1.Key;
                var bucket = new JObject
                {
                    {"docCount", bucket1.DocCount},
                    {"keyAsString", keyedBucket},
                    {"key", bucket1.Key}
                };
                array.Add(bucket);
            }

            return array;
        }
    }
}
