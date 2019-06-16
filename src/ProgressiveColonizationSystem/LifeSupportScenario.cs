using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   This class maintains what we know about individual kerbals (and persists what it knows
    ///   in the save file).
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.EDITOR)]
    public class LifeSupportScenario
        : ScenarioModule
    {
        private Dictionary<string, LifeSupportStatus> knownKerbals = new Dictionary<string, LifeSupportStatus>();

        private Vessel lastVesselComplainedAbout = null;
        private List<ProtoCrewMember> hungryKerbals = new List<ProtoCrewMember>();
        private List<ProtoCrewMember> incapacitatedKerbals = new List<ProtoCrewMember>();

        public static LifeSupportScenario Instance;

        public LifeSupportScenario()
        {
            Instance = this;
        }

        public const int DaysBeforeKerbalStarves = 7;
        private const double secondsBeforeKerbalStarves = DaysBeforeKerbalStarves * 6 * 60 * 60; // 7 kerban days

        public void KerbalsMissedAMeal(Vessel vessel, bool hasActiveProducers)
        {
            if (vessel.isEVA)
            {
                return;
            }

            this.CheckForNewVessel(vessel);

            List<ProtoCrewMember> crewThatBecameHungry = new List<ProtoCrewMember>();
            List<ProtoCrewMember> crewThatBecameIncapacitated = new List<ProtoCrewMember>();
            List<ProtoCrewMember> crewThatAreBecomingAntsy = new List<ProtoCrewMember>();

            foreach (var crew in vessel.GetVesselCrew())
            {
                if (this.knownKerbals.TryGetValue(crew.name, out LifeSupportStatus crewStatus))
                {
                    if (!crewStatus.IsGrouchy && Planetarium.GetUniversalTime() > crewStatus.LastMeal + secondsBeforeKerbalStarves)
                    {
                        crewStatus.IsGrouchy = true;
                        crewStatus.OldTrait = crew.experienceTrait.Title;
                        crew.type = ProtoCrewMember.KerbalType.Tourist;
                        KerbalRoster.SetExperienceTrait(crew, "Tourist");
                    }
                    else if (!crewStatus.IsGrouchy && Planetarium.GetUniversalTime() > crewStatus.LastMeal + secondsBeforeKerbalStarves/2)
                    {
                        crewThatAreBecomingAntsy.Add(crew);
                    }
                }
                else
                {
                    crewStatus = new LifeSupportStatus
                    {
                        IsGrouchy = false,
                        KerbalName = crew.name,
                        LastMeal = Planetarium.GetUniversalTime(),
                        OldTrait = null
                    };
                    this.knownKerbals.Add(crew.name, crewStatus);
                }

                if (crewStatus.IsGrouchy)
                {
                    if (!this.incapacitatedKerbals.Contains(crew))
                    {
                        crewThatBecameIncapacitated.Add(crew);
                        this.incapacitatedKerbals.Add(crew);
                    }
                }
                else
                {
                    if (!this.hungryKerbals.Contains(crew))
                    {
                        crewThatBecameHungry.Add(crew);
                        this.hungryKerbals.Add(crew);
                    }
                }
            }

            if (crewThatBecameIncapacitated.Any())
            {
                ScreenMessages.PostScreenMessage(
                    message: CrewBlurbs.CreateMessage("#LOC_KPBS_KERBAL_INCAPACITATED", crewThatBecameIncapacitated, new string[] { }, TechTier.Tier0),
                    duration: 15f,
                    style: ScreenMessageStyle.UPPER_CENTER);
            }
            else if (hasActiveProducers && crewThatBecameHungry.Any())
            {
                ScreenMessages.PostScreenMessage(
                    message: CrewBlurbs.CreateMessage("#LOC_KPBS_KERBAL_HUNGRY_NO_PRODUCTION", crewThatBecameHungry, new string[] { }, TechTier.Tier0),
                    duration: 15f,
                    style: ScreenMessageStyle.UPPER_CENTER);
            }
            else if (!hasActiveProducers && crewThatAreBecomingAntsy.Any())
            {
                ScreenMessages.PostScreenMessage(
                    message: CrewBlurbs.CreateMessage("#LOC_KPBS_KERBAL_HUNGRY", crewThatBecameHungry, new string[] { }, TechTier.Tier0),
                    duration: 15f,
                    style: ScreenMessageStyle.UPPER_CENTER);
            }
        }

        public bool TryGetStatus(ProtoCrewMember crew, out double daysSinceMeal, out double daysToGrouchy, out bool isGrouchy)
        {
            double now = Planetarium.GetUniversalTime();
            if (this.knownKerbals.TryGetValue(crew.name, out LifeSupportStatus lifeSupportStatus))
            {
                daysSinceMeal = ColonizationResearchScenario.SecondsToKerbalDays(now - lifeSupportStatus.LastMeal);
                isGrouchy = lifeSupportStatus.IsGrouchy;
                daysToGrouchy = ColonizationResearchScenario.SecondsToKerbalDays(lifeSupportStatus.LastMeal + secondsBeforeKerbalStarves - now);
                return true;
            }
            else
            {
                daysSinceMeal = 0;
                daysToGrouchy = ColonizationResearchScenario.SecondsToKerbalDays(secondsBeforeKerbalStarves);
                isGrouchy = false;
                return false;
            }
        }

        public void KerbalsHaveReachedHomeworld(Vessel vessel)
        {
            // Perhaps we should do this when we recover the vessel like ShiniesReputationRewards does.
            foreach (var crew in vessel.GetVesselCrew())
            {
                if (this.knownKerbals.TryGetValue(crew.name, out LifeSupportStatus crewStatus))
                {
                    if (crewStatus.IsGrouchy)
                    {
                        crew.type = ProtoCrewMember.KerbalType.Crew;
                        KerbalRoster.SetExperienceTrait(crew, crewStatus.OldTrait);
                    }
                    this.knownKerbals.Remove(crew.name);
                }
            }
        }

        public void KerbalsHadASnack(Vessel vessel, double lastMealTime)
        {
            this.CheckForNewVessel(vessel);

            foreach (var crew in vessel.GetVesselCrew())
            {
                if (this.knownKerbals.TryGetValue(crew.name, out LifeSupportStatus crewStatus))
                {
                    if (crewStatus.IsGrouchy)
                    {
                        crew.type = ProtoCrewMember.KerbalType.Crew;
                        KerbalRoster.SetExperienceTrait(crew, crewStatus.OldTrait);
                    }
                    crewStatus.LastMeal = lastMealTime;
                    crewStatus.IsGrouchy = false;
                }
                else
                {
                    this.knownKerbals.Add(crew.name, new LifeSupportStatus
                    {
                        IsGrouchy = false,
                        KerbalName = crew.name,
                        LastMeal = Planetarium.GetUniversalTime(),
                        OldTrait = null
                    });
                }
            }

            if (this.incapacitatedKerbals.Any())
            {
                ScreenMessages.PostScreenMessage(
                    message: CrewBlurbs.CreateMessage("#LOC_KPBS_KERBAL_NOT_INCAPACITATED", this.incapacitatedKerbals, new string[] { }, TechTier.Tier0),
                    duration: 15f,
                    style: ScreenMessageStyle.UPPER_CENTER);
                this.incapacitatedKerbals.Clear();
            }
            if (this.hungryKerbals.Any())
            {
                ScreenMessages.PostScreenMessage(
                    message: CrewBlurbs.CreateMessage("#LOC_KPBS_KERBAL_NOT_HUNGRY", this.incapacitatedKerbals, new string[] { }, TechTier.Tier0),
                    duration: 15f,
                    style: ScreenMessageStyle.UPPER_CENTER);
                this.hungryKerbals.Clear();
            }
        }


        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            Dictionary<string, LifeSupportStatus> newState = new Dictionary<string, LifeSupportStatus>();
            foreach (ConfigNode perKerbalNode in node.GetNodes("Kerbals"))
            {
                LifeSupportStatus status = new LifeSupportStatus();
                bool gotit = perKerbalNode.TryGetValue(nameof(status.KerbalName), ref status.KerbalName)
                          && perKerbalNode.TryGetValue(nameof(status.IsGrouchy), ref status.IsGrouchy)
                          && perKerbalNode.TryGetValue(nameof(status.LastMeal), ref status.LastMeal)
                          && perKerbalNode.TryGetValue(nameof(status.OldTrait), ref status.OldTrait);
                if (gotit && !newState.ContainsKey(status.KerbalName))
                {
                    newState.Add(status.KerbalName, status);
                }
                else
                {
                    Debug.LogError($"Failed to find status for {perKerbalNode.name}");
                    // Because we don't add it to the array, the kerbal will appear to be happy
                    // however, if the Kerbal was previously grumpy, the Tourist state will be permanent :(
                }
            }

            this.knownKerbals = newState;
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            foreach (LifeSupportStatus status in this.knownKerbals.Values)
            {
                ConfigNode childNode = new ConfigNode("Kerbals");
                childNode.AddValue(nameof(status.KerbalName), status.KerbalName);
                childNode.AddValue(nameof(status.IsGrouchy), status.IsGrouchy);
                childNode.AddValue(nameof(status.LastMeal), status.LastMeal);
                childNode.AddValue(nameof(status.OldTrait), status.OldTrait ?? "");
                node.AddNode(childNode);
            }
        }

        private void CheckForNewVessel(Vessel vessel)
        {
            if (vessel != this.lastVesselComplainedAbout)
            {
                this.lastVesselComplainedAbout = vessel;
                this.hungryKerbals.Clear();
                this.incapacitatedKerbals.Clear();
            }
        }
    }
}
