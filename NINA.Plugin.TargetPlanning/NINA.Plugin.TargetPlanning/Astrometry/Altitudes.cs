using System;
using System.Collections.Generic;
using System.Text;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class Altitudes {

        public List<AltitudeAtTime> AltitudeList { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }

        public Altitudes(List<AltitudeAtTime> altitudes) {
            Validate.Assert.notNull(altitudes, "altitudes cannot be null");
            Validate.Assert.isTrue(altitudes.Count > 1, "altitudes must have at least two values");

            this.AltitudeList = altitudes;
            this.StartTime = altitudes[0].AtTime;
            this.EndTime = altitudes[altitudes.Count - 1].AtTime;

            Validate.Assert.isTrue(StartTime < EndTime, "startTime must be before endTime");

            DateTime cmp = StartTime.AddSeconds(-1);
            foreach (AltitudeAtTime altitude in altitudes) {
                Validate.Assert.isTrue(cmp < altitude.AtTime, "time is not always increasing");
                cmp = altitude.AtTime;
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < AltitudeList.Count; i++) {
                AltitudeAtTime altitude = AltitudeList[i];
                sb.Append(String.Format("{0,2:F0} {1,9:F2} {2,9:F2} ", i, altitude.Altitude, altitude.Azimuth));
                sb.Append(altitude.AtTime.ToString("MM/dd/yyyy HH:mm:ss"));
                sb.Append("\n");
            }

            return sb.ToString();
        }
    }

    public class AltitudeAtTime {

        public double Altitude { get; private set; }
        public double Azimuth { get; private set; }
        public DateTime AtTime { get; private set; }

        public AltitudeAtTime(double altitude, double azimuth, DateTime atTime) {
            Validate.Assert.isTrue(altitude <= 90 && altitude >= -90, "altitude must be <= 90 and >= -90");
            Validate.Assert.isTrue(azimuth >= 0 && azimuth <= 360, "azimuth must be >= 0 and <= 360");

            this.Altitude = altitude;
            this.Azimuth = azimuth;
            this.AtTime = atTime;
        }

        public override string ToString() {
            return "AltitudeAtTime{" + "altitude=" + Altitude + ", azimuth=" + Azimuth + ", atTime=" + AtTime + '}';
        }
    }

}
