using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Experience;

namespace ProgressiveColonizationSystem
{
    public interface IPksCrewRequirement
    {
        bool IsRunning { get; }
        bool IsStaffed { get; set; }
        float CapacityRequired { get; }
        string RequiredEffect { get; }
        int RequiredLevel { get; }
    }

    public class PksCrewRequirement
        : PartModule, IPksCrewRequirement
    {
        [KSPField]
        public string requiredEffect;

        [KSPField]
        public float requiredCrew;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Has Crew")]
        private bool isStaffed = true;

        private bool isInitialized = false;
        private PksTieredResourceConverter tieredResourceConverter = null;
        private BaseConverter resourceConverter = null;

        [KSPField(isPersistant = true)]
        private string requiredEffectOverride = "";
        [KSPField(isPersistant = true)]
        private float requiredCrewOverride;
        [KSPField(isPersistant = true)]
        private int requiredCrewLevelOverride;

        public override string GetModuleDisplayName()
        {
            return "Crew Requirements";
        }

        public string RequiredEffect => this.requiredEffectOverride == "" ? this.requiredEffect : this.requiredEffectOverride;

        public int RequiredLevel
        {
            get
            {
                this.Initialize();
                if (this.requiredEffectOverride == "")
                {
                    return this.requiredCrewLevelOverride;
                }
                else if (this.tieredResourceConverter == null)
                {
                    return 0;
                }
                else
                {
                    return (int)this.tieredResourceConverter.Tier + 1;
                }
            }
        }

        public override string GetInfo()
        {
            List<ExperienceTraitConfig> careers = GameDatabase.Instance.ExperienceConfigs
                .GetTraitsWithEffect(this.requiredEffect)
                .Select(name => GameDatabase.Instance.ExperienceConfigs.GetExperienceTraitConfig(name))
                .ToList();

            StringBuilder info = new StringBuilder();
            info.AppendLine(TextEffects.Green("Required Crew:"));
            info.AppendLine($"Staffing Level: {this.requiredCrew}");
            info.AppendLine(TextEffects.Green("Traits:"));
            foreach (TechTier tier in TechTierExtensions.AllTiers)
            {
                info.Append($"{tier}: ");

                info.Append(DescribeKerbalsWithEffect(this.requiredEffect, tier));
                info.AppendLine();
            }
            return info.ToString();
        }

        public static string DescribeKerbalsWithEffect(string experienceEffect, TechTier tier)
        {
            List<ExperienceTraitConfig> careers = GameDatabase.Instance.ExperienceConfigs
                .GetTraitsWithEffect(experienceEffect)
                .Select(name => GameDatabase.Instance.ExperienceConfigs.GetExperienceTraitConfig(name))
                .ToList();

            StringBuilder info = new StringBuilder();

            for (int i = 0; i < careers.Count; ++i)
            {
                ExperienceEffectConfig effectConfig = careers[i].Effects.First(effect => effect.Name == experienceEffect);
                int numStars = 1 + (int)tier - int.Parse(effectConfig.Config.GetValue("level"));

                if (i == careers.Count - 1)
                {
                    info.Append(" or a ");
                }
                else if (i > 0)
                {
                    info.Append(", ");
                }

                info.Append(DescribeKerbalTrait(numStars, careers[i].Title));
            }
            return info.ToString();
        }

        public static string DescribeKerbalTrait(int numStars, string trait)
        {
            string result = "";
            if (numStars > 0)
            {
                result = $"{numStars}*"; // sad-face: no joy with  &star; &#x2605 or \u2605
            }
            result += trait;
            return result;
        }

        public BaseConverter ResourceConverter
        {
            get
            {
                this.Initialize();
                return this.resourceConverter;
            }
        }

        internal void OverrideRequirement(string requiredEffect, TechTier techTier, float numCrewRequired)
        {
            this.requiredEffectOverride = requiredEffect;
            this.requiredCrewOverride = numCrewRequired;
            this.requiredCrewLevelOverride = (int)techTier;
        }

        internal void CancelOverride()
        {
            this.requiredEffectOverride = "";
            this.requiredCrewOverride = 0;
            this.requiredCrewLevelOverride = 0;
        }

        public bool IsRunning =>
            HighLogic.LoadedSceneIsEditor
            || this.ResourceConverter == null
            || this.ResourceConverter.IsActivated;

        public bool IsStaffed
        {
            get => this.isStaffed;
            set
            {
                if (this.isStaffed && !value)
                {
                    this.isStaffed = value;
                    if (this.IsRunning)
                    {
                        ScreenMessages.PostScreenMessage($"{this.part.name} has stopped production because there's not enough crew to operate it.", 10f);
                    }
                }
                else if (!this.isStaffed && value && IsRunning)
                {
                    isStaffed = value;
                    if (this.IsRunning)
                    {
                        ScreenMessages.PostScreenMessage($"{this.part.name} has resumed production because it has enough skilled crew to operate now.", 10f);
                    }
                }
            }
        }

        public float CapacityRequired => this.requiredEffectOverride == "" ? this.requiredCrew : this.requiredCrewOverride;

        private void Initialize()
        {
            if (!this.isInitialized)
            {
                this.resourceConverter = this.part.FindModuleImplementing<BaseConverter>();
                this.tieredResourceConverter = this.part.FindModuleImplementing<PksTieredResourceConverter>();
            }
            this.isInitialized = true;
        }
    }
}
