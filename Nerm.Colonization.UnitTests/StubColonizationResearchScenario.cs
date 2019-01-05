using System;
using System.Linq;
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

        // The tests have their own copy of this table - the real one may get tweaked, and that could throw some of
        //  the tests off.
        static TieredResource[] AllTieredResources =
        {
            new EdibleResource("HydroponicSnacks", false, false, .2, .4, .55, .7, .95),
            new EdibleResource("Snacks", true, false, .6, .85, .95, .98, 1.0),
            new TieredResource("Fertilizer", "Kerbal-Days", true, false),
            new TieredResource("Shinies", "Bling-per-day", true, false),
            new TieredResource("Stuff", null, true, false),
            new TieredResource("ScanningData", null, false, true)
        };

        public static TieredResource GetTieredResourceByName(string name)
        {
            return AllTieredResources.First(tr => tr.BaseName == name);
        }

        public bool TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier)
        {
            int dashIndex = tieredResourceName.IndexOf('-');
            if (dashIndex < 0)
            {
                resource = GetTieredResourceByName(tieredResourceName);
                tier = TechTier.Tier4;
                return resource != null;
            }
            else
            {
                try
                {
                    // Oh, but we do pine ever so much for .Net 4.6...
                    tier = (TechTier)Enum.Parse(typeof(TechTier), tieredResourceName.Substring(dashIndex + 1));
                    var tier4Name = tieredResourceName.Substring(0, dashIndex);
                    resource = GetTieredResourceByName(tier4Name);
                    return resource != null;
                }
                catch (Exception)
                {
                    resource = null;
                    tier = TechTier.Tier0;
                    return false;
                }
            }
        }
    }
}
