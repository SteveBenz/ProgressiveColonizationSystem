using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressiveColonizationSystem.UnitTests
{
    [TestClass]
    public class StaticAnalysisTests
    {
        private StubColonizationResearchScenario colonizationResearch;
        private readonly StubProducer drill1 = new StubProducer(StubColonizationResearchScenario.Stuff, null, 11, TechTier.Tier1);
        private readonly StubProducer fertFactory1 = new StubProducer(StubColonizationResearchScenario.Fertilizer, StubColonizationResearchScenario.Stuff, 6, TechTier.Tier1);
        private readonly StubProducer farm1 = new StubProducer(StubColonizationResearchScenario.Snacks, StubColonizationResearchScenario.Fertilizer, 3, TechTier.Tier1);
        private readonly StubProducer farm2 = new StubProducer(StubColonizationResearchScenario.Snacks, StubColonizationResearchScenario.Fertilizer, 3, TechTier.Tier1);
        private readonly StubProducer shinies1 = new StubProducer(StubColonizationResearchScenario.Shinies, StubColonizationResearchScenario.Stuff, 5, TechTier.Tier1);
        private readonly StubProducer hydro1 = new StubProducer(StubColonizationResearchScenario.HydroponicSnacks, StubColonizationResearchScenario.Fertilizer, 1, TechTier.Tier2);
        private readonly StubProducer hydro2 = new StubProducer(StubColonizationResearchScenario.HydroponicSnacks, StubColonizationResearchScenario.Fertilizer, 2, TechTier.Tier2);
        private readonly StubRocketPartCombiner combiner = new StubRocketPartCombiner();
        private readonly StubPartFactory partFactory = new StubPartFactory();
        private readonly Dictionary<string, double> emptyContainers = new Dictionary<string, double>();
        private readonly Dictionary<string, double> basicHydroponicSupplies = new Dictionary<string, double>()
            {
                { "Snacks-Tier4", 100.0 },
                { "Fertilizer-Tier4", 100.0 },
            };
        private Dictionary<string, double> snacksOnly = new Dictionary<string, double>()
            {
                { "Snacks-Tier4", 100.0 },
            };

        private List<ITieredProducer> producers;

        [TestInitialize]
        public void TestInitialize()
        {
            // We set up for complete happiness
            colonizationResearch = new StubColonizationResearchScenario(TechTier.Tier2);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.farmingResearchCategory, "munmuss", TechTier.Tier1);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.productionResearchCategory, "munmuss", TechTier.Tier1);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.scanningResearchCategory, "munmuss", TechTier.Tier1);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.shiniesResearchCategory, "munmuss", TechTier.Tier1);

            producers = new List<ITieredProducer>()
            {
                this.drill1,
                this.fertFactory1,
                this.farm1,
                this.farm2,
                this.shinies1,
            };
        }

        [TestMethod]
        public void WarningsTest_NoPartsTest()
        {
            var result = StaticAnalysis.CheckBodyIsSet(colonizationResearch, new List<ITieredProducer>(), this.emptyContainers, this.emptyContainers);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void WarningsTest_HappyParts()
        {
            var result = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Any());
        }


        [TestMethod]
        public void WarningsTest_MissingBodyAssignment()
        {
            this.farm1.Body = null;
            this.farm2.Body = null;
            var actual = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("Need to set up the target for the world-specific parts", actual[0].Message);
            Assert.IsNotNull(actual[0].FixIt);
            actual[0].FixIt();
            Assert.AreEqual("munmuss", this.farm1.Body);
            actual = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(0, actual.Count);

            // If nothing is set up
            foreach (var p in this.producers)
            {
                p.Body = null;
            }
            actual = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            // Then it gets complained about, but no fix is offered
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("Need to set up the target for the world-specific parts", actual[0].Message);
            Assert.IsNull(actual[0].FixIt);
        }

        [TestMethod]
        public void WarningsTest_MismatchedBodyAssignment()
        {
            this.farm1.Body = "splut";
            this.farm2.Body = null;
            var actual = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("Not all of the body-specific parts are set up for munmuss", actual[0].Message);
            Assert.IsNotNull(actual[0].FixIt);
            actual[0].FixIt();
            Assert.AreEqual("munmuss", this.farm1.Body);
            Assert.AreEqual("munmuss", this.farm2.Body);
            actual = StaticAnalysis.CheckBodyIsSet(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(0, actual.Count);
        }

        // CheckTieredProduction

        [TestMethod]
        public void WarningsTest_CheckTieredProduction_Hydroponics()
        {
            List<ITieredProducer> hydroProducers = new List<ITieredProducer>() { this.hydro1, this.hydro2 };

            // Verify no false-positives.
            var actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, hydroProducers, this.basicHydroponicSupplies, this.emptyContainers).ToList();
            Assert.AreEqual(0, actual.Count);

            hydroProducers[0].Tier = TechTier.Tier0;
            hydroProducers[1].Tier = TechTier.Tier2;
            actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, hydroProducers, this.basicHydroponicSupplies, this.emptyContainers).ToList();
            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual($"All orbital-production parts should be set to {TechTier.Tier2.DisplayName()}", actual[0].Message);
            Assert.IsNotNull(actual[0].FixIt);
            actual[0].FixIt();
            Assert.AreEqual(TechTier.Tier2, hydroProducers[0].Tier);
            Assert.AreEqual(TechTier.Tier2, hydroProducers[0].Tier);

            Assert.AreEqual($"Not all of the parts producing {StubColonizationResearchScenario.HydroponicSnacks.BaseName} are set at {TechTier.Tier2.DisplayName()}", actual[1].Message);
            Assert.IsNotNull(actual[1].FixIt);
        }


        [TestMethod]
        public void WarningsTest_CheckTieredProduction_LandedUndertiered()
        {
            // Verify no false-positives.
            var actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(0, actual.Count);

            // Validate it catches that it's consistent, but undertiered
            foreach (var p in this.producers) p.Tier = TechTier.Tier0;
            actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(actual[0].Message, $"All production parts should be set to {TechTier.Tier1.DisplayName()}");
            Assert.IsNotNull(actual[0].FixIt);
            actual[0].FixIt();
            actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(0, actual.Count);
        }

        [TestMethod]
        public void TestUnderstandsMaxTier()
        {
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.farmingResearchCategory, "munmuss", TechTier.Tier3);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.productionResearchCategory, "munmuss", TechTier.Tier3);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.scanningResearchCategory, "munmuss", TechTier.Tier3);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.shiniesResearchCategory, "munmuss", TechTier.Tier2);

            StubProducer t3fertFactory = new StubProducer(StubColonizationResearchScenario.Fertilizer, StubColonizationResearchScenario.Stuff, 6, TechTier.Tier3);
            StubProducer t3drill = new StubProducer(StubColonizationResearchScenario.Stuff, null, 6, TechTier.Tier3);
            StubProducer t2shinies = new StubProducer(StubColonizationResearchScenario.Shinies, null, 6, TechTier.Tier2);

            var actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(0, actual.Count);
        }

        [TestMethod]
        public void WarningsTest_CheckTieredProduction_LandedMixedupFarms()
        {
            // Validate it catches that it's consistent, but undertiered
            farm1.Tier = TechTier.Tier0;
            var actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual(actual[0].Message, $"All production parts should be set to {TechTier.Tier1.DisplayName()}");
            Assert.AreEqual(actual[1].Message, $"Not all of the parts producing {farm1.Output.BaseName} are set at {farm2.Tier}");
            Assert.IsNotNull(actual[1].FixIt);
            actual[1].FixIt();
            Assert.AreEqual(TechTier.Tier1, farm1.Tier);
            actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(0, actual.Count);
        }

        [TestMethod]
        public void WarningsTest_CheckTieredProduction_MixedTiers()
        {
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.farmingResearchCategory, "munmuss", TechTier.Tier2);

            // Validate it catches that it's consistent, but undertiered
            farm1.Tier = TechTier.Tier2;
            farm2.Tier = TechTier.Tier2;
            var actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(actual[0].Message, $"There are {TechTier.Tier2.DisplayName()} producers of Snacks, but it requires equal-tier {StubColonizationResearchScenario.Fertilizer.BaseName} production in order to work.");
            Assert.IsTrue(actual[0].IsClearlyBroken);
            Assert.IsNull(actual[0].FixIt);

            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.productionResearchCategory, "munmuss", TechTier.Tier2);
            fertFactory1.Tier = TechTier.Tier2;
            drill1.Tier = TechTier.Tier2;
            actual = StaticAnalysis.CheckTieredProduction(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(actual[0].Message, $"Scanning technology at munmuss has not progressed beyond {TechTier.Tier1.DisplayName()} - scroungers won't produce if a scanner at their tier is present in-orbit.");
            Assert.IsTrue(actual[0].IsClearlyBroken);
            Assert.IsNull(actual[0].FixIt);
        }

        [TestMethod]
        public void WarningsTest_CheckCorrectCapacity()
        {
            // Verify no false-positives.
            var actual = StaticAnalysis.CheckCorrectCapacity(colonizationResearch, this.producers, this.snacksOnly, this.emptyContainers).ToList();
            Assert.AreEqual(0, actual.Count);

            // Verify if we need an excess, it gets reported
            this.producers.Add(new StubProducer(StubColonizationResearchScenario.Snacks, StubColonizationResearchScenario.Fertilizer, 3, TechTier.Tier1));
            actual = StaticAnalysis.CheckCorrectCapacity(colonizationResearch, this.producers, this.emptyContainers, this.emptyContainers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual($"The ship needs at least 9 production of {StubColonizationResearchScenario.Fertilizer.BaseName} but it is only producing 6", actual[0].Message);
            Assert.IsFalse(actual[0].IsClearlyBroken);
            Assert.IsNull(actual[0].FixIt);

            // Verify it catches missing stuff in storage - forget the fertilizer
            List<ITieredProducer> hydroProducers = new List<ITieredProducer>() { this.hydro1, this.hydro2 };
            actual = StaticAnalysis.CheckCorrectCapacity(colonizationResearch, hydroProducers, this.snacksOnly, this.emptyContainers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual($"The ship needs {StubColonizationResearchScenario.Fertilizer.BaseName} to produce {StubColonizationResearchScenario.HydroponicSnacks.BaseName}", actual[0].Message);
            Assert.IsNull(actual[0].FixIt);
        }

        [TestMethod]
        public void WarningsTest_CheckTieredProductionStorage()
        {
            Dictionary<string, double> storage = new Dictionary<string, double>
            {
                { "Snacks-Tier1", 100 },
                { "Fertilizer-Tier1", 100 },
                { "Shinies-Tier1", 100 }
            };

            // Verify no false-positives.
            var actual = StaticAnalysis.CheckTieredProductionStorage(colonizationResearch, this.producers, this.snacksOnly, storage).ToList();
            Assert.AreEqual(0, actual.Count);

            storage["Fertilizer-Tier1"] = 0;
            actual = StaticAnalysis.CheckTieredProductionStorage(colonizationResearch, this.producers, this.snacksOnly, storage).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual($"This craft is producing Fertilizer-Tier1 but there's no storage for it.", actual[0].Message);
            Assert.IsFalse(actual[0].IsClearlyBroken);
            Assert.IsNull(actual[0].FixIt);

            storage.Remove("Fertilizer-Tier1");
            actual = StaticAnalysis.CheckTieredProductionStorage(colonizationResearch, this.producers, this.snacksOnly, storage).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual($"This craft is producing Fertilizer-Tier1 but there's no storage for it.", actual[0].Message);
            Assert.IsFalse(actual[0].IsClearlyBroken);
            Assert.IsNull(actual[0].FixIt);
        }

        [TestMethod]
        public void WarningsTest_CheckExtraBaggage()
        {
            // Verify no false-positives.
            var actual = StaticAnalysis.CheckExtraBaggage(colonizationResearch, this.producers, this.snacksOnly, this.emptyContainers).ToList();
            Assert.AreEqual(0, actual.Count);

            Dictionary<string, double> extraBaggage = new Dictionary<string, double>()
            {
                { "Snacks-Tier4", 100.0 },
                { "Fertilizer-Tier3", 100.0 },
            };
            actual = StaticAnalysis.CheckExtraBaggage(colonizationResearch, this.producers, extraBaggage, emptyContainers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual($"This vessel is carrying Fertilizer-Tier3.  That kind of cargo that should just be produced - that's fine for testing mass & delta-v, but you wouldn't really want to fly this way.", actual[0].Message);
            Assert.IsFalse(actual[0].IsClearlyBroken);
            Assert.IsNull(actual[0].FixIt);
        }

        [TestMethod]
        public void WarningsTest_ChecksCombiners()
        {
            var partProducers = new List<ITieredProducer>() { this.drill1, this.partFactory };
            var combiners = new List<ITieredCombiner>() { this.combiner };
            Dictionary<string, double> availableMixins = new Dictionary<string, double>()
            {
                {  StubRocketPartCombiner.ExpectedInputResource, 100.0 }
            };
            Dictionary<string, double> placeForOutput = new Dictionary<string, double>()
            {
                { StubRocketPartCombiner.ExpectedOutputResource, 100.0 }
            };

            // Verify no false-positives.
            var actual = StaticAnalysis.CheckCombiners(colonizationResearch, partProducers, combiners, availableMixins, placeForOutput).ToList();
            Assert.AreEqual(0, actual.Count);

            // Verify finds missing untiered input
            actual = StaticAnalysis.CheckCombiners(colonizationResearch, partProducers, combiners, this.emptyContainers, placeForOutput).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("To produce Rocket Parts you will need to bring some Complex Parts.", actual[0].Message);

            // Verify finds missing tiered input
            actual = StaticAnalysis.CheckCombiners(colonizationResearch, new List<ITieredProducer>(), combiners, availableMixins, placeForOutput).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("To produce Rocket Parts, you need produce LocalParts as input.", actual[0].Message);

            // Verify checks storage for output
            actual = StaticAnalysis.CheckCombiners(colonizationResearch, partProducers, combiners, availableMixins, new Dictionary<string, double>()).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("There's no place to put the Rocket Parts this base is producing.", actual[0].Message);
        }
    }
}
