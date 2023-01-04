﻿using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TargetPlanning.NINAPlugin.AnnualChart;
using TargetPlanning.NINAPlugin.Astrometry;
using TargetPlanning.NINAPlugin.HTMLOutput;
using TargetPlanning.NINAPlugin.ImagingSeason;

namespace TargetPlanning.NINAPlugin {

    [Export(typeof(IPluginManifest))]
    public class TargetPlanningPlugin : PluginBase, INotifyPropertyChanged {
        private IPluginOptionsAccessor pluginSettings;
        private IProfileService profileService;

        private AsyncObservableCollection<KeyValuePair<double, string>> _minimumAltitudeChoices;
        private AsyncObservableCollection<KeyValuePair<int, string>> _minimumTimeChoices;
        private AsyncObservableCollection<KeyValuePair<double, string>> _moonSeparationChoices;
        private AsyncObservableCollection<KeyValuePair<double, string>> _maximumMoonIlluminationChoices;
        private AsyncObservableCollection<KeyValuePair<int, string>> _meridianTimeSpanChoices;
        private AsyncObservableCollection<KeyValuePair<int, string>> _twilightIncludeChoices;

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

            DailyDetailsCommand = new AsyncCommand<bool>(() => ShowDailyDetails());
            CancelDailyDetailsCommand = new RelayCommand(CancelDaily);
            DailyDetailsHTMLReportCommand = new RelayCommand(DailyDetailsHTMLReport);

            AnnualChartCommand = new AsyncCommand<bool>(() => ShowAnnualChart());
            CancelAnnualCommand = new RelayCommand(CancelAnnual);

            ImagingSeasonCommand = new AsyncCommand<bool>(() => ShowImagingSeason());
            CancelImagingSeasonCommand = new RelayCommand(CancelImagingSeason);
            ImagingSeasonHTMLReportCommand = new RelayCommand(ImagingSeasonHTMLReport);

            InitializeCriteria();
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

            TwilightIncludeChoices = new AsyncObservableCollection<KeyValuePair<int, string>>();
            TwilightIncludeChoices.Add(new KeyValuePair<int, string>(TwilightTimeCache.TWILIGHT_INCLUDE_NONE, "None"));
            TwilightIncludeChoices.Add(new KeyValuePair<int, string>(TwilightTimeCache.TWILIGHT_INCLUDE_ASTRO, "Astronomical"));
            TwilightIncludeChoices.Add(new KeyValuePair<int, string>(TwilightTimeCache.TWILIGHT_INCLUDE_NAUTICAL, "Nautical"));
            TwilightIncludeChoices.Add(new KeyValuePair<int, string>(TwilightTimeCache.TWILIGHT_INCLUDE_CIVIL, "Civil"));
        }

