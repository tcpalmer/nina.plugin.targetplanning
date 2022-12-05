using NINA.Astrometry;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Threading;

namespace TargetPlanning.NINAPlugin.Astrometry {

    /// <summary>
    /// The heliacal rising of Sirius was used by the ancient Egyptians to predict the flooding of the Nile.  It turns out
    /// that the heliacal rising/setting of a target are exactly what we need to determine the extremes of the imaging
    /// season for that target. 
    /// 
    /// "The heliacal rising is the first day of the year when the star (after a period when it was invisible) rises
    /// in the morning before the Sun and the Sun is still far enough below the eastern horizon to make it briefly visible
    /// in the morning twilight."
    /// 
    /// "The heliacal setting is the last day when the star (after a period when it was visible) sets after sunset and
    /// the Sun is already far enough below the western horizon to make the star briefly visible in the evening twilight."
    /// 
    /// For out needs, we take astronomical twilight (sun -12°) as the time and 7° above the horizon for the target.  The
    /// methods require a decent initial guess for the date and will then iterate to determine the best date.  The guess is
    /// determined by calculating a delta from the date of midnight transit for the target at location.  See below for more
    /// information on how this delta is calculated.
    /// 
    /// Heliacal rising and setting do not apply in situations where the target is circumpolar or never rises at location.
    /// Those cases should be checked prior to calling these methods.
    /// </summary>
    public class HeliacalSolver {

        private static readonly double TARGET_ALTITUDE = 7;
        private static readonly int MAX_TRIES = 360;

        private readonly ObserverInfo location;
        private readonly Coordinates target;
        private readonly DateTime transitMidnight;

        public HeliacalSolver(ObserverInfo location, Coordinates target, DateTime transitMidnight) {
            this.location = location;
            this.target = target;
            this.transitMidnight = transitMidnight;
        }

        public DateTime GetHeliacalRisingDate(CancellationToken token) {

            DateTime startDate = GetHRInitialGuess();

            DateTime rise = GetTwilightTimes(startDate).Item1;
            double altitude = AstrometryUtils.GetHorizontalCoordinates(location, target, rise).Altitude;
            int dayDelta = altitude < TARGET_ALTITUDE ? 1 : -1;

            DateTime dateTime = startDate;
            for (int i = 0; i < MAX_TRIES; i++) {

                if (token.IsCancellationRequested) {
                    throw new OperationCanceledException();
                }

                rise = GetTwilightTimes(dateTime).Item1;
                altitude = AstrometryUtils.GetHorizontalCoordinates(location, target, rise).Altitude;

                if (dayDelta > 0 && altitude > TARGET_ALTITUDE) {
                    return rise.Date;
                }

                if (dayDelta < 0 && altitude < TARGET_ALTITUDE) {
                    return rise.AddDays(1).Date;
                }

                dateTime = dateTime.AddDays(dayDelta);
            }

            Logger.Error($"failed to find heliacal rising after {MAX_TRIES} attempts");
            throw new Exception($"failed to find heliacal rising after {MAX_TRIES} attempts (target: {target}, location: {location})");
        }

        public DateTime GetHeliacalSettingDate(CancellationToken token) {

            DateTime startDate = GetHSInitialGuess();

            DateTime set = GetTwilightTimes(startDate).Item2;
            double altitude = AstrometryUtils.GetHorizontalCoordinates(location, target, set).Altitude;
            int dayDelta = altitude > TARGET_ALTITUDE ? 1 : -1;

            DateTime dateTime = startDate;
            for (int i = 0; i < MAX_TRIES; i++) {

                if (token.IsCancellationRequested) {
                    throw new OperationCanceledException();
                }

                set = GetTwilightTimes(dateTime).Item2;
                altitude = AstrometryUtils.GetHorizontalCoordinates(location, target, set).Altitude;

                if (dayDelta < 0 && altitude > TARGET_ALTITUDE) {
                    return set.Date;
                }

                if (dayDelta > 0 && altitude < TARGET_ALTITUDE) {
                    return set.AddDays(-1).Date;
                }

                dateTime = dateTime.AddDays(dayDelta);
            }

            Logger.Error($"failed to find heliacal setting after {MAX_TRIES} attempts");
            throw new Exception($"failed to find heliacal setting after {MAX_TRIES} attempts (target: {target}, location: {location})");
        }

        private Tuple<DateTime, DateTime> GetTwilightTimes(DateTime dateTime) {
            RiseAndSetEvent riseAndSetEvent = TwilightTimeCache.Get(dateTime, location.Latitude, location.Longitude);
            if (riseAndSetEvent.Rise == null || riseAndSetEvent.Set == null) {
                throw new Exception($"no rise or set time for location ({location}) on date {dateTime}");
            }

            return Tuple.Create((DateTime)riseAndSetEvent.Rise, (DateTime)riseAndSetEvent.Set);
        }

        /*
         * The code below is used to determine a good initial guess date for the heliacal rising/settings calls.
         * It's based on the observation that the number of days between the date of midnight transit and the 
         * heliacal rising can be estimated by using the value of the constant = latitude+declination to select one
         * of a set of degree 2 polynomial equations with declination as the argument.  Evaluating the equation yields
         * the best guess for number of days from the date of midnight transit.  The same is true of heliacal setting
         * but using a different set of equations.
         * 
         * The coefficients for the polynomials were determined by calculating heliacal rising/setting for values
         * of constant=latitude+declination from -90 to 90 by 10, yielding 19 polynomials for rising and another 19
         * for setting.  Some code in HeliacalSolverTest was used to generate the data and Excel was used to fit the
         * polynomials.
         * 
         * In practice, this yields guesses within a few days (for dec+lat values close to a calculated curve) to around
         * a month for other values.  Could certainly be improved by generating curves for more constant values than
         * just every 10.
         * 
         * I see some indications that a direct solution is possible but what I found seemed more concerned with
         * atmospheric issues of brightness and extinction or for archeoastronomy and determining the dates for 3000 BC.
         */

