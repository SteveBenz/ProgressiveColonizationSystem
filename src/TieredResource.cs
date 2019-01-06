using System;

namespace Nerm.Colonization
{
    public enum ProductionRestriction
    {
        Orbit,
        OrbitOfBody,
        LandedOnBody,
    }

    public class TieredResource
    {
        public TieredResource(string name, string capacityUnits, ProductionRestriction productionRestriction, ResearchCategory researchCategory, bool canBeStored, bool unstoredExcessCanGoToResearch)
        {
            this.BaseName = name;
            this.CapacityUnits = capacityUnits;
            this.CanBeStored = canBeStored;
            this.ExcessProductionCountsTowardsResearch = unstoredExcessCanGoToResearch;
            this.ProductionRestriction = productionRestriction;
            this.ResearchCategory = researchCategory;
        }

        public ProductionRestriction ProductionRestriction { get; }

        public ResearchCategory ResearchCategory { get; }

        public bool LimitedTierOnEasyBodies { get; }

        public string BaseName { get; }

        public string TieredName(TechTier tier)
            => tier == TechTier.Tier4 ? this.BaseName : $"{this.BaseName}-{tier.ToString()}";

        public string CapacityUnits { get; }

        public bool CanBeStored { get; }

        public bool ExcessProductionCountsTowardsResearch { get; }
    }
}
