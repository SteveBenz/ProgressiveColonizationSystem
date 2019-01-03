using System;
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

		public double ProductionResearchProgress { get; set; }

		public double ScanningResearchProgress { get; set; }

		public TechTier AgroponicsMaxTier { get; private set; }

        public bool ContributeAgroponicResearch(double timespent)
        {
            AgroponicResearchProgress += timespent;
            if (this.AgroponicsMaxTier.KerbalSecondsToResearchNextAgroponicsTier() < AgroponicResearchProgress)
            {
                ++AgroponicsMaxTier;
                AgroponicResearchProgress = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        public TechTier GetAgricultureMaxTier(string bodyName) => this.AgroponicsMaxTier;

        public TechTier GetProductionMaxTier(string bodyName) => this.AgroponicsMaxTier;

        public bool ContributeAgricultureResearch(string bodyName, double timespent)
        {
            Assert.AreEqual("test", bodyName);
            AgricultureResearchProgress += timespent;
            return false;
        }

		public bool ContributeProductionResearch(string bodyName, double timespent)
		{
			Assert.AreEqual("test", bodyName);
			ProductionResearchProgress += timespent;
            return false;
        }

        public bool ContributeScanningResearch(string bodyName, double timespent)
		{
			Assert.AreEqual("test", bodyName);
			ScanningResearchProgress += timespent;
            return false;
		}

        internal void Reset()
        {
            AgricultureResearchProgress = 0;
            AgroponicResearchProgress = 0;
            ProductionResearchProgress = 0;
        }
    }
}
