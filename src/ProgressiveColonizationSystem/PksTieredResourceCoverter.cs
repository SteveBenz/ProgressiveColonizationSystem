using System;
using System.Linq;
using System.Text;

using static ProgressiveColonizationSystem.TextEffects;

namespace ProgressiveColonizationSystem
{
    public class PksTieredResourceConverter
        : PartModule, ITieredProducer
    {
        public const string NotSet = "<not set>";

        private double firstNoPowerIndicator = -1.0;
        private double firstNotInSituationIndicator = -1.0;

        [KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Tier", isPersistant = true, guiActiveEditor = true)]
        public int tier;

        [KSPField(advancedTweakable = false, category = "Nermables", guiName = "Target Body", isPersistant = true, guiActiveEditor = true)]
        public string body = NotSet;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Tier")]
        public void ChangeTier()
        {
            this.tier = (this.tier + 1) % ((int)this.MaxTechTierResearched + 1);
        }

        [KSPField(guiActive = false, guiActiveEditor = false, isPersistant = false, guiName = "status")]
        public string reasonWhyDisabled;

        // Apparently, the 'bool enabled' field on the resource converter itself is not to be trusted...
        // we'll keep our own record of what was done.
        bool? resourceConverterIsEnabled = null;

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

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Show PKS UI")]
        public void ShowDialog()
        {
            PksToolbarDialog.Show();
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
        public int inputRequirementStartingTier;

        [KSPField]
        public float capacity;

        private bool isInitialized = false;

        protected virtual TechTier MaxTechTierResearched
            => ColonizationResearchScenario.Instance.GetMaxUnlockedTier(this.Output, this.body);

        public string Body
        {
            get => this.body == NotSet ? null : this.body;
            set
            {
                this.body = value;
                this.tier = (int)ColonizationResearchScenario.Instance.GetMaxUnlockedTier(this.Output, this.body);
            }
        }

        private bool IsSituationCorrect(out string reasonWhyNotMessage)
        {

            if (this.Output.ProductionRestriction == ProductionRestriction.LandedOnBody
             && (this.vessel.situation != Vessel.Situations.LANDED || this.body != this.vessel.mainBody.name))
            {
                reasonWhyNotMessage = $"Not landed on {this.body}";
                return false;
            }

            if (this.Output.ProductionRestriction == ProductionRestriction.OrbitOfBody
             && (this.vessel.situation != Vessel.Situations.ORBITING || this.body != this.vessel.mainBody.name))
            {
                reasonWhyNotMessage = $"Not orbiting {this.body}";
                return false;
            }

            if (this.Output.ProductionRestriction == ProductionRestriction.Orbit
             && this.vessel.situation != Vessel.Situations.ORBITING
             && this.vessel.situation != Vessel.Situations.SUB_ORBITAL
             && this.vessel.situation != Vessel.Situations.ESCAPING)
            {
                reasonWhyNotMessage = $"Not in space";
                return false;
            }

            reasonWhyNotMessage = null;
            return true;
        }

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

            if (!this.IsSituationCorrect(out reasonWhyNotMessage))
            {
                return false;
            }

            reasonWhyNotMessage = null;
            return true;
        }

        private bool CanDoResearch()
        {
            if (this.tier < (int)this.MaxTechTierResearched)
            {
                return false;
            }

            if (!this.Output.ResearchCategory.CanDoResearch(this.vessel, this.Tier, out var _))
            {
                return false;
            }

            return true;
        }

        public void FixedUpdate()
        {
            this.OnFixedUpdate();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            // somehow, if this is called from OnAwake, like it sensibly should be, it breaks
            // the part so that FixedUpdate never gets called.
            if (!isInitialized)
            {
                isInitialized = true;
                initializeEventsAndFields();
            }

            if (!HighLogic.LoadedSceneIsFlight)
            {
                this.IsProductionEnabled = true;
                this.IsResearchEnabled = true;
                return;
            }

            ModuleResourceConverter resourceConverter = this.GetComponent<ModuleResourceConverter>();

            bool isEnableable = this.IsSituationCorrect(out string reasonWhyNotMessage);
            // remember resourceConverterIsEnabled is a 3-way -- true, false, and null for I haven't done anything yet.
            // It's important to not call EnableModule every time through FixedUpdate, as it's very slow.
            if (isEnableable && this.resourceConverterIsEnabled != true)
            {
                this.reasonWhyDisabled = null;
                this.Fields["reasonWhyDisabled"].guiActive = false;
                resourceConverter.EnableModule();
                this.resourceConverterIsEnabled = true;
                this.firstNotInSituationIndicator = -1.0;
            }
            else if (!isEnableable && this.resourceConverterIsEnabled != false)
            {
                // Enabling instantly is okay, but lots of times the base will bounce a bit on loading
                // into a scene, so give 10 seconds before shutting the module down.
                if (this.firstNotInSituationIndicator < 0)
                {
                    this.firstNotInSituationIndicator = Planetarium.GetUniversalTime();
                }
                else if (Planetarium.GetUniversalTime() - this.firstNotInSituationIndicator > 10.0)
                {
                    this.reasonWhyDisabled = Red(reasonWhyNotMessage);
                    this.Fields["reasonWhyDisabled"].guiActive = true;
                    resourceConverter.DisableModule();
                    this.resourceConverterIsEnabled = false;
                }
            }

            if (isEnableable && this.CanDoProduction(resourceConverter, out reasonWhyNotMessage))
            {
                this.IsProductionEnabled = true;

                if (this.CanDoResearch())
                {
                    this.IsResearchEnabled = true;
                }
                else
                {
                    this.IsResearchEnabled = false;
                }
            }
            else
            {
                // Used to do this, but that's nothing but a hassle - when power recovers or whatever, it's nice that it
                // just pops back to life.
                //if (resourceConverter != null && resourceConverter.IsActivated)
                //{
                //ScreenMessages.PostScreenMessage($"{this.name} is shutting down:  {reasonWhyNotMessage}", 10.0f);
                //resourceConverter.StopResourceConverter();
                //}
                this.IsProductionEnabled = false;
                this.IsResearchEnabled = false;
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

            var crewRequirement = this.GetComponent<PksCrewRequirement>();
            return crewRequirement == null || crewRequirement.IsStaffed;
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

        public TechTier Tier
        {
            get => (TechTier)this.tier;
            set
            {
                this.tier = Math.Min((int)value, (int)this.MaxTechTierResearched);
            }
        }

        public bool IsResearchEnabled { get; private set; }

        public bool IsProductionEnabled { get; private set; }

        public double ProductionRate => this.capacity;

        public virtual bool ContributeResearch(IColonizationResearchScenario target, double amount)
            => target.ContributeResearch(this.Output, this.body, amount);

        private TieredResource inputAsTieredResource;
        private TieredResource outputAsTieredResource;

        public TieredResource Input
        {
            get
            {
                if (this.inputAsTieredResource == null && !string.IsNullOrEmpty(this.input) && this.tier >= inputRequirementStartingTier)
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
                info.AppendLine($"{Green("Input:")} {this.Input.BaseName}");
            }

            info.AppendLine($"{Green("Capacity:")} {this.capacity} {this.Output.CapacityUnits}");

            if (this.Output.CanBeStored)
            {
                info.AppendLine($"{Green("Output:")} {this.Output.BaseName}");
            }

            if (this.Output is EdibleResource edible)
            {
                info.AppendLine($"{Green("Quality:")}");
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
