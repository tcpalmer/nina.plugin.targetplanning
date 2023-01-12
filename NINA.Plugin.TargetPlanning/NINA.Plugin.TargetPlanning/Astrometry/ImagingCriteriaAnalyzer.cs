using System;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class ImagingCriteriaAnalyzer {

        public DateTime StartImagingTime { get; private set; }
        public DateTime EndImagingTime { get; private set; }
        public ImagingLimit StartLimitingFactor { get; private set; }
        public ImagingLimit EndLimitingFactor { get; private set; }

        public ImagingCriteriaAnalyzer(DateTime startImagingTime, DateTime endImagingTime) {
            this.StartImagingTime = startImagingTime;
            this.EndImagingTime = endImagingTime;
            this.StartLimitingFactor = ImagingLimit.MinimumAltitude;
            this.EndLimitingFactor = ImagingLimit.MinimumAltitude;
        }

        public bool SessionIsRejected() {
            return StartLimitingFactor.Session;
        }

        public void AdjustForTwilight(DateTime imagingDayStartTime, DateTime imagingDayEndTime) {
            Validate.Assert.isTrue(imagingDayStartTime < imagingDayEndTime, "imagingDayStartTime must be before imagingDayEndTime");

            if (StartImagingTime == DateTime.MinValue) {
                StartImagingTime = imagingDayStartTime;
                StartLimitingFactor = ImagingLimit.Twilight;
            }

            if (EndImagingTime == DateTime.MinValue) {
                EndImagingTime = imagingDayEndTime;
                EndLimitingFactor = ImagingLimit.Twilight;
            }
        }

        public void AdjustForMoonIllumination(double moonIllumination, double maximumMoonIllumination) {

            if (maximumMoonIllumination == double.MinValue) {
                return;
            }

            if (maximumMoonIllumination >= 0 && moonIllumination > maximumMoonIllumination) {
                StartLimitingFactor = ImagingLimit.MoonIllumination;
                EndLimitingFactor = ImagingLimit.MoonIllumination;
            }
        }

        public void AdjustForMoonSeparation(double moonSeparation, double minimumMoonSeparation) {

            if (minimumMoonSeparation == double.MinValue) {
                return;
            }

            if (minimumMoonSeparation >= 0 && moonSeparation < minimumMoonSeparation) {
                StartLimitingFactor = ImagingLimit.MoonSeparation;
                EndLimitingFactor = ImagingLimit.MoonSeparation;
            }
        }

        public double AdjustForMoonAvoidanceSeparation(DateTime atTime, double moonSeparation, double minimumMoonSeparation, int moonAvoidanceWidth) {
            double moonAge = AstrometryUtils.GetMoonAge(atTime);
            double moonAvoidanceSeparation = AstrometryUtils.GetMoonAvoidanceLorentzianSeparation(moonAge, minimumMoonSeparation, moonAvoidanceWidth);

            if (moonSeparation < moonAvoidanceSeparation) {
                StartLimitingFactor = ImagingLimit.MoonAvoidanceSeparation;
                EndLimitingFactor = ImagingLimit.MoonAvoidanceSeparation;
            }

            return moonAvoidanceSeparation;
        }

        public void AdjustForMeridianProximity(DateTime transitTime, long meridianProximityTime) {

            if (transitTime == DateTime.MinValue) {
                throw new ArgumentException("Transit 'clipped' by start/end twilight?  Need to compute ...");
            }

            if (meridianProximityTime == long.MinValue) {
                return;
            }

            // There are eight cases of the timing of start (S), transit (T) and end (E) with respect to the meridian span (M===T===M)
            // Case 1: ------S------M======T======M -> start before the entire span (clip start)
            // Case 2: M======T======M------S------ -> start after the entire span (reject)
            // Case 3: ------M===S===T======M------ -> start in span, before transit (start no change)
            // Case 4: ------M======T===S===M------ -> start in span, after transit (start no change)

            // Case 5: ------E------M======T======M -> end before the entire span (reject)
            // Case 6: M======T======M------E------ -> end after the entire span (clip end)
            // Case 7: ------M===E===T======M------ -> end in span, before transit (end no change)
            // Case 8: ------M======T===E===M------ -> end in span, after transit (end no change)

            long meridianProximitySecs = meridianProximityTime * 60;

            // Case 2: M======T======M------S------ -> start after the entire span (reject)
            if (StartImagingTime > transitTime.AddSeconds(meridianProximitySecs)) {
                //if (StartImagingTime.isAfter(transitTime.plusSeconds(meridianProximitySecs))) {
                StartLimitingFactor = ImagingLimit.Meridian;
                EndLimitingFactor = ImagingLimit.Meridian;
                return;
            }

            // Case 5: ------E------M======T======M -> end before the entire span (reject)
            if (EndImagingTime < transitTime.AddSeconds(-meridianProximitySecs)) {
                //if (EndImagingTime.isBefore(transitTime.minusSeconds(meridianProximitySecs))) {
                StartLimitingFactor = ImagingLimit.Meridian;
                EndLimitingFactor = ImagingLimit.Meridian;
                return;
            }

            // Case 1: ------S------M======T======M -> start before the entire span (clip start)
            long span = (long)transitTime.Subtract(StartImagingTime).TotalSeconds;
            //long span = ChronoUnit.SECONDS.between(startImagingTime, transitTime);
            if (span > meridianProximitySecs) {
                StartImagingTime = transitTime.AddSeconds(-meridianProximitySecs);
                StartLimitingFactor = ImagingLimit.MeridianClip;
            }

            // Case 6: M======T======M------E------ -> end after the entire span (clip end)
            span = (long)EndImagingTime.Subtract(transitTime).TotalSeconds;
            //span = ChronoUnit.SECONDS.between(transitTime, endImagingTime);
            if (span > meridianProximitySecs) {
                EndImagingTime = transitTime.AddSeconds(meridianProximitySecs);
                EndLimitingFactor = ImagingLimit.MeridianClip;
            }

            // Cases 3,4,7,8 (the 'no change' cases) are handled implicitly by the above
        }

        public void AdjustForMinimumImagingTime(long minimumImagingTime) {

            if (minimumImagingTime == long.MinValue) {
                return;
            }

            long span = (long)EndImagingTime.Subtract(StartImagingTime).TotalSeconds;
            if (span < (minimumImagingTime * 60)) {
                StartLimitingFactor = ImagingLimit.MinimumTime;
                EndLimitingFactor = ImagingLimit.MinimumTime;
            }
        }
    }

}
