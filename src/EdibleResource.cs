using System;

namespace Nerm.Colonization
{
    public class EdibleResource
        : TieredResource
    {
        private double maxPercentTier0;
        private double maxPercentTier1;
        private double maxPercentTier2;
        private double maxPercentTier3;
        private double maxPercentTier4;

        public EdibleResource(string name, bool canBeStored, bool unstoredExcessCanGoToResearch, double maxPercentTier0, double maxPercentTier1, double maxPercentTier2, double maxPercentTier3, double maxPercentTier4)
            : base(name, "Kerbal-Days", canBeStored, unstoredExcessCanGoToResearch)
        {
            this.maxPercentTier0 = maxPercentTier0;
            this.maxPercentTier1 = maxPercentTier1;
            this.maxPercentTier2 = maxPercentTier2;
            this.maxPercentTier3 = maxPercentTier3;
            this.maxPercentTier4 = maxPercentTier4;
        }

        public double GetPercentOfDietByTier(TechTier tier)
        {
            switch(tier)
            {
                case TechTier.Tier0: return maxPercentTier0;
                case TechTier.Tier1: return maxPercentTier1;
                case TechTier.Tier2: return maxPercentTier2;
                case TechTier.Tier3: return maxPercentTier3;
                default:
                case TechTier.Tier4: return maxPercentTier4;
            }
        }
    }
}
