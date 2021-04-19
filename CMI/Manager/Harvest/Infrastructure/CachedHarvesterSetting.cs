using System;
using System.Runtime.Caching;
using CMI.Contract.Parameter;

namespace CMI.Manager.Harvest.Infrastructure
{
    public class CachedHarvesterSetting : ICachedHarvesterSetting
    {
        private readonly MemoryCache cache = MemoryCache.Default;
        private readonly IParameterHelper parameterHelper;

        public CachedHarvesterSetting(IParameterHelper parameterHelper)
        {
            this.parameterHelper = parameterHelper;
        }

        public bool EnableFullResync()
        {
            if (cache.Contains(nameof(HarvesterSettings.EnableFullResync)))
            {
                return Convert.ToBoolean(cache.Get(nameof(HarvesterSettings.EnableFullResync)));
            }

            var enableFullResync = parameterHelper.GetSetting<HarvesterSettings>().EnableFullResync;
            cache.Add(nameof(HarvesterSettings.EnableFullResync), enableFullResync, new CacheItemPolicy
            {
                AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(20)),
                Priority = CacheItemPriority.Default
            });

            return enableFullResync;
        }
    }
}