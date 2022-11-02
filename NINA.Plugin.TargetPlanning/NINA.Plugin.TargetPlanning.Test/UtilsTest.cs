
using FluentAssertions;
using NUnit.Framework;
using TargetPlanning.NINAPlugin;

namespace NINA.Plugin.TargetPlanning.Test {

    public class UtilsTest {

        [Test]
        [TestCase(0, "0h 0m")]
        [TestCase(32, "0h 32m")]
        [TestCase(61, "1h 1m")]
        [TestCase(719, "11h 59m")]
        public void TestMtoHM(int min, string expected) {
            Utils.MtoHM(min).Should().Be(expected);
        }
    }

}
