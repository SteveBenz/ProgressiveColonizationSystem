using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nerm.Colonization.UnitTests
{
    public class StubColonizationResearchScenario
        : IColonizationResearchScenario
    {
        public StubColonizationResearchScenario(TechTier currentTier)
        {
            this.AgroponicsMaxTier = currentTier;
        }

        public double ResearchProgress { get; set; }

        public TechTier AgroponicsMaxTier { get; private set; }

        public void ContributeAgroponicResearch(double timespent)
        {
            ResearchProgress += timespent;
            if (this.AgroponicsMaxTier.KerbalSecondsToResearchNextAgroponicsTier() < ResearchProgress)
            {
                ++AgroponicsMaxTier;
                ResearchProgress = 0;
            }
        }
    }
}
