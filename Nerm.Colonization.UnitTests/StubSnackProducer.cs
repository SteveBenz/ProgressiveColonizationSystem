using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nerm.Colonization.UnitTests
{
    public class StubSnackProducer
        : ISnackProducer
    {
        private bool isAgroponics;

        public StubSnackProducer(bool isAgroponics)
        {
            this.isAgroponics = isAgroponics;
        }

        public TechTier Tier { get; set; }

        public double Capacity { get; set; }

        public bool IsResearchEnabled { get; set; }

        public bool IsProductionEnabled { get; set; }

        public double MaxConsumptionForProducedFood => Tier.AgroponicMaxDietRatio();

        public bool ContributeResearch(IColonizationResearchScenario target, double amount)
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
    }
}
