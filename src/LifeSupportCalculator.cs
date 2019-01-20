using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
                    new DialogGUIToggleButton(this.tab == Tab.Warnings, "Warnings", (isSet) => { this.tab = Tab.Warnings; this.Redraw(); }, w: -1, h: -1),
                    new DialogGUIToggleButton(this.tab == Tab.Calculator, "Calculator", (isSet) => { this.tab = Tab.Calculator; this.Redraw(); }, w: -1, h: -1)),
                content);
        }

        private static string ColorRed(string message)
            => $"<color #ff4040>{message}</color>";

        private static string ColorYellow(string message)
            => $"<color #ffff00>{message}</color>";

        private DialogGUIBase DrawCalculatorDialog()
        {
            return new DialogGUILabel("Calculator");
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

            CalculateWarnings();
        }

        int warningsHash = 0;


        private void CalculateWarnings()
        {
            List<Part> parts = EditorLogic.FindPartsInChildren(EditorLogic.RootPart);
            List<ITieredProducer> producers = parts
                .Select(p => p.FindModuleImplementing<ITieredProducer>())
                .Where(p => p != null).ToList();
            List<ITieredContainer> containers = parts
                .Select(p => p.FindModuleImplementing<ITieredContainer>())
                .Where(p => p != null).ToList();

            this.lastWarningList =
                StaticAnalysis.CheckBodyIsSet(ColonizationResearchScenario.Instance, producers, containers)
                .Union(StaticAnalysis.CheckTieredProduction(ColonizationResearchScenario.Instance, producers, containers))
                .Union(StaticAnalysis.CheckTieredProductionStorage(ColonizationResearchScenario.Instance, producers, containers))
                .Union(StaticAnalysis.CheckCorrectCapacity(ColonizationResearchScenario.Instance, producers, containers))
                .Union(StaticAnalysis.CheckExtraBaggage(ColonizationResearchScenario.Instance, producers, containers))
                .ToList();

            // See if anything's actually changed
            int hash = this.lastWarningList.Select(w => w.Message.GetHashCode()).Aggregate((accumulator, value) => accumulator ^ value);
            if (hash != this.warningsHash)
            {
                this.warningsHash = hash;
                this.Redraw();
            }
        }

        // Calculator
        //   Duration: [_____] Days  [x] Landed
        //   Consumes:
        //    ...
        //    [[Fill Cans+10%]] [[Fill Cans+25%]]
        //   Produces:
        //  
        // Warnings
        // -- Not enough TRAITs to operate [part]

        protected override ApplicationLauncher.AppScenes VisibleInScenes { get; } = ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB;
    }
}
