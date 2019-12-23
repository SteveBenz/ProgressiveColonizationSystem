using System;

namespace ProgressiveColonizationSystem.ProductionChain
{
    internal class StorageProducer
        : ITieredProducer
    {
        public StorageProducer(TieredResource resource, TechTier tier, double amount)
        {
            this.Output = resource;
            this.Tier = tier;
            this.Amount = amount;
        }

        public TechTier Tier { get; set; }

        public TechTier MaximumTier => TechTier.Tier4;

        public double ProductionRate => double.MaxValue;

        public bool IsResearchEnabled => false;

        public string ReasonWhyResearchIsDisabled => null;

        public bool IsProductionEnabled => true;

        public bool CanStockpileProduce => false;

        public TieredResource Input => null;

        public TieredResource Output { get; }

        public bool ContributeResearch(IColonizationResearchScenario target, double amount)
            => throw new NotImplementedException();

        // Not part of IProducer

        public double Amount { get; }

        public string Body { get; set; } = null;
    }
}
