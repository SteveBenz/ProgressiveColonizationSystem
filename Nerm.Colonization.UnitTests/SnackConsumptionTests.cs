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
            available.Add(TechTier.Tier4.SnacksResourceName(), 100);
            SnackConsumption.CalculateSnackflow(
                5 /* kerbals */, 1.0 /* seconds*/, new ISnackProducer[0], colonizationResearchScenario, available, out double timePassedInSeconds, out bool agroponicsBreakthroughHappened, out Dictionary<string,double> consumptionPerSecond);
            Assert.AreEqual(timePassedInSeconds, 1.0);
            Assert.AreEqual(false, agroponicsBreakthroughHappened);
            Assert.IsNotNull(consumptionPerSecond);
            Assert.AreEqual(1, consumptionPerSecond.Count);
            Assert.AreEqual("Snacks", consumptionPerSecond.First().Key);
            Assert.AreEqual(5 / SecondsPerKerbanDay, consumptionPerSecond.First().Value);
        }
    }
}
