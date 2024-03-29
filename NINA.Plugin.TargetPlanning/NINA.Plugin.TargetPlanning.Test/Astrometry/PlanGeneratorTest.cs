﻿using NINA.Astrometry;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using TargetPlanning.NINAPlugin.Astrometry;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    public class PlanGeneratorTest {

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

        //[Test]
        public void testSkipDayBug() {
            PlanParameters pp = new PlanParameters();
            pp.Target = new NINA.Astrometry.DeepSkyObject("", TestUtil.M31, null, null);
            pp.ObserverInfo = TestUtil.TEST_LOCATION_1;
            pp.HorizonDefinition = new HorizonDefinition(5);
            pp.MinimumImagingTime = 0;
            pp.MeridianTimeSpan = 0;
            pp.MinimumMoonSeparation = 0;
            pp.MaximumMoonIllumination = 0;
            pp.MoonAvoidanceEnabled = false;
            pp.MoonAvoidanceWidth = 0;
            pp.StartDate = new DateTime(2022, 4, 8);
            pp.PlanDays = 3;

            IEnumerable<ImagingDayPlan> results = new PlanGenerator(pp).Generate(tokenSource.Token);

            foreach (ImagingDayPlan plan in results) {
                bool rejected = plan.StartLimitingFactor.Session;
                Logger.Trace($"{plan.StartImagingTime}: {plan.GetImagingMinutes()} mins - {!rejected}");
            }

            throw new Exception("oops");

        }

        //[Test]
        public void testSkipDayBug2() {

            /*
                This skips a day between these two set/rise times (time likely varies based on location).  Earlier, no skip.
                    12/3/2022 6:08:12 AM: 12/3/2022 6:03:20 PM 12/3/2022 6:08:12 AM
                    12/3/2022 6:08:13 AM: 12/3/2022 6:03:20 PM 12/4/2022 6:08:59 AM
             */

            ObserverInfo location = TestUtil.TEST_LOCATION_1;
            DateTime dt = new DateTime(2022, 12, 3, 6, 8, 0);

            for (int i = 0; i < 60; i++) {
                RiseAndSetEvent day1 = AstroUtil.GetNauticalNightTimes(dt, location.Latitude, location.Longitude);
                TestContext.WriteLine($"{dt}: {day1.Set} {day1.Rise}");
                dt = dt.AddSeconds(1);
            }

            throw new Exception("oops");
        }

        //[Test]
        public void testLowAltitudeBug() {

            PlanParameters pp = new PlanParameters();
            pp.Target = new NINA.Astrometry.DeepSkyObject("", TestUtil.M31, null, null);
            pp.ObserverInfo = TestUtil.TEST_LOCATION_1;
            pp.HorizonDefinition = new HorizonDefinition(5);
            pp.MinimumImagingTime = 0;
            pp.MeridianTimeSpan = 0;
            pp.MinimumMoonSeparation = 0;
            pp.MaximumMoonIllumination = 0;
            pp.MoonAvoidanceEnabled = false;
            pp.MoonAvoidanceWidth = 0;

            /*
             * For 3/14, imaging time is 123 min which looks correct.
             * But for 3/15, it jumps to 612 min
             * It seems to be completely ignoring the horizon check
             * Note that DST switch was 3/13 at 2am ...
             */

            pp.StartDate = new System.DateTime(2022, 3, 15);
            pp.PlanDays = 1;

            IEnumerable<ImagingDayPlan> results = new PlanGenerator(pp).Generate(tokenSource.Token);

            foreach (ImagingDayPlan plan in results) {
                bool rejected = plan.StartLimitingFactor.Session;
                Logger.Trace($"{plan.StartImagingTime}: {plan.GetImagingMinutes()} mins - {!rejected}");
            }

            throw new Exception("oops");
        }
    }

}
