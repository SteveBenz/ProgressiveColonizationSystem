using Experience;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public class CrewRequirementVesselModule
        : VesselModule
    {
        public int hashAtLastCheck = -1;

        /// <summary>
        ///   This is called on each physics frame for the active vessel by reflection-magic from KSP.
        /// </summary>
        public void FixedUpdate()
        {
            if (!this.vessel.loaded)
            {
                return;
            }

            List<IPksCrewRequirement> activatedParts = this.vessel
                .FindPartModulesImplementing<IPksCrewRequirement>()
                .Where(p => p.IsRunning)
                .ToList();
            List<ProtoCrewMember> kspCrew = this.vessel.GetVesselCrew();
            var crew = kspCrew.Select(c => new SkilledCrewman(c)).ToList();

            int hash = activatedParts.Aggregate(0, (accumulator, part) => accumulator ^ part.GetHashCode());
            hash = kspCrew.Aggregate(hash, (accumulator, kerbal) => accumulator ^ kerbal.GetHashCode());

            if (hash == this.hashAtLastCheck)
            {
                return;
            }

            List<IPksCrewRequirement> unstaffableParts = TestIfCrewRequirementsAreMet(activatedParts, crew);
            if (unstaffableParts.Count > 0)
            {
                foreach (var part in activatedParts)
                {
                    part.IsStaffed = !unstaffableParts.Contains(part);
                }
            }
            else
            {
                foreach (var part in activatedParts)
                {
                    part.IsStaffed = true;
                }
            }
            this.hashAtLastCheck = hash;
        }

        private class PartCategory
        {
            public List<IPksCrewRequirement> parts;
            public HashSet<SkilledCrewman> crew;
            public float unfilledCapacity;
            public int crewHash;
        }

        public static List<IPksCrewRequirement> TestIfCrewRequirementsAreMet(List<IPksCrewRequirement> parts, List<SkilledCrewman> crew)
        {
            if (parts.Count == 0)
            {
                return parts;
            }

            HashSet<SkilledCrewman> unassignedCrew = new HashSet<SkilledCrewman>();
            // Categorize parts by what kerbals can staff them
            List<PartCategory> categories = new List<PartCategory>();
            foreach (var part in parts)
            {
                var crewThatCanStaffPart = new HashSet<SkilledCrewman>();
                int crewHash = 0;
                foreach (var kerbal in crew)
                {
                    if (kerbal.CanRunPart(part.RequiredEffect, part.RequiredLevel))
                    {
                        crewThatCanStaffPart.Add(kerbal);
                        crewHash ^= kerbal.GetHashCode();
                        unassignedCrew.Add(kerbal);
                    }
                }

                PartCategory existingCategory = null;
                foreach (var category in categories)
                {
                    if (category.crewHash == crewHash
                     && category.crew.Count == crewThatCanStaffPart.Count
                     && category.crew.SetEquals(crewThatCanStaffPart))
                    {
                        existingCategory = category;
                        break;
                    }
                }

                if (existingCategory != null)
                {
                    existingCategory.parts.Add(part);
                    existingCategory.unfilledCapacity += part.CapacityRequired;
                }
                else
                {
                    existingCategory = new PartCategory
                    {
                        crew = crewThatCanStaffPart,
                        crewHash = crewHash,
                        parts = new List<IPksCrewRequirement>() { part },
                        unfilledCapacity = part.CapacityRequired
                    };
                    categories.Add(existingCategory);
                }
            }

            List<PartCategory> incompletelyStaffedCategories = new List<PartCategory>();

            // See if there are any categories that don't have any potential staff
            int i = 0;
            while (i < categories.Count)
            {
                if (!categories[i].crew.Any())
                {
                    incompletelyStaffedCategories.Add(categories[i]);
                    categories.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }

            // Now start assigning workers
            bool makingProgress;
            do
            {
                makingProgress = false;
                List<SkilledCrewman> kerbalsThatBecameAssigned = new List<SkilledCrewman>();
                // Find all the crew that only belong to one category
                foreach (SkilledCrewman kerbal in unassignedCrew)
                {
                    PartCategory singleCategory = null;
                    foreach (var category in categories)
                    {
                        if (category.crew.Contains(kerbal))
                        {
                            if (singleCategory == null)
                            {
                                singleCategory = category;
                            }
                            else
                            {
                                singleCategory = null;
                                break;
                            }
                        }
                    }

                    if (singleCategory != null)
                    {
                        makingProgress = true;
                        AssignKerbalToPart(categories, incompletelyStaffedCategories, kerbalsThatBecameAssigned, singleCategory, kerbal);
                    }
                }

                // We no longer need to look at Kerbals that got assigned in the previous loop
                unassignedCrew.ExceptWith(kerbalsThatBecameAssigned);

                // See if there are categories that can be combined
                for (i = 0; i < categories.Count - 1; ++i)
                {
                    int j = i + 1;
                    while (j < categories.Count)
                    {
                        if (categories[i].crewHash == categories[j].crewHash && categories[i].crew.SetEquals(categories[j].crew))
                        {
                            categories[i].crew.UnionWith(categories[j].crew);
                            categories[i].crewHash ^= categories[j].crewHash;
                            categories.RemoveAt(j);
                            makingProgress = true;
                        }
                        else
                        {
                            ++j;
                        }
                    }
                }

                if (!makingProgress && categories.Any())
                {
                    // There isn't a non-contrived way to get here that I can find, but if I'm mistaken,
                    // this code should make some kind of progress

                    // But after the tests have proved this is so, just assign a kerbal and force the issue.
                    // By just picking one arbitrarily, we might be missing finding a way to staff all parts,
                    // but doing an exhaustive search of all possible combinations isn't worth doing in
                    // defensive code like this.
                    var singleCategory = categories.First();
                    var kerbal = singleCategory.crew.First();

                    makingProgress = true;
                    AssignKerbalToPart(categories, incompletelyStaffedCategories, kerbalsThatBecameAssigned, singleCategory, kerbal);
                }
            } while (categories.Any());

            List<IPksCrewRequirement> unstaffedParts = new List<IPksCrewRequirement>();
            foreach (var category in incompletelyStaffedCategories)
            {
                // Staff the largest number of parts, so sort the list by capacity
                category.parts.Sort((a, b) => b.CapacityRequired.CompareTo(a.CapacityRequired));
                float unfilledCapacity = category.unfilledCapacity;
                foreach (var part in category.parts)
                {
                    unstaffedParts.Add(part);
                    unfilledCapacity -= part.CapacityRequired;
                    if (unfilledCapacity < 0.01)
                    {
                        break;
                    }
                }
            }

            return unstaffedParts;
        }

        private static void AssignKerbalToPart(List<PartCategory> categories, List<PartCategory> incompletelyStaffedCategories, List<SkilledCrewman> kerbalsThatBecameAssigned, PartCategory singleCategory, SkilledCrewman kerbal)
        {
            if (singleCategory.unfilledCapacity <= 1f)
            {
                // This category is fully staffed - it can just be dropped out of the lists
                categories.Remove(singleCategory);
            }
            else
            {
                singleCategory.unfilledCapacity -= 1f;
                if (singleCategory.crew.Count == 1)
                {
                    // We did the best we could with this category.
                    categories.Remove(singleCategory);
                    incompletelyStaffedCategories.Add(singleCategory);
                }
                else
                {
                    singleCategory.crew.Remove(kerbal);
                    singleCategory.crewHash ^= kerbal.GetHashCode();
                }
            }
            kerbalsThatBecameAssigned.Add(kerbal);
        }
    }

    public class SkilledCrewman
    {
        private readonly ProtoCrewMember protoCrewMember;
        public SkilledCrewman(ProtoCrewMember protoCrewMember)
        {
            this.protoCrewMember = protoCrewMember;
        }

        public virtual bool CanRunPart(string requiredTrait, int requiredLevel)
        {
            ExperienceEffect experienceEffect = this.protoCrewMember.GetEffect(requiredTrait);
            if (experienceEffect == null)
            {
                return false;
            }
            else
            {
                return experienceEffect.Level + this.protoCrewMember.experienceLevel >= requiredLevel;
            }
        }
    }
}
