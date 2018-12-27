using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public enum TechTier
    {
        Tier0 = 0,
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3,
        Tier4 = 4
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

        public static double AgroponicMaxDietRatio(this TechTier techTier)
        {
            // TODO: Make Configurable
            switch(techTier)
            {
                case TechTier.Tier0:
                default:
                    return 0.2;
                case TechTier.Tier1:
                    return 0.4;
                case TechTier.Tier2:
                    return 0.55;
                case TechTier.Tier3:
                    return 0.7;
                case TechTier.Tier4:
                    return 0.95;
            }
        }

        public static double AgricultureMaxDietRatio(this TechTier techTier)
        {
            // TODO: Make Configurable
            switch (techTier)
            {
                case TechTier.Tier0:
                default:
                    return 0.6;
                case TechTier.Tier1:
                    return 0.85;
                case TechTier.Tier2:
                    return 0.95;
                case TechTier.Tier3:
                    return 0.98;
                case TechTier.Tier4:
                    return 1.0;
            }
        }

        public static string FertilizerResourceName(this TechTier techTier)
        {
            return techTier == TechTier.Tier4 ? "Fertilizer" : $"LocalFertilizer-{techTier.ToString()}";
        }

        public static string SnacksResourceName(this TechTier techTier)
        {
            return techTier == TechTier.Tier4 ? "Snacks" : $"LocalSnacks-{techTier.ToString()}";
        }

        private const double KerbalDaysPerKerbalYear = 426.0;

        // Should this be configurable?  Seems like a thing that would be good to start,
        //  but once the values are made sensible, who really cares to do it?
        private static readonly double[] agroponicsResearchTimesInKerbalSeconds = new double[]
        {
            1.0 * KerbalDaysPerKerbalYear, // 1 / (5 kerbals in space * .2) = 1 year
            10.0 * KerbalDaysPerKerbalYear, // 10 / (10 kerbals in space * .4) = 2.5 years  <- waiting for duna/eve landers
            40.0 * KerbalDaysPerKerbalYear, // 40 / (15 kerbals in space * .55) = 5 years
            100.0 * KerbalDaysPerKerbalYear, // 100 / (20 kerbals in space * 0.7) = 7 years
            double.MaxValue,
        };

        public static double KerbalSecondsToResearchNextAgroponicsTier(this TechTier techTier)
            => agroponicsResearchTimesInKerbalSeconds[(int)techTier];

        public static string DisplayName(this TechTier tier) => tier.ToString();
    }
}
