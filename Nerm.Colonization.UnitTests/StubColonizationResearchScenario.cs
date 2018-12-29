using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nerm.Colonization.UnitTests
{
    public class StubColonizationResearchScenario
        : IColonizationResearchScenario
    {
        public StubColonizationResearchScenario(TechTier currentTier)
        {
            this.AgroponicsMaxTier = currentTier;
        }

        public double AgroponicResearchProgress { get; set; }

        public double AgricultureResearchProgress { get; set; }

        public TechTier AgroponicsMaxTier { get; private set; }

        public void ContributeAgroponicResearch(double timespent)
        {
            AgroponicResearchProgress += timespent;
            if (this.AgroponicsMaxTier.KerbalSecondsToResearchNextAgroponicsTier() < AgroponicResearchProgress)
            {
                ++AgroponicsMaxTier;
                AgroponicResearchProgress = 0;
            }
        }

        public TechTier GetAgricultureMaxTier(string bodyName) => this.AgroponicsMaxTier;
        public void ContributeAgricultureResearch(string bodyName, double timespent)
        {
            Assert.AreEqual("test", bodyName);
            AgricultureResearchProgress += timespent;
        }
    }
}
