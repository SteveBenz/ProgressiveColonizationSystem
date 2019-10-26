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
        private SkilledCrewman sci1 = new StubSkilledCrewman("food", 1);
        private SkilledCrewman bio1 = new StubSkilledCrewman("food", 4);
        private SkilledCrewman eng1 = new StubSkilledCrewman("tech", 1);
        private SkilledCrewman eng2 = new StubSkilledCrewman("tech", 1);
        private SkilledCrewman eng3 = new StubSkilledCrewman("tech", 1);
        private SkilledCrewman tech1 = new StubSkilledCrewman("tech", 4);
        private SkilledCrewman tourist1 = new StubSkilledCrewman("tourist", 1);
        private SkilledCrewman tourist2 = new StubSkilledCrewman("tourist", 1);

        [TestMethod]
        public void Crew_NoCrewNoRequirement()
        {
            List<IPksCrewRequirement> shouldBeEmpty = CrewRequirement.FindUnstaffableParts(new List<IPksCrewRequirement>(), new List<SkilledCrewman>());
            Assert.IsNotNull(shouldBeEmpty);
            Assert.AreEqual(0, shouldBeEmpty.Count);
        }

        [TestMethod]
        public void Crew_BasicAllocation()
        {
            var part1 = new StubCrewRequirement("food", 1);
            var part2 = new StubCrewRequirement("food", 4);
            var part3 = new StubCrewRequirement("tech", 1);

            AssertAssignsAll(sci1, bio1, eng1, part1, part2, part3);

            // And test in other orders to ensure it isn't a fluke
            AssertAssignsAll(eng1, bio1, sci1, part1, part2, part3);
            AssertAssignsAll(sci1, bio1, eng1, part3, part2, part1);
        }

        [TestMethod]
        public void Crew_DetectsInsufficientCrew()
        {
            var part1 = new StubCrewRequirement("food", 1);
            var part2 = new StubCrewRequirement("food", 4);
            var part3 = new StubCrewRequirement("tech", 1);
            var part4 = new StubCrewRequirement("tech", 1) { CapacityRequired = 1.1f };

            var uncrewedPart = AssertAssignsAllButOne(sci1, bio1, eng1, part1, part2, part3, part4);
            // It should prefer to not staff the one with less required capacity
            Assert.AreEqual(part4, uncrewedPart);
        }

        [TestMethod]
        public void Crew_AllocatesGeneralist()
        {
            // This one ends up just going through the slot-assignment process twice
            var generalist = new StubSkilledCrewman("food", 5, "tech", 5);
            var part1 = new StubCrewRequirement("food", 1);
            var part2 = new StubCrewRequirement("food", 4);
            var part3 = new StubCrewRequirement("tech", 1);
            var part4 = new StubCrewRequirement("tech", 1);

            // Note the order is important - if we put the single-assignments up first, it would just go
            // through once
            AssertAssignsAll(bio1, generalist, sci1, eng1, part1, part2, part3, part4);
        }


        [TestMethod]
        public void Crew_Pathological()
        {
            // The categorization scheme should work for any sensible means of defining crew capabilities.
            // But there's code in there that will unstick the algorithm, at the expensive of maybe not coming
            // up with an ideal crew assignment.
            var crew1 = new StubSkilledCrewman("part1", 1, "part2", 1);
            var crew2 = new StubSkilledCrewman("part2", 1, "part3", 1);
            var crew3 = new StubSkilledCrewman("part3", 1, "part1", 1);
            var part1 = new StubCrewRequirement("part1", 1);
            var part2 = new StubCrewRequirement("part2", 1);
            var part3 = new StubCrewRequirement("part3", 1);

            // TODO: HUH?  How did this work in the past?  3 crew given but requires 6!
            AssertAssignsAll(crew1, crew2, crew3, part1, part2, part3);
        }

        [TestMethod]
        public void Crew_AllocatesGeneralist_CombinesCategories()
        {
            // This one forces things so that it does the single-assignments and then
            // discoveres there's just one category
            var generalist1 = new StubSkilledCrewman("food", 5, "tech", 5);
            var generalist2 = new StubSkilledCrewman("food", 5, "tech", 5);
            var part1 = new StubCrewRequirement("food", 1) { CapacityRequired = 2f };
            var part2 = new StubCrewRequirement("tech", 1) { CapacityRequired = 2f };

            AssertAssignsAll(sci1, eng1, generalist1, generalist2, part1, part2);
        }

        [TestMethod]
        public void Crew_DetectsUnstaffablePartsAndUselessCrew()
        {
            // This one forces things so that it does the single-assignments and then
            // discoveres there's just one category
            var part1 = new StubCrewRequirement("food", 1);
            var part2 = new StubCrewRequirement("tech", 1);

            Assert.AreEqual(part2, AssertAssignsAllButOne(sci1, tourist1, tourist2, part1, part2));
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
            return CrewRequirement.FindUnstaffableParts(parts, crew);
        }
    }
}
