using TargetPlanning.NINAPlugin.Properties;
using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.ComponentModel;
using NINA.Core.Model;
using System.Windows.Input;
using System.Threading;
using NINA.Astrometry;
using OxyPlot;
using static NINA.Astrometry.NOVAS;

namespace TargetPlanning.NINAPlugin {

    [Export(typeof(IPluginManifest))]
    public class TargetPlanningPlugin : PluginBase, INotifyPropertyChanged {

        private IPluginOptionsAccessor pluginSettings;
        private PagedList<ImagingDay> _searchResult;

        // TODO: be nice to move all the plan display to a separate class.  How to Bind in that case?

        [ImportingConstructor]
        public TargetPlanningPlugin(IProfileService profileService) {

            if (Properties.Settings.Default.UpdateSettings) {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Properties.Settings.Default);
            }

            this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(this.Identifier));
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            SearchCommand = new AsyncCommand<bool>(() => Search());
            CancelSearchCommand = new RelayCommand(CancelSearch);
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            RaisePropertyChanged(nameof(StartDate));
            RaisePropertyChanged(nameof(EndDate));
        }

        public DateTime StartDate {
            get => pluginSettings.GetValueDateTime(nameof(StartDate), DateTime.Now);
            set {
                pluginSettings.SetValueDateTime(nameof(StartDate), value);
                RaisePropertyChanged();
            }
        }

        public DateTime EndDate {
            get => pluginSettings.GetValueDateTime(nameof(EndDate), DateTime.Now.AddDays(5));
            set {
                pluginSettings.SetValueDateTime(nameof(EndDate), value);
                RaisePropertyChanged();
            }
        }

        public ICommand CancelSearchCommand { get; private set; }

        private CancellationTokenSource _searchTokenSource;

        private void CancelSearch(object obj) {
            try { _searchTokenSource?.Cancel(); } catch { }
        }

        public ICommand SearchCommand { get; private set; }

        // TODO: working but need to make async like SkyAtlasVM that this was copied from
        private Task<bool> Search() {
            _searchTokenSource?.Dispose();
            _searchTokenSource = new CancellationTokenSource();
            Logger.Debug($"STARTING SEARCH: {StartDate}");
            return Task.Run(() => {
                try {
                    SearchResult = null;

                    int days = (EndDate - StartDate).Days;
                    List<ImagingDay> items = new List<ImagingDay>(days);
                    DateTime start = StartDate;
                    for (int i = 0; i < days; i++) {
                        items.Add(new ImagingDay(start.AddDays(i), start.AddDays(i + 1)));
                    }

                    SearchResult = new PagedList<ImagingDay>(60, items);
                }
                catch (OperationCanceledException) {
                }

                Logger.Debug("SEARCH DONE");
                return true;
            });
        }

        public PagedList<ImagingDay> SearchResult {
            get {
                return _searchResult;
            }

            set {
                _searchResult = value;
                RaisePropertyChanged();
            }
        }

        // TODO: how to get profile lat/long:
        // var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
        // latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
        // altitude too?


        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            switch (propertyName) {
                case nameof(StartDate):
                    Logger.Trace($"start date changed: {StartDate}");
                    break;
                case nameof(EndDate):
                    Logger.Trace($"end date changed: {EndDate}");
                    break;
            }
        }

    }
}
