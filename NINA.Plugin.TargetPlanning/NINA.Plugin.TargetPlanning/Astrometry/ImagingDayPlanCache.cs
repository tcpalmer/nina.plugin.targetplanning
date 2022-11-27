using System;
using System.Runtime.Caching;
using System.Text;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class ImagingDayPlanCache {

        private static readonly TimeSpan ITEM_TIMEOUT = TimeSpan.FromHours(2);
        private static readonly MemoryCache _cache = new MemoryCache("Target Planning ImagingDayPlan");

        public ImagingDayPlan Get(DateTime start, DateTime end, PlanParameters planParameters) {
            string key = GetCacheKey(start, end, planParameters);

            ImagingDayPlan plan = (ImagingDayPlan)_cache.Get(key);
            if (plan != null) {
                //Logger.Trace($"ImagingDayPlanCache hit:  {key}");
                return plan;
            }
            else {
                //Logger.Trace($"ImagingDayPlanCache miss: {key}");
                return null;
            }
        }

        public void Put(ImagingDayPlan plan, DateTime start, DateTime end, PlanParameters planParameters) {
            _cache.Add(GetCacheKey(start, end, planParameters), plan, DateTime.Now.Add(ITEM_TIMEOUT));
        }

        public static string GetCacheKey(DateTime start, DateTime end, PlanParameters planParameters) {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{start:yyyy-MM-dd-HH-mm-ss}_");
            sb.Append($"{end:yyyy-MM-dd-HH-mm-ss}_");
            sb.Append(planParameters.GetCacheKey());
            return sb.ToString();
        }
    }

}
