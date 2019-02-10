using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public interface IPksCrewRequirement
    {
        bool CanRunPart(SkilledCrewman crewman);
        bool IsRunning { get; }
        bool IsStaffed { get; set; }
        float CapacityRequired { get; }
        IEnumerable<string> RequiredTraits { get; }
    }

    public class PksCrewRequirement
        : PartModule, IPksCrewRequirement
    {
        [KSPField]
        public string specialistTraits;

        [KSPField]
        public string generalistTrait;

        [KSPField]
        public float requiredCrew;

        [KSPField(isPersistant = true)]
        private bool isStaffed = true;

        private bool isInitialized = false;
        private PksTieredResourceConverter tieredResourceConverter = null;
        private HashSet<string> specialistTraitsHash = null;
        private BaseConverter resourceConverter = null;

        private const int specialistStarBonus = 3; // Perhaps this should go in a setting?

        public override string GetModuleDisplayName()
        {
            return "Crew Requirements";
        }

        public IEnumerable<string> SpecialistTraits
        {
            get
            {
                this.Initialize();
                return this.specialistTraitsHash;
            }
        }

        public IEnumerable<string> RequiredTraits => new string[] { generalistTrait }.Union(this.SpecialistTraits);

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            info.AppendLine(PksTieredResourceConverter.GreenInfo("Required Crew:"));
            info.AppendLine($"Staffing Level: {this.requiredCrew}");
            info.AppendLine(PksTieredResourceConverter.GreenInfo("Traits:"));
            foreach (TechTier tier in TechTierExtensions.AllTiers)
            {
                info.Append($"{tier}: ");
                bool any = false;
                if (!string.IsNullOrEmpty(specialistTraits))
                {
                    foreach (string trait in SpecialistTraits)
                    {
                        // TODO: Validate that these traits are actually a thing
                        if (any)
                        {
                            info.Append(", ");
                        }
                        info.Append(this.DescribeKerbalTrait(1 + (int)tier - specialistStarBonus, trait));
                        any = true;
                    }
                }
                if (!string.IsNullOrEmpty(generalistTrait))
                {
                    if (any)
                    {
                        info.Append(" or a ");
                    }
                    info.Append(this.DescribeKerbalTrait(1 + (int)tier, this.generalistTrait));
                }
                info.AppendLine();
            }
            return info.ToString();
        }

        private string DescribeKerbalTrait(int numStars, string trait)
        {
            string result = "";
            if (numStars > 0)
            {
                result = $"{numStars}*"; // sad-face: no joy with  &star; &#x2605 or \u2605
            }
            result += trait;
            return result;
        }

        public bool CanRunPart(SkilledCrewman crewman)
        {
            this.Initialize();
            bool isSpecialistForThisPart = this.specialistTraitsHash.Contains(crewman.Trait);
            bool isGeneralistForThisPart = this.generalistTrait == crewman.Trait;
            if (!isSpecialistForThisPart && !isGeneralistForThisPart)
            {
                return false;
            }

            int tier = this.tieredResourceConverter == null ? 0 : this.tieredResourceConverter.tier;

            return crewman.Stars + (isSpecialistForThisPart ? specialistStarBonus : 0) > tier;
        }

        public BaseConverter ResourceConverter
        {
            get
            {
                this.Initialize();
                return this.resourceConverter;
            }
        }

        public bool IsRunning
        {
            get
            {
                return this.ResourceConverter == null ? true : this.ResourceConverter.IsActivated;
            }
        }

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

        public float CapacityRequired => this.requiredCrew;

        private void Initialize()
        {
            if (!this.isInitialized)
            {
                this.specialistTraitsHash = new HashSet<string>(specialistTraits.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries));
                this.resourceConverter = this.part.FindModuleImplementing<BaseConverter>();
                this.tieredResourceConverter = this.part.FindModuleImplementing<PksTieredResourceConverter>();
            }
            this.isInitialized = true;
        }
    }
}
