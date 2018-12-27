using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class ModuleTieredAgroponics
        : ModuleResourceConverter, // <-  perhaps it should be ModuleResourceConverter?
          ISnackProducer
    {
        [KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Tier", isPersistant = true, guiActiveEditor = true)]
        public int tier;

        [KSPField]
        public float capacity;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Research")]
        private string researchStatus;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Tier")]
        private void ChangeTier()
        {
            tier = (tier + 1) % ((int)ColonizationResearchScenario.Instance.AgroponicsMaxTier + 1);
        }

        public ModuleTieredAgroponics()
        {
            // Default to the max tier for new parts - for old parts, it will be overwritten on load.
            tier = (int)(ColonizationResearchScenario.Instance != null
                ? ColonizationResearchScenario.Instance.AgroponicsMaxTier
                : TechTier.Tier0);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!this.IsActivated)
            {
                this.researchStatus = "Disabled - module is off";
                return;
            }

            if (!this.IsPowered)
            {
                this.researchStatus = "Disabled - module lacks power";
                return;
            }

            if (!IsCrewed())
            {
                this.researchStatus = "Disabled - no qualified crew";
                ScreenMessages.PostScreenMessage($"{this.name} is shutting down because there is no qualified crew (need a scientist with at least as many stars as the tier level)", 10.0f);
                this.StopResourceConverter();
            }

            if (this.tier < (int)ColonizationResearchScenario.Instance.AgroponicsMaxTier)
            {
                this.researchStatus = $"Disabled - Not cutting edge gear";
            }
            else if (ColonizationResearchScenario.Instance.AgroponicsMaxTier >= TechTier.Tier2 && this.IsNearKerbin())
            {
                this.researchStatus = "Disabled - Too near to Kerbin's orbit";
            }
            else
            {
                this.researchStatus = "Active";
            }
        }

        private bool IsCrewed()
        {
            if (this.vessel == null)
            {
                return false;
            }

            if (this.vessel.GetVesselCrew() == null)
            {
                return false;
            }

            // We might want to make this check more elaborate someday - to encourage bigger crews
            // amont other things.
            return this.vessel.GetVesselCrew().Any(crew => crew.trait == "Scientist" && crew.experienceLevel >= this.tier);
        }

        // Not really sure, but it looks like lastTimeFactor is between 0 and 1
        private bool IsPowered => this.lastTimeFactor > .5;

        private bool IsNearKerbin()
        {
            // There are more stylish ways to do this.  It's also a bit problematic for the player
            // because if they ignore a craft on its way back from some faroff world until it
            // reaches kerbin's SOI, then they'll lose all that tasty research.
            //
            // A fix would be to look at the vessel's orbit as well, and, if it just carries the
            // vessel out of the SOI, count that.
            double homeworldDistanceFromSun = FlightGlobals.GetHomeBody().orbit.altitude;
            return this.vessel.distanceToSun > homeworldDistanceFromSun * .9
                && this.vessel.distanceToSun < homeworldDistanceFromSun * 1.1;
        }

        public TechTier Tier => (TechTier)this.tier;

        double ISnackProducer.Capacity => this.capacity;

        public bool IsResearchEnabled => this.IsProductionEnabled && this.tier == (int)ColonizationResearchScenario.Instance.AgroponicsMaxTier;

        public bool IsProductionEnabled => this.IsActivated && this.IsPowered;

        double ISnackProducer.MaxConsumptionForProducedFood => this.Tier.AgroponicMaxDietRatio();

        bool ISnackProducer.ContributeResearch(IColonizationResearchScenario target, double amount)
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
    }
}
