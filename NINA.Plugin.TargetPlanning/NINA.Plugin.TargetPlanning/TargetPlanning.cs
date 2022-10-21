using TargetPlanning.NINAPlugin.Properties;
using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TargetPlanning.NINAPlugin {

    [Export(typeof(IPluginManifest))]
    public class TargetPlanningPlugin : PluginBase {

        [ImportingConstructor]
        public TargetPlanningPlugin() {
            if (Settings.Default.UpdateSettings) {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        public string DefaultNotificationMessage {
            get {
                return Settings.Default.DefaultNotificationMessage;
            }
            set {
                Settings.Default.DefaultNotificationMessage = value;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }
    }
}
