using KSP.UI.Screens;
using KSP.UI.Screens.DebugToolbar.Screens.Debug;
using ProgressiveColonizationSystem.ProductionChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   This class maintains a toolbar button and a GUI display that allows the user to see
    ///   into the life support status of the active vessel.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.EDITOR)]
    public class LifeSupportCalculator
        : PksTabbedDialog
    {
        private List<StaticAnalysis.WarningMessage> lastWarningList;

        private bool isShowingPartsInCrewWindow = false;

        private int warningsHash = 0;
        private int lastCrewHash = -1;

        private int plannedMissionDuration = 100;

        private string productionInfo = "";
        private string consumptionInfo = "";
        private string productionLimitedBy = "";

        private static LifeSupportCalculator instance;

        int btnId;

        public static void ToggleDialogVisibility()
        {
            instance?.ToggleVisibility();
        }

        public void Start()
        {
            // Add this mod to the ButtonManager.  The ButtonManager will control the access to the 
            // specified button (in this case, the launch button), and at the end of the chain, will
            // actually call the launch event

            ButtonManager.BtnManager.InitializeListener(EditorLogic.fetch.launchBtn, EditorLogic.fetch.launchVessel, "ProgressiveColonization");

            btnId = ButtonManager.BtnManager.AddListener(EditorLogic.fetch.launchBtn, OnLaunchClicked, "ProgressiveColonization", "Progressive-Colonization");
;
            instance = this;
        }

        public void OnLaunchClicked()
        {
            this.CalculateWarnings();
            if (this.lastWarningList.Any(w => w.IsClearlyBroken))
            {
                string message = "You might want to check this list of concerns the boys in the office have about this vessel:"
                               + Environment.NewLine
                               + string.Join(Environment.NewLine, this.lastWarningList.Select(w => w.Message).ToArray());
                PopupMessageWithKerbal.ShowOkayCancel("You sure, boss?", message, "Don't worry, I have a plan!", "Good point", () =>
                {
                    StaticAnalysis.FixBannedCargos();
                    ButtonManager.BtnManager.InvokeNextDelegate(btnId, "ProgressiveColonization.LifeSupportCalculator");
                });
            }
            else
            {
                ButtonManager.BtnManager.InvokeNextDelegate(btnId, "ProgressiveColonization.LifeSupportCalculator");
            }
        }

        public LifeSupportCalculator()
            : base(new string[] { "Warnings", "Calculator", "Crew" })
        {
        }

        protected override DialogGUIBase DrawTab(string tab)
        {
            switch(tab)
            {
                case "Warnings":
                    return DrawWarningsDialog();
                case "Calculator":
                default:
                    return DrawCalculatorDialog();
                case "Crew":
                    return DrawCrewDialog();
            }
        }

        protected override MultiOptionDialog DrawDialog(Rect rect)
        {
            return new MultiOptionDialog("LifeSupportCalculator", "", "Life Support Calculator", HighLogic.UISkin, rect, this.DrawTabbedDialog());
        }

        private static string ColorRed(string message)
            => $"<color #ff4040>{message}</color>";

        private static string ColorYellow(string message)
            => $"<color #ffff00>{message}</color>";

        private DialogGUIBase DrawCalculatorDialog()
        {
            List<Part> parts = EditorLogic.fetch.ship.Parts;
            List<ITieredProducer> producers = parts
                .Select(p => p.FindModuleImplementing<ITieredProducer>())
                .Where(p => p != null).ToList();

            if (!producers.Any(p => p.Output.IsEdible && p.Tier == TechTier.Tier4 && p.Output.GetPercentOfDietByTier(TechTier.Tier4) == 1)
                && !parts.Any(p => p.Resources.Any(r => r.resourceName == "Snacks-Tier4" && r.amount > 0)))
            {
                return new DialogGUILabel("There is no source of top-tier Snacks on this vessel - only well-fed and happy Kerbals will produce things");
            }

            // If we have some LandedOnBody and some not, that's going to spell trouble for our ability to calculate anything sensible.
            if (producers.Select(p => p.Output.ProductionRestriction == ProductionRestriction.LandedOnBody).Distinct().Count() > 1)
            {
                return new DialogGUILabel("This ship looks like a composite ship - that is one with several sub-ships to take on "
                                        + "different missions (e.g. landed, in-orbit and in-transit).  The calculator can't work effectively "
                                        + "on ships like that.  Build the sub-ships individually, check the calculator and warnings panel "
                                        + "on each one, then merge them together (using either sub-assemblies or the 'Merge' button on the "
                                        + "load screen.");
            }

            string requiredSituationString;
            if (!producers.Any() || producers[0].Output.ProductionRestriction == ProductionRestriction.Space)
            {
                requiredSituationString = "in space";
            }
            else
            {
                string body = producers.Select(p => p.Body).FirstOrDefault(b => !string.IsNullOrEmpty(b));
                requiredSituationString = producers[0].Output.ProductionRestriction == ProductionRestriction.LandedOnBody
                    ? $"landed at {body}" : $"in orbit of {body}";
            }

            // Calculator
            //   Duration: [_____] Days  [x] Landed
            //   Consumes:
            //    ...
            //    [[Fill Cans+10%]] [[Fill Cans+25%]]
            //   Produces:

            RecalculateResults();

            return new DialogGUIVerticalLayout(
                new DialogGUISpace(3),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                    new DialogGUILabel("Duration:"),
                    new DialogGUITextInput(
                        txt: this.plannedMissionDuration.ToString(),
                        multiline: false,
                        maxlength: 5,
                        textSetFunc: OnInputText,
                        getString: () => this.plannedMissionDuration.ToString(),
                        contentType: TMPro.TMP_InputField.ContentType.IntegerNumber,
                        hght: 24f),
                    new DialogGUILabel("days while " + requiredSituationString),
                    new DialogGUIFlexibleSpace()
                ),
                new DialogGUISpace(3),
                new DialogGUILabel("<b>Production:</b>"),
                new DialogGUILabel(() => this.productionInfo),
                new DialogGUISpace(3),
                new DialogGUILabel("<b>Consumption:</b>"),
                new DialogGUILabel(() => this.consumptionInfo),
                new DialogGUISpace(3),
                new DialogGUILabel(() => string.IsNullOrEmpty(this.productionLimitedBy) ? "" : TextEffects.Yellow("<b>Production Limited By:</b>")),
                new DialogGUILabel(() => this.productionLimitedBy));
        }

        private string OnInputText(string text)
        {
            if (uint.TryParse(text, out uint days))
            {
                this.plannedMissionDuration = Math.Max(1, (int)days);
                RecalculateResults();
                return text;
            }
            else
            {
                return this.plannedMissionDuration.ToString();
            }
        }

        private void RecalculateResults()
        {
            List<Part> parts = EditorLogic.fetch.ship.Parts;
            List<ITieredProducer> producers = parts
                .Select(p => p.FindModuleImplementing<ITieredProducer>())
                .Where(p => p != null)
                .ToList();
            List<ITieredCombiner> combiners = parts
                .Select(p => p.FindModuleImplementing<ITieredCombiner>())
                .Where(p => p != null)
                .ToList();
            Dictionary<string, double> amountAvailable = parts
                .SelectMany(p => p.Resources)
                .GroupBy(r => r.resourceName)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.amount));
            Dictionary<string, double> storageAvailable = parts
                .SelectMany(p => p.Resources)
                .GroupBy(r => r.resourceName)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.maxAmount - r.amount));
            const double aWholeLot = 10000.0;
            Dictionary<string, double> unlimitedAmounts = amountAvailable.ToDictionary(pair => pair.Key, pair => aWholeLot);

            foreach (var producer in producers)
            {
                if (producer.Input != null && producer.Input.IsHarvestedLocally)
                {
                    unlimitedAmounts[producer.Input.TieredName(producer.Tier)] = aWholeLot;
                }
            }

            int crewCount = KSP.UI.CrewAssignmentDialog.Instance.GetManifest(false).CrewCount;
            ResearchSink researchSink = new ResearchSink();
            TieredProduction.CalculateResourceUtilization(
                crewCount, 1.0, producers, combiners, researchSink, unlimitedAmounts, unlimitedAmounts,
                out double timePassed, out var _, out Dictionary<string, double> resourcesConsumedPerSecond,
                out Dictionary<string, double> resourcesProducedPerSecond,
                out IEnumerable<string> limitingResources,
                out Dictionary<string, double> unusedProduction);
            if (timePassed < 1.0)
            {
                this.consumptionInfo = "-";
                this.productionInfo = "-";
                this.productionLimitedBy = string.Join(", ", limitingResources.ToArray());
                return;
            }

            StringBuilder consumption = new StringBuilder();
            foreach (var pair in resourcesConsumedPerSecond.OrderBy(pair => pair.Key))
            {
                var name = pair.Key;
                var amountPerDay = KerbalTime.UnitsPerSecondToUnitsPerDay(pair.Value);
                consumption.Append($"{amountPerDay:N1} {name}/day");

                if (ColonizationResearchScenario.Instance.TryParseTieredResourceName(name, out TieredResource resource, out TechTier tier)
                 && resource.IsHarvestedLocally)
                {
                    consumption.Append(" - will need to be fetched from a resource lode");
                }
                else
                {
                    double available = 0;
                    amountAvailable.TryGetValue(name, out available);
                    string availableBlurb = $"{amountPerDay * this.plannedMissionDuration:N0} needed, {available:N0} available";
                    if (available < amountPerDay * this.plannedMissionDuration)
                    {
                        availableBlurb = ColorRed(availableBlurb);
                    }
                    consumption.Append(" - ");
                    consumption.Append(availableBlurb);
                }
                consumption.AppendLine();
            }
            this.consumptionInfo = consumption.ToString();

            StringBuilder production = new StringBuilder();
            foreach (var pair in resourcesProducedPerSecond.OrderBy(pair => pair.Key))
            {
                var name = pair.Key;
                var amountPerDay = KerbalTime.UnitsPerSecondToUnitsPerDay(pair.Value);

                double available = 0;
                storageAvailable.TryGetValue(name, out available);
                string availableBlurb = $"{amountPerDay * this.plannedMissionDuration:N0} produced, {available:N0} max capacity";
                if (available < amountPerDay * this.plannedMissionDuration)
                {
                    availableBlurb = ColorYellow(availableBlurb);
                }
                production.AppendLine($"{amountPerDay:N1} {name}/day - {availableBlurb}");
            }
            this.productionInfo = production.ToString();
            this.productionLimitedBy = string.Join(", ", limitingResources.ToArray());
        }


        private DialogGUIBase DrawWarningsDialog()
        {
            if (this.lastWarningList == null || this.lastWarningList.Count == 0)
            {
                return new DialogGUILabel("Looks good to me.");
            }

            List<DialogGUIBase> warningLines = new List<DialogGUIBase>();
            foreach (var warning in this.lastWarningList)
            {
                DialogGUILabel message = new DialogGUILabel(warning.IsClearlyBroken ? ColorRed(warning.Message) : ColorYellow(warning.Message));
                warningLines.Add(warning.FixIt == null
                    ? (DialogGUIBase)message
                    : (DialogGUIBase)new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft, new DialogGUIButton("Fix It", () => warning.FixIt()), message));
            }

            return new DialogGUIVerticalLayout(warningLines.ToArray());
        }

        private DialogGUIBase DrawCrewDialog()
        {
            List<Part> parts = EditorLogic.fetch.ship.Parts;
            List<PksCrewRequirement> partsWithCrewRequirements = parts
                .Select(p => p.FindModuleImplementing<PksCrewRequirement>())
                .Where(p => p != null)
                .ToList();
            List<ProtoCrewMember> crew = this.FindCrew();
            bool needsRoverPilot = parts
                .Select(p => p.FindModuleImplementing<ITieredProducer>())
                .Where(p => p != null)
                .Any(p => p.Input == ColonizationResearchScenario.Instance.CrushInsResource);

            return LifeSupportStatusMonitor.DrawCrewDialog(
                partsWithCrewRequirements,
                crew,
                needsRoverPilot,
                () => this.isShowingPartsInCrewWindow,
                (newValue) =>
                {
                    this.isShowingPartsInCrewWindow = newValue;
                    this.Redraw();
                });
        }

        protected override void OnFixedUpdate()
        {
            if (EditorLogic.RootPart == null)
            {
                this.lastWarningList = new List<StaticAnalysis.WarningMessage>();
                return;
            }

            if (!this.isVisible)
            {
                return;
            }

            this.CalculateWarnings();
            this.RecalculateResults();

            int lastCrewHash = FindCrew().Aggregate<ProtoCrewMember, int>(0, (accumulator, crew) => accumulator ^ crew.GetHashCode());
            if (this.lastCrewHash != lastCrewHash)
            {
                this.lastCrewHash = lastCrewHash;
                this.Redraw();
            }
        }

        private void CalculateWarnings()
        {
            List<Part> parts = EditorLogic.fetch.ship.Parts; // EditorLogic.FindPartsInChildren(EditorLogic.RootPart);

            List<ITieredProducer> producers = parts
                .Select(p => p.FindModuleImplementing<ITieredProducer>())
                .Where(p => p != null).ToList();
            List<ITieredCombiner> combiners = parts
                .Select(p => p.FindModuleImplementing<ITieredCombiner>())
                .Where(p => p != null).ToList();
            List<IPksCrewRequirement> crewedParts = parts
                .Select(p => p.FindModuleImplementing<IPksCrewRequirement>())
                .Where(p => p != null)
                .ToList();
            Dictionary<string, double> amountAvailable = parts
                .SelectMany(p => p.Resources)
                .GroupBy(r => r.resourceName)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.amount));
            Dictionary<string, double> storageAvailable = parts
                .SelectMany(p => p.Resources)
                .GroupBy(r => r.resourceName)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.maxAmount - r.amount));

            List<SkilledCrewman> crew = this.FindCrew().Select(c => new SkilledCrewman(c)).ToList();
            int crewCount = parts.Sum(p => p.CrewCapacity);
            this.lastWarningList =
                StaticAnalysis.CheckBodyIsSet(ColonizationResearchScenario.Instance, producers, amountAvailable, storageAvailable)
                .Union(StaticAnalysis.CheckHasCrushinStorage(ColonizationResearchScenario.Instance, producers, amountAvailable, storageAvailable))
                .Union(StaticAnalysis.CheckTieredProduction(ColonizationResearchScenario.Instance, producers, amountAvailable, storageAvailable))
                .Union(StaticAnalysis.CheckTieredProductionStorage(ColonizationResearchScenario.Instance, producers, amountAvailable, storageAvailable))
                .Union(StaticAnalysis.CheckCorrectCapacity(ColonizationResearchScenario.Instance, producers, amountAvailable, storageAvailable))
                .Union(StaticAnalysis.CheckExtraBaggage(ColonizationResearchScenario.Instance, producers, amountAvailable, storageAvailable))
                .Union(StaticAnalysis.CheckHasSomeFood(ColonizationResearchScenario.Instance, producers, amountAvailable, storageAvailable, crew))
                .Union(StaticAnalysis.CheckCombiners(ColonizationResearchScenario.Instance, producers, combiners, amountAvailable, storageAvailable))
                .Union(StaticAnalysis.CheckHasRoverPilot(ColonizationResearchScenario.Instance, producers, amountAvailable, storageAvailable, crew))
                .Union(StaticAnalysis.CheckHasProperCrew(crewedParts, crew))
                .Union(StaticAnalysis.CheckRoverHasTwoSeats(ColonizationResearchScenario.Instance, producers, amountAvailable, storageAvailable, crewCount))
                .ToList();

            // See if anything's actually changed
            int hash = this.lastWarningList.Count == 0 ? 0 : this.lastWarningList.Select(w => w.Message.GetHashCode()).Aggregate((accumulator, value) => accumulator ^ value);
            if (hash != this.warningsHash)
            {
                this.warningsHash = hash;
                this.Redraw();
            }
        }

        private List<ProtoCrewMember> FindCrew()
        {
            VesselCrewManifest crewManifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest(false);
            return crewManifest == null ? new List<ProtoCrewMember>() : crewManifest.GetAllCrew(false);
        }

        protected override ApplicationLauncher.AppScenes VisibleInScenes { get; } = ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB;
    }
}
