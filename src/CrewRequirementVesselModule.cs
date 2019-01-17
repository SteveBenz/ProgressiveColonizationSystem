using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
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
            List<ICbnCrewRequirement> activatedParts = this.vessel
                .FindPartModulesImplementing<ICbnCrewRequirement>()
                .Where(p => p.IsRunning)
                .ToList();
            var crew = this.vessel.GetVesselCrew().Select(k => new SkilledCrewman(k.experienceLevel, k.trait)).ToList();

            int hash = 0;
            foreach (ICbnCrewRequirement part in activatedParts)
            {
                hash ^= part.GetHashCode();
            }
            foreach (var k in crew)
            {
                hash ^= k.GetHashCode();
            }

            if (hash == this.hashAtLastCheck)
            {
                return;
            }

            List<ICbnCrewRequirement> unstaffableParts = TestIfCrewRequirementsAreMet(activatedParts, crew);
            if (unstaffableParts.Count > 0)
            {
                // If we've done better than the status quo, apply the new list
                if ( unstaffableParts.Count < activatedParts.Count(pnf => !pnf.IsStaffed))
                {
                    foreach (var part in activatedParts)
                    {
                        part.IsStaffed = !unstaffableParts.Contains(part);
                    }
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

        public bool TestIfCurrentCrewAssignmentCanWork(List<ICbnCrewRequirement> parts, List<SkilledCrewman> crew)
        {
            if (parts.Count == 0)
            {
                return true;
            }

            // There are definitely awesomer algorithms for this.  But we'll see how we get on with this...
            // It's definitely broken for parts that require more than one kerbal to run.

            ICbnCrewRequirement currentPart = parts[0];
            List<ICbnCrewRequirement> remainingParts = new List<ICbnCrewRequirement>(parts);
            remainingParts.RemoveAt(0);

            if (currentPart.IsStaffed)
            {
                foreach (SkilledCrewman possibleStaffer in crew.Where(k => currentPart.CanRunPart(k) && k.RemainingCapacity >= currentPart.CapacityRequired))
                {
                    possibleStaffer.RemainingCapacity -= currentPart.CapacityRequired;
                    bool canWork = TestIfCurrentCrewAssignmentCanWork(remainingParts, crew);
                    possibleStaffer.RemainingCapacity += currentPart.CapacityRequired;
                    if (canWork)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<ICbnCrewRequirement> TestIfCrewRequirementsAreMet(List<ICbnCrewRequirement> parts, List<SkilledCrewman> crew)
        {
            if (parts.Count == 0)
            {
                return parts;
            }

            // There are definitely awesomeer algorithms for this.  But we'll see how we get on with this...
            // It's definitely broken for parts that require more than one kerbal to run.

            ICbnCrewRequirement currentPart = parts[0];
            List<ICbnCrewRequirement> remainingParts = new List<ICbnCrewRequirement>(parts);
            remainingParts.RemoveAt(0);

            List<ICbnCrewRequirement> bestUnassignedParts = null;
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
            List<ICbnCrewRequirement> unassignedPartsIfWeDontStaffThisPart = TestIfCrewRequirementsAreMet(remainingParts, crew);
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
    }
}
