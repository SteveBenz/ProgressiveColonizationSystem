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

        private const double KerbalDaysPerKerbalYear = 426.0;

        // Should this be configurable?  Seems like a thing that would be good to start,
        //  but once the values are made sensible, who really cares to do it?
        private static readonly double[] agroponicsResearchTimesInKerbalSeconds = new double[]
        {
            1.0 * KerbalDaysPerKerbalYear, // 1 / (5 kerbals in space * .2) = 1 year
            10.0 * KerbalDaysPerKerbalYear, // 10 / (10 kerbals in space * .4) = 2.5 years  <- waiting for duna/eve landers
            40.0 * KerbalDaysPerKerbalYear, // 40 / (15 kerbals in space * .55) = 5 years
            100.0 * KerbalDaysPerKerbalYear, // 100 / (20 kerbals in space * 0.7) = 7 years            double.MaxValue,
        };

        // Should this be configurable?  Seems like a thing that would be good to start,
        //  but once the values are made sensible, who really cares to do it?
        private static readonly double[] agricultureResearchTimesInKerbalSeconds = new double[]
        {
            // These values are total spitballs.
            1.0 * KerbalDaysPerKerbalYear,
            4.0 * KerbalDaysPerKerbalYear,
            8.0 * KerbalDaysPerKerbalYear,
            20.0 * KerbalDaysPerKerbalYear,
            double.MaxValue,
        };

        public static double KerbalSecondsToResearchNextAgroponicsTier(this TechTier techTier)
            => agroponicsResearchTimesInKerbalSeconds[(int)techTier];

        public static double KerbalSecondsToResearchNextAgricultureTier(this TechTier techTier)
            => agricultureResearchTimesInKerbalSeconds[(int)techTier];

		public static double KerbalSecondsToResearchNextProductionTier(this TechTier techTier)
			=> agricultureResearchTimesInKerbalSeconds[(int)techTier];

		public static double KerbalSecondsToResearchNextScanningTier(this TechTier techTier)
			=> agricultureResearchTimesInKerbalSeconds[(int)techTier];
	}
}
