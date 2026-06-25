namespace TargetPlanning.NINAPlugin.Astrometry {

    // Copied from Target Scheduler (NINA.Plugin.TargetScheduler.Astrometry.HorizontalCoordinate).
    public class HorizontalCoordinate {
        public double Altitude { get; private set; }
        public double Azimuth { get; private set; }

        public HorizontalCoordinate(double altitude, double azimuth) {
            this.Altitude = altitude;
            this.Azimuth = azimuth;
        }
    }
}
