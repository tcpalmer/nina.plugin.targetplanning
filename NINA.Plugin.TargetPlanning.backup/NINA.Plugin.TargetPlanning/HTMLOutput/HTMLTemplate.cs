using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TargetPlanning.NINAPlugin.Astrometry;

namespace TargetPlanning.NINAPlugin.HTMLOutput {

    internal class HTMLTemplate {

        internal static string ReplacePreamble(PlanParameters planParameters) {
            return Regex.Replace(Preamble, "TARGET-NAME", GetTargetName(planParameters));
        }

        internal static string ReplaceHeader(PlanParameters planParameters, IEnumerable<ImagingDayPlan> results) {
            Tuple<DateTime, DateTime> dateBounds = GetDateBounds(results);
            string s = Regex.Replace(Header, "TARGET-NAME", GetTargetName(planParameters));
            s = Regex.Replace(s, "START-DATE", $"{dateBounds.Item1:MMMM d, yyyy}");
            s = Regex.Replace(s, "END-DATE", $"{dateBounds.Item2:MMMM d, yyyy}");
            s = Regex.Replace(s, "PLUGIN-VERSION", GetPluginVersion());
            s = Regex.Replace(s, "REPORT-DATE", $"{DateTime.Now:MMM d, yyyy HH:mm}");
            s = Regex.Replace(s, "TARGET-RA", GetRAString(planParameters));
            s = Regex.Replace(s, "TARGET-DEC", planParameters.Target.Coordinates.DecString);
            s = Regex.Replace(s, "LATITUDE", planParameters.ObserverInfo.Latitude.ToString());
            s = Regex.Replace(s, "LONGITUDE", planParameters.ObserverInfo.Longitude.ToString());
            s = Regex.Replace(s, "MINIMUM-ALTITUDE", GetMinAlt(planParameters.HorizonDefinition));
            s = Regex.Replace(s, "MINIMUM-IMAGING-TIME", MinutesZeroCheck(planParameters.MinimumImagingTime));
            s = Regex.Replace(s, "TWILIGHT-INCLUDE", GetTwilightInclude(planParameters.TwilightInclude));
            s = Regex.Replace(s, "MERIDIAN-TIME-SPAN", MinutesZeroCheck(planParameters.MeridianTimeSpan));
            s = Regex.Replace(s, "MAXIMUM-MOON-ILLUMINATION", planParameters.MoonAvoidanceEnabled ? "n/a" : ValueZeroCheck(planParameters.MaximumMoonIllumination * 100, "%"));
            s = Regex.Replace(s, "MINIMUM-MOON-SEPARATION", ValueZeroCheck(planParameters.MinimumMoonSeparation, "&deg;"));
            s = Regex.Replace(s, "MOON-AVOIDANCE", planParameters.MoonAvoidanceEnabled ? "on" : "off");
            s = Regex.Replace(s, "MOON-WIDTH", planParameters.MoonAvoidanceEnabled ? planParameters.MoonAvoidanceWidth.ToString() : "n/a");
            return s;
        }

        private static Tuple<DateTime, DateTime> GetDateBounds(IEnumerable<ImagingDayPlan> results) {
            DateTime startDate = results.First().StartDay;
            DateTime endDate = results.Last().StartDay;
            return new Tuple<DateTime, DateTime>(startDate, endDate);
        }

        private static string GetRAString(PlanParameters planParameters) {
            string[] a = planParameters.Target.Coordinates.RAString.Split(':');
            return $"{int.Parse(a[0])}h {int.Parse(a[1])}m {int.Parse(a[2])}s";
        }

        private static string GetTargetName(PlanParameters planParameters) {
            return planParameters.Target.Name != null ? planParameters.Target.Name : "manually entered";
        }

