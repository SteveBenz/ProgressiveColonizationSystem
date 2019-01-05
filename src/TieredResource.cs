using System;

namespace Nerm.Colonization
{
    public class TieredResource
    {
        public TieredResource(string name, string capacityUnits, bool canBeStored, bool unstoredExcessCanGoToResearch)
        {
            this.BaseName = name;
            this.CapacityUnits = capacityUnits;
            this.CanBeStored = canBeStored;
            this.ExcessProductionCountsTowardsResearch = unstoredExcessCanGoToResearch;
        }

        public string BaseName { get; }

        public string TieredName(TechTier tier)
            => tier == TechTier.Tier4 ? this.BaseName : $"{this.BaseName}-{tier.ToString()}";

        public string CapacityUnits { get; }

        public bool CanBeStored { get; }

        public bool ExcessProductionCountsTowardsResearch { get; }
    }
}
