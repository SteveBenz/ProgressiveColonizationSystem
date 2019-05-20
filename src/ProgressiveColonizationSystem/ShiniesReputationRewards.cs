using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ShiniesReputationRewards
        : MonoBehaviour
    {
        private bool eventsAreAttached = false;
        public void Start()
        {
            if (!eventsAreAttached && HighLogic.CurrentGame?.Mode == Game.Modes.CAREER && (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION))
            {
                AddEvents();
                eventsAreAttached = true;
            }
        }

        public void OnDisable()
        {
            if (eventsAreAttached)
            {
                RemoveEvents();
                eventsAreAttached = false;
            }
        }

        private void AddEvents()
        {
            GameEvents.onVesselRecoveryProcessing.Add(OnVesselRecoveryProcessing);
        }

        private void RemoveEvents()
        {
            GameEvents.onVesselRecoveryProcessing.Remove(OnVesselRecoveryProcessing);
        }

        private void OnVesselRecoveryProcessing(ProtoVessel pv, MissionRecoveryDialog dlg, float idunno)
        {
            List<MessageSystem.Message> messages = new List<MessageSystem.Message>();
            foreach (var group in pv.protoPartSnapshots.SelectMany(p => p.resources).GroupBy(r => r.resourceName))
            {
                if (ColonizationResearchScenario.Instance.TryParseTieredResourceName(group.Key, out TieredResource tieredResource, out TechTier tier))
                {
                    float amount = (float)group.Sum(r => r.amount);
                    float repGain = tieredResource.GetReputationGain(tier, amount);
                    if (repGain > 0)
                    {
                        dlg.reputationEarned += repGain;
                        MessageSystem.Instance.AddMessage(new MessageSystem.Message(
                            "Shinies!", $"You get {repGain:N} reputation for selling {amount:N} {tier.DisplayName()} {tieredResource.DisplayName}.",
                            MessageSystemButton.MessageButtonColor.GREEN, MessageSystemButton.ButtonIcons.ACHIEVE));
                    }
                }
            }
        }
    }
}
