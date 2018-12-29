using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public abstract class ModuleTieredSnackProducer
        : ModuleResourceConverter,
          ISnackProducer
    {
        [KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Tier", isPersistant = true, guiActiveEditor = true)]
        public int tier;

        [KSPField]
        public float capacity;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Research")]
        public string researchStatus;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Tier")]
        public void ChangeTier()
        {
            tier = (tier + 1) % ((int)this.MaxTechTierResearched + 1);
        }

        protected abstract TechTier MaxTechTierResearched { get; }

        protected virtual bool CanDoProduction(out string reasonWhyNotMessage)
        {
            if (!this.IsActivated)
            {
                reasonWhyNotMessage = "Disabled - module is off";
                return false;
            }

            if (!this.IsPowered)
            {
                reasonWhyNotMessage = "Disabled - module lacks power";
                return false;
            }

            if (!IsCrewed())
            {
                reasonWhyNotMessage = "Disabled - no qualified crew";
            }

            reasonWhyNotMessage = null;
            return true;
        }

        protected virtual bool CanDoResearch(out string reasonWhyNotMessage)
        {
            if (this.tier < (int)this.MaxTechTierResearched)
            {
                reasonWhyNotMessage = $"Disabled - Not cutting edge gear";
                return false;
            }

            reasonWhyNotMessage = null;
            return true;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            if (this.CanDoProduction(out string reasonWhyNotMessage))
            {
                this.IsProductionEnabled = true;

                if (this.CanDoResearch(out reasonWhyNotMessage))
                {
                    this.IsResearchEnabled = true;
                    this.researchStatus = "Active";
                }
                else
                {
                    this.IsResearchEnabled = false;
                    this.researchStatus = reasonWhyNotMessage;
                }
            }
            else
            {
                if (this.IsActivated)
                {
                    ScreenMessages.PostScreenMessage($"{this.name} is shutting down:  {reasonWhyNotMessage}", 10.0f);
                    this.StopResourceConverter();
                }
                this.IsProductionEnabled = false;
                this.IsResearchEnabled = false;
                this.researchStatus = reasonWhyNotMessage;
            }
        }

        protected virtual bool IsCrewed()
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

        protected bool IsNearKerbin()
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

        public double Capacity => this.capacity;

        public bool IsResearchEnabled { get; private set; }

        public bool IsProductionEnabled { get; private set; }

        public abstract double MaxConsumptionForProducedFood { get; }

        public abstract bool CanStockpileProduce { get; }

        public abstract bool ContributeResearch(IColonizationResearchScenario target, double amount);
    }
}
