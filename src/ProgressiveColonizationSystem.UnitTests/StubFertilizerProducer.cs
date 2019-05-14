using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressiveColonizationSystem.UnitTests
{
    public class StubFertilizerProducer
        : ITieredProducer
    {
        public TechTier Tier { get; set; }
        public double ProductionRate { get; set; }
        public bool IsResearchEnabled { get; set; }
        public string ReasonWhyResearchIsDisabled { get; } = null;
        public bool IsProductionEnabled { get; set; }
        public TieredResource Output => StubColonizationResearchScenario.GetTieredResourceByName("Fertilizer");
        public TieredResource Input => null;
        public bool ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            return target.ContributeResearch(this.Output, "test", amount);
        }
        public string Body { get; set; }
    }
}
