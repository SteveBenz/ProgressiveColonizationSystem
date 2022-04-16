using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProgressiveColonizationSystem.ProductionChain;

namespace ProgressiveColonizationSystem.UnitTests
{
    [TestClass]
    public class TieredProductionTests
    {
        public double TestTolerance => KerbalTime.KerbalDaysToSeconds(.01);

        // These are mirrored in StubColonizatinoResource - reproduced here to make it easier to read
        private const double Tier0AgroponicMaxDietRatio = .2;
        private const double Tier2AgroponicMaxDietRatio = .55;
        private const double Tier0AgricultureMaxDietRatio = .6;

        /// <summary>
        ///   Validates the basic case where we have some supplies on board from Kerban.
        /// </summary>
        [TestMethod]
        public void TieredProduction_AllSuppliesFromKerban()
        {
            var colonizationResearchScenario = new StubColonizationResearchScenario(TechTier.Tier0);
            Dictionary<string, double> available = new Dictionary<string, double>();
            available.Add("Snacks-Tier4", 1.0); // One days' worth
            Dictionary<string, double> noStorage = new Dictionary<string, double>();
            TieredProduction.CalculateResourceUtilization(
                5 /* kerbals */, 1.0 /* seconds*/, new List<ITieredProducer>(),
                new List<ITieredCombiner>(), colonizationResearchScenario, available, noStorage,
                out double timePassedInSeconds, out List<TieredResource> breakthroughs,
                out Dictionary<string, double> consumptionPerSecond,
                out Dictionary<string, double> productionPerSecond,
                out IEnumerable<string> limitingResources,
                out Dictionary<string, double> unusedProduction);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(false, breakthroughs.Any());
            Assert.IsNotNull(consumptionPerSecond);
            Assert.AreEqual(1, consumptionPerSecond.Count);
            Assert.AreEqual("Snacks-Tier4", consumptionPerSecond.First().Key);
            Assert.AreEqual(KerbalTime.UnitsPerDayToUnitsPerSecond(5), consumptionPerSecond.First().Value);
            Assert.AreEqual(0, productionPerSecond.Count);
            Assert.IsFalse(limitingResources.Any());
            Assert.IsFalse(unusedProduction.Any());

            // There's a days' worth of snacks, but 5 kerbals getting after it.
            TieredProduction.CalculateResourceUtilization(
                5 /* kerbals */, KerbalTime.KerbalDaysToSeconds(1.0), new List<ITieredProducer>(), new List<ITieredCombiner>(),
                colonizationResearchScenario, available, noStorage, out timePassedInSeconds, out breakthroughs,
                out consumptionPerSecond, out productionPerSecond, out limitingResources, out unusedProduction);
            Assert.AreEqual(timePassedInSeconds, KerbalTime.KerbalDaysToSeconds(1.0/5.0));
            Assert.AreEqual(false, breakthroughs.Any());
            Assert.IsNotNull(consumptionPerSecond);
            Assert.AreEqual(1, consumptionPerSecond.Count);
            Assert.AreEqual("Snacks-Tier4", consumptionPerSecond.First().Key);
            Assert.AreEqual(KerbalTime.UnitsPerDayToUnitsPerSecond(5), consumptionPerSecond.First().Value);
            Assert.IsFalse(limitingResources.Any());
            Assert.IsFalse(unusedProduction.Any());

            // Test no snacks at all
            available.Clear();
            TieredProduction.CalculateResourceUtilization(
                5 /* kerbals */, KerbalTime.KerbalDaysToSeconds(1.0), new List<ITieredProducer>(), new List<ITieredCombiner>(),
                colonizationResearchScenario, available, noStorage, out timePassedInSeconds, out breakthroughs,
                out consumptionPerSecond, out productionPerSecond,
                out limitingResources, out unusedProduction);
            Assert.AreEqual(timePassedInSeconds, 0.0);
            Assert.IsNull(breakthroughs);
            Assert.IsNull(consumptionPerSecond);
            Assert.IsNull(productionPerSecond);
            Assert.IsFalse(limitingResources.Any());
            Assert.IsNull(unusedProduction);
        }

