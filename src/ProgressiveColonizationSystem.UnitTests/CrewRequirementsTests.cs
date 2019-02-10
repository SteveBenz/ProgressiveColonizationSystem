using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressiveColonizationSystem.UnitTests
{
    [TestClass]
    public class CrewRequirementsTests
    {
        [TestMethod]
        public void Crew_NoCrewNoRequirement()
        {
            List<IPksCrewRequirement> shouldBeEmpty = CrewRequirementVesselModule.TestIfCrewRequirementsAreMet(new List<IPksCrewRequirement>(), new List<SkilledCrewman>());
            Assert.IsNotNull(shouldBeEmpty);
            Assert.AreEqual(0, shouldBeEmpty.Count);
        }
    }
}
