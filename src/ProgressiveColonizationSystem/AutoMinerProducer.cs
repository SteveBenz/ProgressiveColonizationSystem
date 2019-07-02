using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    internal class AutoMinerProducer
        : ITieredProducer
    {
        private readonly TechTier tier;

        public TechTier Tier { get => this.tier; set => throw new NotImplementedException(); }

        public double ProductionRate => 100000; // An insensibly big number

        public bool IsResearchEnabled => false;

        public bool IsProductionEnabled => true;

        public TieredResource Output { get; }

        public TieredResource Input => null;

        public string Body { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string ReasonWhyResearchIsDisabled => throw new NotImplementedException();

        public bool ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            throw new NotImplementedException();
        }

        public AutoMinerProducer(string resourceName)
        {
            ColonizationResearchScenario.Instance.TryParseTieredResourceName(resourceName, out var resource, out var tier);
            this.Output = resource;
            this.tier = tier;
        }
    }
}
