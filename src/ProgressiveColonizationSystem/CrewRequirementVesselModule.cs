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

        public static List<IPksCrewRequirement> TestIfCrewRequirementsAreMet(List<IPksCrewRequirement> parts, List<SkilledCrewman> crew)
        {
            if (parts.Count == 0)
            {
                return parts;
            }

            // There are definitely awesomeer algorithms for this.  But we'll see how we get on with this...
            // It's definitely broken for parts that require more than one kerbal to run.

            IPksCrewRequirement currentPart = parts[0];
            List<IPksCrewRequirement> remainingParts = new List<IPksCrewRequirement>(parts);
            remainingParts.RemoveAt(0);

            List<IPksCrewRequirement> bestUnassignedParts = null;
            foreach (SkilledCrewman possibleStaffer in crew.Where(k => currentPart.CanRunPart(k) && k.RemainingCapacity >= currentPart.CapacityRequired))
            {
                possibleStaffer.RemainingCapacity -= currentPart.CapacityRequired;
                var unassignedParts = TestIfCrewRequirementsAreMet(remainingParts, crew);
                if (bestUnassignedParts == null || unassignedParts.Count < bestUnassignedParts.Count)
                {
                    bestUnassignedParts = unassignedParts;
                }
                possibleStaffer.RemainingCapacity += currentPart.CapacityRequired;
            }

            if (bestUnassignedParts != null && bestUnassignedParts.Count <= 1)
            {
                // We can't do better
                return bestUnassignedParts;
            }

            // Else try not staffing this part.
            List<IPksCrewRequirement> unassignedPartsIfWeDontStaffThisPart = TestIfCrewRequirementsAreMet(remainingParts, crew);
            unassignedPartsIfWeDontStaffThisPart.Add(currentPart);
            return bestUnassignedParts != null && bestUnassignedParts.Count < unassignedPartsIfWeDontStaffThisPart.Count
                ? bestUnassignedParts
                : unassignedPartsIfWeDontStaffThisPart;
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
