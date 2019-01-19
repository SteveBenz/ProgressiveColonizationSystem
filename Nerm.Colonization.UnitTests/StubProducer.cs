using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nerm.Colonization.UnitTests
{
    public class StubProducer
        : ITieredProducer
    {
        public StubProducer(TieredResource output, TieredResource input, double productionRate, TechTier tier)
        {
            this.Output = output;
            this.Input = input;
            this.ProductionRate = productionRate;
            this.Tier = tier;
            this.Body = (output.ProductionRestriction == ProductionRestriction.Orbit) ? null : "munmuss";
        }

        public TechTier Tier { get; set; }
        public double ProductionRate { get; set; }
        public bool IsResearchEnabled { get; set; } = true;
        public bool IsProductionEnabled { get; set; } = true;
        public TieredResource Output { get; }
        public TieredResource Input { get; }
        public bool ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            return target.ContributeResearch(this.Output, this.Body, amount);
        }
        public string Body { get; set; }
    }
}