        private static string GetPluginVersion() {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private static string GetMinAlt(HorizonDefinition horizonDefinition) {
            return horizonDefinition.IsCustom() ? "local horizon" : $"{horizonDefinition.GetFixedMinimumAltitude()}&deg;";
        }

        private static string MinutesZeroCheck(int value) {
            return value == 0 ? "Any" : Utils.MtoHM(value);
        }

        private static string ValueZeroCheck(double value, string metric) {
            return value == 0 ? "Any" : value.ToString() + metric;
        }

        private static string GetTwilightInclude(int twilightInclude) {
            switch (twilightInclude) {
                case TwilightTimeCache.TWILIGHT_INCLUDE_NONE:
                    return "None";
                case TwilightTimeCache.TWILIGHT_INCLUDE_ASTRO:
                    return "Astronomical";
                case TwilightTimeCache.TWILIGHT_INCLUDE_NAUTICAL:
                    return "Nautical";
                case TwilightTimeCache.TWILIGHT_INCLUDE_CIVIL:
                    return "Civil";
                default: return "?";
            }
        }

        internal static string AddPlot(string plotSVG) {
            return Regex.Replace(PlotSVG, "PLOT-SVG", plotSVG);
        }

        internal static string ReplaceResultsRow(PlanParameters planParameters, ImagingDayPlan plan) {
            string s = plan.IsAccepted() ? ResultRowAccepted : ResultRowRejected;
            s = Regex.Replace(s, "START-TIME", $"{plan.StartImagingTime:MM/dd HH:mm}");
            s = Regex.Replace(s, "END-TIME", $"{plan.EndImagingTime:MM/dd HH:mm}");
            s = Regex.Replace(s, "IMAGING-TIME", GetImagingTimeValue(planParameters, plan));
            s = Regex.Replace(s, "TRANSIT-TIME", $"{plan.TransitTime:MM/dd HH:mm}");
            s = Regex.Replace(s, "MOON-ILLUMINATION", GetMoonIlluminationValue(planParameters, plan));
            s = Regex.Replace(s, "MOON-SEPARATION", GetMoonSeparationValue(planParameters, plan));
            s = Regex.Replace(s, "START-LIMIT", GetLimitName(plan.StartLimitingFactor));
            s = Regex.Replace(s, "END-LIMIT", GetLimitName(plan.EndLimitingFactor));

            return s;
        }

        private static string GetImagingTimeValue(PlanParameters planParameters, ImagingDayPlan plan) {
            string value = Utils.MtoHM((int)plan.GetImagingMinutes());
            return (plan.GetImagingMinutes() < planParameters.MinimumImagingTime) ? $"<td class=\"limit\">{value}</td>" : $"<td>{value}</td>";
        }

        private static string GetMoonIlluminationValue(PlanParameters planParameters, ImagingDayPlan plan) {
            string value = string.Format("{0:F0}", plan.MoonIllumination * 100);

            if (planParameters.MaximumMoonIllumination == 0 || planParameters.MoonAvoidanceEnabled) {
                return $"<td>{value}%</td>";
            }

            return (plan.MoonIllumination > planParameters.MaximumMoonIllumination) ? $"<td class=\"limit\">{value}%</td>" : $"<td>{value}%</td>";
        }

        private static string GetMoonSeparationValue(PlanParameters planParameters, ImagingDayPlan plan) {
            string value = string.Format("{0:F0}", plan.MoonSeparation);

            if (planParameters.MinimumMoonSeparation == 0) {
                return $"<td>{value}&deg;</td>";
            }

            if (planParameters.MoonAvoidanceEnabled) {
                return (plan.MoonSeparation < plan.MoonAvoidanceSeparation) ? $"<td class=\"limit\">{value}&deg;</td>" : $"<td>{value}&deg;</td>";
            }
            else {
                return (plan.MoonSeparation < planParameters.MinimumMoonSeparation) ? $"<td class=\"limit\">{value}&deg;</td>" : $"<td>{value}&deg;</td>";
            }
        }

        private static string GetLimitName(ImagingLimit limit) {

            if (limit.Equals(ImagingLimit.NotVisible)) {
                return "not visible";
            }

            if (limit.Equals(ImagingLimit.MinimumTime)) {
                return "not enough time";
            }

            if (limit.Equals(ImagingLimit.MoonIllumination)) {
                return "moon illumination";
            }

            if (limit.Equals(ImagingLimit.MoonSeparation)) {
                return "moon separation";
            }

            if (limit.Equals(ImagingLimit.MoonAvoidanceSeparation)) {
                return "moon avoidance";
            }

            if (limit.Equals(ImagingLimit.Meridian)) {
                return "meridian span";
            }

            if (limit.Equals(ImagingLimit.MinimumAltitude)) {
                return "altitude";
            }

            if (limit.Equals(ImagingLimit.MeridianClip)) {
                return "meridian clip";
            }

            if (limit.Equals(ImagingLimit.Twilight)) {
                return "twilight";
            }

            if (limit.Equals(ImagingLimit.LocalHorizon)) {
                return "local horizon";
            }

            return $"unknown limiting factor: {limit.Name}";
        }

        static string Preamble =
   @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <link href=""https://fonts.cdnfonts.com/css/segoe-ui-4"" rel=""stylesheet"">
    <title>Target Planning: TARGET-NAME</title>
    <style>
        body {
            margin: 1cm;
            font-family: Helvetica, Verdana, sans-serif;
            font-size: small;
            background-color: #1e1e1e;
            color: #c7c6c3;
        }

        .title {
            display: flex;
            width: 950px;
            height: 146px;
            border: 1px solid #c7c6c3;
            line-height: 1.5em;
            margin: auto;
        }

        .plot {
            width: 950px;
            border: 1px solid #c7c6c3;
            margin: auto;
        }

        .headwrap {
            display: flex;
            width: 950px;
            height: 95px;
            border: 1px solid #c7c6c3;
            margin: auto;
        }

        .headblock {
            border-right: 1px solid #c7c6c3;
            padding: 10px;
        }

        .headblock td {
            padding: 3px 20px 0 3px;
        }

        .report {
            display: flex;
            width: 950px;
            border: 1px solid #c7c6c3;
            line-height: 1.5em;
            margin: auto;
            padding-top: 10px;
        }

        table {
            width: 100%;
            border-collapse: collapse;
        }

        .report th, td {
            padding: 10px;
        }

        .report th {
            text-align:left;
            border-bottom: 1px solid #c7c6c3;
        }

        .report tr:nth-of-type(even) {
            background-color: #323232
        }

        .report td.limit {
            font-weight: bold;
            color: #ff4b4b
        }

        .report td.status {
            padding-top: 0;
            padding-bottom: 0
        }

    </style>
</head>
<body>
<svg style=""display: none"">
    <defs>
        <style>
            .accepted-clr {
                fill: #00a912;
            }

            .rejected-clr {
                fill: #ff4b4b;
            }

            .accepted-clr, .rejected-clr {
                fill-rule: evenodd;
            }
        </style>
        <symbol id=""accepted"" viewBox=""0 0 122.88 122.88"">
            <title>accepted</title>
            <path class=""accepted-clr""
                  d=""M42.37,51.68,53.26,62,79,35.87c2.13-2.16,3.47-3.9,6.1-1.19l8.53,8.74c2.8,2.77,2.66,4.4,0,7L58.14,85.34c-5.58,5.46-4.61,5.79-10.26.19L28,65.77c-1.18-1.28-1.05-2.57.24-3.84l9.9-10.27c1.5-1.58,2.7-1.44,4.22,0Z""/>
        </symbol>
        <symbol id=""rejected"" viewBox=""0 0 122.88 122.88"">
            <title>rejected</title>
            <path class=""rejected-clr""
                  d=""M35.38,49.72c-2.16-2.13-3.9-3.47-1.19-6.1l8.74-8.53c2.77-2.8,4.39-2.66,7,0L61.68,46.86,73.39,35.15c2.14-2.17,3.47-3.91,6.1-1.2L88,42.69c2.8,2.77,2.66,4.4,0,7L76.27,61.44,88,73.21c2.65,2.58,2.79,4.21,0,7l-8.54,8.74c-2.63,2.71-4,1-6.1-1.19L61.68,76,49.9,87.81c-2.58,2.64-4.2,2.78-7,0l-8.74-8.53c-2.71-2.63-1-4,1.19-6.1L47.1,61.44,35.38,49.72Z""/>
        </symbol>
    </defs>
    <use href=""#accepted""/>
</svg>
<div id=""container"">
";

        static string Header = @"
    <div class=""title"" style=""border-bottom: none"">
        <div style=""margin: 10px"">
            <h1>Target: TARGET-NAME</h1>
            <div><h3>START-DATE to END-DATE</h3></div>
            <div><i>Report date: REPORT-DATE</i></div>
            <div><i>N.I.N.A. Target Planning plugin PLUGIN-VERSION</i></div>
        </div>
    </div>
    <div class=""headwrap"">
        <div class=""headblock"" style=""width: 20%"">
            <table>
                <tr>
                    <td>RA</td>
                    <td>TARGET-RA</td>
                </tr>
                <tr>
                    <td>Dec</td>
                    <td>TARGET-DEC</td>
                </tr>
            </table>
        </div>
        <div class=""headblock"" style=""width: 20%"">
            <table>
                <tr>
                    <td>Latitude</td>
                    <td>LATITUDE</td>
                </tr>
                <tr>
                    <td>Longitude</td>
                    <td>LONGITUDE</td>
                </tr>
            </table>
        </div>
        <div class=""headblock"" style=""width: 30%"">
            <table>
                <tr>
                    <td>Minimum Altitude</td>
                    <td>MINIMUM-ALTITUDE</td>
                </tr>
                <tr>
                    <td>Minimum Imaging Time</td>
                    <td>MINIMUM-IMAGING-TIME</td>
                </tr>
                <tr>
                    <td>Include Twilight</td>
                    <td>TWILIGHT-INCLUDE</td>
                </tr>
                <tr>
                    <td>Meridian Span</td>
                    <td>MERIDIAN-TIME-SPAN</td>
                </tr>
            </table>
        </div>
        <div class=""headblock"" style=""width: 30%"">
            <table>
                <tr>
                    <td>Maximum Moon Illumination</td>
                    <td>MAXIMUM-MOON-ILLUMINATION</td>
                </tr>
                <tr>
                    <td>Minimum Moon Separation</td>
                    <td>MINIMUM-MOON-SEPARATION</td>
                </tr>
                <tr>
                    <td>Moon Avoidance</td>
                    <td>MOON-AVOIDANCE</td>
                </tr>
                <tr>
                    <td>Moon Avoidance Width</td>
                    <td>MOON-WIDTH</td>
                </tr>
            </table>
        </div>
    </div>
    <div style=""margin-bottom: 20px"">&nbsp;</div>
";

        public static string PlotSVG = @"
    <div class=""plot"">
PLOT-SVG
    </div>
    <div style=""margin-bottom: 20px"">&nbsp;</div>
";

        public static string ResultTableStart = @"
<div class=""report"">
<table>
    <thead>
    <tr>
        <th>Status</th>
        <th>Start</th>
        <th>End</th>
        <th>Imaging Time</th>
        <th>Transit Time</th>
        <th>Moon Illum</th>
        <th>Moon Angle</th>
        <th>Start Limit</th>
        <th>End Limit</th>
    </tr>
    </thead>
    <tbody>
";

        public static string ResultRowAccepted = @"
    <tr>
        <td class=""status"">
            <svg width=""100%"" height=""25px"" viewBox=""0 0 20 20"" style=""transform: scale(.7, .7)"">
                <use href=""#accepted""/>
            </svg>
        </td>
        <td>START-TIME</td>
        <td>END-TIME</td>
        IMAGING-TIME
        <td>TRANSIT-TIME</td>
        MOON-ILLUMINATION
        MOON-SEPARATION
        <td>START-LIMIT</td>
        <td>END-LIMIT</td>
    </tr>
";

        public static string ResultRowRejected = @"
    <tr>
        <td class=""status"">
            <svg width=""100%"" height=""25px"" viewBox=""0 0 20 20"" style=""transform: scale(.7, .7)"">
                <use href=""#rejected""/>
            </svg>
        </td>
        <td>START-TIME</td>
        <td>END-TIME</td>
        IMAGING-TIME
        <td>TRANSIT-TIME</td>
        MOON-ILLUMINATION
        MOON-SEPARATION
        <td colspan=""2"">START-LIMIT</td>
    </tr>
";

        public static string ResultTableEnd = @"
    </tbody>
</table>
</div>
";

        public static string Epilogue =
            @"</div>
</body>
</html>";

    }



}