        /// <summary>
        ///   Validates initial case with a few agroponics modules
        /// </summary>
        [TestMethod]
        public void TieredProduction_SimpleAgroponics()
        {
            // We have 3 modules on our vessel, but only crew enough to staff
            // two of them (for a net production of 3) and only one of them
            // has crew enough to do research.
            var agroponicModules = new List<ITieredProducer>()
            {
                new StubHydroponic
                {
                    Tier = TechTier.Tier0,
                    ProductionRate = 1,
                    IsProductionEnabled = true,
                    IsResearchEnabled = true
                },
                new StubHydroponic
                {
                    Tier = TechTier.Tier0,
                    ProductionRate = 2,
                    IsProductionEnabled = true,
                    IsResearchEnabled = false
                },
                new StubHydroponic
                {
                    Tier = TechTier.Tier0,
                    ProductionRate = 2,
                    IsProductionEnabled = false,
                    IsResearchEnabled = false
                },
            };
            var colonizationResearchScenario = new StubColonizationResearchScenario(TechTier.Tier0);
            Dictionary<string, double> available = new Dictionary<string, double>();
            available.Add("Snacks-Tier4", 1.0); // One days' worth of food
            available.Add("Fertilizer-Tier4", 1.0); // And one days' worth of running the agroponics
            Dictionary<string, double> noStorage = new Dictionary<string, double>();

            // First we test when we have more than enough production
            TieredProduction.CalculateResourceUtilization(
                5 /* kerbals */, 1.0 /* seconds*/, agroponicModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, available, noStorage,
                out double timePassedInSeconds, out List<TieredResource> breakthroughs,
                out Dictionary<string, double> consumptionPerSecond,
                out Dictionary<string, double> productionPerSecond,
                out IEnumerable<string> limitingResources,
                out Dictionary<string, double> unusedProduction);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(false, breakthroughs.Any());
            Assert.IsNotNull(consumptionPerSecond);
            Assert.AreEqual(2, consumptionPerSecond.Count);
            // With 5 kerbals aboard, our 3 working agroponics farms are more than enough because
            // they can produce 3 snacks per day, but our crew will only eat .2*5=1 of them.
            // So they're running at 1/3 capacity.
            Assert.AreEqual(5 * (1 - Tier0AgroponicMaxDietRatio), KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier4"]));
            Assert.AreEqual(5 * Tier0AgroponicMaxDietRatio, KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Fertilizer-Tier4"]));
            Assert.AreEqual(5 * Tier0AgroponicMaxDietRatio, colonizationResearchScenario.AgroponicResearchProgress);
            Assert.AreEqual(0, productionPerSecond.Count);
            Assert.IsFalse(limitingResources.Any());
            Assert.AreEqual(2.0, unusedProduction["HydroponicSnacks-Tier0"], TestTolerance);

            // now we overwhelm the system with 20 kerbals - they'll be willing to eat (in total)
            // 4 snacks per day, (20 * .2) but our systems can only produce 3 and can only garner
            // research from 1 of the modules.
            TieredProduction.CalculateResourceUtilization(
                20 /* kerbals */, 1.0 /* seconds*/, agroponicModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, available, noStorage,
                out timePassedInSeconds, out breakthroughs, out consumptionPerSecond,
                out productionPerSecond, out limitingResources, out unusedProduction);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(false, breakthroughs.Any());
            Assert.IsNotNull(consumptionPerSecond);
            Assert.AreEqual(2, consumptionPerSecond.Count);
            // With 20 kerbals aboard, our 3 working agroponics farms are more than enough because
            // they can produce 3 snacks per day, but our crew will only eat .2*5=1 of them.
            // So they're running at 1/3 capacity.
            Assert.AreEqual(20.0 - 3.0 /* working facilities */, KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier4"]));
            Assert.AreEqual(3.0, KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Fertilizer-Tier4"]));
            Assert.AreEqual(1.0 /* previous test */ + 1.0 /* current test */, colonizationResearchScenario.AgroponicResearchProgress);
            Assert.IsFalse(limitingResources.Any());
            Assert.IsFalse(unusedProduction.Any());

            // Now let's take that last test and give it a twist that they run out of fertilizer halfway
            // through the second of time
            available["Fertilizer-Tier4"] = KerbalTime.UnitsPerDayToUnitsPerSecond(1.5);
            TieredProduction.CalculateResourceUtilization(
                20 /* kerbals */, 1.0 /* seconds*/, agroponicModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, available, noStorage,
                out timePassedInSeconds, out breakthroughs, out consumptionPerSecond,
                out productionPerSecond, out limitingResources, out unusedProduction);
            Assert.AreEqual(timePassedInSeconds, 0.5);
            Assert.AreEqual(false, breakthroughs.Any());
            Assert.IsNotNull(consumptionPerSecond);
            Assert.AreEqual(2, consumptionPerSecond.Count);
            // Same rates as in previous test
            Assert.AreEqual(20.0 - 3.0 /* working facilities */, KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier4"]));
            Assert.AreEqual(3.0, KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Fertilizer-Tier4"]));

            // And half of the time goes to research
            Assert.AreEqual(2.0 /* previous two tests */ + 0.5 /* current test */, colonizationResearchScenario.AgroponicResearchProgress);
            Assert.IsFalse(limitingResources.Any());
            Assert.IsFalse(unusedProduction.Any());

            available.Remove("Fertilizer-Tier4");
            TieredProduction.CalculateResourceUtilization(
                20 /* kerbals */, 1.0 /* seconds*/, agroponicModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, available, noStorage,
                out timePassedInSeconds, out breakthroughs, out consumptionPerSecond,
                out productionPerSecond, out limitingResources, out unusedProduction);
            Assert.AreEqual("Fertilizer-Tier0", limitingResources.Single());
            Assert.AreEqual(3.0, unusedProduction["HydroponicSnacks-Tier0"], TestTolerance);
        }

        /// <summary>
        ///   Validates that initial agriculture production works with delivered fertilizer.
        /// </summary>
        [TestMethod]
        public void TieredProduction_Tier0Agriculture()
        {
            // Just landed - for grins this validates that it can pull some fertilizer down from storage since
            // our production won't be enough.
            var landedModules = new List<ITieredProducer>()
            {
                // 60% of food capacity  1 module oversupplies our crew of 4 and gives us excess to much on the wayhome
                new StubFarm
                {
                    Tier = TechTier.Tier0, // An agroponics lab that can work with the junky fertilizer we get from Duna
                    ProductionRate = 10,
                    IsProductionEnabled = true,
                    IsResearchEnabled = true,
                },
                new StubFarm
                {
                    Tier = TechTier.Tier0, // An agroponics lab that can work with the junky fertilizer we get from Duna
                    ProductionRate = 10,
                    IsProductionEnabled = true,
                    IsResearchEnabled = true,
                },
                new StubFertilizerProducer()
                {
                    Tier = TechTier.Tier0,
                    ProductionRate = 15,
                    IsProductionEnabled = true,
                    IsResearchEnabled = true,
                }
            };
            var colonizationResearchScenario = new StubColonizationResearchScenario(TechTier.Tier0);

            Dictionary<string, double> inStorage = new Dictionary<string, double>();
            inStorage["Fertilizer-Tier4"] = 1.0;
            inStorage["Snacks-Tier4"] = 1.0;
            Dictionary<string, double> storageSpace = new Dictionary<string, double>();
            storageSpace.Add("Snacks-Tier0", double.MaxValue);
            storageSpace.Add("Fertilizer-Tier0", double.MaxValue);

            TieredProduction.CalculateResourceUtilization(
                4 /* kerbals */, 1.0 /* seconds*/, landedModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, inStorage, storageSpace,
                out double timePassedInSeconds, out List<TieredResource> breakthroughs,
                out Dictionary<string, double> consumptionPerSecond,
                out Dictionary<string, double> productionPerSecond,
                out IEnumerable<string> limitingResources,
                out Dictionary<string,double> unusedProduction);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(4 * (1 - Tier0AgricultureMaxDietRatio), KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier4"]), TestTolerance);
            Assert.AreEqual(5.0 /* 2*10-15 */, KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Fertilizer-Tier4"]), TestTolerance);
            Assert.AreEqual(20.0, colonizationResearchScenario.AgricultureResearchProgress, TestTolerance);
            Assert.AreEqual(15.0, colonizationResearchScenario.ProductionResearchProgress, TestTolerance);
            Assert.AreEqual(20.0 - 4 * Tier0AgricultureMaxDietRatio, KerbalTime.UnitsPerSecondToUnitsPerDay(productionPerSecond["Snacks-Tier0"]), TestTolerance);
            Assert.AreEqual(1, productionPerSecond.Count); // Only producing snacks
            Assert.IsFalse(limitingResources.Any());
            Assert.IsFalse(unusedProduction.Any());

            // Okay, now let's say we run out of that sweet sweet Tier4 fertilizer - it should max out the snack production
            colonizationResearchScenario.Reset();
            inStorage.Remove("Fertilizer-Tier4");

            TieredProduction.CalculateResourceUtilization(
                4 /* kerbals */, 1.0 /* seconds*/, landedModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, inStorage, storageSpace,
                out timePassedInSeconds, out breakthroughs, out consumptionPerSecond, out productionPerSecond,
                out limitingResources, out unusedProduction);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(4 * (1 - Tier0AgricultureMaxDietRatio), KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier4"]), TestTolerance);
            Assert.AreEqual(15.0, colonizationResearchScenario.AgricultureResearchProgress, TestTolerance);
            Assert.AreEqual(15.0, colonizationResearchScenario.ProductionResearchProgress, TestTolerance);
            Assert.AreEqual(15.0 - 4 * Tier0AgricultureMaxDietRatio, KerbalTime.UnitsPerSecondToUnitsPerDay(productionPerSecond["Snacks-Tier0"]), TestTolerance);
            Assert.AreEqual(1, productionPerSecond.Count); // Only producing snacks
            Assert.AreEqual("Fertilizer-Tier0", limitingResources.Single());
            Assert.AreEqual(5.0, unusedProduction["Snacks-Tier0"]);

            // Let's say we fill up the snacks storage midway through:
            colonizationResearchScenario.Reset();
            inStorage.Remove("Fertilizer-Tier4");
            const double expectedTimePassed = 0.25;
            storageSpace["Snacks-Tier0"] = KerbalTime.UnitsPerDayToUnitsPerSecond((15.0 - 4 * Tier0AgricultureMaxDietRatio) * expectedTimePassed);

            TieredProduction.CalculateResourceUtilization(
                4 /* kerbals */, 1.0 /* seconds*/, landedModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, inStorage, storageSpace,
                out timePassedInSeconds, out breakthroughs, out consumptionPerSecond, out productionPerSecond,
                out limitingResources, out unusedProduction);
            Assert.AreEqual(expectedTimePassed, timePassedInSeconds);
            Assert.AreEqual(4 * (1 - Tier0AgricultureMaxDietRatio), KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier4"]), TestTolerance);
            Assert.AreEqual(15.0, colonizationResearchScenario.AgricultureResearchProgress / expectedTimePassed, TestTolerance);
            Assert.AreEqual(15.0, colonizationResearchScenario.ProductionResearchProgress / expectedTimePassed, TestTolerance);
            Assert.AreEqual(15.0 - 4 * Tier0AgricultureMaxDietRatio, KerbalTime.UnitsPerSecondToUnitsPerDay(productionPerSecond["Snacks-Tier0"]), TestTolerance);
            Assert.AreEqual(1, productionPerSecond.Count); // Only producing snacks
            Assert.AreEqual("Fertilizer-Tier0", limitingResources.Single());
            Assert.AreEqual(5.0, unusedProduction["Snacks-Tier0"]);

            // Snacks storage is filled - limiting production and hence research progress
            colonizationResearchScenario.Reset();
            storageSpace.Remove("Snacks-Tier0");
            storageSpace.Remove("Fertilizer-Tier0");
            TieredProduction.CalculateResourceUtilization(
                4 /* kerbals */, 1.0 /* seconds*/, landedModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, inStorage, storageSpace,
                out timePassedInSeconds, out breakthroughs, out consumptionPerSecond, out productionPerSecond,
                out limitingResources, out unusedProduction);
            Assert.AreEqual(1.0, timePassedInSeconds);
            Assert.AreEqual(4 * (1 - Tier0AgricultureMaxDietRatio), KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier4"]), TestTolerance);
            Assert.AreEqual(4 * Tier0AgricultureMaxDietRatio, colonizationResearchScenario.AgricultureResearchProgress, TestTolerance);
            Assert.AreEqual(4 * Tier0AgricultureMaxDietRatio, colonizationResearchScenario.ProductionResearchProgress, TestTolerance);
            Assert.AreEqual(0, productionPerSecond.Count);
            Assert.IsFalse(limitingResources.Any());
            Assert.AreEqual(17.6, unusedProduction["Snacks-Tier0"], TestTolerance);
            Assert.AreEqual(12.6, unusedProduction["Fertilizer-Tier0"], TestTolerance);
        }

        /// <summary>
        ///   Validates that initial agriculture production works with delivered fertilizer.
        /// </summary>
        [TestMethod]
        public void TieredProduction_MixedTiers()
        {
            // Just landed - for grins this validates that it can pull some fertilizer down from storage since
            // our production won't be enough.
            var landedModules = new List<ITieredProducer>()
            {
                new StubFarm
                {
                    Tier = TechTier.Tier0,
                    ProductionRate = 1,
                    IsProductionEnabled = true,
                    IsResearchEnabled = true,
                },
                new StubFarm
                {
                    Tier = TechTier.Tier2,
                    ProductionRate = 1,
                    IsProductionEnabled = true,
                    IsResearchEnabled = true,
                },
                new StubFertilizerProducer()
                {
                    Tier = TechTier.Tier0,
                    ProductionRate = 1,
                    IsProductionEnabled = true,
                    IsResearchEnabled = true,
                },
                new StubFertilizerProducer()
                {
                    Tier = TechTier.Tier2,
                    ProductionRate = 1,
                    IsProductionEnabled = true,
                    IsResearchEnabled = true,
                }
            };
            var colonizationResearchScenario = new StubColonizationResearchScenario(TechTier.Tier2);

            Dictionary<string, double> inStorage = new Dictionary<string, double>();
            inStorage["Snacks-Tier4"] = 1.0;
            Dictionary<string, double> storageSpace = new Dictionary<string, double>();
            storageSpace.Add("Fertilizer-Tier0", double.MaxValue);
            storageSpace.Add("Fertilizer-Tier2", double.MaxValue);
            storageSpace.Add("Snacks-Tier0", double.MaxValue);
            storageSpace.Add("Snacks-Tier2", double.MaxValue);

            TieredProduction.CalculateResourceUtilization(
                1 /* kerbals */, 1.0 /* seconds*/, landedModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, inStorage, storageSpace,
                out double timePassedInSeconds, out List<TieredResource> breakthroughs,
                out Dictionary<string, double> consumptionPerSecond,
                out Dictionary<string, double> productionPerSecond,
                out IEnumerable<string> limitingResources,
                out Dictionary<string,double> unusedProduction);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(2, productionPerSecond.Keys.Count);
            // Tier2 is 95% edible, so remainder is .05
            Assert.AreEqual(0.05, KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier4"]), TestTolerance);
            // Tier0 is 60% edible, and is eaten first, so we eat .6 and save .4 of Tier0
            Assert.AreEqual(0.4, KerbalTime.UnitsPerSecondToUnitsPerDay(productionPerSecond["Snacks-Tier0"]), TestTolerance);
            // .95-.6= .35 eaten, so .65 left over
            Assert.AreEqual(0.65, KerbalTime.UnitsPerSecondToUnitsPerDay(productionPerSecond["Snacks-Tier2"]), TestTolerance);
            Assert.IsFalse(limitingResources.Any());
            Assert.IsFalse(unusedProduction.Any());
        }

        /// <summary>
        ///   Validates a Duna-return mission with some local agriculture on-board
        /// </summary>
        [TestMethod]
        public void TieredProduction_FirstBaseSimulation()
        {
            var enRouteModules = new List<ITieredProducer>()
            {
                // 20% of food capacity  1 module oversupplies our crew of 4
                new StubHydroponic
                {
                    Tier = TechTier.Tier0, // An agroponics lab that can work with the junky fertilizer we get from Duna
                    ProductionRate = 1,
                    IsProductionEnabled = true,
                    IsResearchEnabled = false
                },
                // 55%-20% = 35% of production capacity comes from this - we need two to be over
                new StubHydroponic
                {
                    Tier = TechTier.Tier2,
                    ProductionRate = 2,
                    IsProductionEnabled = true,
                    IsResearchEnabled = true
                },
            };
            var colonizationResearchScenario = new StubColonizationResearchScenario(TechTier.Tier2);
            Dictionary<string, double> available = new Dictionary<string, double>();
            Dictionary<string, double> noStorage = new Dictionary<string, double>();

            // Kerbal->Duna scenario - plenty of maxtier stuff
            available["Fertilizer-Tier4"] = 1.0;
            available["Snacks-Tier4"] = 1.0;
            TieredProduction.CalculateResourceUtilization(
                4 /* kerbals */, 1.0 /* seconds*/, enRouteModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, available, noStorage,
                out double timePassedInSeconds, out List<TieredResource> breakthroughs,
                out Dictionary<string, double> consumptionPerSecond,
                out Dictionary<string, double> productionPerSecond,
                out IEnumerable<string> limitingResources,
                out Dictionary<string, double> unusedProduction);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(false, breakthroughs.Any());
            Assert.AreEqual(4 * (1 - Tier2AgroponicMaxDietRatio), KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier4"]), TestTolerance);
            Assert.AreEqual(4 * Tier2AgroponicMaxDietRatio, KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Fertilizer-Tier4"]), TestTolerance);
            Assert.AreEqual(4 * (Tier2AgroponicMaxDietRatio - Tier0AgroponicMaxDietRatio), colonizationResearchScenario.AgroponicResearchProgress, TestTolerance);
            Assert.IsFalse(limitingResources.Any());
            Assert.AreEqual(0.2, unusedProduction["HydroponicSnacks-Tier0"], TestTolerance);
            Assert.AreEqual(0.6, unusedProduction["HydroponicSnacks-Tier2"], TestTolerance);


            // Duna return scenario - still got the max-tier stuff, but added some local stuff
            available["Fertilizer-Tier0"] = 1.0;
            available["Snacks-Tier0"] = 1.0;
            // Just about ready to tick over the research counter
            colonizationResearchScenario.AgroponicResearchProgress =
                KerbalTime.KerbalYearsToSeconds(StubColonizationResearchScenario.hydroponicResearchCategory.KerbalYearsToNextTier(TechTier.Tier2)) - 0.00001;
            TieredProduction.CalculateResourceUtilization(
                4 /* kerbals */, 1.0 /* seconds*/, enRouteModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, available, noStorage,
                out timePassedInSeconds, out breakthroughs, out consumptionPerSecond,
                out productionPerSecond, out limitingResources, out unusedProduction);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(1, breakthroughs.Count);
            // We're burning T0 fertilizer in the T0 agro lab
            Assert.AreEqual(4 * Tier0AgroponicMaxDietRatio,
                            KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Fertilizer-Tier0"]), TestTolerance);
            // And burning reduced maxtier ferilizer
            Assert.AreEqual(4 * (Tier2AgroponicMaxDietRatio - Tier0AgroponicMaxDietRatio),
                            KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Fertilizer-Tier4"]), TestTolerance);
            // And snacking a few local things
            Assert.AreEqual(4 * (Tier0AgricultureMaxDietRatio - Tier2AgroponicMaxDietRatio),
                            KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier0"]), TestTolerance);
            // And making up the rest from the snack stores.
            Assert.AreEqual(4 * (1 - Tier0AgricultureMaxDietRatio),
                            KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier4"]), TestTolerance);
            Assert.AreEqual(0.2, unusedProduction["HydroponicSnacks-Tier0"], TestTolerance);
            Assert.AreEqual(0.6, unusedProduction["HydroponicSnacks-Tier2"], TestTolerance);

            Assert.AreEqual(0.0, colonizationResearchScenario.AgroponicResearchProgress); // Progress is reset
            Assert.AreEqual(TechTier.Tier3, colonizationResearchScenario.AgroponicsMaxTier); // Next tier is up

            // And we run out of our KSP-provided fertilizer
            available.Remove("Fertilizer-Tier4");
            TieredProduction.CalculateResourceUtilization(
                4 /* kerbals */, 1.0 /* seconds*/, enRouteModules, new List<ITieredCombiner>(),
                colonizationResearchScenario, available, noStorage,
                out timePassedInSeconds, out breakthroughs, out consumptionPerSecond,
                out productionPerSecond, out limitingResources, out unusedProduction);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(false, breakthroughs.Any());
            // We're burning T0 fertilizer in the T0 agro lab
            Assert.AreEqual(4 * Tier0AgroponicMaxDietRatio,
                            KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Fertilizer-Tier0"]), TestTolerance);
            // And snacking on more local things now that the T2-agro-lab is offline
            Assert.AreEqual(4 * (Tier0AgricultureMaxDietRatio - Tier0AgroponicMaxDietRatio),
                            KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier0"]), TestTolerance);
            // And making up the rest from the snack stores.
            Assert.AreEqual(4 * (1 - Tier0AgricultureMaxDietRatio),
                            KerbalTime.UnitsPerSecondToUnitsPerDay(consumptionPerSecond["Snacks-Tier4"]), TestTolerance);

            Assert.AreEqual(0.0, colonizationResearchScenario.AgroponicResearchProgress); // no gear for next tier (and no T2 progress anyway)

            Assert.AreEqual("Fertilizer-Tier2", limitingResources.Single());
            Assert.AreEqual(2.0, unusedProduction["HydroponicSnacks-Tier2"], TestTolerance);
            Assert.AreEqual(0.2, unusedProduction["HydroponicSnacks-Tier0"], TestTolerance);
        }
    }
}