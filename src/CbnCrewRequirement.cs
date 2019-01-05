using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class CbnCrewRequirement
        : PartModule
    {
        [KSPField]
        public string specialistTraits;

        [KSPField]
        public string generalistTrait;

        [KSPField]
        public float requiredCrew;

        public override string GetModuleDisplayName()
        {
            return "Crew Requirements";
        }

        public IEnumerable<string> SpecialistTraits => specialistTraits.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        private const int specialistStarBonus = 3; // Perhaps this should go in a setting?

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            info.AppendLine(TieredResourceCoverter.GreenInfo("Required Crew:"));
            info.AppendLine($"Staffing Level: {this.requiredCrew}");
            info.AppendLine(TieredResourceCoverter.GreenInfo("Traits:"));
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
                        info.Append(this.DescribeKerbal(1 + (int)tier - specialistStarBonus, trait));
                        any = true;
                    }
                }
                if (!string.IsNullOrEmpty(generalistTrait))
                {
                    if (any)
                    {
                        info.Append(" or a ");
                    }
                    info.Append(this.DescribeKerbal(1 + (int)tier, this.generalistTrait));
                }
                info.AppendLine();
            }
            return info.ToString();
        }

        private string DescribeKerbal(int numStars, string trait)
        {
            string result = "";
            if (numStars > 0)
            {
                result = $"{numStars}*"; // sad-face: no joy with  &star; &#x2605 or \u2605
            }
            result += trait;
            return result;
        }

        public bool TryAssignCrew()
        {
            return this.vessel.GetVesselCrew().Any(k => k.trait == generalistTrait);
        }

        public string GetCrewRequirement()
        {
            return $"The crew needs to include at least one {this.generalistTrait}.";
        }
    }
}
