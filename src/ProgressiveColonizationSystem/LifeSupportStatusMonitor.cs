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
        private string productionMessage;
        private List<ResearchData> progress;
        private bool showingWhatIfButtons;
        private bool showingResourceTransfer;

        internal class ResearchData
        {
            public string Category;
            public TechTier Tier;
            public double ProgressPerDay;
            public double AccumulatedProgress;
            public double NextTier;
        }

        private IntervesselResourceTransfer resourceTransfer = new IntervesselResourceTransfer();

        public LifeSupportStatusMonitor()
            : base(new string[] { ProductionTab, ProgressionTab, TransferTab, CrewTab })
        {
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

        protected override bool IsRelevant => FlightGlobals.ActiveVessel.GetCrewCount() > 0 && !FlightGlobals.ActiveVessel.isEVA;
        protected override ApplicationLauncher.AppScenes VisibleInScenes { get; } = ApplicationLauncher.AppScenes.FLIGHT;

        private DialogGUIBase DrawProductionTab()
        {
            var body = new DialogGUILabel(() => this.productionMessage);
            var whatif = new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                            new DialogGUILabel("What if we"),
                            new DialogGUIButton("Add", () => { ++crewDelta; }, () => true, false),
                            new DialogGUILabel("/"),
                            new DialogGUIButton("Remove", () => { --crewDelta; }, () => FlightGlobals.ActiveVessel.GetCrewCount() + this.crewDelta > 1, false),
                            new DialogGUILabel("a kerbal?"));
            return new DialogGUIVerticalLayout(body, new DialogGUIFlexibleSpace(), whatif);
        }

        private DialogGUIBase DrawProgressionTab()
        {
            if (this.progress.Count == 0)
            {
                return new DialogGUILabel("No technologies are being researched here.");
            }

            DialogGUIBase[] rows = new DialogGUIBase[this.progress.Count + 1];
            rows[0] = new DialogGUIHorizontalLayout(
                    new DialogGUILabel("<B><U>Field:</U></B>", 170),
                    new DialogGUILabel("<B><U>Progress:</U></B>", 70),
                    new DialogGUILabel("<B><U>Days To Go:</U></B>", 70)
                );
            
            for (int i = 0; i < this.progress.Count; ++i)
            {
                var field = this.progress[i];
                double progress = field.AccumulatedProgress / field.NextTier;
                double daysToGo = (field.NextTier - field.AccumulatedProgress) / field.ProgressPerDay;
                rows[i + 1] = new DialogGUIHorizontalLayout(
                    new DialogGUILabel(field.Category, 170),
                    new DialogGUILabel($"{100*progress:N}%", 70),
                    new DialogGUILabel(daysToGo.ToString("N"), 70));
            }

            return new DialogGUIVerticalLayout(rows);
        }

        private DialogGUIBase DrawTransferTab()
        {
            return new DialogGUIVerticalLayout(
                new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                    new DialogGUILabel("Target: "),
                    new DialogGUILabel(resourceTransfer.TargetVessel?.GetDisplayName())),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                    new DialogGUIButton("Start", resourceTransfer.StartTransfer, () => resourceTransfer.TargetVessel != null && !resourceTransfer.IsTransferUnderway, dismissOnSelect: false),
                    new DialogGUISlider(() => (float)resourceTransfer.TransferPercent, 0, 1, false, 100, 20, null)));
        }

        private class RequirementAndQuantity
        {
            public string Trait;
            public int SkillLevel;
            public double Quantity;

            public string GetTraitDescription(List<ProtoCrewMember> crew)
            {
                List<ExperienceTraitConfig> careers = GameDatabase.Instance.ExperienceConfigs
                    .GetTraitsWithEffect(this.Trait)
                    .Select(name => GameDatabase.Instance.ExperienceConfigs.GetExperienceTraitConfig(name))
                    .ToList();

                StringBuilder info = new StringBuilder();
                info.Append($"{this.Quantity:N1}: ");
                for (int i = 0; i < careers.Count; ++i)
                {
                    ExperienceEffectConfig effectConfig = careers[i].Effects.First(effect => effect.Name == this.Trait);
                    int numStars = SkillLevel - int.Parse(effectConfig.Config.GetValue("level"));

                    if (i == careers.Count - 1)
                    {
                        info.Append(" or a ");
                    }
                    else if (i > 0)
                    {
                        info.Append(", ");
                    }

                    info.Append(PksCrewRequirement.DescribeKerbalTrait(numStars, careers[i].Title));
                    info.Append($"({crew.Count(c => c.trait == careers[i].Title && c.experienceLevel >= numStars)})");
                }

                return info.ToString();
            }
        }

        private DialogGUIBase DrawCrewTab()
        {
            // Goal:
            // Show how many crew are required to run the thing
            List<IPksCrewRequirement> activatedParts = FlightGlobals.ActiveVessel
                .FindPartModulesImplementing<IPksCrewRequirement>()
                .ToList();
            List<ProtoCrewMember> kspCrew = FlightGlobals.ActiveVessel.GetVesselCrew();

            var requirements = activatedParts
                .GroupBy(p => p.RequiredEffect + p.RequiredLevel.ToString())
                .OrderBy(g => g.Key)
                .ToArray()
                .Select(g => new RequirementAndQuantity()
                {
                    Trait = g.First().RequiredEffect,
                    SkillLevel = g.First().RequiredLevel,
                    Quantity = g.Sum(p => p.CapacityRequired)
                })
                .Select(randq => randq.GetTraitDescription(kspCrew))
                .ToList();
            var snackConsumption = FlightGlobals.ActiveVessel.vesselModules
                .OfType<SnackConsumption>().FirstOrDefault();
            if (!string.IsNullOrEmpty(snackConsumption?.supplierMinerCraftId))
            {
                int numPilots = kspCrew.Count(c => c.trait == KerbalRoster.pilotTrait);
                requirements.Add($"1 Rover Pilot({numPilots})");
            }

            return new DialogGUILabel(string.Join("\r\n", requirements.ToArray()));
        }

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

            BuildStatusStrings(isHookedUp, activeSnackConsumption, availableResources, availableStorage, tieredProducers, tieredCombiners, crewCount, crewDelta
                , out string productionMessage, out List<ResearchData> progress);
            this.productionMessage = (minerStatusMessage == null ? "" : minerStatusMessage + "\r\n\r\n") + productionMessage;
            this.progress = progress;
        }

        internal static void BuildStatusStrings(
            bool hasAutoCrushinSupply,
            SnackConsumption activeSnackConsumption,
            Dictionary<string, double> resources,
            Dictionary<string, double> storage,
            List<ITieredProducer> tieredProducers,
            List<ITieredCombiner> tieredCombiners,
            int crewCount,
            int crewDelta,
            out string productionMessage,
            out List<ResearchData> progress)
        {
            StringBuilder productionMessageBuilder = new StringBuilder();

            ResearchSink researchSink = new ResearchSink();
            TieredProduction.CalculateResourceUtilization(
                crewCount + crewDelta, 1, tieredProducers, tieredCombiners, researchSink, resources, storage,
                out double timePassed, out var _, out Dictionary<string, double> resourcesConsumed,
                out Dictionary<string, double> resourcesProduced);
            if (timePassed == 0)
            {
                productionMessageBuilder.AppendLine("There aren't enough supplies or producers here to feed any kerbals.");
                progress = new List<ResearchData>();

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
                            productionMessageBuilder.AppendLine(CrewBlurbs.StarvingKerbals(crewInBucket));
                        }
                        else if (daysToGrouchy < 2)
                        {
                            productionMessageBuilder.AppendLine(CrewBlurbs.GrumpyKerbals(crewInBucket, daysToGrouchy, tieredProducers.Any()));
                        }
                        else
                        {
                            productionMessageBuilder.AppendLine(CrewBlurbs.HungryKerbals(crewInBucket, daysToGrouchy, tieredProducers.Any()));
                        }
                    }
                }
            }
            else
            {
                if (crewDelta == 0)
                {
                    productionMessageBuilder.AppendLine($"To sustain its crew of {crewCount + crewDelta}, this vessel is using:");
                }
                else
                {
                    productionMessageBuilder.AppendLine($"To sustain a crew of {crewCount + crewDelta} this vessel would use:");
                }

                foreach (var resourceName in resourcesConsumed.Keys.OrderBy(n => n))
                {
                    if (!IsCrushinResource(researchSink, resourceName))
                    {
                        double perDay = TieredProduction.UnitsPerSecondToUnitsPerDay(resourcesConsumed[resourceName]);
                        double daysLeft = resources[resourceName] / perDay;
                        productionMessageBuilder.AppendLine($"{perDay:N1} {resourceName} per day ({daysLeft:N1} days left)");
                    }
                }

                if (resourcesProduced != null && resourcesProduced.Count > 0)
                {
                    productionMessageBuilder.AppendLine();
                    productionMessageBuilder.AppendLine("The crew is also producing:");
                    foreach (var resourceName in resourcesProduced.Keys.OrderBy(n => n))
                    {
                        double perDay = TieredProduction.UnitsPerSecondToUnitsPerDay(resourcesProduced[resourceName]);
                        productionMessageBuilder.AppendLine($"{perDay:N1} {resourceName} per day");
                    }
                }

                progress = researchSink.Data
                    .Select(pair => new ResearchData()
                    {
                        Category = pair.Key.DisplayName,
                        NextTier = pair.Value.KerbalDaysRequired,
                        AccumulatedProgress = pair.Value.AccumulatedKerbalDays,
                        ProgressPerDay = pair.Value.KerbalDaysContributedPerDay
                    })
                    .ToList();
            }

            productionMessage = productionMessageBuilder.ToString();
        }

        private static bool IsCrushinResource(IColonizationResearchScenario researchScenario, string resourceName)
        {
            return researchScenario.TryParseTieredResourceName(resourceName, out TieredResource tieredResource, out var _)
                && tieredResource == researchScenario.CrushInsResource;
        }
    }
}
