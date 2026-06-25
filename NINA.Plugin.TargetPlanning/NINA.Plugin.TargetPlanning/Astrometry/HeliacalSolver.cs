using NINA.Astrometry;
using System;
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
    /// For our needs, we take astronomical twilight (sun -18°) as the time and 7° above the horizon for the target.
    ///
    /// The season is anchored on the date the target transits closest to local midnight (the approximate middle of the
    /// imaging season).  Moving forward in time from roughly six months before that date, the target's altitude at dawn
    /// (astronomical twilight end) increases monotonically through TARGET_ALTITUDE - that crossing is the heliacal rising.
    /// Symmetrically, moving forward from the transit date, the target's altitude at dusk (astronomical twilight start)
    /// decreases monotonically through TARGET_ALTITUDE - that crossing is the heliacal setting.  Because each edge is
    /// monotonic, we locate it with a binary search instead of a day-by-day walk: ~8 probes per edge, each reusing the
    /// cached NighttimeCircumstances.
    ///
    /// Heliacal rising and setting do not apply in situations where the target is circumpolar or never rises at location.
    /// Those cases should be checked prior to calling these methods.
    /// </summary>
    public class HeliacalSolver {

        private const double TARGET_ALTITUDE = 7;
        private const int DAYS_HALF_YEAR = 183;

        private readonly ObserverInfo location;
        private readonly Coordinates target;
        private readonly DateTime transitMidnight;
        private readonly int twilightInclude;

        public HeliacalSolver(ObserverInfo location, Coordinates target, DateTime transitMidnight, int twilightInclude) {
            this.location = location;
            this.target = target;
            this.transitMidnight = transitMidnight;
            this.twilightInclude = twilightInclude;
        }

        public DateTime GetHeliacalRisingDate(CancellationToken token) {

            // The rising edge lies in the ~6 months before the midnight transit date.  At the early end, the target is
            // still below TARGET_ALTITUDE at dawn (out of season); at the transit date it is above (in season).  Binary
            // search the crossing.
            DateTime lo = transitMidnight.AddDays(-DAYS_HALF_YEAR);
            DateTime hi = transitMidnight;

            while ((hi - lo).Days > 1) {
                token.ThrowIfCancellationRequested();
                DateTime mid = lo.AddDays((hi - lo).Days / 2);
                if (DawnAltitude(mid) > TARGET_ALTITUDE) {
                    hi = mid;
                } else {
                    lo = mid;
                }
            }

            // 'hi' is the first night whose dawn clears the target altitude; the heliacal rising date is that dawn date.
            return GetTwilightSpan(hi).EndTime.Date;
        }

        public DateTime GetHeliacalSettingDate(CancellationToken token) {

            // The setting edge lies in the ~6 months after the midnight transit date.  At the transit date the target is
            // still above TARGET_ALTITUDE at dusk (in season); at the late end it is below (out of season).  Binary
            // search the crossing.
            DateTime lo = transitMidnight;
            DateTime hi = transitMidnight.AddDays(DAYS_HALF_YEAR);

            while ((hi - lo).Days > 1) {
                token.ThrowIfCancellationRequested();
                DateTime mid = lo.AddDays((hi - lo).Days / 2);
                if (DuskAltitude(mid) > TARGET_ALTITUDE) {
                    lo = mid;
                } else {
                    hi = mid;
                }
            }

            // 'lo' is the last night whose dusk clears the target altitude; the heliacal setting date is that dusk date.
            return GetTwilightSpan(lo).StartTime.Date;
        }

        // Target altitude at dawn (twilight end) for the night beginning on the given date, or double.MinValue if the
        // requested twilight does not occur (e.g. high latitude near the summer solstice).
        private double DawnAltitude(DateTime date) {
            TimeInterval span = GetTwilightSpan(date);
            return span == null ? double.MinValue : AstrometryUtils.GetHorizontalCoordinates(location, target, span.EndTime).Altitude;
        }

        // Target altitude at dusk (twilight start) for the night beginning on the given date, or double.MinValue if the
        // requested twilight does not occur.
        private double DuskAltitude(DateTime date) {
            TimeInterval span = GetTwilightSpan(date);
            return span == null ? double.MinValue : AstrometryUtils.GetHorizontalCoordinates(location, target, span.StartTime).Altitude;
        }

        private TimeInterval GetTwilightSpan(DateTime date) {
            return new TwilightCircumstances(location, date).GetTwilightSpan(TwilightInclude.ToTwilightLevel(twilightInclude));
        }
    }

}
