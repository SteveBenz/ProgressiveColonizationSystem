using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class ModuleTieredAgriculture
        : ModuleTieredSnackProducer
    {
        [KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Target Body", isPersistant = true, guiActiveEditor = true)]
        public string body = "<not set>";

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Body")]
        public void ChangeBody()
        {
            var validBodies = ColonizationResearchScenario.Instance.ValidBodiesForAgriculture.ToList();
            validBodies.Sort();

            if (body == null && validBodies.Count == 0)
            {
                // Shouldn't be possible without cheating...  Unless this is sandbox
                return;
            }

            if (body == null)
            {
                body = validBodies[0];
            }
            else
            {
                int i = validBodies.IndexOf(this.body);
                i = (i + 1) % validBodies.Count;
                body = validBodies[i];
            }

            this.tier = (int)this.MaxTechTierResearched;
        }

        protected override TechTier MaxTechTierResearched =>
            ColonizationResearchScenario.Instance.GetAgricultureMaxTier(this.body);

        public override double MaxConsumptionForProducedFood => this.Tier.AgricultureMaxDietRatio();

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

        protected override bool CanDoProduction(out string reasonWhyNotMessage)
        {
            if (this.vessel.situation != Vessel.Situations.LANDED || this.body != this.vessel.lastBody.name)
            {
                reasonWhyNotMessage = $"Not landed on {this.body}";
                return false;
            }
            else
            {
                reasonWhyNotMessage = null;
                return true;
            }
        }
    }
}
