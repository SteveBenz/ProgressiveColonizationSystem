using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
    public class ColonizationResearchScenario
        : ScenarioModule, IColonizationResearchScenario
    {
        public static ColonizationResearchScenario Instance;

        [KSPField(isPersistant = true)]
        private float accumulatedAgroponicResearchProgressToNextTier = 0f;
        [KSPField(isPersistant = true)]
        private int agroponicsMaxTier = 0;

        public ColonizationResearchScenario()
        {
            Instance = this;
        }

        public TechTier AgroponicsMaxTier
        {
            get => (TechTier)this.agroponicsMaxTier;
            private set => this.agroponicsMaxTier = (int)value;
        }

        public double AgroponicsResearchProgress
            => this.accumulatedAgroponicResearchProgressToNextTier / AgroponicsMaxTier.KerbalSecondsToResearchNextAgroponicsTier();

        public void ContributeAgroponicResearch(double timespent)
        {
            this.accumulatedAgroponicResearchProgressToNextTier += (float)timespent;
            if (this.accumulatedAgroponicResearchProgressToNextTier > AgroponicsMaxTier.KerbalSecondsToResearchNextAgroponicsTier())
            {
                this.accumulatedAgroponicResearchProgressToNextTier = 0;
                ++this.AgroponicsMaxTier;
            }
        }

        // TODO: Need a method to ask if a vessel, given its current SoI and state (landed, not landed)
        //   can contribute agroponic research.
    }

    // Test interface
    public interface IColonizationResearchScenario
    {
        void ContributeAgroponicResearch(double timespent);
        TechTier AgroponicsMaxTier { get; }
    }
}
