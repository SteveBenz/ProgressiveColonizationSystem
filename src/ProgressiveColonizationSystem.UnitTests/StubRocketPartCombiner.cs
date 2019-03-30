using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressiveColonizationSystem.UnitTests
{
    public class StubRocketPartCombiner
        : ITieredCombiner
    {
        public const double DefaultProductionRate = 10.0;
        public const double ExpectedTier0Ratio = 0.4; /* 40% local parts, 60% kerban parts */
        public const double ExpectedTier1Ratio = 0.65;
        public const double ExpectedTier2Ratio = 0.8;
        public const double ExpectedTier3Ratio = 0.92;
        public const double ExpectedTier4Ratio = 0.98;
        public const string ExpectedOutputResource = "Rocket Parts";
        public const string ExpectedInputResource = "Complex Parts";

        public double ProductionRate { get; set; } = DefaultProductionRate;

        public string NonTieredOutputResourceName => ExpectedOutputResource;

        public TieredResource TieredInput => StubColonizationResearchScenario.LocalParts;

        public string NonTieredInputResourceName => ExpectedInputResource;

        public double GetRatioForTier(TechTier tier)
        {
            switch(tier)
            {
                default:
                case TechTier.Tier0: return ExpectedTier0Ratio;
                case TechTier.Tier1: return ExpectedTier1Ratio;
                case TechTier.Tier2: return ExpectedTier2Ratio;
                case TechTier.Tier3: return ExpectedTier3Ratio;
                case TechTier.Tier4: return ExpectedTier4Ratio;
            }
        }

        public bool IsProductionEnabled { get; set; } = true;
    }
}
