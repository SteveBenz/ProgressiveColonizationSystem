using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
	public class ModuleTieredFactory
		: BodySpecificTieredResourceConverter
    {
		protected override TechTier MaxTechTierResearched
			=> ColonizationResearchScenario.Instance.GetProductionMaxTier(this.body);

		public override bool ContributeResearch(IColonizationResearchScenario target, double amount)
		{
			if (this.IsResearchEnabled)
			{
				target.ContributeProductionResearch(this.body, amount);
				return target.GetProductionMaxTier(this.body) != this.Tier;
			}
			else
			{
				return false;
			}
		}
    }
}
