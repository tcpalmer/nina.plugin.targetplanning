using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Threading;
using TargetPlanning.NINAPlugin.Astrometry;

namespace NINA.Plugin.TargetPlanning.Test.Astrometry {

    public class OptimalImagingSeasonTest {

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
        public void testIt() {
            PlanParameters pp = new PlanParameters();
            pp.MinimumMoonSeparation = 60;
            pp.StartDate = new DateTime(2022, 1, 1);
            pp.PlanDays = 3;
            pp.ObserverInfo = TestUtil.TEST_LOCATION_1;
            pp.Target = new NINA.Astrometry.DeepSkyObject("", TestUtil.M31, null, null);
            pp.HorizonDefinition = new HorizonDefinition(20);

            OptimalImagingSeason sut = new OptimalImagingSeason();
            sut.GetOptimalSeason(pp, tokenSource.Token);

            //throw new Exception("oops");
        }
    }

}
