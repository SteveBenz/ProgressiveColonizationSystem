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
        private SkilledCrewman sci1 = new SkilledCrewman(2, "Scientist");
        private SkilledCrewman bio1 = new SkilledCrewman(1, "Biologist");
        private SkilledCrewman eng1 = new SkilledCrewman(1, "Engineer");
        private SkilledCrewman eng2 = new SkilledCrewman(2, "Engineer");
        private SkilledCrewman eng3 = new SkilledCrewman(3, "Engineer");
        private SkilledCrewman tech1 = new SkilledCrewman(1, "Engineer");

        [TestMethod]
        public void Crew_NoCrewNoRequirement()
        {
            List<IPksCrewRequirement> shouldBeEmpty = CrewRequirementVesselModule.TestIfCrewRequirementsAreMet(new List<IPksCrewRequirement>(), new List<SkilledCrewman>());
            Assert.IsNotNull(shouldBeEmpty);
            Assert.AreEqual(0, shouldBeEmpty.Count);
        }

        [TestMethod]
        public void Crew_BasicAllocation()
        {
            var part1 = new StubCrewRequirement();
            part1.ValidCrew.Add(sci1);
            part1.ValidCrew.Add(bio1);
            var part2 = new StubCrewRequirement();
            part2.ValidCrew.Add(bio1);
            var part3 = new StubCrewRequirement();
            part3.ValidCrew.Add(eng1);

            AssertAssignsAll(sci1, bio1, eng1, part1, part2, part3);

            // And test in other orders to ensure it isn't a fluke
            AssertAssignsAll(eng1, bio1, sci1, part1, part2, part3);
            AssertAssignsAll(sci1, bio1, eng1, part3, part2, part1);
        }

        [TestMethod]
        public void Crew_DetectsInsufficientCrew()
        {
            var part1 = new StubCrewRequirement();
            part1.ValidCrew.Add(sci1);
            part1.ValidCrew.Add(bio1);
            var part2 = new StubCrewRequirement();
            part2.ValidCrew.Add(bio1);
            var part3 = new StubCrewRequirement();
            part3.ValidCrew.Add(eng1);
            var part4 = new StubCrewRequirement() { CapacityRequired = 1.1f };
            part4.ValidCrew.Add(eng1);

            var uncrewedPart = AssertAssignsAllButOne(sci1, bio1, eng1, part1, part2, part3, part4);
            // It should prefer to not staff the one with less required capacity
            Assert.AreEqual(part4, uncrewedPart);
        }

        [TestMethod]
        public void Crew_AllocatesGeneralist()
        {
            // This one ends up just going through the slot-assignment process twice
            var generalist = new SkilledCrewman(5, "jack");
            var part1 = new StubCrewRequirement();
            part1.ValidCrew.Add(sci1);
            part1.ValidCrew.Add(bio1);
            part1.ValidCrew.Add(generalist);
            var part2 = new StubCrewRequirement();
            part2.ValidCrew.Add(bio1);
            part2.ValidCrew.Add(generalist);
            var part3 = new StubCrewRequirement();
            part3.ValidCrew.Add(eng1);
            part3.ValidCrew.Add(generalist);
            var part4 = new StubCrewRequirement();
            part4.ValidCrew.Add(eng1);
            part4.ValidCrew.Add(generalist);

            // Note the order is important - if we put the single-assignments up first, it would just go
            // through once
            AssertAssignsAll(bio1, generalist, sci1, eng1, part1, part2, part3, part4);
        }

        [TestMethod]
        public void Crew_AllocatesGeneralist_CombinesCategories()
        {
            // This one forces things so that it does the single-assignments and then
            // discoveres there's just one category
            var generalist1 = new SkilledCrewman(1, "jack");
            var generalist2 = new SkilledCrewman(2, "jack");
            var part1 = new StubCrewRequirement() { CapacityRequired = 2f };
            part1.ValidCrew.Add(sci1);
            part1.ValidCrew.Add(generalist1);
            part1.ValidCrew.Add(generalist2);
            var part2 = new StubCrewRequirement() { CapacityRequired = 2f };
            part2.ValidCrew.Add(bio1);
            part2.ValidCrew.Add(generalist1);
            part2.ValidCrew.Add(generalist2);

            AssertAssignsAll(sci1, bio1, generalist1, generalist2, part1, part2);
        }

        private void AssertAssignsAll(params object[] partsAndCrew)
        {
            List<IPksCrewRequirement> shouldBeEmpty = TestAssignments(partsAndCrew);
            Assert.IsNotNull(shouldBeEmpty);
            Assert.AreEqual(0, shouldBeEmpty.Count);
        }

        private IPksCrewRequirement AssertAssignsAllButOne(params object[] partsAndCrew)
        {
            List<IPksCrewRequirement> shouldHaveOne = TestAssignments(partsAndCrew);
            Assert.IsNotNull(shouldHaveOne);
            Assert.AreEqual(1, shouldHaveOne.Count);
            return shouldHaveOne[0];
        }

        private static List<IPksCrewRequirement> TestAssignments(object[] partsAndCrew)
        {
            var crew = partsAndCrew.OfType<SkilledCrewman>().ToList();
            var parts = partsAndCrew.OfType<IPksCrewRequirement>().ToList();
            return CrewRequirementVesselModule.TestIfCrewRequirementsAreMet(parts, crew);
        }

        public List<T> NewList<T>(params T[] args) => args.ToList();
    }
}
