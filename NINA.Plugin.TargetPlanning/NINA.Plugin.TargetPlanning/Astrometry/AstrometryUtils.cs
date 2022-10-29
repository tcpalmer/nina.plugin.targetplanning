using NINA.Astrometry.Interfaces;
using NINA.Astrometry;
using System;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class AstrometryUtils {

        public static HorizontalCoordinate GetHorizontalCoordinates(ObserverInfo location, Coordinates coordinates, DateTime atTime) {
            Validate.Assert.notNull(location, "location cannot be null");
            Validate.Assert.notNull(coordinates, "coordinates cannot be null");

            double siderealTime = AstroUtil.GetLocalSiderealTime(atTime, location.Longitude);
            double hourAngle = AstroUtil.GetHourAngle(siderealTime, coordinates.RA);
            double degAngle = AstroUtil.HoursToDegrees(hourAngle);

            double altitude = AstroUtil.GetAltitude(degAngle, location.Latitude, coordinates.Dec);
            double azimuth = AstroUtil.GetAzimuth(degAngle, altitude, location.Latitude, coordinates.Dec);

            return new HorizontalCoordinate(altitude, azimuth);
        }
    }

}
