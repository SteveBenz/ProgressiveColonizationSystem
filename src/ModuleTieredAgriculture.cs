using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class ModuleTieredAgriculture
        : BodySpecificTieredResourceConverter
    {
        protected override string RequiredCrewTrait => "Scientist";

        public override string SourceResourceName => "Fertilizer";

        [KSPField(guiActive = true, isPersistant = true, guiName = "Stockpile extra food")]
        [UI_Toggle]
        public bool isStockpiling = true;

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

        public override bool CanStockpileProduce => this.isStockpiling;

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine($"{GreenInfo("Capacity:")} {this.capacity} kerbal-days");
            info.AppendLine($"{GreenInfo("Quality:")}");
            foreach (TechTier tier in TechTierExtensions.AllTiers)
            {
                info.AppendLine($" {tier.ToString()}: {(int)(tier.AgricultureMaxDietRatio() * 100)}%");
            }
            return info.ToString();
        }
    }
}
