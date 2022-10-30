using System;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public interface IAltitudeRefiner {

        Altitudes Refine(Altitudes altitudes, int numPoints);

        Altitudes GetHourlyAltitudesForDay(DateTime date);

        bool RisesAtLocation();

        bool CircumpolarAtLocation();
    }

}
