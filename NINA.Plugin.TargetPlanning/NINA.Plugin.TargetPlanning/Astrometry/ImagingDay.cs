using NINA.Astrometry;
using System;
using System.Collections.Generic;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin {

    // Note that NINA has ItemUtility.CalculateTimeAtAltitude() which will return the rise/set/meridian for a target
    // based on location and a target altitude.  However, it seems that it's using some approximation since my testing
    // showed its results differed by minutes (even for the transit time where no refraction could be impacting).  Not
    // clear on the nature of the approximation but for now I'm continuing to use my previous methods (which are
    // certainly more expensive).

    public class ImagingDay {

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public Coordinates Target { get; private set; }

        public ObserverInfo Location { get; private set; }

        public Altitudes SamplePositions { get; private set; }

        private readonly HorizonDefinition horizonDefinition;
        private readonly DSORefiner refiner;

        public ImagingDay(DateTime startDate, DateTime endDate, ObserverInfo location, Coordinates target, HorizonDefinition horizonDefinition) {

            Validate.Assert.notNull(location, "location cannot be null");
            Validate.Assert.notNull(target, "target cannot be null");
            Validate.Assert.notNull(horizonDefinition, "horizonDefinition cannot be null");

            this.StartDate = startDate;
            this.EndDate = endDate;
            this.Target = target;
            this.Location = location;
            this.horizonDefinition = horizonDefinition;

            refiner = new DSORefiner(Location, Target);
            SamplePositions = GetInitialSamplePositions();
        }

        public bool IsEverAboveMinimumAltitude() {
            // Console.WriteLine("TA: {0,6:F2}   AT: {1,6:F2}   AZ: {2,6:F2}", targetAltitude, aat.Altitude, aat.Azimuth);

            foreach (AltitudeAtTime aat in SamplePositions.AltitudeList) {
                double targetAltitude = horizonDefinition.GetTargetAltitude(aat);
                if (aat.Altitude > targetAltitude) {
                    return true;
                }
            }

            return false;
        }

        public DateTime GetRiseAboveMinimumTime() {
            Altitudes startStep = new RiseAboveMinimumFunction(horizonDefinition).determineStep(SamplePositions);
            if (startStep == null) {
                return DateTime.MinValue;
            }

            AltitudeAtTime riseAboveMinimum = new CircumstanceSolver(refiner, 1).FindRiseAboveMinimum(startStep,
                                                                                                      horizonDefinition);
            return riseAboveMinimum != null ? riseAboveMinimum.AtTime : DateTime.MinValue;
        }

        public DateTime GetTransitTime() {

            try {
                // Objects that never rise at location won't have a proper transit
                AltitudeAtTime transit = new CircumstanceSolver(refiner, 1).FindTransit(GetInitialTransitSpan());
                return transit != null ? transit.AtTime : DateTime.MinValue;
            }
            catch (Exception) {
                return DateTime.MinValue;
            }
        }

        public DateTime GetSetBelowMinimumTime() {
            Altitudes startStep = new SetBelowMinimumFunction(horizonDefinition).determineStep(SamplePositions);
            if (startStep == null) {
                return DateTime.MinValue;
            }

            AltitudeAtTime setBelowMinimum = new CircumstanceSolver(refiner, 1).FindSetBelowMinimum(startStep,
                                                                                                    horizonDefinition);
            return setBelowMinimum != null ? setBelowMinimum.AtTime : DateTime.MinValue;
        }

        private Altitudes GetInitialSamplePositions() {
            return GetSamplePositions(StartDate, EndDate);
            /*
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>(2);

            HorizontalCoordinate hz = AstrometryUtils.GetHorizontalCoordinates(Location, Target, StartDate);
            alts.Add(new AltitudeAtTime(hz.Altitude, hz.Azimuth, StartDate));

            hz = AstrometryUtils.GetHorizontalCoordinates(Location, Target, EndDate);
            alts.Add(new AltitudeAtTime(hz.Altitude, hz.Azimuth, EndDate));

            // Sample every 5 minutes
            int numPoints = (int)(EndDate.Subtract(StartDate).TotalMinutes / 5);
            return refiner.Refine(new Altitudes(alts), numPoints);
            */
        }

        private Altitudes GetInitialTransitSpan() {
            DateTime noon = StartDate.Date.AddHours(12);
            return GetSamplePositions(noon, noon.AddDays(1));
            /*
            DateTime noon = StartDate.Date.AddHours(12);
            DateTime next = noon.AddDays(1);
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>(2);

            HorizontalCoordinate hz = AstrometryUtils.GetHorizontalCoordinates(Location, Target, noon);
            alts.Add(new AltitudeAtTime(hz.Altitude, hz.Azimuth, noon));

            hz = AstrometryUtils.GetHorizontalCoordinates(Location, Target, next);
            alts.Add(new AltitudeAtTime(hz.Altitude, hz.Azimuth, next));

            // Sample every 5 minutes
            int numPoints = (int)(next.Subtract(noon).TotalMinutes / 5);
            return refiner.Refine(new Altitudes(alts), numPoints);
            */
        }

        private Altitudes GetSamplePositions(DateTime start, DateTime end) {
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>(2);

            HorizontalCoordinate hz = AstrometryUtils.GetHorizontalCoordinates(Location, Target, start);
            alts.Add(new AltitudeAtTime(hz.Altitude, hz.Azimuth, start));

            hz = AstrometryUtils.GetHorizontalCoordinates(Location, Target, end);
            alts.Add(new AltitudeAtTime(hz.Altitude, hz.Azimuth, end));

            // Sample every 10 minutes
            int numPoints = (int)(end.Subtract(start).TotalMinutes / 10);
            return refiner.Refine(new Altitudes(alts), numPoints);
        }
    }

}
