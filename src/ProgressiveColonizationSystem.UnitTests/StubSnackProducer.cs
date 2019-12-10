using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressiveColonizationSystem.UnitTests
{
    public abstract class StubSnackProducer
        : ITieredProducer
    {
        public TechTier Tier { get; set; }
        public TechTier MaximumTier { get; set; } = TechTier.Tier4;
        public double ProductionRate { get; set; }
        public bool IsResearchEnabled { get; set; }
        public string ReasonWhyResearchIsDisabled { get; set; }
        public bool IsProductionEnabled { get; set; }
        public bool CanStockpileProduce { get; set; }
        public abstract TieredResource Output { get; }
        public TieredResource Input => StubColonizationResearchScenario.GetTieredResourceByName("Fertilizer");
        public bool ContributeResearch(IColonizationResearchScenario target, double amount)
            => target.ContributeResearch(this.Output, "test", amount);
        public string Body { get; set; }
    }

    public class StubHydroponic
        : StubSnackProducer
    {
        public StubHydroponic()
        {
            this.CanStockpileProduce = false;
        }

        public override TieredResource Output => StubColonizationResearchScenario.GetTieredResourceByName("HydroponicSnacks");
    }

    public class StubFarm
        : StubSnackProducer
    {
        public StubFarm()
        {
            this.CanStockpileProduce = true;
        }

        public override TieredResource Output => StubColonizationResearchScenario.GetTieredResourceByName("Snacks");
    }
}
