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

        /// <summary>
        ///    Gets the name of the resource as it is in the game configuration
        /// </summary>
        /// <remarks>
        ///   The name is like "resource-Tier3" except if it's a Tier4, where it's just the resource name.  The exception
        ///   is Fertilizer, where, to avoid conflicting with the community resource kit we just do Fertilizer-Tier4.
        /// </remarks>
        public string TieredName(TechTier tier)
            => $"{this.BaseName}-{tier.ToString()}";

        public string CapacityUnits { get; }

        public bool CanBeStored { get; }

        public bool ExcessProductionCountsTowardsResearch { get; }

        public bool IsSupportedByScanning => BaseName == "Stuff"; // TODO: When scanning is better fleshed out, see if we can do this more nicely.
        public bool IsSnacks => BaseName == "Snacks"; // TODO: Is there a nicer way to do this?
    }
}
