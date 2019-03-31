using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public abstract class ResearchCategory
    {
        private readonly double kerbalYearsToTier1, kerbalYearsToTier2, kerbalYearsToTier3, kerbalYearsToTier4;

        protected ResearchCategory(double kerbalYearsToTier1, double kerbalYearsToTier2, double kerbalYearsToTier3, double kerbalYearsToTier4)
        {
            this.kerbalYearsToTier1 = kerbalYearsToTier1;
            this.kerbalYearsToTier2 = kerbalYearsToTier2;
            this.kerbalYearsToTier3 = kerbalYearsToTier3;
            this.kerbalYearsToTier4 = kerbalYearsToTier4;
        }

        public double KerbalYearsToNextTier(TechTier tier)
        {
            switch(tier)
            {
                case TechTier.Tier0: return this.kerbalYearsToTier1;
                case TechTier.Tier1: return this.kerbalYearsToTier2;
                case TechTier.Tier2: return this.kerbalYearsToTier3;
                case TechTier.Tier3: return this.kerbalYearsToTier4;
                case TechTier.Tier4: default: return double.MaxValue;
            }
        }

        public abstract bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot);

        public abstract string BreakthroughMessage(TechTier newTier);

        public abstract string BoringBreakthroughMessage(TechTier newTier);

        public abstract string DisplayName { get; }

        #region Common implementations of CanDoResearch
        protected bool MaxLimitedOnEasyWorlds(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
        {
            switch (currentTier)
            {
                case TechTier.Tier0:
                case TechTier.Tier1:
                    break;
                case TechTier.Tier2:
                    if (IsVeryEasyWorld(vessel))
                    {
                        reasonWhyNot = $"Disabled - Mun and Minmus are boring";
                        return false;
                    }
                    break;
                case TechTier.Tier3:
                    if (!IsHardWorld(vessel))
                    {
                        reasonWhyNot = $"Disabled - Need Eloo, Dres, Eve or Mojo";
                        return false;
                    }
                    break;
                case TechTier.Tier4:
                default:
                    reasonWhyNot = MaxTierMessage;
                    return false;
            }
            reasonWhyNot = null;
            return true;
        }

        protected bool MaxLimitedNearKerbin(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
        {
            switch(currentTier)
            {
                case TechTier.Tier0:
                case TechTier.Tier1:
                    break;
                case TechTier.Tier2:
                    if (IsNearKerbin(vessel))
                    {
                        reasonWhyNot = $"Disabled - Too near {FlightGlobals.GetHomeBody().name}";
                        return false;
                    }
                    break;
                case TechTier.Tier3:
                    if (IsAnywhereCloseToKerbin(vessel))
                    {
                        reasonWhyNot = $"Disabled - Too near {FlightGlobals.GetHomeBody().name}";
                        return false;
                    }
                    break;
                case TechTier.Tier4:
                default:
                    reasonWhyNot = MaxTierMessage;
                    return false;
            }
            reasonWhyNot = null;
            return true;
        }

        protected bool NoResearchLimits(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
        {
            if (currentTier == TechTier.Tier4)
            {
                reasonWhyNot = MaxTierMessage;
                return false;
            }

            reasonWhyNot = null;
            return true;
        }

        protected string MaxTierMessage = $"At max tier";
        private static double awayFromHomeMinDistanceFromSun = -1;
        private static double awayFromHomeMaxDistanceFromSun = -1;
        private static double farAwayFromHomeMinDistanceFromSun = -1;
        private static double farAwayFromHomeMaxDistanceFromSun = -1;

        protected static bool IsNearKerbin(Vessel vessel)
        {
            if (awayFromHomeMinDistanceFromSun < 0)
            {
                double homeworldDistanceFromSun = FlightGlobals.GetHomeBody().orbit.semiMajorAxis;
                awayFromHomeMinDistanceFromSun = homeworldDistanceFromSun * .9;
                awayFromHomeMaxDistanceFromSun = homeworldDistanceFromSun * 1.1;
            }
            // There are more stylish ways to do this.  It's also a bit problematic for the player
            // because if they ignore a craft on its way back from some faroff world until it
            // reaches kerbin's SOI, then they'll lose all that tasty research.
            //
            // A fix would be to look at the vessel's orbit as well, and, if it just carries the
            // vessel out of the SOI, count that.
            return vessel.distanceToSun > awayFromHomeMinDistanceFromSun
                && vessel.distanceToSun < awayFromHomeMaxDistanceFromSun;
        }

        protected static bool IsAnywhereCloseToKerbin(Vessel vessel)
        {
            if (farAwayFromHomeMaxDistanceFromSun < 0)
            {
                var homeworld = FlightGlobals.GetHomeBody();
                var planets = FlightGlobals.Bodies
                    .Where(b => b.referenceBody == homeworld.referenceBody && b.orbit != null)
                    .OrderBy(b => b.orbit.semiMajorAxis)
                    .ToArray();
                var homeworldIndex = planets.IndexOf(homeworld);
                var innerPlanetOrbit = (homeworldIndex == 0) ? null : planets[homeworldIndex - 1].orbit;
                farAwayFromHomeMinDistanceFromSun = (innerPlanetOrbit == null)
                    ? homeworld.orbit.semiMajorAxis * .8
                    : innerPlanetOrbit.semiMajorAxis * (1 - innerPlanetOrbit.eccentricity);
                var outerPlanetOrbit = (homeworldIndex == planets.Length - 1) ? null : planets[homeworldIndex + 1].orbit;
                farAwayFromHomeMaxDistanceFromSun = (outerPlanetOrbit == null)
                    ? homeworld.orbit.semiMajorAxis * 1.2
                    : outerPlanetOrbit.semiMajorAxis * (1 + outerPlanetOrbit.eccentricity);
            }
            return vessel.distanceToSun < farAwayFromHomeMaxDistanceFromSun
                && vessel.distanceToSun > farAwayFromHomeMinDistanceFromSun;
        }

        protected static bool IsVeryEasyWorld(Vessel vessel)
        {
            return vessel.mainBody.name == "Mun" || vessel.mainBody.name == "Minmus";
        }

        protected static bool IsHardWorld(Vessel vessel)
        {
            return vessel.mainBody.name == "Dres" || vessel.mainBody.name == "Moho" || vessel.mainBody.name == "Eve" || vessel.mainBody.name == "Eloo";
        }
        #endregion
    }

    public class HydroponicResearchCategory
        : ResearchCategory
    {
        public HydroponicResearchCategory()
            : base(3.2, 8.0, 6.6, 15.0) 
        { // For hydroponics, getting out there is the hard part, so the requirements diminish at T4
            // 8 kerbals in orbit for 2 years at .2 per => 16*.2 = 3.2
            // 10 kerbals in orbit for 2 years at .4 per => 10*2*.4 = 20*.4 = 8
            // 6 kerbals in orbit for 2 years at .55 per => 12*.55 = 6.6
        }

        public override string DisplayName => "Hydroponic";

        public override bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
            => MaxLimitedNearKerbin(vessel, currentTier, out reasonWhyNot);

        public override string BreakthroughMessage(TechTier newTier)
            => CrewBlurbs.HydroponicBreakthrough(newTier, c => c.trait == "Scientist" || c.trait == "Biologist");

        public override string BoringBreakthroughMessage(TechTier newTier)
            => CrewBlurbs.BoringHydroponicBreakthrough(newTier);
    }

    public class FarmingResearchCategory
        : ResearchCategory
    {
        public FarmingResearchCategory()
            : base(3.0, 6.0, 10.0, 18.0)
        {
        }

        public override string DisplayName => "Farming";

        public override bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
            => NoResearchLimits(vessel, currentTier, out reasonWhyNot);

        public override string BreakthroughMessage(TechTier newTier)
            => CrewBlurbs.FarmingBreakthrough(newTier, c => c.trait == "Scientist" || c.trait == "Farmer");

        public override string BoringBreakthroughMessage(TechTier newTier)
            => CrewBlurbs.BoringFarmingBreakthrough(newTier);
    }

    public class ProductionResearchCategory
        : ResearchCategory
    {
        public ProductionResearchCategory()
            : base(2.0, 6.0, 20.0, 40.0)
        {
        }

        public override string DisplayName => "Drilling and Fertilizer";

        public override bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
            => NoResearchLimits(vessel, currentTier, out reasonWhyNot);

        public override string BreakthroughMessage(TechTier newTier)
            => CrewBlurbs.ProductionBreakthrough(newTier, c => c.trait == "Engineer" || c.trait == "Mechanic");

        public override string BoringBreakthroughMessage(TechTier newTier)
            => CrewBlurbs.BoringProductionBreakthrough(newTier);
    }

    public class ScanningResearchCategory
        : ResearchCategory
    {
        public ScanningResearchCategory()
            : base(.8, 1.6, 3.0, 3.0)
        {
        }

        public override string DisplayName => "Scanning";

        public override bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
            => NoResearchLimits(vessel, currentTier, out reasonWhyNot);

        public override string BreakthroughMessage(TechTier newTier)
            => CrewBlurbs.ScanningBreakthrough(newTier, c => c.trait == "Pilot" || c.trait == "Geologist");

        public override string BoringBreakthroughMessage(TechTier newTier)
            => CrewBlurbs.BoringScanningBreakthrough(newTier);
    }

    public class ShiniesResearchCategory
        : ResearchCategory
    {
        public ShiniesResearchCategory()
            : base(.8, 1.6, 3.0, 3.0)
        {
        }

        public override string DisplayName => "Blingology";

        public override bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
            => MaxLimitedOnEasyWorlds(vessel, currentTier, out reasonWhyNot);

        public override string BreakthroughMessage(TechTier newTier)
            => CrewBlurbs.ShiniesBreakthrough(newTier, c => c.trait == "Engineer" || c.trait == "Technician");

        public override string BoringBreakthroughMessage(TechTier newTier)
            => CrewBlurbs.BoringShiniesBreakthrough(newTier);
    }

    public class RocketPartsResearchCategory
        : ResearchCategory
    {
        public RocketPartsResearchCategory()
            : base(.8, 1.6, 3.0, 3.0)
        {
        }

        public override string DisplayName => "Off-World Construction";

        public override bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
            => NoResearchLimits(vessel, currentTier, out reasonWhyNot);

        public override string BreakthroughMessage(TechTier newTier)
            => CrewBlurbs.ConstructionBreakthrough(newTier, c => c.trait == "Engineer" || c.trait == "Mechanic");

        public override string BoringBreakthroughMessage(TechTier newTier)
            => CrewBlurbs.BoringConstructionBreakthrough(newTier);
    }
}
