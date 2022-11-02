using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using System;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class TargetImagingCircumstances {

        public readonly static int STATUS_POTENTIALLY_VISIBLE = 1;

        public readonly static int STATUS_NEVER_VISIBLE = 2;

        public readonly static int STATUS_NEVER_ABOVE_MINIMUM_ALTITUDE = 3;

        private readonly ObserverInfo location;
        private readonly Coordinates target;
        private readonly DateTime startTime;
        private readonly DateTime endTime;
        private readonly double minimumAltitude;

        public DateTime RiseAboveMinimumTime { get; private set; }
        public DateTime TransitTime { get; private set; }
        public DateTime SetBelowMinimumTime { get; private set; }

        public TargetImagingCircumstances(ObserverInfo location, Coordinates target, DateTime startTime, DateTime endTime, double minimumAltitude) {

            Validate.Assert.notNull(location, "location cannot be null");
            Validate.Assert.notNull(target, "target cannot be null");

            Validate.Assert.isTrue(startTime < endTime, "startTime must be before endTime");
            Validate.Assert.isTrue(minimumAltitude >= 0, "minimumAltitude must be >= 0");

            this.location = location;
            this.target = target;
            this.startTime = startTime;
            this.endTime = endTime;
            this.minimumAltitude = minimumAltitude;
        }

        public int Analyze() {

            // If the target never rises at this location, stop
            if (!AstrometryUtils.RisesAtLocation(location, target)) {
                return STATUS_NEVER_VISIBLE;
            }

            ImagingDay imagingDay = new ImagingDay(startTime, endTime, location, target);

            // If the target never rises above the minimum altitude in this time span, stop
            if (!imagingDay.IsEverAboveMinimumAltitude(minimumAltitude)) {
                return STATUS_NEVER_ABOVE_MINIMUM_ALTITUDE;
            }

            // Determine the applicable circumstances
            RiseAboveMinimumTime = imagingDay.GetRiseAboveMinimumTime(minimumAltitude);
            TransitTime = imagingDay.GetTransitTime();
            SetBelowMinimumTime = imagingDay.GetSetBelowMinimumTime(minimumAltitude);

            return STATUS_POTENTIALLY_VISIBLE;
        }
    }

}
