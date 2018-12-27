using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{
    // TODO: Make it take in the whole crew, rather than just one kerbal for all these operations
    //  so it can show a single message for all the kerbals whose state flips

    /// <summary>
    ///   This class maintains what we know about individual kerbals (and persists what it knows
    ///   in the save file).
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    public class LifeSupportScenario
        : ScenarioModule
    {
        private Dictionary<string, LifeSupportStatus> knownKerbals = new Dictionary<string, LifeSupportStatus>();

        public static LifeSupportScenario Instance;

        public LifeSupportScenario()
        {
            Instance = this;
        }

        // TODO: Configurable?
        private const double timeBeforeKerbalStarves = 7 * 6 * 60 * 60; // 7 kerban days

        public void KerbalMissedAMeal(ProtoCrewMember crew)
        {
            if (this.knownKerbals.TryGetValue(crew.name, out LifeSupportStatus crewStatus))
            {
                if (!crewStatus.IsGrouchy && Planetarium.GetUniversalTime() > crewStatus.LastMeal + timeBeforeKerbalStarves)
                {
                    crewStatus.IsGrouchy = true;
                    crewStatus.OldTrait = crew.experienceTrait.Title;
                    crew.type = ProtoCrewMember.KerbalType.Tourist;
                    KerbalRoster.SetExperienceTrait(crew, "Tourist");
                    ScreenMessages.PostScreenMessage($"{crew.name}'s tummy is growling too loudly to get any work done.", 5f, ScreenMessageStyle.UPPER_CENTER);
                }
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

        public bool TryGetStatus(ProtoCrewMember crew, out double daysSinceMeal, out double daysToGrouchy, out bool isGrouchy)
        {
            double now = Planetarium.GetUniversalTime();
            if (this.knownKerbals.TryGetValue(crew.name, out LifeSupportStatus lifeSupportStatus))
            {
                daysSinceMeal = now - lifeSupportStatus.LastMeal;
                isGrouchy = lifeSupportStatus.IsGrouchy;
                daysToGrouchy = now + timeBeforeKerbalStarves - lifeSupportStatus.LastMeal;
                return true;
            }
            else
            {
                daysSinceMeal = 0;
                daysToGrouchy = timeBeforeKerbalStarves;
                isGrouchy = false;
                return false;
            }
        }

        public void KerbalHasReachedHomeworld(ProtoCrewMember crew)
        {
            if (this.knownKerbals.TryGetValue(crew.name, out LifeSupportStatus crewStatus))
            {
                if (crewStatus.IsGrouchy)
                {
                    crew.type = ProtoCrewMember.KerbalType.Crew;
                    KerbalRoster.SetExperienceTrait(crew, crewStatus.OldTrait);
                    ScreenMessages.PostScreenMessage($"{crew.name} is starving, but can gather the strength to ring for some take-out.", 5f, ScreenMessageStyle.UPPER_CENTER);
                }
                this.knownKerbals.Remove(crew.name);
            }
        }

        public void KerbalHadASnack(ProtoCrewMember crew, double lastMealTime)
        {
            if (this.knownKerbals.TryGetValue(crew.name, out LifeSupportStatus crewStatus))
            {
                if (crewStatus.IsGrouchy)
                {
                    crew.type = ProtoCrewMember.KerbalType.Crew;
                    KerbalRoster.SetExperienceTrait(crew, crewStatus.OldTrait);
                    ScreenMessages.PostScreenMessage($"{crew.name}'s tummy is full now.", 5f, ScreenMessageStyle.UPPER_CENTER);
                }
                crewStatus.LastMeal = lastMealTime;
                crewStatus.IsGrouchy = false;
            }
            else
            {
                this.knownKerbals.Add(crew.name, new LifeSupportStatus {
                    IsGrouchy = false,
                    KerbalName = crew.name,
                    LastMeal = Planetarium.GetUniversalTime(),
                    OldTrait = null
                });
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            Dictionary<string, LifeSupportStatus> newState = new Dictionary<string, LifeSupportStatus>();
            foreach (ConfigNode perKerbalNode in node.GetNodes())
            {
                LifeSupportStatus status = new LifeSupportStatus();
                status.KerbalName = perKerbalNode.name;
                bool gotit = perKerbalNode.TryGetValue(nameof(status.IsGrouchy), ref status.IsGrouchy)
                          && perKerbalNode.TryGetValue(nameof(status.LastMeal), ref status.LastMeal)
                          && perKerbalNode.TryGetValue(nameof(status.OldTrait), ref status.OldTrait);
                if (gotit && !newState.ContainsKey(status.KerbalName))
                {
                    newState.Add(status.KerbalName, status);
                }
                else
                {
                    Debug.LogError($"Failed to add status for {perKerbalNode.name}");
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
                ConfigNode childNode = new ConfigNode(status.KerbalName);
                childNode.AddValue(nameof(status.IsGrouchy), status.IsGrouchy);
                childNode.AddValue(nameof(status.LastMeal), status.LastMeal);
                childNode.AddValue(nameof(status.OldTrait), status.OldTrait ?? "");
                node.AddNode(childNode);
            }
        }
    }
}
