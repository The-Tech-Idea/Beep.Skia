using System.Linq;
using Beep.Skia.WellLogs;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for LAS parsing multi-track grouping.
    /// </summary>
    public class WellLogTrackGroupingTests
    {
        private const string Las = @"~Version
VERS. 2.0 : CWLS LOG ASCII STANDARD - VERSION 2.0
WRAP. NO : ONE LINE PER DEPTH STEP
~Well
STRT.M 1000.0 : Start depth
STOP.M 1002.0 : Stop depth
STEP.M 1.0 : Step
NULL. -999.25 : Null value
WELL. TEST-1 : Well name
~Curve
DEPT.M : Depth
GR.API : Gamma Ray
RHOB.G/C3 : Bulk Density
RT.OHMM : Resistivity
DT.US/F : Sonic
~ASCII
1000.0 50.0 2.40 10.0 80.0
1001.0 55.0 2.30 12.0 82.0
1002.0 60.0 2.20 14.0 84.0
";

        [Fact]
        public void Parse_GroupsCurvesIntoFamilyTracks()
        {
            var document = new LasFileParser().Parse(Las);
            var names = document.Tracks.Select(t => t.Name).ToList();

            Assert.Contains("Depth", names);
            Assert.Contains("Gamma Ray", names);
            Assert.Contains("Resistivity", names);
            Assert.Contains("Porosity", names);
            Assert.Contains("Sonic", names);
        }

        [Fact]
        public void Parse_DepthTrackHoldsInvisibleDepthCurve()
        {
            var document = new LasFileParser().Parse(Las);
            var depth = document.Tracks.Single(t => t.Name == "Depth");

            Assert.Equal(WellLogTrackRole.Depth, depth.Role);
            Assert.Single(depth.Curves);
            Assert.False(depth.Curves[0].IsVisible);
        }

        [Fact]
        public void Parse_FamilyTracksHoldExpectedCurvesWithSamples()
        {
            var document = new LasFileParser().Parse(Las);

            var gamma = document.Tracks.Single(t => t.Name == "Gamma Ray");
            Assert.Single(gamma.Curves);
            Assert.Equal("GR", gamma.Curves[0].Mnemonic);
            Assert.Equal(3, gamma.Curves[0].Samples.Count);

            var porosity = document.Tracks.Single(t => t.Name == "Porosity");
            Assert.Equal("RHOB", porosity.Curves[0].Mnemonic);

            var resistivity = document.Tracks.Single(t => t.Name == "Resistivity");
            Assert.Equal("RT", resistivity.Curves[0].Mnemonic);

            var sonic = document.Tracks.Single(t => t.Name == "Sonic");
            Assert.Equal("DT", sonic.Curves[0].Mnemonic);
        }

        [Fact]
        public void Parse_TrackOrderIsStable()
        {
            var document = new LasFileParser().Parse(Las);
            var curveTracks = document.Tracks.Where(t => t.Role == WellLogTrackRole.Curve).Select(t => t.Name).ToList();

            Assert.Equal(new[] { "Gamma Ray", "Resistivity", "Porosity", "Sonic" }, curveTracks);
        }
    }
}
