﻿using NINA.Astrometry;
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
        private readonly HorizonDefinition horizonDefinition;

        public DateTime RiseAboveMinimumTime { get; private set; }
        public DateTime TransitTime { get; private set; }
        public DateTime SetBelowMinimumTime { get; private set; }

        public TargetImagingCircumstances(ObserverInfo location, Coordinates target, DateTime startTime, DateTime endTime, HorizonDefinition horizonDefinition) {

            Validate.Assert.notNull(location, "location cannot be null");
            Validate.Assert.notNull(target, "target cannot be null");
            Validate.Assert.isTrue(startTime < endTime, "startTime must be before endTime");

            this.location = location;
            this.target = target;
            this.startTime = startTime;
            this.endTime = endTime;
            this.horizonDefinition = horizonDefinition;
        }

        public int Analyze() {

            // If the target never rises at this location, stop
            if (!AstrometryUtils.RisesAtLocation(location, target)) {
                return STATUS_NEVER_VISIBLE;
            }

            ImagingDay imagingDay = new ImagingDay(startTime, endTime, location, target, horizonDefinition);

            // If the target never rises above the minimum altitude in this time span, stop
            if (!imagingDay.IsEverAboveMinimumAltitude()) {
                return STATUS_NEVER_ABOVE_MINIMUM_ALTITUDE;
            }

            // Determine the applicable circumstances
            RiseAboveMinimumTime = imagingDay.GetRiseAboveMinimumTime();
            TransitTime = imagingDay.GetTransitTime();
            SetBelowMinimumTime = imagingDay.GetSetBelowMinimumTime();

            return STATUS_POTENTIALLY_VISIBLE;
        }
    }

}
