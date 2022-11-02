using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TargetPlanning.NINAPlugin {

    public class Utils {

        public static string MtoHM(int minutes) {
            decimal hours = Math.Floor((decimal)minutes / 60);
            int min = minutes % 60;
            return $"{hours}h {min}m";
        }

    }

}
