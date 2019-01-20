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
        : CbnToolbarDialog
    {
        // If simulating + or - crewman, this becomes positive or negative.
        private int crewDelta = 0;
        // CrewDelta gets reset when lastActiveVessel no longer equals the current vessel.
        private Vessel lastActiveVessel;
        private string consumptionAndProductionInformation;
        private bool showingWhatIfButtons;
        private bool showingResourceTransfer;

        private IntervesselResourceTransfer resourceTransfer = new IntervesselResourceTransfer();

        protected override bool IsRelevant => FlightGlobals.ActiveVessel.GetCrewCount() > 0 && !FlightGlobals.ActiveVessel.isEVA;
        protected override ApplicationLauncher.AppScenes VisibleInScenes { get; } = ApplicationLauncher.AppScenes.FLIGHT;

        protected override MultiOptionDialog DrawDialog()
        {
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
                            new DialogGUIButton("Remove", () => { --crewDelta; }, () => FlightGlobals.ActiveVessel.GetCrewCount() + this.crewDelta > 1, false),
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

            return new MultiOptionDialog(
                        "LifeSupportMonitor",  // <- no idea what this does.
                        "",
                        "Colony Status",
                        HighLogic.UISkin,
                        new DialogGUIVerticalLayout(parts.ToArray()));
        }

        protected override void OnFixedUpdate()
        {
            resourceTransfer.OnFixedUpdate();

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
            List<ITieredProducer> snackProducers = activeSnackConsumption.Vessel.FindPartModulesImplementing<ITieredProducer>();

            BuildStatusString(activeSnackConsumption, availableResources, availableStorage, snackProducers, crewCount, crewDelta, out string message);
            this.consumptionAndProductionInformation = message;
        }

        internal static void BuildStatusString(
            SnackConsumption activeSnackConsumption,
            Dictionary<string, double> resources,
            Dictionary<string, double> storage,
            List<ITieredProducer> snackProducers,
            int crewCount,
            int crewDelta,
            out string message)
        {
            StringBuilder text = new StringBuilder();

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
                    Dictionary<int, List<ProtoCrewMember>> buckets = new Dictionary<int, List<ProtoCrewMember>>();
                    // TODO: Somehow bucketize this, since all the crew are likely in the same state.
                    foreach (var crew in activeSnackConsumption.Vessel.GetVesselCrew())
                    {
                        var kerbalIsKnown = LifeSupportScenario.Instance.TryGetStatus(crew, out double daysSinceMeal, out double daysToGrouchy, out bool isGrouchy);
                        if (!kerbalIsKnown)
                        {
                            // TODO: Maybe if ! on kerban we complain about this?
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
                            text.AppendLine(CrewBlurbs.StarvingKerbals(crewInBucket));
                        }
                        else if (daysToGrouchy < 2)
                        {
                            text.AppendLine(CrewBlurbs.GrumpyKerbals(crewInBucket, daysToGrouchy, snackProducers.Any()));
                        }
                        else
                        {
                            text.AppendLine(CrewBlurbs.HungryKerbals(crewInBucket, daysToGrouchy, snackProducers.Any()));
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

            IEnumerable<TieredResource> IColonizationResearchScenario.AllResourcesTypes => ColonizationResearchScenario.Instance.AllResourcesTypes;

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

            TechTier IColonizationResearchScenario.GetMaxUnlockedScanningTier(string atBody)
                => ColonizationResearchScenario.Instance.GetMaxUnlockedScanningTier(atBody);

            TechTier IColonizationResearchScenario.GetMaxUnlockedTier(TieredResource forResource, string atBody)
                => ColonizationResearchScenario.Instance.GetMaxUnlockedTier(forResource, atBody);

            bool IColonizationResearchScenario.TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier)
                => ColonizationResearchScenario.Instance.TryParseTieredResourceName(tieredResourceName, out resource, out tier);


        }
    }
}
