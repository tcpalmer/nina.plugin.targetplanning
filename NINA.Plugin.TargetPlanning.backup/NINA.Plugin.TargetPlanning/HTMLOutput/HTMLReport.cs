using NINA.Core.Utility;
using OxyPlot;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TargetPlanning.NINAPlugin.Astrometry;
using TargetPlanning.NINAPlugin.ImagingSeason;

namespace TargetPlanning.NINAPlugin.HTMLOutput {

    public class HTMLReport {

        private PlanParameters planParameters;

        public HTMLReport(PlanParameters planParameters) {
            this.planParameters = planParameters;
        }

        public void CreateDailyDetailsHTMLReport(IEnumerable<ImagingDayPlan> results) {

            string tmpFilePath = GetDailyDetailsHTMLReportPath();
            Logger.Trace($"DailyDetails HTML Report tmp file: {tmpFilePath}");

            StringBuilder sb = new StringBuilder();
            sb.Append(HTMLTemplate.ReplacePreamble(planParameters));
            sb.Append(HTMLTemplate.ReplaceHeader(planParameters, results));
            sb.Append(HTMLTemplate.ResultTableStart);

            foreach (ImagingDayPlan plan in results) {
                sb.Append(HTMLTemplate.ReplaceResultsRow(planParameters, plan));
            }

            sb.Append(HTMLTemplate.ResultTableEnd);
            sb.Append(HTMLTemplate.Epilogue);

            File.WriteAllText(tmpFilePath, sb.ToString());
        }

        public static string GetDailyDetailsHTMLReportPath() {
            return Path.Combine(Path.GetTempPath(), "DailyDetailsReport.html");
        }

        public void CreateImagingSeasonHTMLReport(IEnumerable<ImagingDayPlan> results, PlotModel plotModel) {
            string tmpFilePath = GetImagingSeasonHTMLReportPath();
            Logger.Trace($"ImagingSeason HTML Report tmp file: {tmpFilePath}");

            IList<ImagingDayPlan> trimmedResults = ImagingSeasonBaseModel.TrimStartEnd((List<ImagingDayPlan>)results);

            SvgExporter svgExporter = new SvgExporter();
            svgExporter.Width = 950;
            svgExporter.Height = 400;
            string plotSVG = svgExporter.ExportToString(plotModel);

            StringBuilder sb = new StringBuilder();
            sb.Append(HTMLTemplate.ReplacePreamble(planParameters));
            sb.Append(HTMLTemplate.ReplaceHeader(planParameters, trimmedResults));
            sb.Append(HTMLTemplate.AddPlot(plotSVG));
            sb.Append(HTMLTemplate.ResultTableStart);

            foreach (ImagingDayPlan plan in trimmedResults) {
                sb.Append(HTMLTemplate.ReplaceResultsRow(planParameters, plan));
            }

            sb.Append(HTMLTemplate.ResultTableEnd);
            sb.Append(HTMLTemplate.Epilogue);

            File.WriteAllText(tmpFilePath, sb.ToString());
        }

        public static string GetImagingSeasonHTMLReportPath() {
            return Path.Combine(Path.GetTempPath(), "ImagingSeasonReport.html");
        }

        private void WriteHeader() {

        }

        private void WriteResultsTable() {

        }
    }

}
