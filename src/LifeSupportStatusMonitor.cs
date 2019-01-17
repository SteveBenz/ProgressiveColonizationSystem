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
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    public class LifeSupportStatusMonitor
        : ScenarioModule
    {
        [KSPField(isPersistant = true)]
        public bool isVisible = false;

        // If simulating + or - crewman, this becomes positive or negative.
        private int crewDelta = 0;

        // CrewDelta gets reset when lastActiveVessel no longer equals the current vessel.
        private Vessel lastActiveVessel;

        private ApplicationLauncherButton toolbarButton;

        private PopupDialog dialog = null;
        private string consumptionAndProductionInformation;

        [KSPField(isPersistant = true)]
        public float x;
        [KSPField(isPersistant = true)]
        public float y;

        private bool toolbarStateMatchedToIsVisible;

        private bool showingWhatIfButtons;
        private bool showingResourceTransfer;

        private IntervesselResourceTransfer resourceTransfer = new IntervesselResourceTransfer();

        public override void OnAwake()
        {
            base.OnAwake();

            AttachToToolbar();
        }

        private void AttachToToolbar()
        {
            if (this.toolbarButton != null)
            {
                // defensive
                return;
            }

            Texture2D appLauncherTexture = new Texture2D(36, 36, TextureFormat.ARGB32, false);
            appLauncherTexture.LoadImage(Properties.Resources.AppLauncherIcon);

            Debug.Assert(ApplicationLauncher.Ready, "ApplicationLauncher is not ready - can't add the toolbar button.  Is this possible, really?  If so maybe we could do it later?");
            this.toolbarButton = ApplicationLauncher.Instance.AddModApplication(ShowDialog, HideDialog, null, null, null, null,
                ApplicationLauncher.AppScenes.FLIGHT, appLauncherTexture);
            this.toolbarStateMatchedToIsVisible = false;
        }

        private void ShowDialog()
        {
            isVisible = true;
            if (this.dialog == null)
            {
                if (this.consumptionAndProductionInformation == null)
                {
                    // Fixed update hasn't run yet.
                    return;
                }

                // FYI, if you want to override a style, here'd be a way to do it:
                // var myStyle = new UIStyle(UISkinManager.defaultSkin.label) { wordWrap = false};
                //
                // Too bad wordWrap doesn't get paid attention to.

                List<DialogGUIBase> parts = new List<DialogGUIBase>();
                parts.Add(new DialogGUILabel(() => this.consumptionAndProductionInformation));
                parts.Add(new DialogGUIFlexibleSpace());
                if (showingWhatIfButtons)
                {
                    parts.Add(new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                                new DialogGUILabel("What if we"),
                                new DialogGUIButton("Add", () => { ++crewDelta; }, () => true, false),
                                new DialogGUILabel("/"),
                                new DialogGUIButton("Remove", () => { --crewDelta; }, () => FlightGlobals.ActiveVessel.GetCrewCount() + this.crewDelta > 0, false),
                                new DialogGUILabel("a kerbal?")));
                }

                if (showingResourceTransfer)
                {
                    parts.Add(
                        new DialogGUIVerticalLayout(
                            new DialogGUILabel("<color #c0c0c0>______________________________________________</color>"),
                            new DialogGUILabel("<b>Resource Transfer</b>"),
                            new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                                new DialogGUILabel("Target: "),
                                new DialogGUILabel(resourceTransfer.TargetVessel?.GetDisplayName())),
                            new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                                new DialogGUIButton("Start", resourceTransfer.StartTransfer, () => resourceTransfer.TargetVessel != null && !resourceTransfer.IsTransferUnderway, dismissOnSelect: false),
                                new DialogGUISlider(() => (float)resourceTransfer.TransferPercent, 0, 1, false, 100, 20, null))));
                }

                this.dialog = PopupDialog.SpawnPopupDialog(
                    new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f),
                    new MultiOptionDialog(
                        "LifeSupportMonitor",  // <- no idea what this does.
                        "",
                        "Colony Status",
                        HighLogic.UISkin,
                        new DialogGUIVerticalLayout(parts.ToArray())),
                    persistAcrossScenes: false,
                    skin: HighLogic.UISkin,
                    isModal: false,
                    titleExtra: "TITLE EXTRA!"); // <- no idea what that does.
            }
        }

        private void HideDialog()
        {
            isVisible = false;
            this.dialog?.Dismiss();
            this.dialog = null;
            this.crewDelta = 0;
        }

        private void OnDestroy()
        {
            if (this.toolbarButton != null)
            {
                if (ApplicationLauncher.Instance != null)
                {
                    ApplicationLauncher.Instance.RemoveModApplication(this.toolbarButton);
                }
                this.toolbarButton = null;
            }
        }

        private void FixedUpdate()
        {
            resourceTransfer.OnFixedUpdate();

            if (FlightGlobals.ActiveVessel.GetCrewCount() == 0 || FlightGlobals.ActiveVessel.isEVA)
            {
                if (this.dialog != null)
                {
                    this.dialog.Dismiss();
                    this.dialog = null;
                    // But leave isVisible set, as that's the persistent view.
                }
                this.toolbarButton.Disable();
                return;
            }

            this.toolbarButton.Enable();

            if (this.lastActiveVessel != FlightGlobals.ActiveVessel)
            {
                this.crewDelta = 0;
            }

            this.lastActiveVessel = FlightGlobals.ActiveVessel;
            var activeSnackConsumption = FlightGlobals.ActiveVessel?.GetComponent<SnackConsumption>();
            if (activeSnackConsumption == null)
            {
                // Shouldn't happen, but just in case...
                return;
            }

            // Shenanigans!  This gets around the apparent fact that you can't tell the toolbar what state to start in.
            if (!this.toolbarStateMatchedToIsVisible)
            {
                this.toolbarButton.toggleButton.Value = this.isVisible;
                this.toolbarStateMatchedToIsVisible = true;
            }

            // Shenanigans!  This hack gets around the apparent fact that you can't tell the window where to position itself.
            // Unity Shenanigans!  this.dialog?.dialog?.popupwindow can throw a null reference exception...  huh?
            if (this.dialog != null && this.dialog.popupWindow?.transform?.localPosition != null)
            {
                if ((x > 1f || y > 1f || x < 1f || y < 1f)
                    && this.dialog.popupWindow.transform.localPosition.x == 0 && this.dialog.popupWindow.transform.localPosition.y == 0)
                {
                    if (x > 1f || y > 1f || x < 1f || y < 1f)
                    {
                        // Re-apply the previous translation - adjusting for UI Scale
                        this.dialog.popupWindow.transform.Translate(x * GameSettings.UI_SCALE, y * GameSettings.UI_SCALE, 0f);
                        // If we have to persist this hack, we should detect whether the thing is pushed off the screen.
                    }
                }
                else
                {
                    // Record the translation for the future.
                    x = this.dialog.popupWindow.transform.localPosition.x;
                    y = this.dialog.popupWindow.transform.localPosition.y;
                }
            }

            int crewCount = FlightGlobals.ActiveVessel.GetCrewCount();
            if ((crewCount > 0) != this.showingWhatIfButtons)
            {
                this.showingWhatIfButtons = (crewCount > 0);
                if (this.dialog)
                {
                    this.dialog.Dismiss();
                    this.dialog = null;
                }
            }

            if ((resourceTransfer.TargetVessel != null) != this.showingResourceTransfer)
            {
                this.showingResourceTransfer = (resourceTransfer.TargetVessel != null);
                if (this.dialog)
                {
                    this.dialog.Dismiss();
                    this.dialog = null;
                }
            }

            activeSnackConsumption.ResourceQuantities(out var availableResources, out var availableStorage);
            List<IProducer> snackProducers = activeSnackConsumption.Vessel.FindPartModulesImplementing<IProducer>();

            BuildStatusString(activeSnackConsumption, availableResources, availableStorage, snackProducers, crewCount, crewDelta, out string message);
            this.consumptionAndProductionInformation = message;

            if (this.isVisible && this.dialog == null)
            {
                this.ShowDialog();
            }
        }




        internal static void BuildStatusString(
            SnackConsumption activeSnackConsumption,
            Dictionary<string, double> resources,
            Dictionary<string, double> storage,
            List<IProducer> snackProducers,
            int crewCount,
            int crewDelta,
            out string message)
        {
            StringBuilder text = new StringBuilder();

            if (crewCount + crewDelta == 0 && snackProducers.Count == 0)
            {
                text.AppendLine("Oy!  Robots don't eat!");
                // "This ship apparently ate all its crew"
            }
            else if (crewCount + crewDelta == 0)
            {
                text.AppendLine("With no crew aboard, not much is going on life-support wise...");
            }
            else
            {
                ResearchSink researchSink = new ResearchSink();
                TieredProduction.CalculateResourceUtilization(
                    crewCount + crewDelta, 1, snackProducers, researchSink, resources, storage,
                    out double timePassed, out var _, out Dictionary<string, double> resourcesConsumed,
                    out Dictionary<string, double> resourcesProduced);
                if (timePassed == 0)
                {
                    text.AppendLine("There aren't enough supplies or producers here to feed any kerbals.");

                    if (!activeSnackConsumption.IsAtHome)
                    {
                        // TODO: Somehow bucketize this, since all the crew are likely in the same state.
                        foreach (var crew in activeSnackConsumption.Vessel.GetVesselCrew())
                        {
                            var kerbalIsKnown = LifeSupportScenario.Instance.TryGetStatus(crew, out double daysSinceMeal, out double daysToGrouchy, out bool isGrouchy);
                            if (!kerbalIsKnown)
                            {
                                // TODO: Maybe if ! on kerban we complain about this?
                                // Debug.LogError($"Couldn't find a life support record for {crew.name}");
                            }

                            if (isGrouchy)
                            {
                                text.AppendLine($"<color #ff4040>{crew.name} hasn't eaten in {(int)daysSinceMeal} days and is too grouchy to work.</color>");
                            }
                            else if (daysToGrouchy > 5)
                            {
                                text.AppendLine($"{crew.name} is secretly munching a smuggled bag of potato chips");
                            }
                            else if (daysToGrouchy < 2)
                            {
                                text.AppendLine($"<color #ffff00>{crew.name} hasn't eaten in {(int)daysSinceMeal} days and will quit working in a couple of days if this keeps up.</color>");
                            }
                        }
                    }
                }
                else
                {
                    if (crewDelta == 0)
                    {
                        text.AppendLine($"To sustain its crew of {crewCount + crewDelta}, this vessel is using:");
                    }
                    else
                    {
                        text.AppendLine($"To sustain a crew of {crewCount + crewDelta} this vessel would use:");
                    }

                    foreach (var resourceName in resourcesConsumed.Keys.OrderBy(n => n))
                    {
                        double perDay = TieredProduction.UnitsPerSecondToUnitsPerDay(resourcesConsumed[resourceName]);
                        double daysLeft = resources[resourceName] / perDay;
                        text.AppendLine($"{perDay:N1} {resourceName} per day ({daysLeft:N1} days left)");
                    }

                    if (resourcesProduced != null && resourcesProduced.Count > 0)
                    {
                        text.AppendLine();
                        text.AppendLine("The crew is also producing:");
                        foreach (var resourceName in resourcesProduced.Keys.OrderBy(n => n))
                        {
                            double perDay = TieredProduction.UnitsPerSecondToUnitsPerDay(resourcesProduced[resourceName]);
                            double daysLeft = resources[resourceName] / perDay;
                            text.AppendLine($"{perDay:N1} {resourceName} per day");
                        }
                    }

                    bool addedResearchLineBreak = false;
                    foreach (var pair in researchSink.Data)
                    {
                        if (!addedResearchLineBreak)
                        {
                            text.AppendLine();
                            addedResearchLineBreak = true;
                        }

                        text.AppendLine($"This vessel {(crewDelta == 0 ? "is contributing" : "would contribute")} {pair.Value.KerbalDaysContributedPerDay:N1} units of {pair.Key.DisplayName} research per day.  ({pair.Value.KerbalDaysUntilNextTier:N} are needed to reach the next tier).");
                    }
                }
            }

            message = text.ToString();
        }

        private class ResearchData
        {
            public double KerbalDaysContributedPerDay;
            public double KerbalDaysUntilNextTier;
        }


        private class ResearchSink
            : IColonizationResearchScenario
        {
            public Dictionary<ResearchCategory, ResearchData> Data { get; } = new Dictionary<ResearchCategory, ResearchData>();

            bool IColonizationResearchScenario.ContributeResearch(TieredResource source, string atBody, double timespentInKerbalSeconds)
            {
                if (!this.Data.TryGetValue(source.ResearchCategory, out ResearchData data))
                {
                    data = new ResearchData();
                    this.Data.Add(source.ResearchCategory, data);
                    data.KerbalDaysUntilNextTier = ColonizationResearchScenario.Instance.GetKerbalDaysUntilNextTier(source, atBody);
                }

                // KerbalDaysContributedPerDay is equal to Kerbals.
                // timeSpentInKerbalSeconds works out to be time spent in a kerbal second (because that's the timespan
                // we passed into the production engine), so it's really kerbalSecondsContributedPerKerbalSecond.
                data.KerbalDaysContributedPerDay = timespentInKerbalSeconds;
                return false;
            }

            TechTier IColonizationResearchScenario.GetMaxUnlockedTier(TieredResource forResource, string atBody)
                => ColonizationResearchScenario.Instance.GetMaxUnlockedTier(forResource, atBody);

            bool IColonizationResearchScenario.TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier)
                => ColonizationResearchScenario.Instance.TryParseTieredResourceName(tieredResourceName, out resource, out tier);
        }
    }
}
