using NINA.Core.Utility;

namespace TargetPlanning.NINAPlugin.Astrometry {

    // Adapter shim: the migrated Target Scheduler astrometry code logs via TSLogger.  Forward to NINA's Logger so the
    // copied code can be used as-is.
    public static class TSLogger {

        public static void Trace(string message) => Logger.Trace(message);

        public static void Debug(string message) => Logger.Debug(message);

        public static void Info(string message) => Logger.Info(message);

        public static void Warning(string message) => Logger.Warning(message);

        public static void Error(string message) => Logger.Error(message);
    }
}
