﻿using System;

namespace TargetPlanning.NINAPlugin.Astrometry {

    public class ImagingDayPlan {

        private DateTime _startDate;

        public DateTime StartDay {
            get => _startDate;
            set {
                _startDate = value.Date;
            }
        }

        private DateTime _endDate;

        public DateTime EndDay {
            get => _endDate;
            set {
                _endDate = value.Date;
            }
        }

        public DateTime StartImagingTime { get; private set; }
        public DateTime EndImagingTime { get; private set; }
        public DateTime TransitTime { get; private set; }

        public ImagingLimit StartLimitingFactor { get; private set; }
        public ImagingLimit EndLimitingFactor { get; private set; }

        public double MoonIllumination { get; private set; }
        public double MoonSeparation { get; private set; }
        public double MoonAvoidanceSeparation { get; private set; }

        public ImagingDayPlan(DateTime startImagingTime, DateTime endImagingTime, DateTime transitTime,
            ImagingLimit startLimitingFactor, ImagingLimit endLimitingFactor,
            double moonIllumination, double moonSeparation, double moonAvoidanceSeparation) {

            Validate.Assert.notNull(startLimitingFactor, "startLimitingFactor cannot be null");
            Validate.Assert.notNull(endLimitingFactor, "endLimitingFactor cannot be null");

            StartImagingTime = startImagingTime;
            EndImagingTime = endImagingTime;
            TransitTime = transitTime;
            StartLimitingFactor = startLimitingFactor;
            EndLimitingFactor = endLimitingFactor;
            MoonIllumination = moonIllumination;
            MoonSeparation = moonSeparation;
            MoonAvoidanceSeparation = moonAvoidanceSeparation;

            if (startImagingTime != null) {
                StartDay = startImagingTime;
            }

            if (endImagingTime != null) {
                EndDay = endImagingTime;
            }
        }

        public bool IsAccepted() {
            return !StartLimitingFactor.Session;
        }

        public long GetImagingMinutes() {
            return (long)(NotVisible() || NoTwilight() ? 0 : EndImagingTime.Subtract(StartImagingTime).TotalMinutes);
        }

        public bool NotVisible() {
            return StartLimitingFactor.Equals(ImagingLimit.NotVisible) && EndLimitingFactor.Equals(ImagingLimit.NotVisible);
        }

        public bool NoTwilight() {
            return StartLimitingFactor.Equals(ImagingLimit.NoTwilight) && EndLimitingFactor.Equals(ImagingLimit.NoTwilight);
        }

        public bool NotAboveMinimumImagingTime() {
            return StartLimitingFactor.Equals(ImagingLimit.MinimumTime);
        }

        public bool AboveMaximumMoonIllumination() {
            return StartLimitingFactor.Equals(ImagingLimit.MoonIllumination);
        }

        public bool BelowMinimumMoonSeparation() {
            return StartLimitingFactor.Equals(ImagingLimit.MoonSeparation);
        }

        public bool BelowMinimumMoonAvoidanceSeparation() {
            return StartLimitingFactor.Equals(ImagingLimit.MoonAvoidanceSeparation);
        }

        public String GetStatusMessage() {
            return StartLimitingFactor.Session ? StartLimitingFactor.Name : "yes";
        }

        public String GetStartLimitMessage() {
            return StartLimitingFactor.Session ? "" : StartLimitingFactor.Name;
        }

        public String GetEndLimitMessage() {
            return EndLimitingFactor.Session ? "" : EndLimitingFactor.Name;
        }
    }

    public class ImagingLimit {

        public bool Session { get; private set; }
        public string Name { get; private set; }

        public static ImagingLimit NotVisible { get { return new ImagingLimit(true, "not visible"); } }
        public static ImagingLimit NoTwilight { get { return new ImagingLimit(true, "darkness"); } }
        public static ImagingLimit MinimumTime { get { return new ImagingLimit(true, "not enough time"); } }
        public static ImagingLimit MoonIllumination { get { return new ImagingLimit(true, "moon illum"); } }
        public static ImagingLimit MoonSeparation { get { return new ImagingLimit(true, "moon angle"); } }
        public static ImagingLimit MoonAvoidanceSeparation { get { return new ImagingLimit(true, "moon avoidance"); } }
        public static ImagingLimit Meridian { get { return new ImagingLimit(true, "meridian time span"); } }
        public static ImagingLimit MinimumAltitude { get { return new ImagingLimit(false, "minimum altitude"); } }
        public static ImagingLimit MeridianClip { get { return new ImagingLimit(false, "meridian"); } }
        public static ImagingLimit Twilight { get { return new ImagingLimit(false, "twilight"); } }
        public static ImagingLimit LocalHorizon { get { return new ImagingLimit(false, "custom horizon"); } }

        public bool Equals(ImagingLimit other) {
            return Session == other.Session && Name == other.Name;
        }

        private ImagingLimit(bool session, string name) {
            this.Session = session;
            this.Name = String.Format("  (limited by {0})", name);
        }

    }

}
