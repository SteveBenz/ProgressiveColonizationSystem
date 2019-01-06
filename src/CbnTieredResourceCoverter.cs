using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{
    public class CbnTieredResourceConverter
        : PartModule, IProducer
    {
        private double firstNoPowerIndicator = -1.0;

        [KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Tier", isPersistant = true, guiActiveEditor = true)]
        public int tier;

        [KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Target Body", isPersistant = true, guiActiveEditor = true)]
        public string body = "<not set>";

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Tier")]
        public void ChangeTier()
        {
            this.tier = (this.tier + 1) % ((int)this.MaxTechTierResearched + 1);
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Body")]
        public void ChangeBody()
        {
            var validBodies = ColonizationResearchScenario.Instance.UnlockedBodies.ToList();
            validBodies.Sort();

            if (string.IsNullOrEmpty(body) && validBodies.Count == 0)
            {
                // Shouldn't be possible without cheating...  Unless this is sandbox
                return;
            }

            if (string.IsNullOrEmpty(body))
            {
                body = validBodies[0];
            }
            else
            {
                int i = validBodies.IndexOf(this.body);
                i = (i + 1) % validBodies.Count;
                body = validBodies[i];
            }

            this.tier = (int)ColonizationResearchScenario.Instance.GetMaxUnlockedTier(this.Output, this.body);
        }
        /// <summary>
        ///   The name of the output resource (as a Tier4 resource)
        /// </summary>
        [KSPField]
        public string output;

        /// <summary>
        ///   The name of the input resource (as a Tier4 resource)
        /// </summary>
        [KSPField]
        public string input;

        [KSPField]
        public float capacity;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Research")]
        public string researchStatus;

        private bool isInitialized = false;

        protected virtual TechTier MaxTechTierResearched
            => ColonizationResearchScenario.Instance.GetMaxUnlockedTier(this.Output, this.body);

        protected virtual bool CanDoProduction(ModuleResourceConverter resourceConverter, out string reasonWhyNotMessage)
        {
            if (!resourceConverter.IsActivated)
            {
                reasonWhyNotMessage = "Disabled - module is off";
                return false;
            }

            if (!this.IsPowered(resourceConverter))
            {
                reasonWhyNotMessage = "Disabled - module lacks power";
                return false;
            }

            if (!IsCrewed())
            {
                reasonWhyNotMessage = "Disabled - no qualified crew";
                return false;
            }

            if (this.Output.ProductionRestriction == ProductionRestriction.LandedOnBody
             && (this.vessel.situation != Vessel.Situations.LANDED || this.body != this.vessel.mainBody.name))
            {
                reasonWhyNotMessage = $"Not landed on {this.body}";
                return false;
            }

            if (this.Output.ProductionRestriction == ProductionRestriction.OrbitOfBody
             && (this.vessel.situation != Vessel.Situations.ORBITING || this.body != this.vessel.mainBody.name))
            {
                reasonWhyNotMessage = $"Not landed on {this.body}";
                return false;
            }

            reasonWhyNotMessage = null;
            return true;
        }

        private bool CanDoResearch(out string reasonWhyNotMessage)
        {
            if (this.tier < (int)this.MaxTechTierResearched)
            {
                reasonWhyNotMessage = $"Disabled - Not cutting edge gear";
                return false;
            }

            if (!this.Output.ResearchCategory.CanDoResearch(this.vessel, this.Tier, out reasonWhyNotMessage))
            {
                return false;
            }

            reasonWhyNotMessage = null;
            return true;
        }

        public void FixedUpdate()
        {
            // somehow, if this is called from OnAwake, like it sensibly should be, it breaks
            // the part so that FixedUpdate never gets called.
            if (!isInitialized)
            {
                isInitialized = true;
                initializeEventsAndFields();
            }

            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            ModuleResourceConverter resourceConverter = this.GetComponent<ModuleResourceConverter>();

            if (this.CanDoProduction(resourceConverter, out string reasonWhyNotMessage))
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
                //if (resourceConverter != null && resourceConverter.IsActivated)
                //{
                //ScreenMessages.PostScreenMessage($"{this.name} is shutting down:  {reasonWhyNotMessage}", 10.0f);
                //resourceConverter.StopResourceConverter();
                //}
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

            if (this.vessel.GetCrewCount() == 0)
            {
                return false;
            }

            var crewRequirement = this.GetComponent<CbnCrewRequirement>();
            return crewRequirement == null || crewRequirement.TryAssignCrew();
        }

        /// <summary>
        ///   Returns true if the part has electrical power
        /// </summary>
		private bool IsPowered(ModuleResourceConverter resourceConverter)
        {
            if (resourceConverter == null)
            {
                // This module doesn't have a power requirement
                return true;
            }

            // I don't see a good way to determine if a converter is running stably.
            //  lastTimeFactor seems to be the amount of the last recipe that it was able
            //  to successfully convert, which ought to be it, but lastTimeFactor is zero
            //  for several iterations after unpacking the vessel.  This code attempts to
            //  compensate for that by waiting at least 10 seconds before declaring itself
            //  unpowered.
            if (resourceConverter.lastTimeFactor == 0)
            {
                if (this.firstNoPowerIndicator < 0)
                {
                    this.firstNoPowerIndicator = Planetarium.GetUniversalTime();
                    return true;
                }
                else
                {
                    return Planetarium.GetUniversalTime() - this.firstNoPowerIndicator < 10.0;
                }
            }
            else
            {
                this.firstNoPowerIndicator = -1;
                return true;
            }
        }

        public TechTier Tier => (TechTier)this.tier;

        public bool IsResearchEnabled { get; private set; }

        public bool IsProductionEnabled { get; private set; }

        public double ProductionRate => this.capacity;

        public virtual bool ContributeResearch(IColonizationResearchScenario target, double amount)
            => target.ContributeResearch(this.Output, this.body, amount);

        public static string GreenInfo(string info)
        {
            return $"<color=#99FF00>{info}</color>";
        }

        private TieredResource inputAsTieredResource;
        private TieredResource outputAsTieredResource;

        public TieredResource Input
        {
            get
            {
                if (this.inputAsTieredResource == null && !string.IsNullOrEmpty(this.input))
                {
                    this.inputAsTieredResource = ColonizationResearchScenario.GetTieredResourceByName(this.input);
                }
                return this.inputAsTieredResource;
            }
        }

        public TieredResource Output
        {
            get
            {
                if (this.outputAsTieredResource == null && !string.IsNullOrEmpty(this.output))
                {
                    this.outputAsTieredResource = ColonizationResearchScenario.GetTieredResourceByName(this.output);
                }
                return this.outputAsTieredResource;
            }
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            if (this.Input != null)
            {
                info.AppendLine($"{GreenInfo("Input:")} {this.Input.BaseName}");
            }

            info.AppendLine($"{GreenInfo("Capacity:")} {this.capacity} {this.Output.CapacityUnits}");

            if (this.Output.CanBeStored)
            {
                info.AppendLine($"{GreenInfo("Output:")} {this.Output.BaseName}");
            }

            if (this.Output is EdibleResource edible)
            {
                info.AppendLine($"{GreenInfo("Quality:")}");
                foreach (TechTier tier in TechTierExtensions.AllTiers)
                {
                    info.AppendLine($" {tier.ToString()}: {(int)(edible.GetPercentOfDietByTier(tier) * 100)}%");
                }
            }

            return info.ToString();
        }

        private void initializeEventsAndFields()
        {
            if (this.Output.ProductionRestriction == ProductionRestriction.Orbit)
            {
                Events["ChangeBody"].guiActive = false;
                Events["ChangeBody"].guiActiveEditor = false;
                Fields["body"].guiActive = false;
                Fields["body"].guiActiveEditor = false;
            }
        }
    }
}
