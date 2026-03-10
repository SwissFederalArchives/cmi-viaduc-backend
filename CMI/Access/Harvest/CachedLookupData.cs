using Serilog;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace CMI.Access.Harvest
{
    public class CachedLookupData
    {
        private readonly IAISDataProvider dataProvider;
        private static readonly MemoryCache cache = MemoryCache.Default;

        public List<FondLink> FondsOverview => LoadFondsOverview();

        public CachedLookupData(IAISDataProvider dataProvider)
        {
            this.dataProvider = dataProvider;
        }

        private List<FondLink> LoadFondsOverview()
        {
            try
            {
                var cacheKey = $"{nameof(CachedLookupData)}.{nameof(LoadFondsOverview)}";
                if (cache.Contains(cacheKey))
                {
                    System.Diagnostics.Debug.WriteLine("Fetch fonds list from cache");
                    return cache.Get(cacheKey) as List<FondLink>;
                }

                var fondLinks = dataProvider.LoadFondLinks().Result;

                cache.Add(cacheKey, fondLinks, new CacheItemPolicy
                {
                    AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(60)),
                    Priority = CacheItemPriority.Default
                });

                return fondLinks;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error while loading the fonds list.");
                return new List<FondLink>();
            }
        }
    }
}