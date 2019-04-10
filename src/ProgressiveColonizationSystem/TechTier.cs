using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public enum TechTier
    {
        Tier0 = 0,
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3,
        Tier4 = 4,
    }

    public static class TechTierExtensions
    {
        public static IEnumerable<TechTier> AllTiers
        {
            get
            {
                return Enum.GetValues(typeof(TechTier)).Cast<TechTier>();
            }
        }

        public static string DisplayName(this TechTier techTier)
            => techTier.ToString();
    }
}
