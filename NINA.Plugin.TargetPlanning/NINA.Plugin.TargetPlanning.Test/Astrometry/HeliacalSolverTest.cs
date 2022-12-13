using FluentAssertions;
using NINA.Astrometry;
using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Threading;
using TargetPlanning.NINAPlugin.Astrometry;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    public class HeliacalSolverTest {

        private CancellationTokenSource tokenSource;

        [SetUp]
        public void SetUp() {
            Logger.SetLogLevel(Core.Enum.LogLevelEnum.TRACE);
            tokenSource = new CancellationTokenSource();
        }

        [TearDown]
        public void TearDown() {
            Logger.SetLogLevel(Core.Enum.LogLevelEnum.INFO);
            tokenSource.Dispose();
        }

        [Test]
        public void testGetHeliacalRisingDate() {

            ObserverInfo location = TestUtil.TEST_LOCATION_1;
            Coordinates target = TestUtil.SPICA;
            DateTime transitMidnight = AstrometryUtils.GetMidnightTransitDate(location, target, 2022, tokenSource.Token);
            HeliacalSolver solver = new HeliacalSolver(location, target, transitMidnight, TwilightTimeCache.TWILIGHT_INCLUDE_ASTRO);

            DateTime hr = solver.GetHeliacalRisingDate(tokenSource.Token);
            hr.Month.Should().Be(11);
            hr.Day.Should().Be(7);

            DateTime hs = solver.GetHeliacalSettingDate(tokenSource.Token);
            hs.Month.Should().Be(9);
            hs.Day.Should().Be(5);

            target = TestUtil.BETELGEUSE;
            transitMidnight = AstrometryUtils.GetMidnightTransitDate(location, target, 2022, tokenSource.Token);
            solver = new HeliacalSolver(location, target, transitMidnight, TwilightTimeCache.TWILIGHT_INCLUDE_ASTRO);

            hr = solver.GetHeliacalRisingDate(tokenSource.Token);
            hr.Month.Should().Be(7);
            hr.Day.Should().Be(24);

            hs = solver.GetHeliacalSettingDate(tokenSource.Token);
            hs.Month.Should().Be(5);
            hs.Day.Should().Be(16);

            target = TestUtil.M42;
            transitMidnight = AstrometryUtils.GetMidnightTransitDate(location, target, 2022, tokenSource.Token);
            solver = new HeliacalSolver(location, target, transitMidnight, TwilightTimeCache.TWILIGHT_INCLUDE_ASTRO);

            hr = solver.GetHeliacalRisingDate(tokenSource.Token);
            hr.Month.Should().Be(7);
            hr.Day.Should().Be(28);

            hs = solver.GetHeliacalSettingDate(tokenSource.Token);
            hs.Month.Should().Be(5);
            hs.Day.Should().Be(5);
        }

        [Test]
        [TestCase(80, 80, 90)]
        [TestCase(-80, -80, -90)]
        [TestCase(35, 15, 50)]
        [TestCase(35, 19, 50)]
        [TestCase(35, 21, 60)]
        [TestCase(0, 4, 0)]
        [TestCase(0, -4, 0)]
        [TestCase(-35, -15, -50)]
        [TestCase(-35, -19, -50)]
        [TestCase(-35, -21, -60)]
        public void testGetMappedConstant(double latitude, double dec, int expected) {
            ObserverInfo location = new ObserverInfo();
            location.Latitude = latitude;
            location.Longitude = -80;
            Coordinates target = new Coordinates(0, dec, Epoch.J2000, Coordinates.RAType.Degrees);

            DateTime transitMidnight = AstrometryUtils.GetMidnightTransitDate(location, target, 2022, tokenSource.Token);
            HeliacalSolver solver = new HeliacalSolver(location, target, transitMidnight, TwilightTimeCache.TWILIGHT_INCLUDE_ASTRO);
            solver.GetMappedConstant().Should().Be(expected);
        }

        /*
        //[Test]
        public void testGenConstantSheets() {

            ObserverInfo location = new ObserverInfo();
            location.Latitude = -35;
            location.Longitude = -80;
            location.Elevation = 0;

            for (int constant = 90; constant >= -90; constant -= 10) {

                TestContext.WriteLine($"\n--- CONSTANT {constant} ------------------------------------------\n");

                StringBuilder sb = new StringBuilder();
                sb.Append("CONST,DEC,LAT,HR,HS,HR Off,HS Off\n");

                for (int dec = 90; dec >= -90; dec -= 5) {
                    double lat = constant - dec;

                    Coordinates coords = new Coordinates(AstroUtil.HMSToDegrees("0:0:0"),
                                                AstroUtil.DMSToDegrees(string.Format("{0}:0:0", dec)),
                                                Epoch.J2000, Coordinates.RAType.Degrees);

                    //TestContext.WriteLine($"{dec} {lat}");
                    location.Latitude = lat;

                    if (lat > 66 || lat < -66) {
                        TestContext.WriteLine($"dec={dec} lat={lat} skipping: APC");
                        continue;
                    }

                    if (!AstrometryUtils.RisesAtLocation(location, coords)) {
                        TestContext.WriteLine($"dec={dec} lat={lat} skipping: NV");
                        continue;
                    }

                    if (AstrometryUtils.CircumpolarAtLocation(location, coords)) {
                        TestContext.WriteLine($"at dec={dec} lat={lat} skipping: CP");
                        continue;
                    }

                    try {
                        DateTime tt = AstrometryUtils.GetMidnightTransitDate(location, coords, 2022, tokenSource.Token);
                        HeliacalSolver solver = new HeliacalSolver(location, coords);

                        DateTime guessHR = tt.AddMonths(-6);
                        DateTime hr = solver.GetHeliacalRisingDate(guessHR);

                        DateTime guessHS = tt.AddMonths(6);
                        DateTime hs = solver.GetHeliacalSettingDate(guessHS);

                        TestContext.WriteLine($"{dec},{lat},{tt:MM/dd/yyyy},{hr:MM/dd/yyyy},{hs:MM/dd/yyyy},{(hr - tt).Days},{(hs - tt).Days}");
                        sb.Append($"{constant},{dec},{lat},{hr:MM/dd/yyyy},{hs:MM/dd/yyyy},{(hr - tt).Days},{(hs - tt).Days}\n");
                    }
                    catch (Exception e) {
                        TestContext.WriteLine(e.Message);
                    }
                }

                File.WriteAllText($"G:\\Photography\\Astrophotography\\Notes\\C{constant}.csv", sb.ToString());
            }

            throw new Exception("oops");
        }*/

        /*
        //[Test]
        public void testGenConstantRange() {
            ObserverInfo location = new ObserverInfo();
            location.Latitude = -35;
            location.Longitude = -80;
            location.Elevation = 0;


            TestContext.Write($",");
            for (int dec = 90; dec >= -90; dec--) {
                TestContext.Write($"{dec},");
            }

            TestContext.WriteLine("");

            for (int lat = 90; lat >= -90; lat--) {
                TestContext.Write($"{lat},");

                for (int dec = 90; dec >= -90; dec--) {

                    double constant = dec + lat;
                    location.Latitude = lat;
                    Coordinates coords = new Coordinates(AstroUtil.HMSToDegrees("0:0:0"),
                                                AstroUtil.DMSToDegrees(String.Format("{0}:0:0", dec)),
                                                Epoch.J2000, Coordinates.RAType.Degrees);

                    String display = constant.ToString();

                    if (AstrometryUtils.IsAbovePolarCircle(location)) {
                        display = "APC";
                    }
                    else if (!AstrometryUtils.RisesAtLocation(location, coords)) {
                        display = "NV";
                    }
                    else if (AstrometryUtils.CircumpolarAtLocation(location, coords)) {
                        display = "CP";
                    }

                    TestContext.Write($"{display},");
                }

                TestContext.WriteLine("");
            }

            throw new Exception("oops");
        }*/
    }

}
