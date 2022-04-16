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
        private List<ResearchData> progress;
        private bool showingWhatIfButtons;
        private bool showingResourceTransfer;
        private bool showPartsInCrewDialog = false;

        private string productionMessage;
        private string introLineMessage;
        private string unusedCapacityMessage;
        private string limitedByMessage;

        private readonly Dictionary<string, TransferDirection> userOverrides = new Dictionary<string, TransferDirection>();

        private IntervesselResourceTransfer resourceTransfer = new IntervesselResourceTransfer();

        /// <summary>
        ///   Published in order that we can cooperate with <see cref="PksToolbar"/>
        /// </summary>
        private static LifeSupportStatusMonitor instance;

        public LifeSupportStatusMonitor()
            : base(new string[] { ProductionTab, ProgressionTab, TransferTab, CrewTab })
        {
            instance = this;
        }

        public static void ToggleDialogVisibility()
        {
            instance?.ToggleVisibility();
        }

        public static void ShowDialog()
        {
            instance?.Show();
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

        protected override bool IsRelevant => LifeSupportStatusMonitor.IsRelevant_static;

        public static bool IsRelevant_static =>
               !FlightGlobals.ActiveVessel.isEVA
               && (FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED
                || FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED
                || FlightGlobals.ActiveVessel.GetCrewCount() > 0);

        protected override ApplicationLauncher.AppScenes VisibleInScenes { get; } = ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW;

        private DialogGUIBase DrawProductionTab()
        {
            return new DialogGUIVerticalLayout(
                new DialogGUILabel(() => this.introLineMessage),
                new DialogGUISpace(5),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(
                        () => $"{TextEffects.DialogHeading("Production/day:")}\r\n{this.productionMessage ?? "<none>"}"),
                    new DialogGUISpace(30),
                    new DialogGUILabel(() => this.unusedCapacityMessage == null
                        ? ""
                        : $"{TextEffects.DialogHeading("Unused Capacity:")}\r\n{this.unusedCapacityMessage}")),
                new DialogGUISpace(5),
                new DialogGUILabel(() => this.limitedByMessage == null
                    ? ""
                    : $"{TextEffects.DialogHeading("Limited by:")} {this.limitedByMessage}"),
                new DialogGUIFlexibleSpace(),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                    new DialogGUILabel("What if we"),
                    new DialogGUIButton("Add", () => { ++crewDelta; }, () => true, false),
                    new DialogGUILabel("/"),
                    new DialogGUIButton("Remove", () => { --crewDelta; }, () => FlightGlobals.ActiveVessel.GetCrewCount() + this.crewDelta > 1, false),
                    new DialogGUILabel("a kerbal?")));
        }

        private DialogGUIBase DrawProgressionTab()
        {
            if (this.progress.Count == 0)
            {
                return new DialogGUILabel("No technologies are being researched here.");
            }

            DialogGUIBase[] rows = new DialogGUIBase[this.progress.Count + 1];
            rows[0] = new DialogGUIHorizontalLayout(
                    new DialogGUILabel(TextEffects.DialogHeading("Researching:"), 160),
                    new DialogGUILabel(TextEffects.DialogHeading("Progress:"), 70),
                    new DialogGUILabel(TextEffects.DialogHeading("Notes:"), 140)
                );
            
            for (int i = 0; i < this.progress.Count; ++i)
            {
                var field = this.progress[i];

                string fieldName = field.IsAtMaxTier
                    ? field.Category.DisplayName
                    : $"{field.TierBeingResearched.DisplayName()} {field.Category.DisplayName}";
                if (this.progress.Any(p => p != field && p.Category == field.Category))
                {
                    fieldName += $"({field.AtBody})";
                }

                string progressText = field.HasProgress ? $"{100 * field.AccumulatedKerbalDays / field.KerbalDaysRequired:N}%" : "-";
                string notes = field.KerbalDaysContributedPerDay > 0
                    ? $"{(field.KerbalDaysRequired - field.AccumulatedKerbalDays) / field.KerbalDaysContributedPerDay:N} days to go"
                    : field.WhyBlocked;

                rows[i + 1] = new DialogGUIHorizontalLayout(
                    new DialogGUILabel(fieldName, 160),
                    new DialogGUILabel(progressText, 70),
                    new DialogGUILabel(notes, 140));
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
                        new DialogGUIButton(
                            "Start",
                            () => resourceTransfer.StartTransfer(this.userOverrides),
                            () => this.resourceTransfer.TargetVessel != null && !this.resourceTransfer.IsTransferUnderway,
                            dismissOnSelect: false),
                        new DialogGUISlider(() => (float)this.resourceTransfer.TransferPercent, 0, 1, false, 100, 20, (f) => { }),
                        new DialogGUIButton(
                            "Refresh",
                            () => { this.OnTargetChanged(); this.resourceTransfer.Reset(); },
                            () => this.resourceTransfer.TargetVessel != null && (this.resourceTransfer.IsTransferComplete || !this.resourceTransfer.IsTransferUnderway),
                            dismissOnSelect: false),
                        new DialogGUIButton(
                            "Abort",
                            () => this.resourceTransfer.Reset(),
                            () => this.resourceTransfer.TargetVessel != null && this.resourceTransfer.IsTransferUnderway && !this.resourceTransfer.IsTransferComplete,
                            dismissOnSelect: false)
                ));

            if (resourceTransfer.TargetVessel != null)
            {
                var toTransfer = IntervesselResourceTransfer.TryFindResourceToTransfer(FlightGlobals.ActiveVessel, resourceTransfer.TargetVessel);

                if (!toTransfer.Any())
                {
                    vertical.AddChild(new DialogGUILabel("Nothing to transfer"));
                }
                else
                {
                    foreach (var item in toTransfer.OrderBy(i => i.ResourceName))
                    {

                        float getSettingAsFloat()
                        {
                            if (!this.userOverrides.TryGetValue(item.ResourceName, out var direction))
                            {
                                direction = item.SuggestedDirection;
                            }
                            switch (direction) {
                                case TransferDirection.Receive:
                                    return -1;
                                case TransferDirection.Neither:
                                default:
                                    return 0;
                                case TransferDirection.Send:
                                    return 1;
                            }
                        }
                        void setSetting(float value)
                        {
                            var direction = value < 0
                                ? TransferDirection.Receive
                                : (value > 0 ? TransferDirection.Send : TransferDirection.Neither);
                            if (!((direction == TransferDirection.Receive && item.MaxCanReceive == 0)
                                || (direction == TransferDirection.Send && item.MaxCanSend ==0)))
                            {
                                this.userOverrides[item.ResourceName] = direction;
                            }
                        }

                        string getSettingAsString()
                        {
                            if (!this.userOverrides.TryGetValue(item.ResourceName, out var direction))
                            {
                                direction = item.SuggestedDirection;
                            }
                            switch (direction)
                            {
                                case TransferDirection.Receive:
                                    return "Take";
                                case TransferDirection.Neither:
                                default:
                                    return "Hold";
                                case TransferDirection.Send:
                                    return "Send";
                            }
                        }

                        vertical.AddChild(
                            new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                                new DialogGUILabel(item.ResourceName + ":"),
                                new DialogGUISlider(getSettingAsFloat, -1, 1, true, 100, 20, setSetting)
                                {
                                    OptionInteractableCondition = () => !resourceTransfer.IsTransferUnderway || resourceTransfer.IsTransferComplete
                                },
                                new DialogGUILabel(getSettingAsString)
                            ));
                    }
                }
            }

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
            this.userOverrides.Clear();
            this.Redraw();
        }

        const float NumberColumnWidth = 50;
        const float ProfessionColumnWidth = 100;


        private class RequirementAndQuantity
        {
            public string Trait;
            public int SkillLevel;
            public double TotalQuantity;
            public double UncrewedQuantity;
            public double DisabledQuantity;
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

                string quantity = UncrewedQuantity + DisabledQuantity > 0.001
                    ? $"{this.TotalQuantity - UncrewedQuantity - DisabledQuantity:N1}/{this.TotalQuantity:N1}"
                    : this.TotalQuantity.ToString("N1");
                if (UncrewedQuantity > 0.001)
                {
                    quantity = TextEffects.Red(quantity);
                }
                else if (DisabledQuantity > 0.001)
                {
                    quantity = TextEffects.Yellow(quantity);
                }
                requirementRow.AddChild(new DialogGUILabel(quantity, width: NumberColumnWidth));
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
            List<PksCrewRequirement> crewedParts,
            List<ProtoCrewMember> kspCrew,
            bool needsRoverPilot,
            Func<bool> getIsShowingParts,
            Action<bool> setIsShowingParts)
        {
            // The factoring on this method is, well, forced...  But it's straightforward to do.  I'm not
            // clear on what the factoring really should be right now -- it seems to me that the crew management
            // could be split into its own mod.  If you did that, then it'd be worth it to consider whether
            // the same dialog applies to EDITOR and FLIGHT scenarios...  Then you wouldn't have a problem.

            // It'd be nice if this thing could show you directly how many redundant crew and which crew are
            // redundant, but given the specialist/generalist thing, there's no good way to do this that I can
            // think of.
            List<DialogGUIBase> rows = new List<DialogGUIBase>();

            rows.Add(new DialogGUIHorizontalLayout(
                    new DialogGUILabel(TextEffects.DialogHeading("#Crew"), NumberColumnWidth),
                    new DialogGUILabel(TextEffects.DialogHeading("Specialist"), ProfessionColumnWidth),
                    new DialogGUILabel(TextEffects.DialogHeading("#Avail"), NumberColumnWidth),
                    new DialogGUILabel(TextEffects.DialogHeading("Generalist"), ProfessionColumnWidth),
                    new DialogGUILabel(TextEffects.DialogHeading("#Avail"), NumberColumnWidth)
                ));

            rows.AddRange(crewedParts
                .GroupBy(p => p.RequiredEffect + p.RequiredLevel.ToString())
                .OrderBy(g => g.Key)
                .ToArray()
                .Select(g => new RequirementAndQuantity()
                {
                    Trait = g.First().RequiredEffect,
                    SkillLevel = g.First().RequiredLevel,
                    PartNames = g.OrderBy(p => p.part.partInfo.title)
                                 .GroupBy(p => p.part.partInfo.title)
                                 .Select(p => MakePartCountString(p.Key, p))
                                 .ToArray(),
                    TotalQuantity = g.Sum(p => p.CapacityRequired),
                    DisabledQuantity = g.Where(p => !p.IsRunning).Sum(p => p.CapacityRequired),
                    UncrewedQuantity = g.Where(p => p.IsRunning && !p.IsStaffed).Sum(p => p.CapacityRequired),
                }).
                Union(needsRoverPilot
                    ? new RequirementAndQuantity[] {
                        new RequirementAndQuantity
                        {
                            PartNames = new string[] { "Crushins Rover" },
                            TotalQuantity = 1,
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

        private static string MakePartCountString(string partName, IEnumerable<PksCrewRequirement> parts)
        {
            var partsArray = parts.ToArray();
            int numUncrewed = parts.Count(p => p.IsRunning && !p.IsStaffed);
            int numTurnedOff = parts.Count(p => !p.IsRunning);
            if (numUncrewed == 0 && numTurnedOff == 0)
            {
                return MakePartAndCountString(partsArray.Length, partName);
            }
            else if (numUncrewed > 0 && numTurnedOff == 0)
            {
                return $"{MakePartAndCountString(partsArray.Length - numUncrewed, partName)} {TextEffects.Red($"({numUncrewed} unstaffed)")}";
            }
            else if (numUncrewed == 0 && numTurnedOff == partsArray.Length)
            {
                return TextEffects.Yellow($"{MakePartAndCountString(numTurnedOff, partName)} (disabled)");
            }
            else if (numUncrewed == 0 && numTurnedOff > 0)
            {
                return $"{MakePartAndCountString(partsArray.Length - numTurnedOff, partName)} {TextEffects.Yellow($"({numTurnedOff} disabled)")}";
            }
            else
            {
                return $"{MakePartAndCountString(partsArray.Length - numUncrewed - numTurnedOff, partName)} {TextEffects.Red($"({numUncrewed} unstaffed, {numTurnedOff} disabled)")}";
            }
        }

        private static string MakePartAndCountString(int count, string partName)
            => count == 1 ? partName : $"{count}x{partName}";

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
            bool anyCrewDeficiencies = false;
            bool anyDisabledParts = false;
            foreach (var cr in FlightGlobals.ActiveVessel.FindPartModulesImplementing<PksCrewRequirement>())
            {
                if (!cr.IsStaffed && cr.IsRunning)
                {
                    anyCrewDeficiencies = true;
                }
                else if (!cr.IsRunning)
                {
                    anyDisabledParts = true;
                }
            }

            BuildStatusStrings(isHookedUp, minerStatusMessage, anyCrewDeficiencies, anyDisabledParts
                , activeSnackConsumption, availableResources, availableStorage
                , tieredProducers, tieredCombiners, crewCount, crewDelta
                , out this.introLineMessage, out this.productionMessage, out this.unusedCapacityMessage
                , out this.limitedByMessage, out List<ResearchData> progress);
            this.progress = progress;
        }

        internal static void BuildStatusStrings(
            bool isAutoMining,
            string minerStatusMessage,
            bool anyCrewDeficiencies,
            bool anyDisabledParts,
            SnackConsumption activeSnackConsumption,
            Dictionary<string, double> resources,
            Dictionary<string, double> storage,
            List<ITieredProducer> tieredProducers,
            List<ITieredCombiner> tieredCombiners,
            int crewCount,
            int crewDelta,
            out string introLineMessage,
            out string productionMessage,
            out string unusedCapacityMessage,
            out string limitedByMessage,
            out List<ResearchData> progress)
        {
            ResearchSink researchSink = new ResearchSink();
            TieredProduction.CalculateResourceUtilization(
                crewCount + crewDelta, 1, tieredProducers, tieredCombiners, researchSink, resources, storage,
                out double timePassed, out var _, out Dictionary<string, double> resourcesConsumed,
                out Dictionary<string, double> resourcesProduced,
                out IEnumerable<string> limitingResources,
                out Dictionary<string, double> unusedProduction);

            if (timePassed == 0)
            {
                var introMessageBuilder = new StringBuilder();

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
                            introMessageBuilder.AppendLine(TextEffects.Red(CrewBlurbs.StarvingKerbals(crewInBucket)));
                        }
                        else if (daysToGrouchy < 2)
                        {
                            introMessageBuilder.AppendLine(CrewBlurbs.GrumpyKerbals(crewInBucket, daysToGrouchy, tieredProducers.Any()));
                        }
                        else
                        {
                            introMessageBuilder.AppendLine(CrewBlurbs.HungryKerbals(crewInBucket, daysToGrouchy, tieredProducers.Any()));
                        }
                    }

                    if (tieredProducers.Any())
                    {
                        introMessageBuilder.AppendLine();
                        introMessageBuilder.AppendLine("Nothing can be produced at this base until the snack situation gets fixed.");
                    }
                }

                introLineMessage = introMessageBuilder.ToString();
                productionMessage = null;
                unusedCapacityMessage = null;
                limitedByMessage = null;
                progress = new List<ResearchData>();
            }
            else if (crewCount == 0)
            {
                introLineMessage = "No kerbals are aboard to produce anything.";
                productionMessage = null;
                unusedCapacityMessage = null;
                limitedByMessage = null;
                progress = new List<ResearchData>();
            }
            else
            {
                if (resourcesConsumed.Any())
                {
                    var consumptionBuilder = new StringBuilder();
                    if (minerStatusMessage != null)
                    {
                        consumptionBuilder.AppendLine(minerStatusMessage);
                        consumptionBuilder.AppendLine();
                    }

                    consumptionBuilder.AppendLine(TextEffects.DialogHeading(crewDelta == 0
                        ? $"The crew of {crewCount + crewDelta} is using:"
                        : $"A crew of {crewCount + crewDelta} would use:"));
                    foreach (var resourceName in resourcesConsumed.Keys.OrderBy(n => n))
                    {
                        if (!isAutoMining || !IsCrushinResource(researchSink, resourceName))
                        {
                            double perDay = KerbalTime.UnitsPerSecondToUnitsPerDay(resourcesConsumed[resourceName]);
                            double daysLeft = resources[resourceName] / perDay;
                            consumptionBuilder.AppendLine($"{perDay:N1} {resourceName} per day ({daysLeft:N1} days left)");
                        }
                    }

                    introLineMessage = consumptionBuilder.ToString();
                }
                else
                {
                    introLineMessage = $"This vessel can sustain a crew of {crewCount + crewDelta} indefinitely.";
                }

                if (resourcesProduced != null && resourcesProduced.Count > 0)
                {
                    var productionMessageBuilder = new StringBuilder();
                    foreach (var resourceName in resourcesProduced.Keys.OrderBy(n => n))
                    {
                        double perDay = KerbalTime.UnitsPerSecondToUnitsPerDay(resourcesProduced[resourceName]);
                        productionMessageBuilder.AppendLine($"{perDay:N1} {resourceName}");
                    }

                    productionMessage = productionMessageBuilder.ToString();
                }
                else
                {
                    productionMessage = null;
                }

                // Because of the way PksTieredCombiners work, we'll often end up with the non-tiered stuff
                // showing up as a rate-limiter.  While it's technically correct, it's not going to be a thing
                // that the player will want to know about.
                var localParts = ColonizationResearchScenario.Instance.TryGetTieredResourceByName("LocalParts");
                if (localParts != null)
                {
                    foreach (TechTier t in Enum.GetValues(typeof(TechTier)))
                    {
                        unusedProduction.Remove(localParts.TieredName(t));
                    }
                }

                unusedCapacityMessage = unusedProduction.Any()
                    ? string.Join("\r\n", unusedProduction.Select(pair => $"{pair.Value:N1} {pair.Key}").ToArray())
                    : null;

                List<string> shortfalls = new List<string>();
                if (anyCrewDeficiencies)
                {
                    shortfalls.Add("uncrewed parts");
                }
                if (anyDisabledParts)
                {
                    shortfalls.Add("disabled parts");
                }
                shortfalls.AddRange(limitingResources);
                shortfalls.AddRange(tieredCombiners
                    .Where(tc => tc.IsProductionEnabled)
                    .Select(tc => tc.NonTieredOutputResourceName)
                    .Where(resourceName => !storage.ContainsKey(resourceName))
                    .Distinct()
                    .Select(resourceName => $"storage for {resourceName}"));
                shortfalls.AddRange(tieredProducers
                    .Where(tp => tp.IsProductionEnabled && tp.Output.CanBeStored)
                    .Select(tp => tp.Output.TieredName(tp.Tier))
                    .Where(resourceName => !storage.ContainsKey(resourceName))
                    .Distinct()
                    .Select(resourceName => $"storage for {resourceName}"));

                limitedByMessage = shortfalls.Count == 0 ? null : string.Join(", ", shortfalls.ToArray());

                var allResearchEntries = researchSink.ResearchData;
                // allResearchEntries now contains all the research that's actively going on.
                // The next block adds any research categories that are not represented here because
                // they are in non-functioning modules.
                foreach (var producersByCategory in tieredProducers
                    // Only look at producers that are properly set up...
                    .Where(tp => tp.Output.ProductionRestriction != ProductionRestriction.Space || tp.Body != null)
                    // ...and aren't accounted for already
                    .Where(tp => !researchSink.ResearchData.Any(rc => tp.Output.ResearchCategory == rc.Category && tp.Body == rc.AtBody))
                    .GroupBy(tp => tp.Output.ResearchCategory))
                {
                    // Scanning is unique in that one vessel can research scanning for more than one
                    // body, thus this code looks to see if the resulting group can be further subdivided
                    // by body.
                    var producersByPlanet = producersByCategory.GroupBy(p => p.Body).ToArray();
                    foreach (var group in producersByPlanet)
                    {
                        var maxTier = group.Max(tp => tp.Tier);
                        var topTierProducers = group.Where(tp => tp.Tier == maxTier).ToArray();

                        ITieredProducer exampleProducer = group.FirstOrDefault(tp => tp.IsResearchEnabled && tp.IsProductionEnabled);
                        // We're looking for the best example of why research isn't enabled - maxtier is the top
                        exampleProducer = topTierProducers.FirstOrDefault(tp => tp.Tier == TechTier.Tier4);
                        if (exampleProducer == null)
                        {
                            exampleProducer = topTierProducers.FirstOrDefault(tp => tp.IsProductionEnabled);
                        }

                        if (exampleProducer == null)
                        {
                            exampleProducer = topTierProducers.First();
                        }

                        if (exampleProducer.Output.ProductionRestriction != ProductionRestriction.Space && exampleProducer.Body == null)
                        {
                            continue;
                        }

                        var researchEntry = ColonizationResearchScenario.Instance.GetResearchProgress(
                            exampleProducer.Output, exampleProducer.Body, exampleProducer.Tier,
                            exampleProducer.IsResearchEnabled ? "Production Blocked" : exampleProducer.ReasonWhyResearchIsDisabled);
                        allResearchEntries.Add(researchEntry);
                    }
                }

                progress = allResearchEntries;
            }
        }

        private static bool IsCrushinResource(IColonizationResearchScenario researchScenario, string resourceName)
        {
            return researchScenario.TryParseTieredResourceName(resourceName, out TieredResource tieredResource, out var _)
                && tieredResource == researchScenario.CrushInsResource;
        }
    }
}
