using IplanHEE.BuildingBlocks.Domain.CacheStores;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace IplanHEE.BuildingBlocks.Infrastructure.CacheStores
{
    public class MemoryCacheStore : ICacheStore
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheStore(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }


        public void Add<TItem>(List<TItem> item, ICacheKey<TItem> key) where TItem : class
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.NeverRemove
            };
            this._memoryCache.Set(key.CacheKey, item, cacheOptions);
        }

        public List<TItem> Get<TItem>(ICacheKey<TItem> key) where TItem : class
        {
            if (this._memoryCache.TryGetValue(key.CacheKey, out List<TItem> value))
            {
                return value;
            }

            return null;
        }

        public void Remove<TItem>(ICacheKey<TItem> key) where TItem : class
        {
            this._memoryCache.Remove(key.CacheKey);
        }
    }
}
