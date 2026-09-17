using System.Linq;
using Beep.Skia;
using Beep.Skia.Security;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for Security family analysis: DREAD scoring and MITRE ATT&amp;CK mapping.
    /// </summary>
    public class SecurityAnalysisTests
    {
        [Fact]
        public void DreadCalculator_MapsSeverityAndLikelihood()
        {
            var high = DreadCalculator.Calculate(Severity.High, Likelihood.Likely);
            Assert.Equal("High", high.RiskLevel);
            Assert.Equal(3.0, high.Average, 3);

            var low = DreadCalculator.Calculate(Severity.Low, Likelihood.Rare);
            Assert.Equal("Low", low.RiskLevel);

            var medium = DreadCalculator.Calculate(Severity.Medium, Likelihood.Possible);
            Assert.Equal("Medium", medium.RiskLevel);
        }

        [Fact]
        public void DreadCalculator_ParsesTextValues()
        {
            var score = DreadCalculator.Calculate("High", "Possible");
            Assert.Equal(DreadRating.High, score.Damage);
            Assert.Equal(DreadRating.Medium, score.Reproducibility);

            var fallback = DreadCalculator.Calculate("bogus", "bogus");
            Assert.Equal(DreadRating.Medium, fallback.Damage);
            Assert.Equal(DreadRating.Medium, fallback.Reproducibility);
        }

        [Fact]
        public void DreadCalculator_FromThreatNode()
        {
            var threat = new ThreatNode { ThreatName = "Data exfiltration", Severity = Severity.High, Likelihood = Likelihood.Likely };
            var score = DreadCalculator.Calculate(threat);

            Assert.Equal("High", score.RiskLevel);
        }

        [Fact]
        public void MitreLibrary_ContainsWellFormedTechniques()
        {
            var techniques = MitreAttackLibrary.Techniques;

            Assert.True(techniques.Count >= 25);
            Assert.All(techniques, t => Assert.StartsWith("T", t.Id));
            Assert.Equal(techniques.Count, techniques.Select(t => t.Id).Distinct().Count());
            Assert.Contains("Impact", MitreAttackLibrary.Tactics);
            Assert.NotNull(MitreAttackLibrary.Find("T1078"));
            Assert.Null(MitreAttackLibrary.Find("T9999"));
        }

        [Fact]
        public void MitreLibrary_MapsEveryStrideCategory()
        {
            foreach (StrideAnalyzer.StrideCategory category in System.Enum.GetValues(typeof(StrideAnalyzer.StrideCategory)))
            {
                var techniques = MitreAttackLibrary.ForStrideCategory(category);
                Assert.NotEmpty(techniques);
            }

            var spoofing = MitreAttackLibrary.ForStrideCategory(StrideAnalyzer.StrideCategory.Spoofing);
            Assert.Contains(spoofing, t => t.Id == "T1078");
        }

        [Fact]
        public void StrideAnalyzer_EnrichesThreatsWithTechniquesAndDread()
        {
            var manager = new DrawingManager();
            manager.AddComponent(new AssetNode { AssetName = "Customer DB", Category = AssetCategory.Data, Criticality = Criticality.High });

            var analyzer = new StrideAnalyzer();
            analyzer.Analyze(manager.GetComponents());

            Assert.NotEmpty(analyzer.Threats);
            Assert.All(analyzer.Threats, t =>
            {
                Assert.NotEmpty(t.Techniques);
                Assert.NotNull(t.Dread);
                Assert.Contains(t.Dread.RiskLevel, new[] { "Low", "Medium", "High" });
            });
        }
    }
}
