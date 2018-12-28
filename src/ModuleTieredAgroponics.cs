using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class ModuleTieredAgroponics
        : ModuleTieredSnackProducer
    {
        protected override TechTier MaxTechTierResearched =>
            ColonizationResearchScenario.Instance?.AgroponicsMaxTier ?? TechTier.Tier0;

        public override double MaxConsumptionForProducedFood => this.Tier.AgroponicMaxDietRatio();

        public override bool ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            if (this.IsResearchEnabled)
            {
                target.ContributeAgroponicResearch(amount);
                return target.AgroponicsMaxTier != this.Tier;
            }
            else
            {
                return false;
            }
        }

        protected override bool CanDoResearch(out string reasonWhyNotMessage)
        {
            if (!CanDoResearch(out reasonWhyNotMessage))
            {
                return false;
            }
            else if (ColonizationResearchScenario.Instance.AgroponicsMaxTier >= TechTier.Tier2 && this.IsNearKerbin())
            {
                reasonWhyNotMessage = "Disabled - Too near to Kerbin's orbit";
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
