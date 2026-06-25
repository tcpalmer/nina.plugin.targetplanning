using System;

namespace TargetPlanning.NINAPlugin.Astrometry {

    // The plugin persists the user's "twilight include" choice as an int (see plugin settings).  This maps those
    // stable int codes onto the Target Scheduler TwilightLevel enum used by TwilightCircumstances.  The codes match
    // the values previously exposed by NighttimeCircumstances.
    public static class TwilightInclude {
        public const int NONE = 0;
        public const int ASTRO = 1;
        public const int NAUTICAL = 2;
        public const int CIVIL = 3;

        public static TwilightLevel ToTwilightLevel(int twilightInclude) {
            switch (twilightInclude) {
                case NONE: return TwilightLevel.Nighttime;
                case ASTRO: return TwilightLevel.Astronomical;
                case NAUTICAL: return TwilightLevel.Nautical;
                case CIVIL: return TwilightLevel.Civil;
                default:
                    throw new ArgumentException($"unknown twilight include code: {twilightInclude}");
            }
        }
    }
}
