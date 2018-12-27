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
        Rect extent = new Rect(100, 100, 800, 200);

        bool isVisible = false;

        // If simulating + or - crewman, this becomes positive or negative.
        private int crewDelta = 0;

        // CrewDelta gets reset when lastActiveVessel no longer equals the current vessel.
        private Vessel lastActiveVessel;

        private ApplicationLauncherButton toolbarButton;

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

            Texture2D texture2D;
            if (GameDatabase.Instance.ExistsTexture("ColonizationByNerm/IFI_LS_GRN_38"))
            {
                Debug.Log("DebugControl - Using blank texture");
                texture2D = new Texture2D(38, 38, TextureFormat.ARGB32, false);
            }
            else
            {
                texture2D = GameDatabase.Instance.GetTexture("ColonizationByNerm/IFI_LS_GRN_38", true);
            }

            Debug.Assert(ApplicationLauncher.Ready, "ApplicationLauncher is not ready - can't add the toolbar button.  Is this possible, really?  If so maybe we could do it later?");
            this.toolbarButton = ApplicationLauncher.Instance.AddModApplication(
                () => { isVisible = true; }, () => { isVisible = false; }, null, null, null, null,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                texture2D);
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

        public void OnGUI()
        {
            if (!this.isVisible)
            {
                return;
            }

            //windowPos = ClickThruBlocker.GUILayoutWindow(99977, windowPos, DebugModeDialog, "Debug modes");
            this.extent = GUI.Window(GetInstanceID(), this.extent, WindowCallback, "Life Support Status");
        }

        private void WindowCallback(int windowId)
        {
            var activeSnackConsumption = FlightGlobals.ActiveVessel?.GetComponent<SnackConsumption>();
            if (activeSnackConsumption == null)
            {
                GUILayout.Label("No active vessel.");
                return;
            }

            if (activeSnackConsumption.Vessel != this.lastActiveVessel)
            {
                this.crewDelta = 0;
                this.lastActiveVessel = activeSnackConsumption.Vessel;
            }

            GUILayout.BeginHorizontal();

            Dictionary<string, double> resources = activeSnackConsumption.ResourceQuantities();
            List<ISnackProducer> snackProducers = activeSnackConsumption.Vessel.FindPartModulesImplementing<ISnackProducer>();
            int crewCount = activeSnackConsumption.Vessel.GetCrewCount();

            // If there are no top-tier supplies, no producers, and no crew
            if (crewCount == 0
             && snackProducers.Count == 0
             && !activeSnackConsumption.ResourceQuantities().ContainsKey(TechTier.Tier4.SnacksResourceName()))
            {
                GUILayout.Label("Oy!  Robots don't eat!");
                // "This ship apparently ate all its crew"
            }
            else
            {
                DrawDialog(activeSnackConsumption, resources, snackProducers, crewCount);
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void DrawDialog(SnackConsumption activeSnackConsumption, Dictionary<string, double> resources, List<ISnackProducer> snackProducers, int crewCount)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUILayout.Label("What if we ");
            if (GUILayout.Button("add"))
            {
                ++crewDelta;
            }
            if (crewCount + crewDelta > 1)
            {
                GUILayout.Label(" / ");
                if (GUILayout.Button("remove"))
                {
                    --crewDelta;
                }
                GUILayout.Label(" a kerbal?");
            }
            GUILayout.EndHorizontal();

            if (crewCount + crewDelta == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("With no crew aboard, not much is going on life-support wise...");
                GUILayout.EndHorizontal();
            }
            else
            {
                ResearchSink researchSink = new ResearchSink();
                SnackConsumption.CalculateSnackflow(
                    crewCount + crewDelta, 1, snackProducers, researchSink, resources,
                    out double timePassed, out bool _, out Dictionary<string, double> resourcesConsumed);
                if (timePassed == 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("There aren't enough supplies or producers here to feed any kerbals.");
                    GUILayout.EndHorizontal();

                    Debug.Assert(LifeSupportScenario.Instance != null);
                    if (LifeSupportScenario.Instance != null)
                    {
                        // TODO: Somehow bucketize this, since all the crew are likely in the same state.
                        foreach (var crew in activeSnackConsumption.Vessel.GetVesselCrew())
                        {
                            GUILayout.BeginHorizontal();
                            var kerbalIsKnown = LifeSupportScenario.Instance.TryGetStatus(crew, out double daysSinceMeal, out double daysToGrouchy, out bool isGrouchy);
                            if (!kerbalIsKnown)
                            {
                                // TODO: Maybe if ! on kerban we complain about this?
                                // Debug.LogError($"Couldn't find a life support record for {crew.name}");
                            }

                            if (isGrouchy)
                            {
                                GUILayout.Label($"{crew.name} hasn't eaten in {(int)daysSinceMeal} days and is too grouchy to work");
                            }
                            else if (daysToGrouchy > 5)
                            {
                                GUILayout.Label($"{crew.name} is secretly munching a smuggled bag of potato chips");
                            }
                            else if (daysToGrouchy < 2)
                            {
                                GUILayout.Label($"{crew.name} hasn't eaten in {(int)daysSinceMeal} days and will quit working in a couple of days if this keeps up.");
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (crewDelta == 0)
                    {
                        GUILayout.Label($"To sustain its crew of {crewCount + crewDelta}, this vessel is using:");
                    }
                    else
                    {
                        GUILayout.Label($"To sustain a crew of {crewCount + crewDelta} this vessel would use:");
                    }
                    GUILayout.EndHorizontal();


                    // TODO:
                    //  ((**If the crew has active producers but no fertilizer consumption for the given tier:))
                    //      The Nom-o-tron(Tier3) is offline for lack of appropriate fertilizer
                    //  


                    foreach (var resourceName in resourcesConsumed.Keys.OrderBy(n => n))
                    {
                        double perDay = SnackConsumption.UnitsPerSecondToUnitsPerDay(resourcesConsumed[resourceName]);
                        double daysLeft = resources[resourceName] / perDay;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{perDay:N1} {resourceName} per day ({daysLeft:N1} days left)");
                        GUILayout.EndHorizontal();
                    }

                    if (ColonizationResearchScenario.Instance.AgroponicsMaxTier == TechTier.Tier4)
                    {
                        // No point in talking about research anymore.
                    }
                    else if (researchSink.AgroponicResearch > 0)
                    {
                        double perDay = SnackConsumption.UnitsPerSecondToUnitsPerDay(researchSink.AgroponicResearch);
                        GUILayout.Label($"This vessel {(crewDelta == 0 ? "is contributing" : "would contribute")} {perDay:N1} units of agroponics research per day.  ({ColonizationResearchScenario.Instance.KerbalSecondsToGoUntilNextTier:N} are needed to reach the next tier).");
                    }
                    else if (snackProducers.Count > 0)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("This vessel is not contributing agroponic research.");
                        GUILayout.EndHorizontal();
                    }
                    // TODO: <IN VAB>
                    // Balance supply load for a trip duration of:
                    // [---------------------------*------------] kerbal days
                }
            }
        }

        private class ResearchSink
            : IColonizationResearchScenario
        {
            public double AgroponicResearch { get; private set; }

            TechTier IColonizationResearchScenario.AgroponicsMaxTier =>
                ColonizationResearchScenario.Instance?.AgroponicsMaxTier ?? TechTier.Tier4;

            void IColonizationResearchScenario.ContributeAgroponicResearch(double timespent)
            {
                this.AgroponicResearch += timespent;
            }
        }
    }
}
