using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

// TODO: Boy Voyage has a less bad way of doing positioning:
//   https://github.com/jarosm/KSP-BonVoyage/blob/master/BonVoyage/gui/MainWindowView.cs

namespace Nerm.Colonization
{
    /// <summary>
    ///   This class maintains a toolbar button and a GUI display that allows the user to see
    ///   into the life support status of the active vessel.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.EDITOR)]
    public class LifeSupportCalculator
        : CbnToolbarDialog
    {
        private enum Tab {
            Warnings,
            Calculator,
        };

        private Tab tab = Tab.Warnings;

        private List<StaticAnalysis.WarningMessage> lastWarningList;

        protected override MultiOptionDialog DrawDialog()
        {
            return new MultiOptionDialog("LifeSupportCalculator", "", "Life Support Calculator", HighLogic.UISkin, this.DrawTabbedDialog() );
        }

        private float buttonWidth = 80f; // Shenanigans - Can't figure out how to calculate this, but these numbers work somehow.
        private float buttonHeight = 30f;

        private int warningsHash = 0;

        private int plannedMissionDuration = 100;

        private string productionInfo = "";
        private string consumptionInfo = "";


        public void Start()
        {
            EditorLogic.fetch.launchBtn.onClick.RemoveListener(EditorLogic.fetch.launchVessel);
            EditorLogic.fetch.launchBtn.onClick.AddListener(OnLaunchClicked);
        }

        public void OnLaunchClicked()
        {
            this.CalculateWarnings();
            if (this.lastWarningList.Any(w => w.IsClearlyBroken))
            {
                string message = "You might want to check this list of concerns the boys in the office have about this vessel:"
                               + Environment.NewLine
                               + string.Join(Environment.NewLine, this.lastWarningList.Select(w => w.Message).ToArray());
                PopupMessageWithKerbal.ShowOkayCancel("You sure, boss?", message, "Don't worry, I have a plan!", "Good point", EditorLogic.fetch.launchVessel);
            }
            else
            {
                EditorLogic.fetch.launchVessel();
            }
        }

        private DialogGUIBase DrawTabbedDialog()
        {
            DialogGUIBase content;
            switch(this.tab)
            {
                default:
                case Tab.Warnings:
                    content = DrawWarningsDialog();
                    break;
                case Tab.Calculator:
                    content = DrawCalculatorDialog();
                    break;
            }

            return new DialogGUIVerticalLayout(
                new DialogGUIHorizontalLayout(
                    new DialogGUIToggleButton(this.tab == Tab.Warnings, "Warnings", (isSet) => { this.tab = Tab.Warnings; this.Redraw(); }, w: buttonWidth, h: buttonHeight),
                    new DialogGUIToggleButton(this.tab == Tab.Calculator, "Calculator", (isSet) => { this.tab = Tab.Calculator; this.Redraw(); }, w: buttonWidth, h: buttonHeight)),
                content);
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
            List<ITieredContainer> containers = TieredContainer.FindAllTieredResourceContainers(parts).ToList();

            if (!producers.Any(p => p.Output.IsSnacks && p.Tier == TechTier.Tier4)
                && !containers.Any(c => c.Content.IsSnacks && c.Tier == TechTier.Tier4 && c.Amount > 0))
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
            if (!producers.Any() || producers[0].Output.ProductionRestriction == ProductionRestriction.Orbit)
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
                new DialogGUILabel(() => this.consumptionInfo));
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
                .Where(p => p != null).ToList();
            List<ITieredContainer> containers = TieredContainer.FindAllTieredResourceContainers(parts).ToList();

            const double aWholeLot = 10000.0;
            Dictionary<string, double> unlimitedInputs = containers
                .Select(c => c.Content.TieredName(c.Tier))
                .Distinct()
                .ToDictionary(n => n, n => aWholeLot);
            Dictionary<string, double> unlimitedOutputs = containers
                .Select(c => c.Content.TieredName(c.Tier))
                .Distinct()
                .ToDictionary(n => n, n => aWholeLot);

            int crewCount = KSP.UI.CrewAssignmentDialog.Instance.GetManifest(false).CrewCount;
            ResearchSink researchSink = new ResearchSink();
            TieredProduction.CalculateResourceUtilization(
                crewCount, 1.0, producers, researchSink, unlimitedInputs, unlimitedOutputs,
                out double timePassed, out var _, out Dictionary<string, double> resourcesConsumedPerSecond,
                out Dictionary<string, double> resourcesProducedPerSecond);
            if (timePassed < 1.0)
            {
                this.consumptionInfo = "-";
                this.productionInfo = "-";
                return;
            }

            Dictionary<string, double> actualInputs = containers
                .GroupBy(c => c.Content.TieredName(c.Tier))
                .ToDictionary(c => c.Key, c => c.Sum(x => (double)x.Amount));
            Dictionary<string, double> actualStorage = containers
                .GroupBy(c => c.Content.TieredName(c.Tier))
                .ToDictionary(c => c.Key, c => c.Sum(x => (double)(x.MaxAmount - x.Amount)));

            StringBuilder consumption = new StringBuilder();
            foreach (var pair in resourcesConsumedPerSecond.OrderBy(pair => pair.Key))
            {
                var name = pair.Key;
                var amountPerDay = TieredProduction.UnitsPerSecondToUnitsPerDay(pair.Value);

                double available = 0;
                actualInputs.TryGetValue(name, out available);
                string availableBlurb = $"{amountPerDay * this.plannedMissionDuration:N0} needed, {available:N0} available";
                if (available < amountPerDay * this.plannedMissionDuration)
                {
                    availableBlurb = ColorRed(availableBlurb);
                }
                consumption.AppendLine($"{amountPerDay:N1} {name}/day - {availableBlurb}");
            }
            this.consumptionInfo = consumption.ToString();

            StringBuilder production = new StringBuilder();
            foreach (var pair in resourcesProducedPerSecond.OrderBy(pair => pair.Key))
            {
                var name = pair.Key;
                var amountPerDay = TieredProduction.UnitsPerSecondToUnitsPerDay(pair.Value);

                double available = 0;
                actualStorage.TryGetValue(name, out available);
                string availableBlurb = $"{amountPerDay * this.plannedMissionDuration:N0} produced, {available:N0} max capacity";
                if (available < amountPerDay * this.plannedMissionDuration)
                {
                    availableBlurb = ColorYellow(availableBlurb);
                }
                production.AppendLine($"{amountPerDay:N1} {name}/day - {availableBlurb}");
            }
            this.productionInfo = production.ToString();
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

        protected override void OnFixedUpdate()
        {
            if (EditorLogic.RootPart == null)
            {
                this.lastWarningList = new List<StaticAnalysis.WarningMessage>();
                return;
            }

            if (this.isVisible)
            {
                CalculateWarnings();
                RecalculateResults();
            }
        }

        private void CalculateWarnings()
        {
            List<Part> parts = EditorLogic.fetch.ship.Parts; // EditorLogic.FindPartsInChildren(EditorLogic.RootPart);
            
            List<ITieredProducer> producers = parts
                .Select(p => p.FindModuleImplementing<ITieredProducer>())
                .Where(p => p != null).ToList();
            List<ITieredContainer> containers = TieredContainer.FindAllTieredResourceContainers(parts).ToList();
            List<ICbnCrewRequirement> crewedParts = parts
                .Select(p => p.FindModuleImplementing<ICbnCrewRequirement>())
                .Where(p => p != null)
                .ToList();

            List<SkilledCrewman> crew = this.FindAssignedCrew();
            this.lastWarningList =
                StaticAnalysis.CheckBodyIsSet(ColonizationResearchScenario.Instance, producers, containers)
                .Union(StaticAnalysis.CheckTieredProduction(ColonizationResearchScenario.Instance, producers, containers))
                .Union(StaticAnalysis.CheckTieredProductionStorage(ColonizationResearchScenario.Instance, producers, containers))
                .Union(StaticAnalysis.CheckCorrectCapacity(ColonizationResearchScenario.Instance, producers, containers))
                .Union(StaticAnalysis.CheckExtraBaggage(ColonizationResearchScenario.Instance, producers, containers))
                .Union(StaticAnalysis.CheckExtraBaggage(ColonizationResearchScenario.Instance, producers, containers))
                .Union(StaticAnalysis.CheckHasSomeFood(ColonizationResearchScenario.Instance, producers, containers, crew))
                .Union(StaticAnalysis.CheckHasProperCrew(crewedParts, crew))
                .ToList();

            // See if anything's actually changed
            int hash = this.lastWarningList.Count == 0 ? 0 : this.lastWarningList.Select(w => w.Message.GetHashCode()).Aggregate((accumulator, value) => accumulator ^ value);
            if (hash != this.warningsHash)
            {
                this.warningsHash = hash;
                this.Redraw();
            }
        }

        private List<SkilledCrewman> FindAssignedCrew()
        {
            VesselCrewManifest crewManifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest(false);
            return crewManifest.GetAllCrew(false).Select(k => new SkilledCrewman(k.experienceLevel, k.trait)).ToList();
        }

        protected override ApplicationLauncher.AppScenes VisibleInScenes { get; } = ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB;
    }
}
