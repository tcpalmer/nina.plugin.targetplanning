using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using System;
using System.Collections.Generic;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin {
    public class ImagingDay {

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public Coordinates Target { get; private set; }

        public ObserverInfo Location { get; private set; }

        public Altitudes SamplePositions { get; private set; }

        private readonly DSORefiner refiner;

        public ImagingDay(DateTime startDate, DateTime endDate, ObserverInfo location, Coordinates target) {

            Validate.Assert.notNull(location, "location cannot be null");
            Validate.Assert.notNull(target, "target cannot be null");

            this.StartDate = startDate;
            this.EndDate = endDate;
            this.Target = target;
            this.Location = location;

            refiner = new DSORefiner(Location, Target);
            SamplePositions = GetInitialSamplePositions();
        }

        public bool IsEverAboveMinimumAltitude(double minimumAltitude) {
            foreach (AltitudeAtTime aat in SamplePositions.AltitudeList) {
                if (aat.Altitude > minimumAltitude) {
                    return true;
                }
            }

            return false;
        }

        public DateTime GetRiseAboveMinimumTime(double minimumAltitude) {
            Validate.Assert.isTrue(minimumAltitude >= 0, "minimumAltitude must be >= 0");

            Altitudes startStep = new RiseAboveMinimumFunction(minimumAltitude).determineStep(SamplePositions);
            if (startStep == null) {
                return DateTime.MinValue;
            }

            AltitudeAtTime riseAboveMinimum = new CircumstanceSolver(refiner, 1).FindRiseAboveMinimum(startStep,
                                                                                                      minimumAltitude);
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

        public DateTime GetSetBelowMinimumTime(double minimumAltitude) {
            Validate.Assert.isTrue(minimumAltitude >= 0, "minimumAltitude must be >= 0");

            Altitudes startStep = new SetBelowMinimumFunction(minimumAltitude).determineStep(SamplePositions);
            if (startStep == null) {
                return DateTime.MinValue;
            }

            AltitudeAtTime setBelowMinimum = new CircumstanceSolver(refiner, 1).FindSetBelowMinimum(startStep,
                                                                                                    minimumAltitude);
            return setBelowMinimum != null ? setBelowMinimum.AtTime : DateTime.MinValue;
        }

        private Altitudes GetInitialSamplePositions() {
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>(2);

            HorizontalCoordinate hz = AstrometryUtils.GetHorizontalCoordinates(Location, Target, StartDate);
            alts.Add(new AltitudeAtTime(hz.Altitude, StartDate));

            hz = AstrometryUtils.GetHorizontalCoordinates(Location, Target, EndDate);
            alts.Add(new AltitudeAtTime(hz.Altitude, EndDate));

            // Sample every 5 minutes
            int numPoints = (int)(EndDate.Subtract(StartDate).TotalMinutes / 5);
            return refiner.Refine(new Altitudes(alts), numPoints);
        }

        private Altitudes GetInitialTransitSpan() {
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>(3);
            int size = SamplePositions.AltitudeList.Count;

            alts.Add(SamplePositions.AltitudeList[0]);
            alts.Add(SamplePositions.AltitudeList[size / 2]);
            alts.Add(SamplePositions.AltitudeList[size - 1]);

            return new Altitudes(alts);
        }
    }

}
