using Experience;
using KSP.UI.Screens;
using ProgressiveColonizationSystem.ProductionChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   This class maintains a toolbar button and a GUI display that allows the user to see
    ///   into the life support status of the active vessel.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    public class LifeSupportStatusMonitor
        : PksTabbedDialog
    {
        const string ProductionTab = "Production";
        const string ProgressionTab = "Progression";
        const string TransferTab = "Transfer";
        const string CrewTab = "Crew";

        // If simulating + or - crewman, this becomes positive or negative.
        private int crewDelta = 0;
        // CrewDelta gets reset when lastActiveVessel no longer equals the current vessel.
        private Vessel lastActiveVessel;
        private string productionMessage;
        private List<ResearchData> progress;
        private bool showingWhatIfButtons;
        private bool showingResourceTransfer;
        private bool showPartsInCrewDialog = false;

        private string transferringMessage;

        internal class ResearchData
        {
            public string Category;
            public TechTier Tier;
            public double ProgressPerDay;
            public double AccumulatedProgress;
            public double NextTier;
        }

        private IntervesselResourceTransfer resourceTransfer = new IntervesselResourceTransfer();

        public LifeSupportStatusMonitor()
            : base(new string[] { ProductionTab, ProgressionTab, TransferTab, CrewTab })
        {
        }

        protected override DialogGUIBase DrawTab(string tab)
        {
            switch(tab)
            {
                default:
                case ProductionTab:
                    return DrawProductionTab();
                case ProgressionTab:
                    return DrawProgressionTab();
                case TransferTab:
                    return DrawTransferTab();
                case CrewTab:
                    return DrawCrewTab();
            }
        }

        protected override bool IsRelevant => FlightGlobals.ActiveVessel.GetCrewCount() > 0 && !FlightGlobals.ActiveVessel.isEVA;
        protected override ApplicationLauncher.AppScenes VisibleInScenes { get; } = ApplicationLauncher.AppScenes.FLIGHT;

        private DialogGUIBase DrawProductionTab()
        {
            var body = new DialogGUILabel(() => this.productionMessage);
            var whatif = new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                            new DialogGUILabel("What if we"),
                            new DialogGUIButton("Add", () => { ++crewDelta; }, () => true, false),
                            new DialogGUILabel("/"),
                            new DialogGUIButton("Remove", () => { --crewDelta; }, () => FlightGlobals.ActiveVessel.GetCrewCount() + this.crewDelta > 1, false),
                            new DialogGUILabel("a kerbal?"));
            return new DialogGUIVerticalLayout(body, new DialogGUIFlexibleSpace(), whatif);
        }

        private DialogGUIBase DrawProgressionTab()
        {
            if (this.progress.Count == 0)
            {
                return new DialogGUILabel("No technologies are being researched here.");
            }

            DialogGUIBase[] rows = new DialogGUIBase[this.progress.Count + 1];
            rows[0] = new DialogGUIHorizontalLayout(
                    new DialogGUILabel("<B><U>Field:</U></B>", 170),
                    new DialogGUILabel("<B><U>Progress:</U></B>", 70),
                    new DialogGUILabel("<B><U>Days To Go:</U></B>", 70)
                );
            
            for (int i = 0; i < this.progress.Count; ++i)
            {
                var field = this.progress[i];
                double progress = field.AccumulatedProgress / field.NextTier;
                double daysToGo = (field.NextTier - field.AccumulatedProgress) / field.ProgressPerDay;
                rows[i + 1] = new DialogGUIHorizontalLayout(
                    new DialogGUILabel(field.Category, 170),
                    new DialogGUILabel($"{100*progress:N}%", 70),
                    new DialogGUILabel(daysToGo.ToString("N"), 70));
            }

            return new DialogGUIVerticalLayout(rows);
        }

        private DialogGUIBase DrawTransferTab()
        {
            if (FlightGlobals.ActiveVessel.situation != Vessel.Situations.LANDED)
            {
                return new DialogGUILabel("Resource transfer only works for landed vessels.");
            }

            List<Vessel> candidates = FlightGlobals.VesselsLoaded
                .Where(v => v != FlightGlobals.ActiveVessel && v.situation == Vessel.Situations.LANDED)
                .ToList();

            if (!candidates.Any())
            {
                return new DialogGUILabel("No nearby vessels to trade with.");
            }

            var vertical = new DialogGUIVerticalLayout();
            if (candidates.Count == 1)
            {
                this.resourceTransfer.TargetVessel = candidates[0];
                this.OnTargetChanged();
                vertical.AddChild(new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                            new DialogGUILabel("Target: "),
                            new DialogGUILabel(resourceTransfer.TargetVessel?.GetDisplayName())));
            }
            else
            {
                vertical.AddChild(new DialogGUILabel("Target:"));
                vertical.AddChildren(candidates
                    .Select(c =>
                        new DialogGUIHorizontalLayout(
                            new DialogGUISpace(10),
                            new DialogGUIToggle(
                                set: () => (c == resourceTransfer.TargetVessel), // actually more of an isSet
                                lbel: c.vesselName, //  label
                                selected: (value) => OnTargetShipClicked(c, value)))
                        {
                            OptionInteractableCondition = () => !resourceTransfer.IsTransferUnderway || resourceTransfer.IsTransferComplete
                        })
                    .ToArray());
            }

            vertical.AddChild(
                new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                        new DialogGUIButton("Start", resourceTransfer.StartTransfer, () => resourceTransfer.TargetVessel != null && !resourceTransfer.IsTransferUnderway, dismissOnSelect: false),
                        new DialogGUISlider(() => (float)resourceTransfer.TransferPercent, 0, 1, false, 100, 20, null)));
            vertical.AddChild(new DialogGUILabel(() => this.transferringMessage));

            return vertical;
        }

        private void OnTargetShipClicked(Vessel c, bool value)
        {
            if (!value
             || (resourceTransfer.IsTransferUnderway && !resourceTransfer.IsTransferComplete)
             || resourceTransfer.TargetVessel == c)
            {
                return;
            }

            resourceTransfer.TargetVessel = c;
            resourceTransfer.Reset();
            this.OnTargetChanged();
        }

        private void OnTargetChanged()
        {
            IntervesselResourceTransfer.TryFindResourceToTransfer(
                FlightGlobals.ActiveVessel,
                resourceTransfer.TargetVessel,
                out var toSend,
                out var toRecieve);
            if (!toSend.Any() && !toRecieve.Any())
            {
                this.transferringMessage = "Nothing to transfer";
                return;
            }

            string text = "";
            if (toSend.Any())
            {
                text = "<B>Sending:</B> " + string.Join(", ", toSend.Keys.OrderBy(k => k).ToArray());
            }

            if (toRecieve.Any())
            {
                if (text != "")
                {
                    text += "\r\n";
                }
                text += "<B>Receiving:</B> " + string.Join(", ", toRecieve.Keys.OrderBy(k => k).ToArray());
            }

            this.transferringMessage = text;
        }

        const float NumberColumnWidth = 50;
        const float ProfessionColumnWidth = 100;


        private class RequirementAndQuantity
        {
            public string Trait;
            public int SkillLevel;
            public double Quantity;
            public string[] PartNames;

            public IEnumerable<DialogGUIBase> GetTraitDescription(bool showParts, List<ProtoCrewMember> crew)
            {
                List<ExperienceTraitConfig> careers = GameDatabase.Instance.ExperienceConfigs
                    .GetTraitsWithEffect(this.Trait)
                    .Select(name => GameDatabase.Instance.ExperienceConfigs.GetExperienceTraitConfig(name))
                    .ToList();

                //  #Needed    Specialist   #Crew    Generalist   #Crew
                //  1.5        Miner        2        3*Engineer   0
                //    Scrounger Drill-

                var requirementRow = new DialogGUIHorizontalLayout();

                requirementRow.AddChild(new DialogGUILabel(this.Quantity.ToString("N1"), width: NumberColumnWidth));
                for (int i = 0; i < careers.Count; ++i)
                {
                    ExperienceEffectConfig effectConfig = careers[i].Effects.First(effect => effect.Name == this.Trait);
                    var levelString = effectConfig.Config.GetValue("level");
                    int numStars = SkillLevel - int.Parse(levelString ?? "0");

                    requirementRow.AddChild(new DialogGUILabel(
                        PksCrewRequirement.DescribeKerbalTrait(numStars, careers[i].Title), ProfessionColumnWidth));
                    requirementRow.AddChild(new DialogGUILabel(
                        $"{crew.Count(c => c.trait == careers[i].Title && c.experienceLevel >= numStars)}",
                        width: NumberColumnWidth));
                }
                yield return requirementRow;

                if (showParts)
                {
                    var partsRow = new DialogGUIHorizontalLayout();
                    partsRow.AddChild(new DialogGUISpace(15));
                    partsRow.AddChild(new DialogGUILabel($"<I>{string.Join(", ", this.PartNames)}</I>"));
                    yield return partsRow;
                }
            }
        }

        private DialogGUIBase DrawCrewTab()
        {
            List<PksCrewRequirement> activatedParts = FlightGlobals.ActiveVessel
                .FindPartModulesImplementing<PksCrewRequirement>()
                .Where(p => p.IsRunning)
                .ToList();
            List<ProtoCrewMember> kspCrew = FlightGlobals.ActiveVessel.GetVesselCrew();
            var snackConsumption = FlightGlobals.ActiveVessel.vesselModules
                .OfType<SnackConsumption>().FirstOrDefault();
            bool needsRoverPilot = !string.IsNullOrEmpty(snackConsumption?.supplierMinerCraftId);

            return DrawCrewDialog(
                activatedParts,
                kspCrew,
                needsRoverPilot,
                () => this.showPartsInCrewDialog,
                (bool value) =>
                {
                    this.showPartsInCrewDialog = value;
                    this.Redraw();
                });
        }

        internal static DialogGUIBase DrawCrewDialog(
            List<PksCrewRequirement> activatedParts,
            List<ProtoCrewMember> kspCrew,
            bool needsRoverPilot,
            Func<bool> getIsShowingParts,
            Action<bool> setIsShowingParts)
        {
            // The factoring on this method is, well, forced...  But it's straightforward to do.  I'm not
            // clear on what the factoring really should be right now -- it seems to me that the crew management
            // could be split into its own mod.  If you did that, then it'd be worth it to consider whether
            // the same dialog applies to EDITOR and FLIGHT scenarios...  Then you wouldn't have a problem.

            // TODO:
            //   Make it list the redundant crew
            List<DialogGUIBase> rows = new List<DialogGUIBase>();

            rows.Add(new DialogGUIHorizontalLayout(
                    new DialogGUILabel("<B><U>#Rqd</U></B>", NumberColumnWidth),
                    new DialogGUILabel("<B><U>Specialist</U></B>", ProfessionColumnWidth),
                    new DialogGUILabel("<B><U>#Avail</U></B>", NumberColumnWidth),
                    new DialogGUILabel("<B><U>Generalist</U></B>", ProfessionColumnWidth),
                    new DialogGUILabel("<B><U>#Avail</U></B>", NumberColumnWidth)
                ));


            rows.AddRange(activatedParts
                .GroupBy(p => p.RequiredEffect + p.RequiredLevel.ToString())
                .OrderBy(g => g.Key)
                .ToArray()
                .Select(g => new RequirementAndQuantity()
                {
                    Trait = g.First().RequiredEffect,
                    SkillLevel = g.First().RequiredLevel,
                    PartNames = g.OrderBy(p => p.part.partInfo.title)
                                 .GroupBy(p => p.part.partInfo.title)
                                 .Select(p => $"{p.Count()}x{p.Key}")
                                 .ToArray(),
                    Quantity = g.Sum(p => p.CapacityRequired)
                }).
                Union(needsRoverPilot
                    ? new RequirementAndQuantity[] {
                        new RequirementAndQuantity
                        {
                            PartNames = new string[] { "Crushins Rover" },
                            Quantity = 1,
                            SkillLevel = 0,
                            Trait = "FullVesselControlSkill"
                        }
                    }
                    : new RequirementAndQuantity[0])
                .SelectMany(randq => randq.GetTraitDescription(getIsShowingParts(), kspCrew)));
            rows.Add(new DialogGUIFlexibleSpace());
            rows.Add(new DialogGUIToggle(getIsShowingParts(), "Show parts", (x) => setIsShowingParts(!getIsShowingParts())));

            return new DialogGUIVerticalLayout(rows.ToArray());
        }

        protected override MultiOptionDialog DrawDialog(Rect rect)
        {
            // FYI, if you want to override a style, here'd be a way to do it:
            // var myStyle = new UIStyle(UISkinManager.defaultSkin.label) { wordWrap = false};
            //
            // Too bad wordWrap doesn't get paid attention to.
            return new MultiOptionDialog(
                        "LifeSupportMonitor",  // <- no idea what this does.
                        "",
                        "Colony Status",
                        HighLogic.UISkin,
                        rect,
                        DrawTabbedDialog());
        }

        protected override void OnFixedUpdate()
        {
            resourceTransfer.OnFixedUpdate();

            if (this.lastActiveVessel != FlightGlobals.ActiveVessel)
            {
                this.crewDelta = 0;
                this.Redraw();
            }

            this.lastActiveVessel = FlightGlobals.ActiveVessel;
            var activeSnackConsumption = FlightGlobals.ActiveVessel?.GetComponent<SnackConsumption>();
            if (activeSnackConsumption == null)
            {
                // Shouldn't happen, but just in case...
                return;
            }

            int crewCount = FlightGlobals.ActiveVessel.GetCrewCount();
            if ((crewCount > 0) != this.showingWhatIfButtons)
            {
                this.showingWhatIfButtons = (crewCount > 0);
                this.Redraw();
            }

            if ((resourceTransfer.TargetVessel != null) != this.showingResourceTransfer)
            {
                this.showingResourceTransfer = (resourceTransfer.TargetVessel != null);
                this.Redraw();
            }

            activeSnackConsumption.ResourceQuantities(out var availableResources, out var availableStorage);
            List<ITieredProducer> tieredProducers = activeSnackConsumption.Vessel.FindPartModulesImplementing<ITieredProducer>();
            List<ITieredCombiner> tieredCombiners = activeSnackConsumption.Vessel.FindPartModulesImplementing<ITieredCombiner>();

            bool isHookedUp = false;
            string minerStatusMessage = null;
            FlightGlobals.ActiveVessel.vesselModules
                .OfType<SnackConsumption>()
                .FirstOrDefault()
                ?.GetMinerStatusMessage(out isHookedUp, out minerStatusMessage);

            BuildStatusStrings(isHookedUp, activeSnackConsumption, availableResources, availableStorage, tieredProducers, tieredCombiners, crewCount, crewDelta
                , out string productionMessage, out List<ResearchData> progress);
            this.productionMessage = (minerStatusMessage == null ? "" : minerStatusMessage + "\r\n\r\n") + productionMessage;
            this.progress = progress;
        }

        internal static void BuildStatusStrings(
            bool hasAutoCrushinSupply,
            SnackConsumption activeSnackConsumption,
            Dictionary<string, double> resources,
            Dictionary<string, double> storage,
            List<ITieredProducer> tieredProducers,
            List<ITieredCombiner> tieredCombiners,
            int crewCount,
            int crewDelta,
            out string productionMessage,
            out List<ResearchData> progress)
        {
            StringBuilder productionMessageBuilder = new StringBuilder();

            ResearchSink researchSink = new ResearchSink();
            TieredProduction.CalculateResourceUtilization(
                crewCount + crewDelta, 1, tieredProducers, tieredCombiners, researchSink, resources, storage,
                out double timePassed, out var _, out Dictionary<string, double> resourcesConsumed,
                out Dictionary<string, double> resourcesProduced);
            if (timePassed == 0)
            {
                productionMessageBuilder.AppendLine("There aren't enough supplies or producers here to feed any kerbals.");
                progress = new List<ResearchData>();

                if (!activeSnackConsumption.IsAtHome)
                {
                    Dictionary<int, List<ProtoCrewMember>> buckets = new Dictionary<int, List<ProtoCrewMember>>();
                    foreach (var crew in activeSnackConsumption.Vessel.GetVesselCrew())
                    {
                        var kerbalIsKnown = LifeSupportScenario.Instance.TryGetStatus(crew, out double daysSinceMeal, out double daysToGrouchy, out bool isGrouchy);
                        if (!kerbalIsKnown)
                        {
                            // Maybe if ! on kerban we complain about this?
                            // Debug.LogError($"Couldn't find a life support record for {crew.name}");
                        }

                        int bucketKey = isGrouchy ? -1 : (int)daysToGrouchy;
                        if (!buckets.TryGetValue(bucketKey, out var crewInBucket))
                        {
                            crewInBucket = new List<ProtoCrewMember>();
                            buckets.Add(bucketKey, crewInBucket);
                        }
                        crewInBucket.Add(crew);
                    }

                    CrewBlurbs.random = new System.Random(FlightGlobals.ActiveVessel.GetHashCode());
                    foreach (List<ProtoCrewMember> crewInBucket in buckets.Values)
                    {
                        // yeah yeah, recomputing this is wasteful & all...
                        LifeSupportScenario.Instance.TryGetStatus(crewInBucket[0], out double daysSinceMeal, out double daysToGrouchy, out bool isGrouchy);
                        if (isGrouchy)
                        {
                            productionMessageBuilder.AppendLine(CrewBlurbs.StarvingKerbals(crewInBucket));
                        }
                        else if (daysToGrouchy < 2)
                        {
                            productionMessageBuilder.AppendLine(CrewBlurbs.GrumpyKerbals(crewInBucket, daysToGrouchy, tieredProducers.Any()));
                        }
                        else
                        {
                            productionMessageBuilder.AppendLine(CrewBlurbs.HungryKerbals(crewInBucket, daysToGrouchy, tieredProducers.Any()));
                        }
                    }
                }
            }
            else
            {
                if (crewDelta == 0)
                {
                    productionMessageBuilder.AppendLine($"To sustain its crew of {crewCount + crewDelta}, this vessel is using:");
                }
                else
                {
                    productionMessageBuilder.AppendLine($"To sustain a crew of {crewCount + crewDelta} this vessel would use:");
                }

                foreach (var resourceName in resourcesConsumed.Keys.OrderBy(n => n))
                {
                    if (!IsCrushinResource(researchSink, resourceName))
                    {
                        double perDay = TieredProduction.UnitsPerSecondToUnitsPerDay(resourcesConsumed[resourceName]);
                        double daysLeft = resources[resourceName] / perDay;
                        productionMessageBuilder.AppendLine($"{perDay:N1} {resourceName} per day ({daysLeft:N1} days left)");
                    }
                }

                if (resourcesProduced != null && resourcesProduced.Count > 0)
                {
                    productionMessageBuilder.AppendLine();
                    productionMessageBuilder.AppendLine("The crew is also producing:");
                    foreach (var resourceName in resourcesProduced.Keys.OrderBy(n => n))
                    {
                        double perDay = TieredProduction.UnitsPerSecondToUnitsPerDay(resourcesProduced[resourceName]);
                        productionMessageBuilder.AppendLine($"{perDay:N1} {resourceName} per day");
                    }
                }

                progress = researchSink.Data
                    .Select(pair => new ResearchData()
                    {
                        Category = pair.Key.DisplayName,
                        NextTier = pair.Value.KerbalDaysRequired,
                        AccumulatedProgress = pair.Value.AccumulatedKerbalDays,
                        ProgressPerDay = pair.Value.KerbalDaysContributedPerDay
                    })
                    .ToList();
            }

            productionMessage = productionMessageBuilder.ToString();
        }

        private static bool IsCrushinResource(IColonizationResearchScenario researchScenario, string resourceName)
        {
            return researchScenario.TryParseTieredResourceName(resourceName, out TieredResource tieredResource, out var _)
                && tieredResource == researchScenario.CrushInsResource;
        }
    }
}
