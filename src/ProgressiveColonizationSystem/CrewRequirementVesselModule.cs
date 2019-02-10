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
            var crew = SkilledCrewman.Build(kspCrew).ToList();

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

            // Categorize parts by what kerbals can staff them
            List<PartCategory> categories = new List<PartCategory>();
            foreach (var part in parts)
            {
                var crewThatCanStaffPart = new HashSet<SkilledCrewman>();
                int crewHash = 0;
                foreach (var kerbal in crew)
                {
                    if (part.CanRunPart(kerbal))
                    {
                        crewThatCanStaffPart.Add(kerbal);
                        crewHash ^= kerbal.GetHashCode();
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

            // Now start assigning workers
            bool makingProgress = false;
            List<PartCategory> partlyStaffedPartCategories = new List<PartCategory>();
            do
            {
                // Find all the crew that only belong to one category
                foreach (SkilledCrewman kerbal in crew)
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
                        if (singleCategory.unfilledCapacity <= 1f)
                        {
                            // This category is fully staffed - it can just be dropped out of the lists
                            categories.Remove(singleCategory);
                        }
                        else
                        {
                            singleCategory.unfilledCapacity -= 1f;
                            singleCategory.crew.Remove(kerbal);
                            singleCategory.crewHash ^= kerbal.GetHashCode();
                        }
                    }
                }

                // See if there are any categories that don't have any potential staff
                int i = 0;
                while (i < categories.Count)
                {
                    if (!categories[i].crew.Any())
                    {
                        partlyStaffedPartCategories.Add(categories[i]);
                        categories.RemoveAt(i);
                        makingProgress = true;
                    }
                    else
                    {
                        ++i;
                    }
                }

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
                    // There isn't a non-contrived way to get here.

                    // But after the tests have proved this is so, just assign a kerbal and force the issue.
                    throw new NotImplementedException();
                }
            } while (categories.Any());

            List<IPksCrewRequirement> unstaffedParts = new List<IPksCrewRequirement>();

            foreach (var category in partlyStaffedPartCategories)
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
    }

    public class SkilledCrewman
    {
        public SkilledCrewman(int stars, string trait)
        {
            this.Stars = stars;
            this.Trait = trait;
        }

        public int Stars { get; }
        public string Trait { get; }

        public float RemainingCapacity { get; set; } = 1;

        public static IEnumerable<SkilledCrewman> Build(IEnumerable<ProtoCrewMember> realCrew)
            => realCrew.GroupBy(k => $"{k.trait}{k.experienceLevel}")
                       .Select(group => new SkilledCrewman(group.First().experienceLevel, group.First().trait) { RemainingCapacity = group.Count() });
    }
}
