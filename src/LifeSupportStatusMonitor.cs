using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{
    /// <summary>
    ///   This class maintains a toolbar button and a GUI display that allows the user to see
    ///   into the life support status of the active vessel.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    class LifeSupportStatusMonitor
        : ScenarioModule
    {
        [KSPField(isPersistant = true)]
        public bool isVisible = false;

        // If simulating + or - crewman, this becomes positive or negative.
        private int crewDelta = 0;

        // CrewDelta gets reset when lastActiveVessel no longer equals the current vessel.
        private Vessel lastActiveVessel;

        private ApplicationLauncherButton toolbarButton;

        internal enum CrewState { Nonexistant, Happy, Antsy, Angry };

        private PopupDialog dialog = null;
        private string consumptionAndProductionInformation;
        private CrewState crewState;

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
            this.toolbarButton = ApplicationLauncher.Instance.AddModApplication(
                () => {
                    isVisible = true;
                    ShowDialog();
                }, () => {
                    isVisible = false;
                    HideDialog();
                }, null, null, null, null,
                ApplicationLauncher.AppScenes.FLIGHT,
                appLauncherTexture);
        }

        private void ShowDialog()
        {
            if (this.dialog == null)
            {
                if (this.consumptionAndProductionInformation == null)
                {
                    // Fixed update hasn't run yet.
                    return;
                }
                this.dialog = PopupDialog.SpawnPopupDialog(
                    new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f),
                    new MultiOptionDialog(
                        "LifeSupportMonitor",  // <- no idea what this does.
                        "",
                        "Colony Status",
                        HighLogic.UISkin,
                        new DialogGUIVerticalLayout(
                            new DialogGUILabel(() => this.consumptionAndProductionInformation),
                            new DialogGUIFlexibleSpace(),

                            new DialogGUIHorizontalLayout(
                                new DialogGUILabel("What if we"),
                                new DialogGUIButton("Add", () => { ++crewDelta; }, () => true, false),
                                new DialogGUILabel("/"),
                                new DialogGUIButton("Remove", () => { --crewDelta; }, () => FlightGlobals.ActiveVessel.GetCrewCount() + this.crewDelta > 0, false),
                                new DialogGUILabel("a kerbal")))),
                    persistAcrossScenes: false,
                    skin: HighLogic.UISkin,
                    isModal: false,
                    titleExtra: "TITLE EXTRA!"); // <- no idea what that does.
            }
        }

        private void HideDialog()
        {
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

            int crewCount = FlightGlobals.ActiveVessel.GetCrewCount();

            activeSnackConsumption.ResourceQuantities(out var availableResources, out var availableStorage);
            List<IProducer> snackProducers = activeSnackConsumption.Vessel.FindPartModulesImplementing<IProducer>();

            // If there are no top-tier supplies, no producers, and no crew
            if (crewCount == 0
             && snackProducers.Count == 0
             && !availableResources.ContainsKey("Snacks"))
            {
                GUILayout.Label("Oy!  Robots don't eat!");
                // "This ship apparently ate all its crew"
            }
            else
            {
                BuildStatusString(activeSnackConsumption, availableResources, availableStorage, snackProducers, crewCount, crewDelta, out string message, out CrewState crewState);
                this.consumptionAndProductionInformation = message;
                this.crewState = crewState;
            }

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
            out string message,
            out CrewState crewState)
        {
            StringBuilder text = new StringBuilder();

            if (crewCount == 0 && snackProducers.Count == 0)
            {
                crewState = CrewState.Nonexistant;
                text.AppendLine("Oy!  Robots don't eat!");
                // "This ship apparently ate all its crew"
            }
            else if (crewCount + crewDelta == 0)
            {
                crewState = CrewState.Nonexistant;
                text.AppendLine("With no crew aboard, not much is going on life-support wise...");
            }
            else
            {
                ResearchSink researchSink = new ResearchSink();
                TieredProduction.CalculateResourceUtilization(
                    crewCount + crewDelta, 1, snackProducers, researchSink, resources, storage,
                    out double timePassed, out bool _, out Dictionary<string, double> resourcesConsumed,
                    out Dictionary<string, double> resourcesProduced);
                if (timePassed == 0)
                {
                    text.AppendLine("There aren't enough supplies or producers here to feed any kerbals.");
                    crewState = CrewState.Antsy;

                    Debug.Assert(LifeSupportScenario.Instance != null);
                    if (LifeSupportScenario.Instance != null)
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
                                crewState = CrewState.Angry;
                                text.AppendLine($"{crew.name} hasn't eaten in {(int)daysSinceMeal} days and is too grouchy to work");
                            }
                            else if (daysToGrouchy > 5)
                            {
                                text.AppendLine($"{crew.name} is secretly munching a smuggled bag of potato chips");
                            }
                            else if (daysToGrouchy < 2)
                            {
                                text.AppendLine($"{crew.name} hasn't eaten in {(int)daysSinceMeal} days and will quit working in a couple of days if this keeps up.");
                            }
                        }
                    }
                }
                else
                {
                    crewState = CrewState.Happy;
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
                        text.AppendLine("The crew is also producing:");
                        foreach (var resourceName in resourcesProduced.Keys.OrderBy(n => n))
                        {
                            double perDay = TieredProduction.UnitsPerSecondToUnitsPerDay(resourcesProduced[resourceName]);
                            double daysLeft = resources[resourceName] / perDay;
                            text.AppendLine($"{perDay:N1} {resourceName} per day");
                        }
                    }

                    foreach (var pair in researchSink.Data)
                    {
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
