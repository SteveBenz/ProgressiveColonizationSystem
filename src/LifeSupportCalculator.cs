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
            return (DialogGUIBase)this.warnings ?? new DialogGUILabel("Looks good to me.");
        }

        protected override void OnFixedUpdate()
        {
            if (EditorLogic.RootPart == null)
            {
                this.anyErrors = false;
                this.anyWarnings = false;
                return;
            }

            List<Part> parts = EditorLogic.FindPartsInChildren(EditorLogic.RootPart);
            List<CbnTieredResourceConverter> tieredResourceConverters = parts
                .Select(p => p.FindModuleImplementing<CbnTieredResourceConverter>())
                .Where(p => p != null).ToList();
            List<CbnTieredContainer> tieredContainers = parts
                .Select(p => p.FindModuleImplementing<CbnTieredContainer>())
                .Where(p => p != null).ToList();
            CalculateWarnings(tieredResourceConverters, tieredContainers);

            this.anyErrors = parts.Any(p => p.FindModuleImplementing<CbnTieredResourceConverter>() != null);
            this.anyWarnings = !this.anyErrors && parts.Any(p => p.FindModuleImplementing<CbnTieredContainer>() != null);
        }

        int warningsHash = 0;
        DialogGUIVerticalLayout warnings;

        private void CalculateWarnings(List<CbnTieredResourceConverter> tieredResourceConverters, List<CbnTieredContainer> parts)
        {
            int hash = 0;
            bool anyErrors = false;
            bool anyWarnings = false;
            List<DialogGUIBase> warningLines = new List<DialogGUIBase>();
            // Check for body parts
            List<CbnTieredResourceConverter> bodySpecific = tieredResourceConverters.Where(c => c.Output.ProductionRestriction != ProductionRestriction.Orbit).ToList();
            var mostUsedBodyAndCount = bodySpecific
                .Where(c => c.body != CbnTieredResourceConverter.NotSet)
                .GroupBy(c => c.body)
                .Select(g => new { body = g.Key, count = g.Count() })
                .OrderByDescending(o => o.count)
                .FirstOrDefault();
            var mostUsedBody = mostUsedBodyAndCount?.body;
            int? numSetToMostUsed = mostUsedBodyAndCount?.count;
            int numNotSet = bodySpecific.Count(c => c.body == CbnTieredResourceConverter.NotSet);
            if (numNotSet > 0 && numNotSet == bodySpecific.Count)
            {
                // Some not set to the same body
                string message = $"Need to set up the target for the world-specific parts";
                hash ^= message.GetHashCode();
                warningLines.Add(new DialogGUILabel(message));
                anyErrors = true;
            }
            else if (numNotSet + numSetToMostUsed < bodySpecific.Count)
            {
                // Mixed up body
                string message = $"Not all of the body-specific parts are set up for {mostUsedBody}";
                hash ^= message.GetHashCode();
                warningLines.Add(new DialogGUILabel(message));
                // TODO: Fix it
            }

            if (hash != this.warningsHash)
            {
                this.anyErrors = anyErrors;
                this.anyWarnings = anyWarnings;
                this.warningsHash = hash;
                this.warnings = warningLines.Count == 0 ? null : new DialogGUIVerticalLayout(warningLines.ToArray());
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
