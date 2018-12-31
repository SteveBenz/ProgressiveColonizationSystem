using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nerm.Colonization.UnitTests
{
	public abstract class StubSnackProducer
        : IProducer
	{
		public TechTier Tier { get; set; }
		public double ProductionRate { get; set; }
		public bool IsResearchEnabled { get; set; }
		public bool IsProductionEnabled { get; set; }
		public abstract string ProductResourceName { get; }

        public abstract bool CanStockpileProduce { get; }

        public string SourceResourceName => "Fertilizer";

        public abstract bool ContributeResearch(IColonizationResearchScenario target, double amount);
    }

	public class StubHydroponic
        : StubSnackProducer
    {
        public override bool ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            // Copied from the real class.  Yuk.  Gotta get a better mock framework.
            if (target.AgroponicsMaxTier == this.Tier && this.IsResearchEnabled)
            {
                target.ContributeAgroponicResearch(amount);
                return target.AgroponicsMaxTier != this.Tier;
            }
            else
            {
                return false;
            }
        }

        public override bool CanStockpileProduce => false;

        public override string ProductResourceName => Snacks.AgroponicSnackResourceBaseName;
    }

    public class StubFarm
		: StubSnackProducer
	{
        public override bool ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            // Copied from the real class.  Yuk.  Gotta get a better mock framework.
            if (target.GetAgricultureMaxTier("test") == this.Tier && this.IsResearchEnabled)
            {
                target.ContributeAgricultureResearch("test", amount);
                return target.GetAgricultureMaxTier("test") != this.Tier;
            }
            else
            {
                return false;
            }
        }

        public override bool CanStockpileProduce => true;

        public override string ProductResourceName => Snacks.AgriculturalSnackResourceBaseName;
    }
}
