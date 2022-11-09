using NINA.Core.Model;
using System;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class HorizonDefinition {

        private readonly double minimumAltitude;
        private readonly CustomHorizon horizon;
        private readonly double offset;

        public HorizonDefinition(double minimumAltitude) {
            Validate.Assert.isTrue(minimumAltitude >= 0 && minimumAltitude < 90, "minimumAltitude must be >= 0 and < 90");
            this.minimumAltitude = minimumAltitude;
        }

        public HorizonDefinition(CustomHorizon customHorizon, double offset) {
            Validate.Assert.notNull(customHorizon, "customHorizon cannot be null");
            Validate.Assert.isTrue(offset >= 0, "offset must be >= 0");

            this.minimumAltitude = TargetPlanningPlugin.HORIZON_VALUE;
            this.horizon = customHorizon;
            this.offset = offset;
        }

        public double GetTargetAltitude(AltitudeAtTime aat) {
            if (minimumAltitude != TargetPlanningPlugin.HORIZON_VALUE) {
                return minimumAltitude;
            }

            return horizon.GetAltitude(aat.Azimuth) + offset;
        }

        public bool IsCustom() {
            return minimumAltitude == TargetPlanningPlugin.HORIZON_VALUE;
        }

        public double GetFixedMinimumAltitude() {
            if (IsCustom()) {
                throw new ArgumentException("minimumAltitude n/a in this context");
            }

            return this.minimumAltitude;
        }
    }

}
