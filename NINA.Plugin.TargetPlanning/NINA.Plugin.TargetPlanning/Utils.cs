using System;

namespace TargetPlanning.NINAPlugin {

    public class Utils {

        public static string MtoHM(int minutes) {
            decimal hours = Math.Floor((decimal)minutes / 60);
            int min = minutes % 60;
            return $"{hours}h {min}m";
        }

        public static DateTime GetMidpointTime(DateTime startTime, DateTime endTime) {
            long span = (long)endTime.Subtract(startTime).TotalSeconds;
            return startTime.AddSeconds(span / 2);
        }
    }

}
