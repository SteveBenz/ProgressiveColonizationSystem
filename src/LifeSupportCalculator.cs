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

        private bool anyWarnings = false;
        private bool anyErrors = false;

        protected override MultiOptionDialog DrawDialog()
        {
            return new MultiOptionDialog("LifeSupportCalculator", "", "Life Support Calculator", HighLogic.UISkin, this.DrawTabbedDialog() );
        }

        private DialogGUIButton calculatorButton;
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
                    new DialogGUIButton(() => GetWarningsMessage(), () => { this.tab = Tab.Warnings; this.Redraw(); }, buttonWidth, buttonHeight, dismissOnSelect: false),
                    new DialogGUIButton("Calculator", () => { this.tab = Tab.Calculator; this.Redraw(); }, dismissOnSelect: false)
                ),
                content);
        }

        private string GetWarningsMessage()
            => this.anyErrors ? ColorRed("Warnings") : this.anyWarnings ? ColorYellow("Warnings") : "Warnings";

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
            return new DialogGUILabel("Warnings");
        }

        protected override void OnFixedUpdate()
        {
            if (this.calculatorButton != null && this.calculatorButton.width > 0f && this.buttonWidth == 0f)
            {
                this.buttonWidth = this.calculatorButton.width;
                this.buttonHeight = this.calculatorButton.height;
                this.Redraw();
            }

            if (EditorLogic.RootPart == null)
            {
                this.anyErrors = false;
                this.anyWarnings = false;
                return;
            }

            List<Part> allParts = EditorLogic.FindPartsInChildren(EditorLogic.RootPart);
            this.anyErrors = allParts.Any(p => p.FindModuleImplementing<CbnTieredResourceConverter>() != null);
            this.anyWarnings = !this.anyErrors && allParts.Any(p => p.FindModuleImplementing<CbnTieredContainer>() != null);
        }

        // Calculator
        //   Duration: [_____] Days  [x] Landed
        //   Consumes:
        //    ...
        //    [[Fill Cans+10%]] [[Fill Cans+25%]]
        //   Produces:
        //  
        // Warnings
        // -- Body-specific parts don't have a target body
        // [Fix it] Not all the body-specific parts point to Mun
        // [Fix it] Not all the agriculture&production parts are set to Tier-X
        // -- Tier1 Snacks are being produced but there's no place to store them
        // -- Tier1 Fertilizer is being produced but there's no place to store them
        // -- Tier1 Shinies are being produced ...
        // -- Not enough TRAITs to operate [part]

        protected override ApplicationLauncher.AppScenes VisibleInScenes { get; } = ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB;
    }
}