        public DateTime GetHRInitialGuess() {
            ConstantCoefficients coeffs = hrCoefficientMap[GetMappedConstant()];
            int delta = coeffs.evaluate(target.Dec);
            return transitMidnight.AddDays(delta);
        }

        public DateTime GetHSInitialGuess() {
            ConstantCoefficients coeffs = hsCoefficientMap[GetMappedConstant()];
            int delta = coeffs.evaluate(target.Dec);
            return transitMidnight.AddDays(delta);
        }

        public int GetMappedConstant() {
            double constant = location.Latitude + target.Dec;
            if (constant < -90) {
                return -90;
            }

            if (constant > 90) {
                return 90;
            }

            return ((int)Math.Round(constant / 10.0)) * 10;
        }

        private static readonly Dictionary<int, ConstantCoefficients> hrCoefficientMap = GenHRCoefficientMap();
        private static readonly Dictionary<int, ConstantCoefficients> hsCoefficientMap = GenHSCoefficientMap();

        private static Dictionary<int, ConstantCoefficients> GenHRCoefficientMap() {
            Dictionary<int, ConstantCoefficients> map = new Dictionary<int, ConstantCoefficients>();

            map.Add(90, new ConstantCoefficients(0.0379, -2.9516, -207.03));
            map.Add(80, new ConstantCoefficients(0.0551, -4.7003, -131.13));
            map.Add(70, new ConstantCoefficients(0.0655, -5.5106, -97.543));
            map.Add(60, new ConstantCoefficients(0.0551, -4.0248, -128.23));
            map.Add(50, new ConstantCoefficients(0.0527, -3.3608, -141.55));
            map.Add(40, new ConstantCoefficients(0.0494, -2.6396, -155.91));
            map.Add(30, new ConstantCoefficients(0.0464, -1.9744, -167.58));
            map.Add(20, new ConstantCoefficients(0.0453, -1.4422, -176.46));
            map.Add(10, new ConstantCoefficients(0.0423, -0.8857, -182.59));
            map.Add(0, new ConstantCoefficients(0.04, -0.4083, -186.36));
            map.Add(-10, new ConstantCoefficients(0.0384, 0.0249, -188.3));
            map.Add(-20, new ConstantCoefficients(0.0373, 0.4267, -188.84));
            map.Add(-30, new ConstantCoefficients(0.0359, 0.7868, -188.07));
            map.Add(-40, new ConstantCoefficients(0.037, 1.2591, -184.89));
            map.Add(-50, new ConstantCoefficients(0.0398, 1.8895, -177.53));
            map.Add(-60, new ConstantCoefficients(0.0447, 2.7821, -162.26));
            map.Add(-70, new ConstantCoefficients(0.0455, 3.3725, -149.59));
            map.Add(-80, new ConstantCoefficients(0.052, 4.6913, -115.77));
            map.Add(-90, new ConstantCoefficients(0.0445, 4.5813, -117.64));

            return map;
        }

        private static Dictionary<int, ConstantCoefficients> GenHSCoefficientMap() {
            Dictionary<int, ConstantCoefficients> map = new Dictionary<int, ConstantCoefficients>();

            map.Add(90, new ConstantCoefficients(-0.0444, 4.5703, 72.861));
            map.Add(80, new ConstantCoefficients(-0.0519, 4.6871, 71.124));
            map.Add(70, new ConstantCoefficients(-0.0439, 3.236, 107.61));
            map.Add(60, new ConstantCoefficients(-0.0425, 2.6305, 119.5));
            map.Add(50, new ConstantCoefficients(-0.0387, 1.8373, 133.1));
            map.Add(40, new ConstantCoefficients(-0.0354, 1.2053, 140.3));
            map.Add(30, new ConstantCoefficients(-0.0347, 0.7663, 142.99));
            map.Add(20, new ConstantCoefficients(-0.0356, 0.4115, 143.67));
            map.Add(10, new ConstantCoefficients(-0.0369, 0.0303, 143.65));
            map.Add(0, new ConstantCoefficients(-0.0382, -0.3868, 141.52));
            map.Add(-10, new ConstantCoefficients(-0.0401, -0.8388, 137.84));
            map.Add(-20, new ConstantCoefficients(-0.043, -1.374, 132.02));
            map.Add(-30, new ConstantCoefficients(-0.0442, -1.8838, 123.59));
            map.Add(-40, new ConstantCoefficients(-0.0475, -2.5316, 112.25));
            map.Add(-50, new ConstantCoefficients(-0.0509, -3.243, 98.157));
            map.Add(-60, new ConstantCoefficients(-0.0534, -3.9017, 85.113));
            map.Add(-70, new ConstantCoefficients(-0.0645, -5.4259, 54.427));
            map.Add(-80, new ConstantCoefficients(-0.0534, -4.529, 90.392));
            map.Add(-90, new ConstantCoefficients(-0.0386, -2.9846, 163.61));

            return map;
        }
    }

    public class ConstantCoefficients {

        private readonly double x2;
        private readonly double x;
        private readonly double c;

        public ConstantCoefficients(double x2, double x, double c) {
            this.x2 = x2;
            this.x = x;
            this.c = c;
        }

        public int evaluate(double declination) {
            return (int)Math.Round(x2 * declination * declination + x * declination + c);
        }
    }

}
