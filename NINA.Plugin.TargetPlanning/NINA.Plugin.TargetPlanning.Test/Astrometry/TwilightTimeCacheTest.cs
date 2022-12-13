using NINA.Astrometry.RiseAndSet;
using NUnit.Framework;
using System;
using TargetPlanning.NINAPlugin.Astrometry;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    internal class TwilightTimeCacheTest {

        [Test]
        public void testTimes() {
            DateTime dt = new DateTime(2022, 12, 12);

            RiseAndSetEvent none = TwilightTimeCache.Get(dt, TestUtil.TEST_LOCATION_1.Latitude, TestUtil.TEST_LOCATION_1.Longitude, TwilightTimeCache.TWILIGHT_INCLUDE_NONE);
            RiseAndSetEvent astro = TwilightTimeCache.Get(dt, TestUtil.TEST_LOCATION_1.Latitude, TestUtil.TEST_LOCATION_1.Longitude, TwilightTimeCache.TWILIGHT_INCLUDE_ASTRO);
            RiseAndSetEvent nautical = TwilightTimeCache.Get(dt, TestUtil.TEST_LOCATION_1.Latitude, TestUtil.TEST_LOCATION_1.Longitude, TwilightTimeCache.TWILIGHT_INCLUDE_NAUTICAL);
            RiseAndSetEvent civil = TwilightTimeCache.Get(dt, TestUtil.TEST_LOCATION_1.Latitude, TestUtil.TEST_LOCATION_1.Longitude, TwilightTimeCache.TWILIGHT_INCLUDE_CIVIL);

            TestUtil.AssertTime(dt, (DateTime)none.Rise, 5, 44, 3);
            TestUtil.AssertTime(dt, (DateTime)none.Set, 18, 35, 21);
            TestUtil.AssertTime(dt, (DateTime)astro.Rise, 6, 14, 44);
            TestUtil.AssertTime(dt, (DateTime)astro.Set, 18, 4, 34);
            TestUtil.AssertTime(dt, (DateTime)nautical.Rise, 6, 46, 22);
            TestUtil.AssertTime(dt, (DateTime)nautical.Set, 17, 33, 5);
            TestUtil.AssertTime(dt, (DateTime)civil.Rise, 7, 14, 35);
            TestUtil.AssertTime(dt, (DateTime)civil.Set, 17, 4, 55);
        }

    }

}
