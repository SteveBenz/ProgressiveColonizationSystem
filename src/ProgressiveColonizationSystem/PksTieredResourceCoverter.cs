using System;
using System.Collections.Generic;
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

        private static TierSuitability RiskTolerance = TierSuitability.Ideal;
        private static Part DefaultPartSetFor = null;
        private static TechTier LastTierSelected;

        [KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Tier", isPersistant = true, guiActiveEditor = true)]
        public int tier;

        [KSPField(advancedTweakable = false, category = "Nermables", guiName = "Target Body", isPersistant = true, guiActiveEditor = true)]
        public string body = NotSet;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Setup")]
        public void ChangeTier()
            => PartSetupDialog.Show(this.outputAsTieredResource, this.Body, this.Tier, this.OnSetupSelected);

        public void OnSetupSelected(PartSetupDialog dialog)
        {
            this.tier = (int)dialog.Tier;
            this.body = dialog.Body ?? NotSet;
            PksTieredResourceConverter.RiskTolerance = dialog.RiskLevel;
            PksTieredResourceConverter.DefaultPartSetFor = this.part;
            PksTieredResourceConverter.LastTierSelected = dialog.Tier;
            if (dialog.Applicability == PartSetupDialog.DecisionImpact.AllParts)
            {
                this.SetupAllParts();
            }
        }

        [KSPField(guiActive = false, guiActiveEditor = false, isPersistant = false, guiName = "status")]
        public string reasonWhyDisabled;

        // Apparently, the 'bool enabled' field on the resource converter itself is not to be trusted...
        // we'll keep our own record of what was done.
        bool? resourceConverterIsEnabled = null;

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

            if (this.Output.ProductionRestriction == ProductionRestriction.Space
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

        public void FixedUpdate()
        {
            this.OnFixedUpdate();
        }

        internal static TechTier GetTechTierFromConfig(ConfigNode moduleValues)
        {
            int tierInt = 0;
            moduleValues.TryGetValue(nameof(tier), ref tierInt);
            return (TechTier)tierInt;
        }

        private void SetupFromDefault()
        {
            if (DefaultPartSetFor == null || !EditorLogic.fetch.ship.Parts.Contains(DefaultPartSetFor))
            {
                // Either we are new in the editor, or we are editing a new ship, so reset to default
                DefaultPartSetFor = null;
                RiskTolerance = TierSuitability.Ideal;
            }

            string body = null;
            if (this.outputAsTieredResource.ResearchCategory.Type != ProductionRestriction.Space)
            {
                body = DefaultPartSetFor == null ? null : EditorLogic.fetch.ship.Parts
                    .Select(p => p.FindModuleImplementing<PksTieredResourceConverter>())
                    .Select(trc => trc?.body)
                    .FirstOrDefault(b => b != null && b != "" && b != NotSet);
                if (body == null)
                {
                    // Can't fix it.
                    return;
                }
            }

            for (TechTier tier = (RiskTolerance == TierSuitability.UnderTier ? LastTierSelected : TechTier.Tier4)
                ; tier >= TechTier.Tier0; --tier)
            {
                var suitability = StaticAnalysis.GetTierSuitability(ColonizationResearchScenario.Instance, this.outputAsTieredResource, tier, body);
                if (suitability <= RiskTolerance)
                {
                    this.tier = (int)tier;
                    this.body = body ?? NotSet;
                    return;
                }
            }
        }

        private void SetupAllParts()
        {
            List<Part> parts = EditorLogic.fetch.ship.Parts;
            foreach (var part in EditorLogic.fetch.ship.Parts)
            {
                var module = part.FindModuleImplementing<PksTieredResourceConverter>();
                if (module != null)
                {
                    module.SetupFromDefault();
                }
            }
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

                // We can tell the difference between a part that's been initialized (in a save file) by whether the
                // body has been set -- for parts that are landed or orbiting.  But for space, there's no sure way to
                // tell (although maybe there would be if the 'tier' field was initialized to -1?)  Anyway, with this,
                // if we load a ship that was saved as a Tier0 ship with just hydroponics, and we load it up after
                // we've advanced to Tier1, this will update it to Tier1, but if we then advance to tier 2 and load
                // the Tier1-version of the ship, it'll stay at Tier1 until the user clicks the dialog.  Meh, I can live
                // with that bug.
                bool partIsUninitialized = (this.outputAsTieredResource.ResearchCategory.Type == ProductionRestriction.Space
                     ? this.tier == 0 : (string.IsNullOrEmpty(this.body) || this.body == NotSet));
                if (HighLogic.LoadedSceneIsEditor && partIsUninitialized)
                {
                    SetupFromDefault();
                }
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

                if (this.tier < (int)this.MaxTechTierResearched)
                {
                    this.ReasonWhyResearchIsDisabled = "Not cutting edge gear";
                    this.IsResearchEnabled = false;
                }
                else if (!this.Output.ResearchCategory.CanDoResearch(this.vessel, this.Tier, out var reason))
                {
                    this.ReasonWhyResearchIsDisabled = reason;
                    this.IsResearchEnabled = false;
                }
                else
                {
                    this.ReasonWhyResearchIsDisabled = null;
                    this.IsResearchEnabled = true;
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
                this.ReasonWhyResearchIsDisabled = "Production disabled";
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

        public string ReasonWhyResearchIsDisabled { get; private set; }

        public bool IsProductionEnabled { get; private set; }

        public double ProductionRate => this.capacity;

        public virtual bool ContributeResearch(IColonizationResearchScenario target, double amount)
            => target.ContributeResearch(this.Output, this.body, amount);

        private TieredResource outputAsTieredResource;

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

        public TieredResource Input => this.Output.MadeFrom(this.Tier);

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            var madeFrom = this.Input;
            if (madeFrom != null)
            {
                info.AppendLine($"{Green("Input:")} {madeFrom.BaseName}");
            }

            info.AppendLine($"{Green("Capacity:")} {this.capacity} {this.Output.CapacityUnits}");

            if (this.Output.CanBeStored)
            {
                info.AppendLine($"{Green("Output:")} {this.Output.BaseName}");
            }

            if (this.Output.IsEdible)
            {
                info.AppendLine($"{Green("Quality:")}");
                foreach (TechTier tier in TechTierExtensions.AllTiers)
                {
                    info.AppendLine($" {tier.ToString()}: {(int)(this.Output.GetPercentOfDietByTier(tier) * 100)}%");
                }
            }

            return info.ToString();
        }

        private void initializeEventsAndFields()
        {
            if (this.Output.ProductionRestriction == ProductionRestriction.Space)
            {
                Fields["body"].guiActive = false;
                Fields["body"].guiActiveEditor = false;
            }
        }
    }
}
