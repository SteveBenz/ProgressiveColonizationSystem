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

            List<IPksCrewRequirement> allCrewableParts = this.vessel
                .FindPartModulesImplementing<IPksCrewRequirement>();
            List<IPksCrewRequirement> activatedParts = allCrewableParts
                .Where(p => p.IsRunning)
                .ToList();
            List<ProtoCrewMember> kspCrew = this.vessel.GetVesselCrew();
            var crew = kspCrew.Select(c => new SkilledCrewman(c)).ToList();

            int hash = activatedParts.Aggregate(0, (accumulator, part) => accumulator ^ part.RequiredEffect.GetHashCode() ^ part.RequiredLevel.GetHashCode());
            hash = kspCrew.Aggregate(hash, (accumulator, kerbal) => accumulator ^ kerbal.GetHashCode());

            if (hash == this.hashAtLastCheck)
            {
                return;
            }

            List<IPksCrewRequirement> unstaffableParts = CrewRequirement.FindUnstaffableParts(activatedParts, crew);
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

        public virtual bool CanPilotRover()
        {
            return this.protoCrewMember.trait == KerbalRoster.pilotTrait;
        }
    }
}
