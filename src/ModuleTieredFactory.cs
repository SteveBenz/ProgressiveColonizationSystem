using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
	public class ModuleTieredFactory
		: BodySpecificTieredResourceConverter
    {
        [KSPField(guiActive = true, isPersistant = true, guiName = "Stockpile output")]
        [UI_Toggle]
        public bool isStockpiling = true;

        public override bool CanStockpileProduce => this.isStockpiling;

		protected override TechTier MaxTechTierResearched
			=> ColonizationResearchScenario.Instance.GetProductionMaxTier(this.body);

		protected override string RequiredCrewTrait => "Engineer";

        /// <summary>
        ///   The name of the input resource (as a Tier4 resource)
        /// </summary>
        [KSPField]
        public string input;

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

        public override string SourceResourceName => this.input == "" ? null : this.input;

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine($"Capacity: {this.capacity}");
            if (!string.IsNullOrEmpty(this.input))
            {
                info.AppendLine($"Input: {this.input}");
            }

            return info.ToString();
        }
    }
}
