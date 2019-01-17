using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public interface ICbnCrewRequirement
    {
        bool CanRunPart(SkilledCrewman crewman);
        bool IsRunning { get; }
        bool IsStaffed { get; set; }
        float CapacityRequired { get; }
    }

    public class CbnCrewRequirement
        : PartModule, ICbnCrewRequirement
    {
        [KSPField]
        public string specialistTraits;

        [KSPField]
        public string generalistTrait;

        [KSPField]
        public float requiredCrew;

        [KSPField]
        public int tier = -1;

        private BaseConverter resourceConverter = null;

        [KSPField(isPersistant = true)]
        private bool isStaffed = true;

        public override string GetModuleDisplayName()
        {
            return "Crew Requirements";
        }

        public IEnumerable<string> SpecialistTraits => specialistTraits.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        private const int specialistStarBonus = 3; // Perhaps this should go in a setting?

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            info.AppendLine(CbnTieredResourceConverter.GreenInfo("Required Crew:"));
            info.AppendLine($"Staffing Level: {this.requiredCrew}");
            info.AppendLine(CbnTieredResourceConverter.GreenInfo("Traits:"));
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
            bool isSpecialistForThisPart = this.SpecialistTraits.Contains(crewman.Trait);
            bool isGeneralistForThisPart = this.generalistTrait == crewman.Trait;
            if (!isSpecialistForThisPart && !isGeneralistForThisPart)
            {
                return false;
            }

            int tier;
            if (this.tier < 0)
            {
                var tieredResourceModule = this.part.FindModuleImplementing<CbnTieredResourceConverter>();
                tier = tieredResourceModule == null ? 0 : tieredResourceModule.tier;
            }
            else
            {
                tier = this.tier;
            }

            return crewman.Stars + (isSpecialistForThisPart ? specialistStarBonus : 0) > tier;
        }

        public BaseConverter ResourceConverter
        {
            get
            {
                if (this.resourceConverter == null)
                {
                    this.resourceConverter = this.part.FindModuleImplementing<BaseConverter>();
                }
                return this.resourceConverter;
            }
        }

        public bool IsRunning
        {
            get
            {
                return this.ResourceConverter == null ? true : this.ResourceConverter.isActiveAndEnabled;
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
                        ScreenMessages.PostScreenMessage($"{this.part.name} has stopped production because there's not enough crew to operate it.");
                    }
                }
                else if (!this.isStaffed && value && IsRunning)
                {
                    isStaffed = value;
                    if (this.IsRunning)
                    {
                        ScreenMessages.PostScreenMessage($"{this.part.name} has resumed production because it has enough skilled crew to operate now.");
                    }
                }
            }
        }

        public float CapacityRequired => this.requiredCrew;
    }
}
