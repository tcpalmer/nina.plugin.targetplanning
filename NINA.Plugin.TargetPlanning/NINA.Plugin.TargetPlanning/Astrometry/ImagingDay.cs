using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using System;

namespace TargetPlanning.NINAPlugin {
    public class ImagingDay {

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public IDeepSkyObject Target { get; private set; }

        public ObserverInfo Location { get; private set; }

        public ImagingDay(DateTime startDate, DateTime endDate, ObserverInfo location, IDeepSkyObject target) {

            Validate.Assert.notNull(location, "location cannot be null");
            Validate.Assert.notNull(target, "target cannot be null");

            this.StartDate = startDate;
            this.EndDate = endDate;
            this.Target = target;
            this.Location = location;
        }

        /*
    public boolean isEverAboveMinimumAltitude(double minimumAltitude) {

        for (AltitudeAtTime aat : samplePositions.getAltitudes()) {
            if (aat.getAltitude() > minimumAltitude) {
                return true;
            }
        }

        return false;
    }

    public ZonedDateTime getRiseAboveMinimumTime(double minimumAltitude) {
        Validate.isTrue(minimumAltitude >= 0, "minimumAltitude must be >= 0");

        Altitudes startStep = new Solver.RiseAboveMinimumFunction(minimumAltitude).determineStep(samplePositions);
        if (startStep == null) {
            return null;
        }

        AltitudeAtTime riseAboveMinimum = new CircumstanceSolver(refiner, 1).findRiseAboveMinimum(startStep,
                                                                                                  minimumAltitude);
        return riseAboveMinimum != null ? riseAboveMinimum.getAtTime() : null;
    }

    public ZonedDateTime getTransitTime() {

        // TODO: once we add transit-based imaging determination, we'll need to get the actual transit,
        //   even if not inside the time span.  We can just calculate altitude going in the direction of
        //   increasing altitude until it decreases.
        //   This should be a separate method to call only if needed.

        try {
            // Objects that never rise at location won't have a proper transit
            AltitudeAtTime transit = new CircumstanceSolver(refiner, 1).findTransit(getInitialTransitSpan());
            return transit != null ? transit.getAtTime() : null;
        } catch (Exception e) {
            return null;
        }
    }

    public ZonedDateTime getSetBelowMinimumTime(double minimumAltitude) {
        Validate.isTrue(minimumAltitude >= 0, "minimumAltitude must be >= 0");

        Altitudes startStep = new Solver.SetBelowMinimumFunction(minimumAltitude).determineStep(samplePositions);
        if (startStep == null) {
            return null;
        }

        AltitudeAtTime setBelowMinimum = new CircumstanceSolver(refiner, 1).findSetBelowMinimum(startStep,
                                                                                                minimumAltitude);

        return setBelowMinimum != null ? setBelowMinimum.getAtTime() : null;
    }

    public Altitudes getSamplePositions() {
        return samplePositions;
    }

    private Altitudes getInitialSamplePositions() {
        List<AltitudeAtTime> alts = new ArrayList<>(2);

        HorizontalCoordinate hz = Astrometry.computeLocalPosition(location, startDateTime, target, quality);
        alts.add(new AltitudeAtTime(hz.getAltitude()
                                            .getAngle(), startDateTime));

        hz = Astrometry.computeLocalPosition(location, endDateTime, target, quality);
        alts.add(new AltitudeAtTime(hz.getAltitude()
                                            .getAngle(), endDateTime));

        // Sample every 5 minutes
        int numPoints = (int) ChronoUnit.MINUTES.between(startDateTime, endDateTime) / 5;

        return refiner.refine(new Altitudes(alts), numPoints);
    }

    private Altitudes getInitialTransitSpan() {
        List<AltitudeAtTime> alts = new ArrayList<>(3);
        alts.add(samplePositions.getAltitudes()
                         .get(0));
        alts.add(samplePositions.getAltitudes()
                         .get(samplePositions.getSize() / 2));
        alts.add(samplePositions.getAltitudes()
                         .get(samplePositions.getSize() - 1));
        return new Altitudes(alts);
    }
         */
    }
}
