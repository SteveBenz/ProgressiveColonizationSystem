using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class Snacks
    {
        public const string AgroponicSnackResourceBaseName = "HydroponicSnacks";
        public const string AgriculturalSnackResourceBaseName = "Snacks";

        public static double AgroponicMaxDietRatio(TechTier techTier)
        {
            // TODO: Make Configurable
            switch (techTier)
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

        public static double AgricultureMaxDietRatio(TechTier techTier)
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
    }
}
