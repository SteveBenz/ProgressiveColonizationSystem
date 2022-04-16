using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   This vessel module provides the GUI interaction for starting a kerbal
    ///   on a path to a new specialization.
    /// </summary>
    public class PksRetrainingModule
        : PartModule
    {
        private readonly List<RetrainingEntry> trainingInfo = new List<RetrainingEntry>();
        private double lastUpdateTime = -1;
        private RetrainingDialog dialog;

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Crew Training")]
        public void ShowTrainingUi()
        {
            // This is the base training cost - the actual cost is this cost * (1 + stupidity)
            double baseTrainingCostInSeconds = KerbalTime.KerbalDaysToSeconds(60);
            if (dialog == null)
            {
                this.dialog = RetrainingDialog.Show(trainingInfo, this.part.protoModuleCrew, baseTrainingCostInSeconds);
            }
            else
            {
                this.dialog.Redraw();
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (!node.TryGetValue(nameof(lastUpdateTime), ref this.lastUpdateTime))
            {
                this.lastUpdateTime = -1;
            }

            var trainees = node.GetNode("Trainees");
            if (trainees != null)
            {
                this.trainingInfo.Clear();
                foreach (ConfigNode child in trainees.nodes)
                {
                    if (RetrainingEntry.TryCreateFromNode(child, out var childEntry))
                    {
                        this.trainingInfo.Add(childEntry);
                    }
                    else
                    {
                        Debug.LogError("Failed to parse a Kerbal Retraining record");
                    }
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (this.trainingInfo.Count > 0)
            {
                node.AddValue(nameof(lastUpdateTime), lastUpdateTime);

                var trainingNodes = new ConfigNode("Trainees");
                int seatNo = 0;
                foreach (var entry in this.trainingInfo)
                {
                    trainingNodes.AddNode($"Desk{seatNo++}", entry.CreateNode());
                }
                node.AddNode(trainingNodes);
            }
        }

        private string oldStateMush = null;

        public void FixedUpdate()
        {
            if (this.lastUpdateTime < 0)
            {
                this.lastUpdateTime = Planetarium.GetUniversalTime();
                return;
            }

            if (FlightGlobals.ActiveVessel != this.vessel && this.dialog != null)
            {
                this.dialog.Dismiss();
            }

            var oldNow = this.lastUpdateTime;
            this.lastUpdateTime = Planetarium.GetUniversalTime();
            var elapsed = this.lastUpdateTime - oldNow;

            for (int i = 0; i < this.trainingInfo.Count; )
            {
                var entry = this.trainingInfo[i];
                var crew = this.part.protoModuleCrew.FirstOrDefault(c => c.name == entry.KerbalName);
                if (crew == null)
                {
                    // At this point, we might want to just remove the Kerbal from the list and move on.
                    // That'd be a good thing to do from the standpoint of keeping save files clean even
                    // in the face of users doing odd things, but maybe the user just takes the kerbal
                    // out for a walk...  It'd be surprising to the user that their retraining got canceled.
                    // So for now we'll just do nothing if the trainee isn't in the part.
                    ++i;
                    continue;
                }

                if (crew.experienceTrait.TypeName == "Tourist")
                {
                    // This could happen if the base ran out of food.  Skipping it means that the 
                    // training will resume once snacks have been had.
                    ++i;
                    continue;
                }

                if (crew.experienceTrait.TypeName != "Trainee")
                {
                    Debug.LogWarning($"That's odd - {entry.KerbalName} was training to be a {entry.FutureTrait}, but it looks like they already have become a {crew.experienceTrait.TypeName} -- aborting the training");
                    this.trainingInfo.RemoveAt(i);
                    continue;
                }

                entry.AddStudyTime(elapsed);
                if (entry.IsComplete)
                {
                    this.trainingInfo.RemoveAt(i);
                    if (KerbalRoster.TryGetExperienceTraitConfig(entry.FutureTrait, out var _))
                    {
                        KerbalRoster.SetExperienceTrait(crew, entry.FutureTrait);
                        ScreenMessages.PostScreenMessage(
                            message: $"{entry.KerbalName} has finished training to become a {entry.FutureTrait}!",
                            duration: 15f,
                            style: ScreenMessageStyle.UPPER_CENTER);
                    }
                    else
                    {
                        Debug.LogError($"Kerbal would graduate to an unknown career - {entry.FutureTrait}");
                    }
                }
                else
                {
                    ++i;
                }
            }

            // state munge
            if (this.dialog != null && this.dialog.IsVisible)
            {
                var currentStateMush = string.Join("", this.part.protoModuleCrew.OrderBy(c => c.name).Select(c => c.name + c.trait));
                if (this.oldStateMush != currentStateMush)
                {
                    this.oldStateMush = currentStateMush;
                    dialog.Redraw();
                }
            }
        }
    }
}
