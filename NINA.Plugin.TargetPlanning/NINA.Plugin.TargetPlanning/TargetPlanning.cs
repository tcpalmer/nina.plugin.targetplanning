﻿using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin {

    [Export(typeof(IPluginManifest))]
    public class TargetPlanningPlugin : PluginBase, INotifyPropertyChanged {
        private IPluginOptionsAccessor pluginSettings;
        private IProfileService profileService;

        private PagedList<ImagingDayPlanViewAdapter> _searchResult;

        private AsyncObservableCollection<KeyValuePair<double, string>> _minimumAltitudeChoices;
        private AsyncObservableCollection<KeyValuePair<int, string>> _minimumTimeChoices;
        private AsyncObservableCollection<KeyValuePair<double, string>> _moonSeparationChoices;
        private AsyncObservableCollection<KeyValuePair<double, string>> _maximumMoonIlluminationChoices;
        private AsyncObservableCollection<KeyValuePair<int, string>> _meridianTimeSpanChoices;

        [ImportingConstructor]
        public TargetPlanningPlugin(IProfileService profileService, IDeepSkyObjectSearchVM deepSkyObjectSearchVM) {
            if (Properties.Settings.Default.UpdateSettings) {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Properties.Settings.Default);
            }

            this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(this.Identifier));
            this.profileService = profileService;
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            if (profileService.ActiveProfile != null) {
                profileService.ActiveProfile.AstrometrySettings.PropertyChanged += ProfileService_ProfileChanged;
            }

            var defaultCoordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees);
            DSO = new DeepSkyObject(string.Empty, defaultCoordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository, profileService.ActiveProfile.AstrometrySettings.Horizon);
            DeepSkyObjectSearchVM = deepSkyObjectSearchVM;
            DeepSkyObjectSearchVM.PropertyChanged += DeepSkyObjectSearchVM_PropertyChanged;

            SearchCommand = new AsyncCommand<bool>(() => Search());
            CancelSearchCommand = new RelayCommand(CancelSearch);

            InitializeCriteria();

            AnnualPlanningChart = new PlotModel { Title = "Example 1" };
            AnnualPlanningChart.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
        }

        public static readonly double HORIZON_VALUE = double.MinValue;

        private void InitializeCriteria() {

            MinimumAltitudeChoices = new AsyncObservableCollection<KeyValuePair<double, string>>();
            for (int i = 0; i <= 60; i += 5) {
                MinimumAltitudeChoices.Add(new KeyValuePair<double, string>(i, i + "°"));
            }

            // Add 'Above Horizon' to altitude choices to trigger use of custom horizon (if available)
            MinimumAltitudeChoices.Add(new KeyValuePair<double, string>(HORIZON_VALUE, "Above Horizon"));

            MinimumTimeChoices = new AsyncObservableCollection<KeyValuePair<int, string>>();
            MinimumTimeChoices.Add(new KeyValuePair<int, string>(0, Loc.Instance["LblAny"]));
            for (int i = 30; i <= 240; i += 30) {
                MinimumTimeChoices.Add(new KeyValuePair<int, string>(i, Utils.MtoHM(i)));
            }

            MoonSeparationChoices = new AsyncObservableCollection<KeyValuePair<double, string>>();
            MoonSeparationChoices.Add(new KeyValuePair<double, string>(0, Loc.Instance["LblAny"]));
            for (double i = 5; i < 180; i += 5) {
                MoonSeparationChoices.Add(new KeyValuePair<double, string>(i, i + "°"));
            }

            MaximumMoonIlluminationChoices = new AsyncObservableCollection<KeyValuePair<double, string>>();
            MaximumMoonIlluminationChoices.Add(new KeyValuePair<double, string>(0, Loc.Instance["LblAny"]));
            for (int i = 10; i < 100; i += 10) {
                MaximumMoonIlluminationChoices.Add(new KeyValuePair<double, string>(((double)i) / 100, i + "%"));
            }

            MeridianTimeSpanChoices = new AsyncObservableCollection<KeyValuePair<int, string>>();
            MeridianTimeSpanChoices.Add(new KeyValuePair<int, string>(0, Loc.Instance["LblAny"]));
            for (int i = 30; i <= 240; i += 30) {
                MeridianTimeSpanChoices.Add(new KeyValuePair<int, string>(i, Utils.MtoHM(i)));
            }
        }

        public override Task Teardown() {
            profileService.ProfileChanged -= ProfileService_ProfileChanged;
            profileService.ActiveProfile.AstrometrySettings.PropertyChanged -= ProfileService_ProfileChanged;
            DeepSkyObjectSearchVM.PropertyChanged -= DeepSkyObjectSearchVM_PropertyChanged;
            return base.Teardown();
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            SearchResult = null;
            InitializeCriteria();

            RaisePropertyChanged(nameof(StartDate));
            RaisePropertyChanged(nameof(PlanDays));
            RaisePropertyChanged(nameof(MinimumTime));
            RaisePropertyChanged(nameof(MinimumAltitude));
            RaisePropertyChanged(nameof(MinimumMoonSeparation));
            RaisePropertyChanged(nameof(MaximumMoonIllumination));
            RaisePropertyChanged(nameof(MeridianTimeSpan));
            RaisePropertyChanged(nameof(MoonAvoidanceEnabled));
            RaisePropertyChanged(nameof(MoonAvoidanceWidth));

            if (profileService.ActiveProfile != null) {
                profileService.ActiveProfile.AstrometrySettings.PropertyChanged -= ProfileService_ProfileChanged;
                profileService.ActiveProfile.AstrometrySettings.PropertyChanged += ProfileService_ProfileChanged;
            }
        }

        private DeepSkyObject _dSO;

        public DeepSkyObject DSO {
            get {
                return _dSO;
            }
            set {
                _dSO = value;
                RaisePropertyChanged();
            }
        }

        private void DeepSkyObjectSearchVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DeepSkyObjectSearchVM.Coordinates) && DeepSkyObjectSearchVM.Coordinates != null) {
                DSO = new DeepSkyObject(DeepSkyObjectSearchVM.TargetName, DeepSkyObjectSearchVM.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository, profileService.ActiveProfile.AstrometrySettings.Horizon);
                RaiseCoordinatesChanged();
            }
            else if (e.PropertyName == nameof(DeepSkyObjectSearchVM.TargetName) && DSO != null) {
                DSO.Name = DeepSkyObjectSearchVM.TargetName;
            }
        }

        public IDeepSkyObjectSearchVM DeepSkyObjectSearchVM { get; private set; }

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

        public bool MoonAvoidanceEnabled {
            get => pluginSettings.GetValueBoolean(nameof(MoonAvoidanceEnabled), false);
            set {
                pluginSettings.SetValueBoolean(nameof(MoonAvoidanceEnabled), value);
                RaisePropertyChanged();
            }
        }

        public int MoonAvoidanceWidth {
            get => pluginSettings.GetValueInt32(nameof(MoonAvoidanceWidth), 14);
            set {
                pluginSettings.SetValueInt32(nameof(MoonAvoidanceWidth), value);
                RaisePropertyChanged();
            }
        }

        private PlotModel _annualPlanningChart;
        public PlotModel AnnualPlanningChart {
            get => _annualPlanningChart;
            set {
                _annualPlanningChart = value;
            }
        }

        public ICommand CancelSearchCommand { get; private set; }

        private CancellationTokenSource _searchTokenSource;

        private void CancelSearch(object obj) {
            try { _searchTokenSource?.Cancel(); } catch { }
        }

        public ICommand SearchCommand { get; private set; }

        private async Task<bool> Search() {
            _searchTokenSource?.Dispose();
            _searchTokenSource = new CancellationTokenSource();

            return await Task.Run(() => {

                // Leaving this fixed - at least for now: Target Planning isn't meant to determine exact timings for a real session.
                double horizonOffset = 0;

                PlanParameters planParams = new PlanParameters();
                planParams.Target = DSO;
                planParams.ObserverInfo = getObserverInfo(profileService.ActiveProfile.AstrometrySettings);
                planParams.StartDate = StartDate;
                planParams.PlanDays = PlanDays;
                planParams.HorizonDefinition = MinimumAltitude != HORIZON_VALUE ?
                        new HorizonDefinition(MinimumAltitude) :
                        new HorizonDefinition(profileService.ActiveProfile.AstrometrySettings.Horizon, horizonOffset);
                planParams.MinimumImagingTime = MinimumTime;
                planParams.MeridianTimeSpan = MeridianTimeSpan;
                planParams.MinimumMoonSeparation = MinimumMoonSeparation;
                planParams.MaximumMoonIllumination = MaximumMoonIllumination;
                planParams.MoonAvoidanceEnabled = MoonAvoidanceEnabled;
                planParams.MoonAvoidanceWidth = MoonAvoidanceWidth;

                Logger.Debug($"Starting Target Planning for: {Utils.FormatDateTimeFull(planParams.StartDate)}, {planParams.PlanDays} days");
                Logger.Debug($"          Target: {planParams.Target.Name} RA: {planParams.Target.Coordinates.RA} Dec: {planParams.Target.Coordinates.Dec}");
                Logger.Trace($"    Location Lat: {planParams.ObserverInfo.Latitude} Long: {planParams.ObserverInfo.Longitude}, Ele: {planParams.ObserverInfo.Elevation}\n");

                if (MinimumAltitude == HORIZON_VALUE) {
                    Logger.Debug($"         Min alt: n/a");
                    Logger.Debug($"  Custom horizon: applies, offset: {horizonOffset}\n");
                }
                else {
                    Logger.Debug($"         Min alt: {MinimumAltitude}");
                    Logger.Debug($"  Custom horizon: n/a\n");
                }

                Logger.Debug($"        Min time: {planParams.MinimumImagingTime}");
                Logger.Debug($"   Meridian span: {planParams.MeridianTimeSpan}\n");
                Logger.Debug($"  Max moon illum: {planParams.MaximumMoonIllumination}");
                Logger.Debug($"    Min moon sep: {planParams.MinimumMoonSeparation}");
                Logger.Debug($"      Moon avoid: {planParams.MoonAvoidanceEnabled}");
                Logger.Debug($"Moon avoid width: {planParams.MoonAvoidanceWidth}");

                try {
                    SearchResult = null;
                    IEnumerable<ImagingDayPlan> results = new PlanGenerator(planParams).Generate(_searchTokenSource.Token);

                    List<ImagingDayPlanViewAdapter> wrappedResults = new List<ImagingDayPlanViewAdapter>(results.Count());
                    foreach (ImagingDayPlan plan in results) {
                        wrappedResults.Add(new ImagingDayPlanViewAdapter(plan, new ImagingDayPlanContext(planParams, profileService)));
                    }

                    LogResults(planParams, wrappedResults);
                    SearchResult = new PagedList<ImagingDayPlanViewAdapter>(22, wrappedResults);
                }
                catch (OperationCanceledException) {
                    Logger.Debug("target planning canceled");
                    SearchResult = null;
                }
                catch (Exception ex) {
                    Logger.Error($"target planning search exception: {ex.Message} {ex.StackTrace}");
                    SearchResult = null;
                }

                return true;
            });
        }

        public ICommand ShowDetailCommand { get; private set; }

        private void LogResults(PlanParameters planParams, IEnumerable<ImagingDayPlanViewAdapter> results) {
            Logger.Trace("Search results:");
            foreach (ImagingDayPlanViewAdapter plan in results) {
                Logger.Trace($"      status: {plan.StatusMessage}");
                Logger.Trace($"  start time: {Utils.FormatDateTimeFull(plan.StartImagingTime)}");
                Logger.Trace($"    end time: {Utils.FormatDateTimeFull(plan.EndImagingTime)}");
                Logger.Trace($"        time: {plan.ImagingTime}");

                Logger.Trace($" start limit: {plan.StartLimitingFactor}");
                Logger.Trace($"   end limit: {plan.EndLimitingFactor}");

                Logger.Trace($"  moon illum: {plan.MoonIllumination}");
                Logger.Trace($"    moon sep: {plan.MoonSeparation}");

                if (planParams.MoonAvoidanceEnabled) {
                    Logger.Trace($"   avoid sep: {plan.MoonAvoidanceSeparation}\n");
                }
                else {
                    Logger.Trace($"   avoid sep: n/a\n");
                }
            }
        }

        private ObserverInfo getObserverInfo(IAstrometrySettings astrometrySettings) {
            ObserverInfo oi = new ObserverInfo();
            oi.Latitude = astrometrySettings.Latitude;
            oi.Longitude = astrometrySettings.Longitude;
            oi.Elevation = astrometrySettings.Elevation;
            return oi;
        }

        public PagedList<ImagingDayPlanViewAdapter> SearchResult {
            get {
                return _searchResult;
            }

            set {
                _searchResult = value;
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int RAHours {
            get {
                return (int)Math.Truncate(DSO.Coordinates.RA);
            }
            set {
                if (value >= 0) {
                    DSO.Coordinates.RA = DSO.Coordinates.RA - RAHours + value;
                    RaiseCoordinatesChanged();
                }
            }
        }

        public int RAMinutes {
            get {
                var minutes = (Math.Abs(DSO.Coordinates.RA * 60.0d) % 60);
                var seconds = (int)Math.Round((Math.Abs(DSO.Coordinates.RA * 60.0d * 60.0d) % 60));
                if (seconds > 59) {
                    minutes += 1;
                }

                return (int)Math.Floor(minutes);
            }
            set {
                if (value >= 0) {
                    DSO.Coordinates.RA = DSO.Coordinates.RA - RAMinutes / 60.0d + value / 60.0d;
                    RaiseCoordinatesChanged();
                }
            }
        }

        public int RASeconds {
            get {
                var seconds = (int)Math.Round((Math.Abs(DSO.Coordinates.RA * 60.0d * 60.0d) % 60));
                if (seconds > 59) {
                    seconds = 0;
                }
                return seconds;
            }
            set {
                if (value >= 0) {
                    DSO.Coordinates.RA = DSO.Coordinates.RA - RASeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                    RaiseCoordinatesChanged();
                }
            }
        }

        private bool negativeDec;

        public bool NegativeDec {
            get => negativeDec;
            set {
                negativeDec = value;
                RaisePropertyChanged();
            }
        }

        public int DecDegrees {
            get {
                return (int)Math.Truncate(DSO.Coordinates.Dec);
            }
            set {
                if (NegativeDec) {
                    DSO.Coordinates.Dec = value - DecMinutes / 60.0d - DecSeconds / (60.0d * 60.0d);
                }
                else {
                    DSO.Coordinates.Dec = value + DecMinutes / 60.0d + DecSeconds / (60.0d * 60.0d);
                }
                RaiseCoordinatesChanged();
            }
        }

        public int DecMinutes {
            get {
                var minutes = (Math.Abs(DSO.Coordinates.Dec * 60.0d) % 60);
                var seconds = (int)Math.Round((Math.Abs(DSO.Coordinates.Dec * 60.0d * 60.0d) % 60));
                if (seconds > 59) {
                    minutes += 1;
                }

                return (int)Math.Floor(minutes);
            }
            set {
                if (NegativeDec) {
                    DSO.Coordinates.Dec = DSO.Coordinates.Dec + DecMinutes / 60.0d - value / 60.0d;
                }
                else {
                    DSO.Coordinates.Dec = DSO.Coordinates.Dec - DecMinutes / 60.0d + value / 60.0d;
                }

                RaiseCoordinatesChanged();
            }
        }

        public int DecSeconds {
            get {
                var seconds = (int)Math.Round((Math.Abs(DSO.Coordinates.Dec * 60.0d * 60.0d) % 60));
                if (seconds > 59) {
                    seconds = 0;
                }
                return seconds;
            }
            set {
                if (NegativeDec) {
                    DSO.Coordinates.Dec = DSO.Coordinates.Dec + DecSeconds / (60.0d * 60.0d) - value / (60.0d * 60.0d);
                }
                else {
                    DSO.Coordinates.Dec = DSO.Coordinates.Dec - DecSeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                }

                RaiseCoordinatesChanged();
            }
        }

        private void RaiseCoordinatesChanged() {
            RaisePropertyChanged(nameof(RAHours));
            RaisePropertyChanged(nameof(RAMinutes));
            RaisePropertyChanged(nameof(RASeconds));
            RaisePropertyChanged(nameof(DecDegrees));
            RaisePropertyChanged(nameof(DecMinutes));
            RaisePropertyChanged(nameof(DecSeconds));
            NegativeDec = DSO?.Coordinates?.Dec < 0;
        }
    }

}