using FluentAssertions;
using NINA.Astrometry;
using NUnit.Framework;
using System;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    public class TestUtil {

        public static readonly ObserverInfo TEST_LOCATION_1, TEST_LOCATION_2, TEST_LOCATION_3;

        public static readonly Coordinates BETELGEUSE = new Coordinates(AstroUtil.HMSToDegrees("5:55:11"), AstroUtil.DMSToDegrees("7:24:30"), Epoch.J2000, Coordinates.RAType.Degrees);

        public static readonly Coordinates SPICA = new Coordinates(AstroUtil.HMSToDegrees("13:26:25.92"), AstroUtil.DMSToDegrees("-11:17:2.6"), Epoch.J2000, Coordinates.RAType.Degrees);

        public static readonly Coordinates STAR_NORTH_CIRCP = new Coordinates(AstroUtil.HMSToDegrees("0:0:0"), AstroUtil.DMSToDegrees("80:0:0"), Epoch.J2000, Coordinates.RAType.Degrees);

        public static readonly Coordinates STAR_SOUTH_CIRCP = new Coordinates(AstroUtil.HMSToDegrees("0:0:0"), AstroUtil.DMSToDegrees("-80:0:0"), Epoch.J2000, Coordinates.RAType.Degrees);

        static TestUtil() {

            // Northern hemisphere
            TEST_LOCATION_1 = new ObserverInfo();
            TEST_LOCATION_1.Latitude = 35;
            TEST_LOCATION_1.Longitude = -79;
            TEST_LOCATION_1.Elevation = 165;

            // Southern hemisphere
            TEST_LOCATION_2 = new ObserverInfo();
            TEST_LOCATION_2.Latitude = -35;
            TEST_LOCATION_2.Longitude = -80;
            TEST_LOCATION_2.Elevation = 165;

            // Northern hemisphere, above artic circle
            TEST_LOCATION_3 = new ObserverInfo();
            TEST_LOCATION_3.Latitude = 67;
            TEST_LOCATION_3.Longitude = -80;
            TEST_LOCATION_3.Elevation = 165;
        }

        public static void AssertTime(DateTime expected, DateTime actual, int hours, int minutes, int seconds) {
            DateTime edt = expected.Date.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);
            DateTime adt = new DateTime(actual.Year, actual.Month, actual.Day, actual.Hour, actual.Minute, actual.Second);

            bool cond = (edt == adt);
            if (!cond) {
                TestContext.WriteLine($"assertTime failed:");
                TestContext.WriteLine($"  expected: {edt}");
                TestContext.WriteLine($"  actual:   {adt}");
            }

            cond.Should().BeTrue();
        }

    }

}
