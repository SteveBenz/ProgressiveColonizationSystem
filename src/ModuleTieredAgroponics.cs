using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class ModuleTieredAgroponics
        : TieredResourceCoverter
    {
        protected override TechTier MaxTechTierResearched =>
            ColonizationResearchScenario.Instance?.AgroponicsMaxTier ?? TechTier.Tier0;

        public override bool ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            if (this.IsResearchEnabled)
            {
                target.ContributeAgroponicResearch(amount);
                return target.AgroponicsMaxTier != this.Tier;
            }
            else
            {
                return false;
            }
        }

        protected override bool CanDoProduction(ModuleResourceConverter resourceConverter, out string reasonWhyNotMessage)
        {
            if (!base.CanDoProduction(resourceConverter, out reasonWhyNotMessage))
            {
                return false;
            }
            else if (this.vessel.situation != Vessel.Situations.ORBITING)
            {
                reasonWhyNotMessage = "Not in a stable orbit";
                return false;
            }
            else
            {
                reasonWhyNotMessage = null;
                return true;
            }
        }

        protected override bool CanDoResearch(out string reasonWhyNotMessage)
        {
            if (!base.CanDoResearch(out reasonWhyNotMessage))
            {
                return false;
            }
            else if (ColonizationResearchScenario.Instance.AgroponicsMaxTier >= TechTier.Tier2 && this.IsNearKerbin())
            {
                reasonWhyNotMessage = "Disabled - Too near to Kerbin's orbit";
                return false;
            }
            else
            {
                return true;
            }
        }

		protected bool IsNearKerbin()
		{
			// There are more stylish ways to do this.  It's also a bit problematic for the player
			// because if they ignore a craft on its way back from some faroff world until it
			// reaches kerbin's SOI, then they'll lose all that tasty research.
			//
			// A fix would be to look at the vessel's orbit as well, and, if it just carries the
			// vessel out of the SOI, count that.
			double homeworldDistanceFromSun = FlightGlobals.GetHomeBody().orbit.altitude;
			return this.vessel.distanceToSun > homeworldDistanceFromSun * .9
				&& this.vessel.distanceToSun < homeworldDistanceFromSun * 1.1;
		}
    }
}
