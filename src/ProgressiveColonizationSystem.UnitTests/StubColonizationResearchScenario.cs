using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ProgressiveColonizationSystem.UnitTests
{
    public class StubColonizationResearchScenario
        : IColonizationResearchScenario
    {
        public StubColonizationResearchScenario(TechTier agroponicsTier)
        {
            this.AgroponicsMaxTier = agroponicsTier;
        }

        public double AgroponicResearchProgress { get; set; }

        public double AgricultureResearchProgress { get; set; }

        public double ProductionResearchProgress { get; set; }

        public TechTier AgroponicsMaxTier { get; private set; }

        public IEnumerable<TieredResource> AllResourcesTypes => AllTieredResources;

        internal void Reset()
        {
            AgricultureResearchProgress = 0;
            AgroponicResearchProgress = 0;
            ProductionResearchProgress = 0;
        }

        // The tests have their own copy of this table - the real one may get tweaked, and that could throw some of
        //  the tests off.
        public static ResearchCategory hydroponicResearchCategory = new ResearchCategory("hydroponics", ProductionRestriction.Space);
        public static ResearchCategory farmingResearchCategory = new ResearchCategory("agriculture", ProductionRestriction.LandedOnBody);
        public static ResearchCategory productionResearchCategory = new ResearchCategory("production", ProductionRestriction.LandedOnBody);
        public static ResearchCategory scanningResearchCategory = new ResearchCategory("scanning", ProductionRestriction.OrbitOfBody);
        public static ResearchCategory shiniesResearchCategory = new ResearchCategory("shinies", ProductionRestriction.LandedOnBody);
        public static ResearchCategory rocketPartsResearchCategory = new ResearchCategory("construction", ProductionRestriction.LandedOnBody);

        public static TieredResource Stuff;
        public static TieredResource HydroponicSnacks;
        public static TieredResource Snacks;
        public static TieredResource Fertilizer;
        public static TieredResource CrushIns;
        public static TieredResource Scanning;
        public static TieredResource Shinies;
        public static TieredResource LocalParts;

        static StubColonizationResearchScenario()
        {
            Stuff = new TieredResource("Stuff", null, productionResearchCategory, false, false, true, null, TechTier.Tier0);
            Fertilizer = new TieredResource("Fertilizer", "Kerbal-Days", productionResearchCategory, true, false, false, null, TechTier.Tier0);
            HydroponicSnacks = new TieredResource("HydroponicSnacks", "kerbal-days", hydroponicResearchCategory, false, false, false, Fertilizer, TechTier.Tier0, .2, .4, .55, .7, .95);
            Snacks = new TieredResource("Snacks", "kerbal-days", farmingResearchCategory, true, false, false, Fertilizer, TechTier.Tier0, .6, .85, .95, .98, 1.0);
            CrushIns = new TieredResource("CrushIns", null, productionResearchCategory, false, false, true, null, TechTier.Tier0);
            Scanning = new TieredResource("ScanningData", "Kerbal-Days", scanningResearchCategory, false, false, true, null, TechTier.Tier0);
            Shinies = new TieredResource("Shinies", "Bling-per-day", shiniesResearchCategory, true, false, false, null, TechTier.Tier0);
            LocalParts = new TieredResource("LocalParts", "Parts", rocketPartsResearchCategory, false, false, false, Stuff, TechTier.Tier0);
            AllTieredResources = new TieredResource[]
            {
                HydroponicSnacks,
                Snacks,
                Fertilizer,
                Shinies,
                Stuff,
                Scanning
            };
        }

        private static TieredResource[] AllTieredResources;

        public static TieredResource GetTieredResourceByName(string name)
        {
            return AllTieredResources.First(tr => tr.BaseName == name);
        }

        public bool TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier)
        {
            int dashIndex = tieredResourceName.IndexOf('-');
            if (dashIndex < 0)
            {
                resource = AllTieredResources.FirstOrDefault(tr => tr.BaseName == tieredResourceName);
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

        public bool ContributeResearch(TieredResource source, string atBody, double timespentInKerbalSeconds)
        {
            if (source.ResearchCategory == hydroponicResearchCategory)
            {
                this.AgroponicResearchProgress += timespentInKerbalSeconds;
                if (this.AgroponicResearchProgress > KerbalTime.KerbalYearsToSeconds(source.ResearchCategory.KerbalYearsToNextTier(this.AgroponicsMaxTier)))
                {
                    this.AgroponicResearchProgress = 0;
                    ++this.AgroponicsMaxTier;
                    return true;
                }
            }
            else if (source.ResearchCategory == farmingResearchCategory)
            {
                this.AgricultureResearchProgress += timespentInKerbalSeconds;
            }
            else if (source.ResearchCategory == productionResearchCategory)
            {
                this.ProductionResearchProgress += timespentInKerbalSeconds;
            }
            return false;
        }

        private Dictionary<string, TechTier> maxTiers = new Dictionary<string, TechTier>();

        public TechTier GetMaxUnlockedTier(TieredResource forResource, string atBody)
        {
            if (atBody == null)
            {
                return AgroponicsMaxTier;
            }
            else
            {
                return maxTiers.TryGetValue($"{atBody}-{forResource.ResearchCategory.DisplayName}", out TechTier tier) ? tier : TechTier.Tier0;
            }
        }

        public void SetMaxTier(ResearchCategory researchCategory, string atBody, TechTier tier)
        {
            maxTiers[$"{atBody}-{researchCategory.DisplayName}"] = tier;
        }

        public TechTier GetMaxUnlockedScanningTier(string atBody)
        {
            return GetMaxUnlockedTier(Scanning, atBody);
        }

        public TieredResource CrushInsResource => StubColonizationResearchScenario.CrushIns;
    }
}
