using NINA.Astrometry;
using NINA.Astrometry.RiseAndSet;
using System;
using System.Globalization;
using System.Runtime.Caching;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class TwilightTimeCache {

        private static readonly TimeSpan ITEM_TIMEOUT = TimeSpan.FromHours(2);
        private static readonly MemoryCache _cache = new MemoryCache("Target Planning Twilight Times");

        public static RiseAndSetEvent Get(DateTime dateTime, double latitude, double longitude) {
            DateTime checkDate = dateTime.Date;
            string key = GetCacheKey(checkDate, latitude, longitude);
            RiseAndSetEvent riseAndSetEvent = (RiseAndSetEvent)_cache.Get(key);

            if (riseAndSetEvent == null) {
                //Logger.Trace($"twilight time cache miss: {key}");
                riseAndSetEvent = AstroUtil.GetNauticalNightTimes(checkDate, latitude, longitude);
                _cache.Add(key, riseAndSetEvent, DateTime.Now.Add(ITEM_TIMEOUT));
            }
            else {
                //Logger.Trace($"twilight time cache hit: {key}");
            }

            return riseAndSetEvent;
        }

        private static string GetCacheKey(DateTime dateTime, double latitude, double longitude) {
            return $"{dateTime:yyyy-MM-dd-HH-mm-ss}_{latitude.ToString("0.000000", CultureInfo.InvariantCulture)}_{longitude.ToString("0.000000", CultureInfo.InvariantCulture)}";
        }
    }

}
