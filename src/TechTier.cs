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
            return techTier.GetTieredResourceName("Fertilizer");
        }

        public static string SnacksResourceName(this TechTier techTier)
        {
            return techTier.GetTieredResourceName("Snacks");
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

		public static string DisplayName(this TechTier tier) => tier.ToString();

		public static string GetTieredResourceName(this TechTier techTier, string name)
		{
			int dashIndex = name.IndexOf('-');
			string baseName = dashIndex < 0 ? name : name.Substring(0, dashIndex);
			return techTier == TechTier.Tier4 ? baseName : $"{baseName}-{techTier.ToString()}";
		}

		public static bool TryParseTieredResourceName(string tieredResourceName, out string tier4Name, out TechTier tier)
		{
			int dashIndex = tieredResourceName.IndexOf('-');
			if (dashIndex < 0)
			{
				tier4Name = tieredResourceName;
				tier = TechTier.Tier4;
				return true;
			}
			else
			{
				try
				{
					// Oh, but we do pine ever so much for .Net 4.6...
					tier = (TechTier)Enum.Parse(typeof(TechTier), tieredResourceName.Substring(dashIndex + 1));
					tier4Name = tieredResourceName.Substring(0, dashIndex);
					return true;
				}
				catch (Exception)
				{
					tier4Name = null;
					tier = TechTier.Tier0;
					return false;
				}
			}
		}
	}
}
