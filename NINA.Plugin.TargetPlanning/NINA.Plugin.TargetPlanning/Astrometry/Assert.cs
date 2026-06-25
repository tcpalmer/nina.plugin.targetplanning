using System;

namespace TargetPlanning.NINAPlugin.Astrometry {

    // Copied from Target Scheduler (NINA.Plugin.TargetScheduler.Util.Assert) to support the migrated astrometry code.
    // Internal so it does not collide with NUnit.Framework.Assert in the test assembly.
    internal static class Assert {

        public static void isTrue(bool expression, string message) {
            if (!expression) {
                throw new ArgumentException(message);
            }
        }

        public static void isFalse(bool expression, string message) {
            if (expression) {
                throw new ArgumentException(message);
            }
        }

        public static void notNull<T>(T obj, string message) {
            if (obj == null) {
                throw new ArgumentException(message);
            }
        }
    }
}