        public override Task Teardown() {
            profileService.ProfileChanged -= ProfileService_ProfileChanged;
            profileService.ActiveProfile.AstrometrySettings.PropertyChanged -= ProfileService_ProfileChanged;
            DeepSkyObjectSearchVM.PropertyChanged -= DeepSkyObjectSearchVM_PropertyChanged;
            return base.Teardown();
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            DailyDetailsResults = null;
            EmptyReport = true;
            DailyDetailsEnabled = false;
            AnnualChartEnabled = false;

            InitializeCriteria();

            RaisePropertyChanged(nameof(StartDate));
            RaisePropertyChanged(nameof(PlanDays));
            RaisePropertyChanged(nameof(MinimumTime));
            RaisePropertyChanged(nameof(MinimumAltitude));
            RaisePropertyChanged(nameof(MinimumMoonSeparation));
            RaisePropertyChanged(nameof(MaximumMoonIllumination));
            RaisePropertyChanged(nameof(MeridianTimeSpan));
            RaisePropertyChanged(nameof(TwilightInclude));
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
                Logger.Trace($"{_dSO.Name} {_dSO.Coordinates.RA} {_dSO.Coordinates.Dec}");
                RaisePropertyChanged();
            }
        }

        private void DeepSkyObjectSearchVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DeepSkyObjectSearchVM.Coordinates) && DeepSkyObjectSearchVM.Coordinates != null) {
                DSO = new DeepSkyObject(DeepSkyObjectSearchVM.TargetName, DeepSkyObjectSearchVM.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository, profileService.ActiveProfile.AstrometrySettings.Horizon);
                RaiseCoordinatesChanged(false);
            }
            else if (e.PropertyName == nameof(DeepSkyObjectSearchVM.TargetName) && DSO != null) {
                DSO.Name = DeepSkyObjectSearchVM.TargetName;
            }
        }

        public IDeepSkyObjectSearchVM DeepSkyObjectSearchVM { get; private set; }

        public DateTime StartDate {
            get => pluginSettings.GetValueDateTime(nameof(StartDate), DateTime.Now.Date);
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

        public int TwilightInclude {
            get => pluginSettings.GetValueInt32(nameof(TwilightInclude), TwilightTimeCache.TWILIGHT_INCLUDE_ASTRO);
            set {
                pluginSettings.SetValueInt32(nameof(TwilightInclude), value);
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<int, string>> TwilightIncludeChoices {
            get {
                return _twilightIncludeChoices;
            }
            set {
                _twilightIncludeChoices = value;
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
            get => pluginSettings.GetValueInt32(nameof(MoonAvoidanceWidth), 7);
            set {
                pluginSettings.SetValueInt32(nameof(MoonAvoidanceWidth), value);
                RaisePropertyChanged();
            }
        }

        public ICommand CancelDailyDetailsCommand { get; private set; }

        private CancellationTokenSource _dailyDetailsCommandTokenSource;

        private void CancelDaily(object obj) {
            try { _dailyDetailsCommandTokenSource?.Cancel(); } catch { }
        }

        public ICommand DailyDetailsCommand { get; private set; }

        private bool _dailyDetailsEnabled = false;
        public bool DailyDetailsEnabled {
            get => _dailyDetailsEnabled;
            set {
                _dailyDetailsEnabled = value;
                RaisePropertyChanged();
            }
        }

        public ICommand DailyDetailsHTMLReportCommand { get; private set; }

        private void DailyDetailsHTMLReport(object obj) {
            try {
                System.Diagnostics.Process.Start(HTMLReport.GetDailyDetailsHTMLReportPath());
            }
            catch (Exception ex) {
                Logger.Error($"DailyDetails HTML report exception: {ex.Message} {ex.StackTrace}");
            }
        }

        private async Task<bool> ShowDailyDetails() {
            AnnualChartEnabled = false;
            DailyDetailsEnabled = false;

            _dailyDetailsCommandTokenSource?.Dispose();
            _dailyDetailsCommandTokenSource = new CancellationTokenSource();

            return await Task.Run(() => {

                PlanParameters planParams = GetCurrentPlanParameters();

                try {
                    DailyDetailsResults = null;
                    IEnumerable<ImagingDayPlan> results = new PlanGenerator(planParams).Generate(_dailyDetailsCommandTokenSource.Token);
                    new HTMLReport(planParams).CreateDailyDetailsHTMLReport(results);

                    List<ImagingDayPlanViewAdapter> wrappedResults = new List<ImagingDayPlanViewAdapter>(results.Count());
                    foreach (ImagingDayPlan plan in results) {
                        wrappedResults.Add(new ImagingDayPlanViewAdapter(plan, new ImagingDayPlanContext(planParams, profileService)));
                    }

                    LogResults(planParams, wrappedResults);
                    DailyDetailsResults = new PagedList<ImagingDayPlanViewAdapter>(22, wrappedResults);

                    EmptyReport = false;
                    AnnualChartEnabled = false;
                    DailyDetailsEnabled = true;
                    ImagingSeasonEnabled = false;
                }
                catch (OperationCanceledException) {
                    DailyDetailsResults = null;
                }
                catch (Exception ex) {
                    Logger.Error($"daily details exception: {ex.Message} {ex.StackTrace}");
                    DailyDetailsResults = null;
                }

                return true;
            });
        }

        private PlanParameters GetCurrentPlanParameters() {
            PlanParameters planParams = new PlanParameters();
            planParams.Target = DSO;
            planParams.ObserverInfo = getObserverInfo(profileService.ActiveProfile.AstrometrySettings);
            planParams.StartDate = StartDate;
            planParams.PlanDays = PlanDays;
            planParams.HorizonDefinition = MinimumAltitude != HORIZON_VALUE ?
                    new HorizonDefinition(MinimumAltitude) :
                    new HorizonDefinition(profileService.ActiveProfile.AstrometrySettings.Horizon, 0);
            planParams.MinimumImagingTime = MinimumTime;
            planParams.MeridianTimeSpan = MeridianTimeSpan;
            planParams.TwilightInclude = TwilightInclude;
            planParams.MinimumMoonSeparation = MinimumMoonSeparation;
            planParams.MaximumMoonIllumination = MaximumMoonIllumination;
            planParams.MoonAvoidanceEnabled = MoonAvoidanceEnabled;
            planParams.MoonAvoidanceWidth = MoonAvoidanceWidth;

            Logger.Debug($"Starting planning for: {Utils.FormatDateTimeFull(planParams.StartDate)}, {planParams.PlanDays} days");
            Logger.Debug($"          Target: {planParams.Target.Name} RA: {planParams.Target.Coordinates.RA} Dec: {planParams.Target.Coordinates.Dec}");
            Logger.Trace($"    Location Lat: {planParams.ObserverInfo.Latitude} Long: {planParams.ObserverInfo.Longitude}, Ele: {planParams.ObserverInfo.Elevation}\n");

            if (MinimumAltitude == HORIZON_VALUE) {
                Logger.Debug($"         Min alt: n/a");
                Logger.Debug($"  Custom horizon: applies\n");
            }
            else {
                Logger.Debug($"         Min alt: {MinimumAltitude}");
                Logger.Debug($"  Custom horizon: n/a\n");
            }

            Logger.Debug($"        Min time: {planParams.MinimumImagingTime}");
            Logger.Debug($"Twilight include: {planParams.TwilightInclude}");
            Logger.Debug($"   Meridian span: {planParams.MeridianTimeSpan}\n");
            Logger.Debug($"  Max moon illum: {planParams.MaximumMoonIllumination}");
            Logger.Debug($"    Min moon sep: {planParams.MinimumMoonSeparation}");
            Logger.Debug($"      Moon avoid: {planParams.MoonAvoidanceEnabled}");
            Logger.Debug($"Moon avoid width: {planParams.MoonAvoidanceWidth}");

            return planParams;
        }

        private AnnualPlanningChartModel _annualPlanningChartModel;
        public AnnualPlanningChartModel AnnualPlanningChartModel {
            get => _annualPlanningChartModel;
            set {
                _annualPlanningChartModel = value;
                RaisePropertyChanged();
            }
        }

        private bool _annualChartEnabled = false;
        public bool AnnualChartEnabled {
            get => _annualChartEnabled;
            set {
                _annualChartEnabled = value;
                RaisePropertyChanged();
            }
        }

        public ICommand CancelAnnualCommand { get; private set; }

        private CancellationTokenSource _annualTokenSource;

        private void CancelAnnual(object obj) {
            try { _annualTokenSource?.Cancel(); } catch { }
        }

        public ICommand AnnualChartCommand { get; private set; }

        private async Task<bool> ShowAnnualChart() {
            _annualTokenSource?.Dispose();
            _annualTokenSource = new CancellationTokenSource();

            return await Task.Run(() => {
                AnnualChartEnabled = false;
                DailyDetailsEnabled = false;
                ImagingSeasonEnabled = false;

                try {
                    AnnualPlanningChartModel = new AnnualPlanningChartModel(getObserverInfo(profileService.ActiveProfile.AstrometrySettings), DSO, StartDate, _annualTokenSource.Token);
                    EmptyReport = false;
                    DailyDetailsEnabled = false;
                    AnnualChartEnabled = true;
                    ImagingSeasonEnabled = false;
                }
                catch (OperationCanceledException) {
                    AnnualPlanningChartModel = null;
                }
                catch (Exception ex) {
                    Logger.Error($"annual chart exception: {ex.Message} {ex.StackTrace}");
                    AnnualPlanningChartModel = null;
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

        private ImagingSeasonPlotModel _imagingSeasonPlotModel;
        public ImagingSeasonPlotModel ImagingSeasonPlotModel {
            get => _imagingSeasonPlotModel;
            set {
                _imagingSeasonPlotModel = value;
                RaisePropertyChanged();
            }
        }

        private bool _imagingSeasonEnabled = false;
        public bool ImagingSeasonEnabled {
            get => _imagingSeasonEnabled;
            set {
                _imagingSeasonEnabled = value;
                RaisePropertyChanged();
            }
        }

        public ICommand CancelImagingSeasonCommand { get; private set; }

        private CancellationTokenSource _imagingSeasonTokenSource;

        private void CancelImagingSeason(object obj) {
            try { _imagingSeasonTokenSource?.Cancel(); } catch { }
        }

        public ICommand ImagingSeasonCommand { get; private set; }

        public ICommand ImagingSeasonHTMLReportCommand { get; private set; }

        private void ImagingSeasonHTMLReport(object obj) {
            try {
                System.Diagnostics.Process.Start(HTMLReport.GetImagingSeasonHTMLReportPath());
            }
            catch (Exception ex) {
                Logger.Error($"ImagingSeason HTML report exception: {ex.Message} {ex.StackTrace}");
            }
        }

        private async Task<bool> ShowImagingSeason() {
            _imagingSeasonTokenSource?.Dispose();
            _imagingSeasonTokenSource = new CancellationTokenSource();

            return await Task.Run(() => {
                AnnualChartEnabled = false;
                DailyDetailsEnabled = false;
                ImagingSeasonEnabled = false;

                PlanParameters planParams = GetCurrentPlanParameters();

                try {
                    IList<ImagingDayPlan> results = new OptimalImagingSeason().GetOptimalSeason(planParams, _imagingSeasonTokenSource.Token);
                    ImagingSeasonPlotModel = new ImagingSeasonPlotModel(profileService.ActiveProfile, planParams, results);
                    new HTMLReport(planParams).CreateImagingSeasonHTMLReport(results, ImagingSeasonPlotModel.GetPlotModel());
                    EmptyReport = false;
                    ImagingSeasonEnabled = true;
                }
                catch (OperationCanceledException) {
                    ImagingSeasonPlotModel = null;
                }
                catch (Exception ex) {
                    Logger.Error($"imaging season exception: {ex.Message} {ex.StackTrace}");
                    ImagingSeasonPlotModel = null;
                }

                return true;
            });

        }

        private ObserverInfo getObserverInfo(IAstrometrySettings astrometrySettings) {
            ObserverInfo oi = new ObserverInfo();
            oi.Latitude = astrometrySettings.Latitude;
            oi.Longitude = astrometrySettings.Longitude;
            oi.Elevation = astrometrySettings.Elevation;
            return oi;
        }

        private PagedList<ImagingDayPlanViewAdapter> _dailyDetailsResults = null;

        public PagedList<ImagingDayPlanViewAdapter> DailyDetailsResults {
            get {
                return _dailyDetailsResults;
            }

            set {
                _dailyDetailsResults = value;
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
                    RaiseCoordinatesChanged(true);
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
                    RaiseCoordinatesChanged(true);
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
                    RaiseCoordinatesChanged(true);
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
                RaiseCoordinatesChanged(true);
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

                RaiseCoordinatesChanged(true);
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

                RaiseCoordinatesChanged(true);
            }
        }

        private bool _dsoIsSelected = false;
        public bool DSOIsSelected {
            get => _dsoIsSelected;
            set {
                _dsoIsSelected = value;
                RaisePropertyChanged();
            }
        }

        private bool _emptyReport = true;
        public bool EmptyReport {
            get => _emptyReport;
            set {
                _emptyReport = value;
                RaisePropertyChanged();
            }
        }

        private void RaiseCoordinatesChanged(bool manualChange) {
            RaisePropertyChanged(nameof(RAHours));
            RaisePropertyChanged(nameof(RAMinutes));
            RaisePropertyChanged(nameof(RASeconds));
            RaisePropertyChanged(nameof(DecDegrees));
            RaisePropertyChanged(nameof(DecMinutes));
            RaisePropertyChanged(nameof(DecSeconds));
            NegativeDec = DSO?.Coordinates?.Dec < 0;

            if (DSO?.Coordinates != null && (DSO.Coordinates.RA != 0 || DSO.Coordinates.Dec == 0)) {
                DSOIsSelected = true;
            }

            if (manualChange) {
                DeepSkyObjectSearchVM.TargetName = "";
                DSO.Name = null;
            }
        }
    }

}