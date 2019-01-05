using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class ModuleTieredAgriculture
        : BodySpecificTieredResourceConverter
    {
        protected override TechTier MaxTechTierResearched =>
            ColonizationResearchScenario.Instance.GetAgricultureMaxTier(this.body);

        public override bool ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            if (this.IsResearchEnabled)
            {
                target.ContributeAgricultureResearch(this.body, amount);
                return target.GetAgricultureMaxTier(this.body) != this.Tier;
            }
            else
            {
                return false;
            }
        }
    }
}
