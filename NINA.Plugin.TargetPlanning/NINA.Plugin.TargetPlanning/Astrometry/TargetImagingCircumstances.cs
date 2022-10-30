using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using System;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class TargetImagingCircumstances {

        public readonly static int STATUS_POTENTIALLY_VISIBLE = 1;

        public readonly static int STATUS_NEVER_VISIBLE = 2;

        public readonly static int STATUS_NEVER_ABOVE_MINIMUM_ALTITUDE = 3;

        private ObserverInfo location;
        private IDeepSkyObject target;
        private DateTime startTime;
        private DateTime endTime;
        private double minimumAltitude;

        public DateTime RiseAboveMinimumTime { get; private set; }
        public DateTime TransitTime { get; private set; }
        public DateTime SetBelowMinimumTime { get; private set; }

        public TargetImagingCircumstances(ObserverInfo location, IDeepSkyObject target, DateTime startTime, DateTime endTime, double minimumAltitude) {

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

        public int analyze() {

            // If the target never rises at this location, stop
            if (!AstrometryUtils.RisesAtLocation(location, target.Coordinates)) {
                return STATUS_NEVER_VISIBLE;
            }

            ImagingDay imagingDay = new ImagingDay(startTime, endTime, location, target.Coordinates);

            // If the target never rises above the minimum altitude in this time span, stop
            if (!imagingDay.IsEverAboveMinimumAltitude(minimumAltitude)) {
                return STATUS_NEVER_ABOVE_MINIMUM_ALTITUDE;
            }

            /*



            // TODO: once we add transit-based imaging determination, we'll need to get the actual transit,
            //   even if not inside the time span.  We can just calculate altitude going in the direction of
            //   increasing altitude until it decreases.
            //   This should be a separate method to call only if needed.

            // Determine the applicable circumstances
            riseAboveMinimumTime = imagingDay.getRiseAboveMinimumTime(minimumAltitude);
            transitTime = imagingDay.getTransitTime();
            setBelowMinimumTime = imagingDay.getSetBelowMinimumTime(minimumAltitude);

            return STATUS_POTENTIALLY_VISIBLE;
             */
            return 0;
        }
    }

}
