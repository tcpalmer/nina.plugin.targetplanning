using NINA.Astrometry;
using System;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class AstrometryUtils {

        /// <summary>
        /// Return the horizontal coordinates for the coordinates at the specific location and time.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="coordinates"></param>
        /// <param name="atTime"></param>
        /// <returns>horizontal coordinates</returns>
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

        /// <summary>
        /// Return true if the target object can ever rise above the horizon at the location.  Note that this doesn't
        /// necessarily mean that the target has a rising event (which a circumpolar target would not), just that it
        /// can be above the horizon at some point.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="coordinates"></param>
        /// <returns>true/false</returns>
        public static bool RisesAtLocation(ObserverInfo location, Coordinates coordinates) {
            Validate.Assert.notNull(location, "location cannot be null");
            Validate.Assert.notNull(coordinates, "coordinates cannot be null");

            double declination = coordinates.Dec;
            double latitude = location.Latitude;

            // The object will never rise above the local horizon if dec - lat is less than -90° (observer in Northern Hemisphere),
            // or dec - lat is greater than +90° (observer in Southern Hemisphere)
            return !((declination - latitude) < -90) && !(declination - latitude > 90);
        }

        /// <summary>
        /// Return true if the target object is circumpolar at the location.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="coordinates"></param>
        /// <returns>true/false</returns>
        public static bool CircumpolarAtLocation(ObserverInfo location, Coordinates coordinates) {
            Validate.Assert.notNull(location, "location cannot be null");
            Validate.Assert.notNull(coordinates, "coordinates cannot be null");

            double declination = coordinates.Dec;
            double latitude = location.Latitude;

            // The object is circumpolar if lat + dec is greater than +90° (observer in Northern Hemisphere), or lat + dec is less
            // than -90° (observer in Southern Hemisphere)
            return (latitude + declination) > 90 || latitude + declination < -90;
        }

        /// <summary>
        /// Return true if the target object is still 'circumpolar' (has no rising or setting) when a minimum viewing
        /// altitude is considered.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="coordinates"></param>
        /// <param name="minimumAltitude"></param>
        /// <returns>true/false</returns>
        public static bool CircumpolarAtLocationWithMinimumAltitude(ObserverInfo location, Coordinates coordinates, double minimumAltitude) {
            Validate.Assert.notNull(location, "location cannot be null");
            Validate.Assert.notNull(coordinates, "coordinates cannot be null");
            Validate.Assert.isTrue(minimumAltitude > 0, "minimumAltitude must be > 0");

            double declination = coordinates.Dec;
            double latitude = location.Latitude;

            // The object is 'circumpolar with minimum' if lat + (dec-min) is greater than +90° (observer in Northern Hemisphere),
            // or lat + (dec+min) is less than -90° (observer in Southern Hemisphere)
            return (latitude + (declination - minimumAltitude)) > 90 || (latitude + (declination + minimumAltitude)) < -90;
        }

        /// <summary>
        /// Return true if the location latitude is above the corresponding polar (arctic or antarctic) circle.
        /// </summary>
        /// <param name="location"></param>
        /// <returns>true/false</returns>
        public static bool IsAbovePolarCircle(ObserverInfo location) {
            return location.Latitude >= 66.6 || location.Latitude <= -66.6;
        }

    }

}
