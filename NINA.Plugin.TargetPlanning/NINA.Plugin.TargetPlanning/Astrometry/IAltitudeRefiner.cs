using NINA.Core.Enum;
using nom.tam.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public interface IAltitudeRefiner {

        /// <summary>
        /// Add additional calculated points between the two elements in the altitudes list.
        /// </summary>
        /// <param name="altitudes"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        Altitudes refine(Altitudes altitudes, int numPoints);

        /// <summary>
        /// Generate altitude values for the target at location on date from midnight to midnight for every hour.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        Altitudes getHourlyAltitudesForDay(DateTime date);

        /// <summary>
        /// Return true if the target object ever rises at the location.  An object never rises at a location if it is
        /// circumpolar around the pole in the opposite hemisphere from location.
        /// </summary>
        /// <returns></returns>
        bool risesAtLocation();

        /// <summary>
        /// Return true if the target object is circumpolar at the location.
        /// </summary>
        /// <returns></returns>
        bool circumpolarAtLocation();
    }

}
