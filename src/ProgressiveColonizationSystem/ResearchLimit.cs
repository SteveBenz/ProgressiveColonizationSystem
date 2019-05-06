using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgressiveColonizationSystem
{
    public class ResearchLimit
    {
        private readonly bool notNearHome;
        private readonly bool notRemotelyCloseToHome;
        private readonly HashSet<string> bodies;
        private bool maxTierLimit;

        private static double awayFromHomeMinDistanceFromSun = -1;
        private static double awayFromHomeMaxDistanceFromSun = -1;
        private static double farAwayFromHomeMinDistanceFromSun = -1;
        private static double farAwayFromHomeMaxDistanceFromSun = -1;

        public static ResearchLimit MaxTierLimit = new ResearchLimit(null) { maxTierLimit = true };

        public ResearchLimit(string nodeValue)
        {
            this.notNearHome = false;
            this.notRemotelyCloseToHome = false;
            this.bodies = null;

            if (!string.IsNullOrEmpty(nodeValue))
            {
                switch(nodeValue.ToLowerInvariant())
                {
                    case "close_to_kerban":
                        this.notNearHome = true;
                        break;
                    case "outer_system":
                        this.notRemotelyCloseToHome = true;
                        break;
                    default:
                        this.bodies = new HashSet<string>(nodeValue.Split(',').Select(s => s.Trim()), StringComparer.OrdinalIgnoreCase);
                        break;
                }
            }
        }

        public bool IsResearchAllowed(Vessel vessel, out string reasonWhyNot)
        {
            if (this.maxTierLimit)
            {
                reasonWhyNot = $"Maximum tier reached";
                return false;
            }

            if (this.notNearHome && IsNearKerbin(vessel))
            {
                reasonWhyNot = $"Too near {FlightGlobals.GetHomeBody().name}";
                return false;
            }

            if (this.notRemotelyCloseToHome && IsAnywhereCloseToKerbin(vessel))
            {
                reasonWhyNot = $"Too near {FlightGlobals.GetHomeBody().name}";
                return false;
            }

            if (this.bodies != null)
            {
                if (this.bodies.Contains(vessel.mainBody.name))
                {
                    reasonWhyNot = "On an easy world";
                    return false;
                }
            }

            reasonWhyNot = null;
            return true;
        }

        private static bool IsNearKerbin(Vessel vessel)
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

        private static bool IsAnywhereCloseToKerbin(Vessel vessel)
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
    }
}
