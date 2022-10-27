using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TargetPlanning.NINAPlugin {

    [Export(typeof(IPluginManifest))]
    public class TargetPlanningPlugin : PluginBase, INotifyPropertyChanged {
        private IPluginOptionsAccessor pluginSettings;
        private IProfileService profileService;

        private PagedList<ImagingDay> _searchResult;

        private AsyncObservableCollection<KeyValuePair<double, string>> _minimumAltitudeChoices;
        private AsyncObservableCollection<KeyValuePair<int, string>> _minimumTimeChoices;
        private AsyncObservableCollection<KeyValuePair<double, string>> _moonSeparationChoices;
        private AsyncObservableCollection<KeyValuePair<double, string>> _maximumMoonIlluminationChoices;
        private AsyncObservableCollection<KeyValuePair<int, string>> _meridianTimeSpanChoices;

        [ImportingConstructor]
        public TargetPlanningPlugin(IProfileService profileService) {
            if (Properties.Settings.Default.UpdateSettings) {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Properties.Settings.Default);
            }

            this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(this.Identifier));
            this.profileService = profileService;
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            SearchCommand = new AsyncCommand<bool>(() => Search());
            CancelSearchCommand = new RelayCommand(CancelSearch);

            InitializeCriteria();
        }

        private void InitializeCriteria() {

            MinimumTimeChoices = new AsyncObservableCollection<KeyValuePair<int, string>>();
            MinimumTimeChoices.Add(new KeyValuePair<int, string>(0, Loc.Instance["LblAny"]));
            for (int i = 30; i <= 240; i += 30) {
                MinimumTimeChoices.Add(new KeyValuePair<int, string>(i, MtoHM(i)));
            }

            MinimumAltitudeChoices = new AsyncObservableCollection<KeyValuePair<double, string>>();
            MinimumAltitudeChoices.Add(new KeyValuePair<double, string>(0, Loc.Instance["LblAny"]));
            for (int i = 10; i < 90; i += 10) {
                MinimumAltitudeChoices.Add(new KeyValuePair<double, string>(i, i + "°"));
            }
            // Sky Atlas adds 'Horizon' to altitude choices to trigger use of custom horizon

            MoonSeparationChoices = new AsyncObservableCollection<KeyValuePair<double, string>>();
            MoonSeparationChoices.Add(new KeyValuePair<double, string>(0, Loc.Instance["LblAny"]));
            for (double i = 5; i < 180; i += 5) {
                MoonSeparationChoices.Add(new KeyValuePair<double, string>(i, i + "°"));
            }

            MaximumMoonIlluminationChoices = new AsyncObservableCollection<KeyValuePair<double, string>>();
            MaximumMoonIlluminationChoices.Add(new KeyValuePair<double, string>(0, Loc.Instance["LblAny"]));
            for (int i = 10; i < 100; i += 10) {
                MaximumMoonIlluminationChoices.Add(new KeyValuePair<double, string>(((double)i)/100, i + "%"));
            }

            MeridianTimeSpanChoices = new AsyncObservableCollection<KeyValuePair<int, string>>();
            MeridianTimeSpanChoices.Add(new KeyValuePair<int, string>(0, Loc.Instance["LblAny"]));
            for (int i = 30; i <= 240; i += 30) {
                MeridianTimeSpanChoices.Add(new KeyValuePair<int, string>(i, MtoHM(i)));
            }
        }

        private string MtoHM(int minutes) {
            decimal hours = Math.Floor((decimal) minutes / 60);
            int min = minutes % 60;
            return $"{hours}h {min}m";
        }

        public override Task Teardown() {
            Logger.Debug($"StartDate:      {StartDate}");
            Logger.Debug($"PlanDays:       {PlanDays}");
            Logger.Debug($"MinTime:        {MinimumTime}");
            Logger.Debug($"MinAlt:         {MinimumAltitude}");
            Logger.Debug($"Min Moon Sep:   {MinimumMoonSeparation}");
            Logger.Debug($"Max Moon Illum: {MaximumMoonIllumination}");
            Logger.Debug($"Mer Span:       {MeridianTimeSpan}");

            return base.Teardown();
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            RaisePropertyChanged(nameof(StartDate));
            RaisePropertyChanged(nameof(PlanDays));
            RaisePropertyChanged(nameof(MinimumTime));
            RaisePropertyChanged(nameof(MinimumAltitude));
            RaisePropertyChanged(nameof(MinimumMoonSeparation));
            RaisePropertyChanged(nameof(MaximumMoonIllumination));
            RaisePropertyChanged(nameof(MeridianTimeSpan));
        }

        public DateTime StartDate {
            get => pluginSettings.GetValueDateTime(nameof(StartDate), DateTime.Now);
            set {
                pluginSettings.SetValueDateTime(nameof(StartDate), value);
                RaisePropertyChanged();
            }
        }

        public int PlanDays {
            get => pluginSettings.GetValueInt32(nameof(PlanDays), 7);
            set {
                pluginSettings.SetValueInt32(nameof(PlanDays), value);
                RaisePropertyChanged();
            }
        }

        public double MinimumAltitude {
            get => pluginSettings.GetValueDouble(nameof(MinimumAltitude), 0);
            set {
                pluginSettings.SetValueDouble(nameof(MinimumAltitude), value);
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double, string>> MinimumAltitudeChoices {
            get {
                return _minimumAltitudeChoices;
            }
            set {
                _minimumAltitudeChoices = value;
            }
        }

        public int MinimumTime {
            get => pluginSettings.GetValueInt32(nameof(MinimumTime), 60);
            set {
                pluginSettings.SetValueInt32(nameof(MinimumTime), value);
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<int, string>> MinimumTimeChoices {
            get {
                return _minimumTimeChoices;
            }
            set {
                _minimumTimeChoices = value;
            }
        }

        public double MinimumMoonSeparation {
            get => pluginSettings.GetValueDouble(nameof(MinimumMoonSeparation), 0);
            set {
                pluginSettings.SetValueDouble(nameof(MinimumMoonSeparation), value);
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double, string>> MoonSeparationChoices {
            get {
                return _moonSeparationChoices;
            }
            set {
                _moonSeparationChoices = value;
            }
        }

        public double MaximumMoonIllumination {
            get => pluginSettings.GetValueDouble(nameof(MaximumMoonIllumination), 0);
            set {
                pluginSettings.SetValueDouble(nameof(MaximumMoonIllumination), value);
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double, string>> MaximumMoonIlluminationChoices {
            get {
                return _maximumMoonIlluminationChoices;
            }
            set {
                _maximumMoonIlluminationChoices = value;
            }
        }

        public int MeridianTimeSpan {
            get => pluginSettings.GetValueInt32(nameof(MeridianTimeSpan), 0);
            set {
                pluginSettings.SetValueInt32(nameof(MeridianTimeSpan), value);
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<int, string>> MeridianTimeSpanChoices {
            get {
                return _meridianTimeSpanChoices;
            }
            set {
                _meridianTimeSpanChoices = value;
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
            return Task.Run(async () => {
                try {
                    SearchResult = null;

                    int days = PlanDays;
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
                //await testLoadDSO();
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

        public IEnumerable<DeepSkyObject> DSOResult { get; set; }

        // TODO: how to get profile lat/long:
        // var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
        // latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
        // altitude too?

        // Test load DSO
        private async Task<bool> testLoadDSO() {
            var db = new DatabaseInteraction();
            var searchParams = new DatabaseInteraction.DeepSkyObjectSearchParams();

            searchParams.ObjectName = "sh2 240";
            searchParams.SearchOrder.Direction = "ASC";

            DSOResult = await db.GetDeepSkyObjects(
                profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository,
                profileService.ActiveProfile.AstrometrySettings.Horizon,
                searchParams,
                _searchTokenSource.Token);

            Logger.Debug("DSO results:");
            foreach (DeepSkyObject obj in DSOResult) {
                Logger.Debug($"{obj.Name} {obj.Coordinates.ToString()}");
            }

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}