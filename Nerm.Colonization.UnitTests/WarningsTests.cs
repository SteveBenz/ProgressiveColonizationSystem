using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nerm.Colonization.UnitTests
{
    [TestClass]
    public class WarningsTests
    {
        private StubColonizationResearchScenario colonizationResearch;
        private StubProducer drill1 = new StubProducer(StubColonizationResearchScenario.Stuff, null, 10, TechTier.Tier1);
        private StubProducer fertFactory1 = new StubProducer(StubColonizationResearchScenario.Fertilizer, StubColonizationResearchScenario.Stuff, 6, TechTier.Tier1);
        private StubProducer farm1 = new StubProducer(StubColonizationResearchScenario.Snacks, StubColonizationResearchScenario.Stuff, 3, TechTier.Tier1);
        private StubProducer farm2 = new StubProducer(StubColonizationResearchScenario.Snacks, StubColonizationResearchScenario.Stuff, 3, TechTier.Tier1);
        private StubContainer snacksContainer = new StubContainer() { Content = StubColonizationResearchScenario.Snacks, Tier = TechTier.Tier1 };
        private StubContainer fertContainer = new StubContainer() { Content = StubColonizationResearchScenario.Fertilizer, Tier = TechTier.Tier1 };

        private List<ITieredProducer> producers;
        private List<ITieredContainer> containers;

        [TestInitialize]
        public void TestInitialize()
        {
            // We set up for complete happiness
            colonizationResearch = new StubColonizationResearchScenario(TechTier.Tier2);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.farmingResearchCategory, "test", TechTier.Tier1);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.productionResearchCategory, "test", TechTier.Tier1);
            colonizationResearch.SetMaxTier(StubColonizationResearchScenario.scanningResearchCategory, "test", TechTier.Tier1);

            producers = new List<ITieredProducer>()
            {
                this.drill1,
                this.fertFactory1,
                this.farm1,
                this.farm2,
            };
            containers = new List<ITieredContainer>()
            {
                this.snacksContainer,
                this.fertContainer,
            };
        }

        [TestMethod]
        public void WarningsTest_NoPartsTest()
        {
            var result = LifeSupportCalculator.CheckBodyIsSet(new List<ITieredProducer>(), new List<ITieredContainer>());
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void WarningsTest_HappyParts()
        {
            var result = LifeSupportCalculator.CheckBodyIsSet(this.producers, this.containers);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Any());
        }


        [TestMethod]
        public void WarningsTest_MissingBodyAssignment()
        {
            this.farm1.Body = null;
            this.farm2.Body = null;
            var actual = LifeSupportCalculator.CheckBodyIsSet(this.producers, this.containers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("Need to set up the target for the world-specific parts", actual[0].Message);
            Assert.IsNotNull(actual[0].FixIt);
            actual[0].FixIt();
            Assert.AreEqual("munmuss", this.farm1.Body);
            actual = LifeSupportCalculator.CheckBodyIsSet(this.producers, this.containers).ToList();
            Assert.AreEqual(0, actual.Count);

            // If nothing is set up
            foreach (var p in this.producers)
            {
                p.Body = null;
            }
            actual = LifeSupportCalculator.CheckBodyIsSet(this.producers, this.containers).ToList();
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
            var actual = LifeSupportCalculator.CheckBodyIsSet(this.producers, this.containers).ToList();
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("Not all of the body-specific parts are set up for munmuss", actual[0].Message);
            Assert.IsNotNull(actual[0].FixIt);
            actual[0].FixIt();
            Assert.AreEqual("munmuss", this.farm1.Body);
            Assert.AreEqual("munmuss", this.farm2.Body);
            actual = LifeSupportCalculator.CheckBodyIsSet(this.producers, this.containers).ToList();
            Assert.AreEqual(0, actual.Count);
        }
    }
}
