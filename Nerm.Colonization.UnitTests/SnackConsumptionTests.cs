using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nerm.Colonization.UnitTests
{
    [TestClass]
    public class SnackConsumptionTests
    {
        public const double SecondsPerKerbanDay = 6.0 * 60.0 * 60.0;

        /// <summary>
        ///   Validates the basic case where we have some supplies on board from Kerban.
        /// </summary>
        [TestMethod]
        public void SnackConsumption_AllSuppliesFromKerban()
        {
            var colonizationResearchScenario = new StubColonizationResearchScenario(TechTier.Tier0);
            Dictionary<string, double> available = new Dictionary<string, double>();
            available.Add(TechTier.Tier4.SnacksResourceName(), 1.0); // One days' worth
            SnackConsumption.CalculateSnackflow(
                5 /* kerbals */, 1.0 /* seconds*/, new ISnackProducer[0], colonizationResearchScenario, available, out double timePassedInSeconds, out bool agroponicsBreakthroughHappened, out Dictionary<string, double> consumptionPerSecond);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(false, agroponicsBreakthroughHappened);
            Assert.IsNotNull(consumptionPerSecond);
            Assert.AreEqual(1, consumptionPerSecond.Count);
            Assert.AreEqual("Snacks", consumptionPerSecond.First().Key);
            Assert.AreEqual(5 / SecondsPerKerbanDay, consumptionPerSecond.First().Value);

            // There's a days' worth of snacks, but 5 kerbals getting after it.
            SnackConsumption.CalculateSnackflow(
                5 /* kerbals */, 1.0 * SecondsPerKerbanDay, new ISnackProducer[0], colonizationResearchScenario, available, out timePassedInSeconds, out agroponicsBreakthroughHappened, out consumptionPerSecond);
            Assert.AreEqual(timePassedInSeconds, SecondsPerKerbanDay / 5);
            Assert.AreEqual(false, agroponicsBreakthroughHappened);
            Assert.IsNotNull(consumptionPerSecond);
            Assert.AreEqual(1, consumptionPerSecond.Count);
            Assert.AreEqual("Snacks", consumptionPerSecond.First().Key);
            Assert.AreEqual(5 / SecondsPerKerbanDay, consumptionPerSecond.First().Value);

            // Test no snacks at all
            available.Clear();
            SnackConsumption.CalculateSnackflow(
                5 /* kerbals */, 1.0 * SecondsPerKerbanDay, new ISnackProducer[0], colonizationResearchScenario, available, out timePassedInSeconds, out agroponicsBreakthroughHappened, out consumptionPerSecond);
            Assert.AreEqual(timePassedInSeconds, 0.0);
            Assert.AreEqual(false, agroponicsBreakthroughHappened);
            Assert.IsNull(consumptionPerSecond);
        }

        /// <summary>
        ///   Validates initial case with a few agroponics modules
        /// </summary>
        [TestMethod]
        public void SnackConsumption_SimpleAgroponics()
        {
            // We have 3 modules on our vessel, but only crew enough to staff
            // two of them (for a net production of 3) and only one of them
            // has crew enough to do research.
            var agroponicModules = new ISnackProducer[]
            {
                new StubSnackProducer(isAgroponics: true)
                {
                    Tier = TechTier.Tier0,
                    Capacity = 1,
                    IsProductionEnabled = true,
                    IsResearchEnabled = true
                },
                new StubSnackProducer(isAgroponics: true)
                {
                    Tier = TechTier.Tier0,
                    Capacity = 2,
                    IsProductionEnabled = true,
                    IsResearchEnabled = false
                },
                new StubSnackProducer(isAgroponics: true)
                {
                    Tier = TechTier.Tier0,
                    Capacity = 2,
                    IsProductionEnabled = false,
                    IsResearchEnabled = false
                },
            };
            var colonizationResearchScenario = new StubColonizationResearchScenario(TechTier.Tier0);
            Dictionary<string, double> available = new Dictionary<string, double>();
            available.Add(TechTier.Tier4.SnacksResourceName(), 1.0); // One days' worth of food
            available.Add(TechTier.Tier4.FertilizerResourceName(), 1.0); // And one days' worth of running the agroponics
            SnackConsumption.CalculateSnackflow(
                5 /* kerbals */, 1.0 /* seconds*/, agroponicModules, colonizationResearchScenario, available, out double timePassedInSeconds, out bool agroponicsBreakthroughHappened, out Dictionary<string, double> consumptionPerSecond);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(false, agroponicsBreakthroughHappened);
            Assert.IsNotNull(consumptionPerSecond);
            Assert.AreEqual(2, consumptionPerSecond.Count);
            // With 5 kerbals aboard, our 3 working agroponics farms are more than enough because
            // they can produce 3 snacks per day, but our crew will only eat .2*5=1 of them.
            // So they're running at 1/3 capacity.
            Assert.AreEqual(5 * (1 - TechTier.Tier0.AgroponicMaxDietRatio()), consumptionPerSecond["Snacks"] * SecondsPerKerbanDay);
            // bug? actually used .6 fertilizer per day, which is
            Assert.AreEqual(5 * TechTier.Tier0.AgroponicMaxDietRatio(), consumptionPerSecond["Fertilizer"] * SecondsPerKerbanDay);
        }
    }
}