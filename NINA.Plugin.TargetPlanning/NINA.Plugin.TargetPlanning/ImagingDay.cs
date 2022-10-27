using NINA.Core.Utility;
using System;

namespace TargetPlanning.NINAPlugin {

    public class ImagingDay {

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string StartDateStr { get; set; }

        public string EndDateStr { get; set; }

        public ImagingDay(DateTime startDate, DateTime endDate) {
            this.StartDate = startDate;
            this.EndDate = endDate;
            this.StartDateStr = startDate.ToShortDateString();
            this.EndDateStr = endDate.ToShortDateString();
        }

    }
}
