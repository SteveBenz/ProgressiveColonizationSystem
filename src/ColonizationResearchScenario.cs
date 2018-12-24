using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
    public class ColonizationResearchScenario
        : ScenarioModule
    {
        public static ColonizationResearchScenario Instance;

        [KSPField(isPersistant = true)]
        private double accumulatedAgroponicResearchProgressToNextTier;

        // Configurable?
        private static readonly double[] KerbalYearsRequiredToGetToNextTier = new double[] { .1, 1, 10, 50, double.MaxValue };

        public ColonizationResearchScenario()
        {
            Instance = this;
        }

        [KSPField(isPersistant = true)]
        public TechTier AgroponicsMaxTier { get; private set; }

        public double AgroponicsResearchProgress
            => this.accumulatedAgroponicResearchProgressToNextTier / KerbalYearsRequiredToGetToNextTier[(int)AgroponicsMaxTier];

        public void ContributeAgroponicResearch(double timespent)
        {
            accumulatedAgroponicResearchProgressToNextTier += timespent;
            if (AgroponicsResearchProgress > 1)
            {
                accumulatedAgroponicResearchProgressToNextTier = 0;
                ++this.AgroponicsMaxTier;
            }
        }

        // TODO: Need a method to ask if a vessel, given its current SoI and state (landed, not landed)
        //   can contribute agroponic research.
    }
}
