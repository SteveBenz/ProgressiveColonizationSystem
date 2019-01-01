using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nerm.Colonization.UnitTests
{
	public class StubFertilizerProducer
        : IProducer
	{
		public TechTier Tier { get; set; }
		public double ProductionRate { get; set; }
		public bool IsResearchEnabled { get; set; }
		public bool IsProductionEnabled { get; set; }
        public string ProductResourceName => "Fertilizer";
        public bool CanStockpileProduce { get; set; } = true;
        public string SourceResourceName { get; set; } = null;
        public bool ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            // Copied from the real class.  Yuk.  Gotta get a better mock framework.
            if (target.GetProductionMaxTier("test") == this.Tier && this.IsResearchEnabled)
            {
                target.ContributeProductionResearch("test", amount);
                return target.GetProductionMaxTier("test") != this.Tier;
            }
            else
            {
                return false;
            }
        }
    }
}
