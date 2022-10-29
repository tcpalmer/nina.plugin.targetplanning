using Accord.Math.Random;
using NINA.Astrometry.Interfaces;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TargetPlanning.NINAPlugin.Astrometry {

    /// <summary>
    /// DSORefiner generates altitudes and times from the equatorial coordinates of a target, a geographic
    /// location, and a date.  Provided coordinates should represent a deep space object or a star - not a
    /// solar system object.
    /// </summary>
    public class DSORefiner : IAltitudeRefiner {

        private ObserverInfo location;
        private IDeepSkyObject target;

        public DSORefiner(ObserverInfo location, IDeepSkyObject target) {
            Validate.Assert.notNull(location, "location cannot be null");
            Validate.Assert.notNull(target, "target cannot be null");

            this.location = location;
            this.target = target;
        }

        Altitudes IAltitudeRefiner.refine(Altitudes altitudes, int numPoints) {

            Validate.Assert.notNull(altitudes, "altitudes cannot be null");
            Validate.Assert.isTrue(altitudes.AltitudeList.Count == 2, "altitudes must have exactly two elements");
            Validate.Assert.isTrue(numPoints > 0, "numPoints must be >= 1");

            AltitudeAtTime start = altitudes.AltitudeList[0];
            AltitudeAtTime end = altitudes.AltitudeList[1];

            List<AltitudeAtTime> newAltitudes = new List<AltitudeAtTime>(numPoints+2);
            newAltitudes.Add(start);

            TimeSpan span = start.AtTime.Subtract(end.AtTime);
            int timeDiffSpanMS = span.Milliseconds;
            int timeIncrement = timeDiffSpanMS / (numPoints + 1);

            DateTime atTime = start.AtTime;
            for (int i = 0; i < numPoints; i++) {
                atTime = atTime.AddMilliseconds(timeIncrement);
            }

            /*

        ZonedDateTime atTime = start.getAtTime();
        for (int i = 0; i < numPoints; i++) {
            atTime = atTime.plus(timeIncrement, ChronoUnit.MILLIS);
            HorizontalCoordinate hz = Astrometry.computeLocalPosition(location, atTime, target, quality);
            newAltitudes.add(new AltitudeAtTime(hz.getAltitude()
                                                        .getAngle(), atTime));
        }

        newAltitudes.add(end);
        return new Altitudes(newAltitudes);
             */

            return null;
        }

        Altitudes IAltitudeRefiner.getHourlyAltitudesForDay(DateTime date) {
            throw new NotImplementedException();
        }

        bool IAltitudeRefiner.risesAtLocation() {
            throw new NotImplementedException();
        }

        bool IAltitudeRefiner.circumpolarAtLocation() {
            throw new NotImplementedException();
        }

        private double getAltitude(DateTime atTime) {


            return 0;
        }
    }

    /*
    @Override
    public Altitudes refine(Altitudes altitudes, int numPoints) {

        AltitudeAtTime start = altitudes.getAltitudes()
                .get(0);
        AltitudeAtTime end = altitudes.getAltitudes()
                .get(1);

        List<AltitudeAtTime> newAltitudes = new ArrayList<>(numPoints + 2);
        newAltitudes.add(start);

        long timeDiffSpanMS = ChronoUnit.MILLIS.between(start.getAtTime(), end.getAtTime());
        long timeIncrement = timeDiffSpanMS / (numPoints + 1);

        ZonedDateTime atTime = start.getAtTime();
        for (int i = 0; i < numPoints; i++) {
            atTime = atTime.plus(timeIncrement, ChronoUnit.MILLIS);
            HorizontalCoordinate hz = Astrometry.computeLocalPosition(location, atTime, target, quality);
            newAltitudes.add(new AltitudeAtTime(hz.getAltitude()
                                                        .getAngle(), atTime));
        }

        newAltitudes.add(end);
        return new Altitudes(newAltitudes);
    }

    @Override
    public Altitudes getHourlyAltitudesForDay(ZonedDateTime date) {
        Validate.notNull(date, "date cannot be null");

        List<AltitudeAtTime> alts = new ArrayList<>(24);
        ZonedDateTime dateTime = date.truncatedTo(ChronoUnit.DAYS);

        for (int i = 0; i < 24; i++) {
            HorizontalCoordinate hz = Astrometry.computeLocalPosition(location, dateTime, target, quality);
            alts.add(new AltitudeAtTime(hz.getAltitude()
                                                .getAngle(), dateTime));
            dateTime = dateTime.plusHours(1);
        }

        return new Altitudes(alts);
    }

    @Override
    public boolean risesAtLocation() {
        return Utils.risesAtLocation(target, location);
    }

    @Override
    public boolean circumpolarAtLocation() {
        return Utils.circumpolarAtLocation(target, location);
    }
     */
}
