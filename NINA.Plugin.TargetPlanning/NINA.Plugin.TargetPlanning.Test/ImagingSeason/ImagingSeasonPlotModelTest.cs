using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using TargetPlanning.NINAPlugin.Astrometry;
using TargetPlanning.NINAPlugin.ImagingSeason;

namespace NINA.Plugin.TargetPlanning.Test.ImagingSeason {

    public class ImagingSeasonPlotModelTest {

        [Test]
        public void TestGetImagingSpans() {
            List<ImagingDayPlan> list = new List<ImagingDayPlan>();

            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            List<ImagingSpan> spans = ImagingSeasonPlotModel.GetImagingSpans(list);
            spans.Count.Should().Be(3);
            spans[0].Start.Should().Be(0);
            spans[0].End.Should().Be(2);
            spans[0].Accepted.Should().BeTrue();
            spans[1].Start.Should().Be(3);
            spans[1].End.Should().Be(5);
            spans[1].Accepted.Should().BeFalse();
            spans[2].Start.Should().Be(6);
            spans[2].End.Should().Be(7);
            spans[2].Accepted.Should().BeTrue();

            list.Clear();
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            spans = ImagingSeasonPlotModel.GetImagingSpans(list);
            spans.Count.Should().Be(1);
            spans[0].Start.Should().Be(0);
            spans[0].End.Should().Be(2);
            spans[0].Accepted.Should().BeTrue();

            list.Clear();
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            spans = ImagingSeasonPlotModel.GetImagingSpans(list);
            spans.Count.Should().Be(1);
            spans[0].Start.Should().Be(0);
            spans[0].End.Should().Be(2);
            spans[0].Accepted.Should().BeFalse();

            list.Clear();
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            spans = ImagingSeasonPlotModel.GetImagingSpans(list);
            spans.Count.Should().Be(5);
            spans[0].Start.Should().Be(0);
            spans[0].End.Should().Be(2);
            spans[0].Accepted.Should().BeTrue();
            spans[1].Start.Should().Be(3);
            spans[1].End.Should().Be(5);
            spans[1].Accepted.Should().BeFalse();
            spans[2].Start.Should().Be(6);
            spans[2].End.Should().Be(8);
            spans[2].Accepted.Should().BeTrue();
            spans[3].Start.Should().Be(9);
            spans[3].End.Should().Be(11);
            spans[3].Accepted.Should().BeFalse();
            spans[4].Start.Should().Be(12);
            spans[4].End.Should().Be(14);
            spans[4].Accepted.Should().BeTrue();
        }

        [Test]
        public void TestTrimStartEnd() {
            List<ImagingDayPlan> list = new List<ImagingDayPlan>();

            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(false));
            IList<ImagingDayPlan> trimmed = ImagingSeasonPlotModel.TrimStartEnd(list);
            trimmed.Count.Should().Be(3);

            list.Clear();
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            trimmed = ImagingSeasonPlotModel.TrimStartEnd(list);
            trimmed.Count.Should().Be(4);

            list.Clear();
            list.Add(GetPlan(true));
            list.Add(GetPlan(true));
            list.Add(GetPlan(false));
            list.Add(GetPlan(true));
            trimmed = ImagingSeasonPlotModel.TrimStartEnd(list);
            trimmed.Count.Should().Be(4);

            list.Clear();
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            trimmed = ImagingSeasonPlotModel.TrimStartEnd(list);
            trimmed.Count.Should().Be(0);

            list.Clear();
            list.Add(GetPlan(false));
            list.Add(GetPlan(false));
            list.Add(GetPlan(true));
            list.Add(GetPlan(false));
            trimmed = ImagingSeasonPlotModel.TrimStartEnd(list);
            trimmed.Count.Should().Be(1);
        }

        private ImagingDayPlan GetPlan(bool accepted) {
            ImagingLimit limit = accepted ? ImagingLimit.Twilight : ImagingLimit.MinimumTime;
            return new ImagingDayPlan(DateTime.Now, DateTime.Now, DateTime.Now, limit, limit, 0, 0, 0);
        }
    }


}
