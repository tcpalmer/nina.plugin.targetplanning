using NINA.Astrometry;
using NINA.Profile.Interfaces;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin {

    public class ImagingDayPlanContext {

        public PlanParameters PlanParameters { get; private set; }
        public IProfileService Profile { get; private set; }
        public NighttimeCalculator NighttimeCalculator { get; private set; }

        public ImagingDayPlanContext(PlanParameters planParameters, IProfileService profile) {
            PlanParameters = planParameters;
            Profile = profile;
            NighttimeCalculator = new NighttimeCalculator(profile);
        }
    }

}
