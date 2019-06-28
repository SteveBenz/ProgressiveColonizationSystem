using ProgressiveColonizationSystem.ProductionChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    public class SnackConsumption
        : VesselModule
    {
        public const double DrillCapacityMultiplierForAutomaticMiningQualification = 5.0;

        [KSPField(isPersistant = true)]
        public double LastUpdateTime;

        /// <summary>
        ///   This is the vessel ID of the rover or lander that is used to automatically supply
        ///   the base.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string supplierMinerCraftId = "";

        /// <summary>
        ///   The vessel ID of the rover that last pushed the minimum quantity of resources to the station.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string lastMinerToDepositCraftId = "";

        protected IResourceBroker _resBroker;
        public IResourceBroker ResBroker
        {
            get { return _resBroker ?? (_resBroker = new ResourceBroker()); }
        }

        protected ResourceConverter _resConverter;
        public ResourceConverter ResConverter
        {
            get { return _resConverter ?? (_resConverter = new ResourceConverter()); }
        }

        /// <summary>
        ///   This is called on each physics frame for the active vessel by reflection-magic from KSP.
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel == null || !vessel.loaded || LifeSupportScenario.Instance == null)
            {
                return;
            }

            // Compute this early, since it sets the last update time member
            double deltaTime = GetDeltaTime();
            if (deltaTime < 0)
            {
                // This is the vessel launch - nothing to do but set the update time.
                return;
            }

            if (vessel.isEVA)
            {
                // TODO: What to do here?  Should Kerbals on EVA ever go hungry?
                return;
            }

            List<ProtoCrewMember> crew = vessel.GetVesselCrew();
            if (crew.Count == 0)
            {
                // Nobody on board
                return;
            }

            if (this.IsAtHome)
            {
                // While actually on Kerbal, the Kerbals will order take-out rather than consuming
                // what's in the ship.
                LifeSupportScenario.Instance?.KerbalsHaveReachedHomeworld(this.vessel);
            }
            else
            {
                this.ProduceAndConsume(crew, deltaTime);
            }
        }

        private bool IsMiningLanderPresent
        {
            get
            {
                if (string.IsNullOrEmpty(this.supplierMinerCraftId))
                {
                    return false;
                }
                else
                {
                    Guid vesselId = new Guid(this.supplierMinerCraftId);
                    Vessel vessel = FlightGlobals.Vessels.FirstOrDefault(v => v.id == vesselId);
                    return vessel != null
                        && Waypoints.StraightLineDistanceInMeters(this.vessel, vessel) < 2200
                        && (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED)
                        && this.vessel.GetVesselCrew().Any(c => c.trait == KerbalRoster.pilotTrait);
                }
            }
        }

        /// <summary>
        ///   This gets a string that summarizes the state of the miner for the <see cref="LifeSupportStatusMonitor"/>
        /// </summary>
        public void GetMinerStatusMessage(out bool isHookedUp, out string message)
        {
            if (string.IsNullOrEmpty(this.supplierMinerCraftId))
            {
                isHookedUp = false;
                message = null;
                return;
            }

            Guid vesselId = new Guid(this.supplierMinerCraftId);
            Vessel minerVessel = FlightGlobals.Vessels.FirstOrDefault(v => v.id == vesselId);
            if (minerVessel == null)
            {
                isHookedUp = false;
                message = null;
                return;
            }

            if (!minerVessel.loaded || (minerVessel.situation != Vessel.Situations.LANDED && minerVessel.situation != Vessel.Situations.SPLASHED))
            {
                isHookedUp = false;
                message = $"{minerVessel.vesselName} is set up to automatically fetch Crush-Ins for this base, but it's not present.";
                return;
            }

            if (!this.vessel.GetVesselCrew().Any(c => c.trait == KerbalRoster.pilotTrait))
            {
                isHookedUp = false;
                message = $"{minerVessel.vesselName} is set up to automatically fetch Crush-Ins for this base, but there's no pilot here to drive it.";
                return;
            }

            isHookedUp = true;
            message = $"{minerVessel.vesselName} is automatically fetching Crush-Ins for this base.";
        }

        internal void MiningMissionFinished(Vessel sourceVessel, double amountSent)
        {
            if (sourceVessel.GetCrewCapacity() < 2)
            {
                ScreenMessages.PostScreenMessage("This vessel doesn't qualify to be an automatic miner -- it doesn't have a crew capacity of 2 or more", 15.0f);
                return;
            }

            double totalCapacityAtBase = this.vessel.FindPartModulesImplementing<ITieredProducer>()
                .Where(p => p.Input == ColonizationResearchScenario.Instance.CrushInsResource)
                .Sum(p => p.ProductionRate);
            double minimumQualifyingAmount = totalCapacityAtBase * DrillCapacityMultiplierForAutomaticMiningQualification;
            if (amountSent < minimumQualifyingAmount)
            {
                ScreenMessages.PostScreenMessage($"This vessel doesn't qualify towards becoming an automatic miner -- less than {minimumQualifyingAmount} was transferred.", 15.0f);
                return;
            }

            string sourceVesselId = sourceVessel.id.ToString();
            if (this.lastMinerToDepositCraftId == sourceVesselId)
            {
                if (this.supplierMinerCraftId != sourceVesselId)
                {
                    PopupMessageWithKerbal.ShowPopup("We Got it From here!",
                        $"Looks like the {sourceVessel.vesselName} is a fine vessel for grabbing resources.  "
                        + "If you don't mind, leaving her parked here, Kerbals at the base will automatically "
                        + "drive it out and gather resources in the future.",
                        "Because you delivered two loads of Crush-Ins to your base with this craft, you qualify "
                        + "for automatic mining.  That means that if you leave your base alone, even for a long "
                        + "time, when you return they'll still be full of Crush-Ins.  This depends on you leaving "
                        + "the ship parked in physics-range of the base (2.2km) and the base having a Pilot on "
                        + "board to do the driving.",
                        "Thanks!");
                    this.supplierMinerCraftId = sourceVesselId;
                }
            }
            else
            {
                ScreenMessages.PostScreenMessage($"{this.vessel.vesselName} has given a signed bill of lading to {sourceVessel.vesselName} -- one more delivery and it'll be certified for automatic use!", 15.0f);
                this.lastMinerToDepositCraftId = sourceVesselId;
            }
        }

        /// <summary>
        ///   Calculates snacks consumption aboard the vessel.
        /// </summary>
        /// <param name="crew">The crew</param>
        /// <param name="deltaTime">The amount of time (in seconds) since the last calculation was done</param>
        /// <returns>The amount of <paramref name="deltaTime"/> in which food was supplied.</returns>
        private void ProduceAndConsume(List<ProtoCrewMember> crew, double deltaTime)
        {
            var tieredProducers = this.vessel.FindPartModulesImplementing<ITieredProducer>();
            var combiners = this.vessel.FindPartModulesImplementing<ITieredCombiner>();
            this.ResourceQuantities(out var availableResources, out var availableStorage);
            var crewPart = vessel.parts.FirstOrDefault(p => p.CrewCapacity > 0);
            double remainingTime = deltaTime;

            while (remainingTime > ResourceUtilities.FLOAT_TOLERANCE)
            {
                TieredProduction.CalculateResourceUtilization(
                    crew.Count,
                    remainingTime,
                    tieredProducers,
                    combiners,
                    ColonizationResearchScenario.Instance,
                    availableResources,
                    availableStorage,
                    out double elapsedTime,
                    out List<TieredResource> breakthroughCategories,
                    out Dictionary<string,double> resourceConsumptionPerSecond,
                    out Dictionary<string,double> resourceProductionPerSecond,
                    out var _, out var _);

                if (elapsedTime == 0)
                {
                    LifeSupportScenario.Instance.KerbalsMissedAMeal(this.vessel,
                        hasActiveProducers: tieredProducers.Any(p => p.IsProductionEnabled));
                    break;
                }

                if (resourceConsumptionPerSecond != null || resourceProductionPerSecond != null)
                {
                    ConversionRecipe consumptionRecipe = new ConversionRecipe();
                    if (resourceConsumptionPerSecond != null)
                    {
                        foreach (var pair in resourceConsumptionPerSecond)
                        {
                            double newAmount = availableResources[pair.Key] - pair.Value * elapsedTime;
                            if (newAmount < ResourceUtilities.FLOAT_TOLERANCE)
                            {
                                availableResources.Remove(pair.Key);
                            }
                            else
                            {
                                availableResources[pair.Key] = newAmount;
                            }

                            ColonizationResearchScenario.Instance.TryParseTieredResourceName(pair.Key, out var consumedResource, out var consumedResourceTier);
                            if (consumedResource == ColonizationResearchScenario.LodeResource &&
                                ResourceLodeScenario.Instance.TryFindResourceLodeInRange(vessel, consumedResourceTier, out var resourceLode))
                            {
                                ResourceLodeScenario.Instance.TryConsume(resourceLode, pair.Value * elapsedTime, out _);
                            }
                            else if (!ResourceIsAutosupplied(pair.Key))
                            {
                                consumptionRecipe.Inputs.Add(new ResourceRatio()
                                {
                                    ResourceName = pair.Key,
                                    Ratio = pair.Value,
                                    DumpExcess = false,
                                    FlowMode = ResourceFlowMode.ALL_VESSEL
                                });
                            }
                        }
                    }
                    if (resourceProductionPerSecond != null)
                    {
                        foreach (var pair in resourceProductionPerSecond)
                        {
                            double newAmount = availableStorage[pair.Key] - elapsedTime * pair.Value;
                            if (newAmount < ResourceUtilities.FLOAT_TOLERANCE)
                            {
                                availableStorage.Remove(pair.Key);
                            }
                            else
                            {
                                availableStorage[pair.Key] = newAmount;
                            }
                        }

                        consumptionRecipe.Outputs.AddRange(
                            resourceProductionPerSecond.Select(pair => new ResourceRatio()
                            {
                                ResourceName = pair.Key,
                                Ratio = pair.Value,
                                DumpExcess = true,
                                FlowMode = ResourceFlowMode.ALL_VESSEL
                            }));
                    }

                    var consumptionResult = this.ResConverter.ProcessRecipe(elapsedTime, consumptionRecipe, crewPart, null, 1f);
                    Debug.Assert(Math.Abs(consumptionResult.TimeFactor - elapsedTime) < ResourceUtilities.FLOAT_TOLERANCE,
                        "ProgressiveColonizationSystem.SnackConsumption.CalculateSnackFlow is busted - it somehow got the consumption recipe wrong.");
                }

                foreach (TieredResource resource in breakthroughCategories)
                {
                    TechTier newTier = ColonizationResearchScenario.Instance.GetMaxUnlockedTier(resource, this.vessel.lastBody.name);
                    string title = $"{resource.ResearchCategory.DisplayName} has progressed to {newTier.DisplayName()}!";
                    string message = resource.ResearchCategory.BreakthroughMessage(crew, newTier);
                    string boringMessage = resource.ResearchCategory.BoringBreakthroughMessage(crew, newTier);
                    PopupMessageWithKerbal.ShowPopup(title, message, boringMessage, "That's Just Swell");
                }

                remainingTime -= elapsedTime;

                double lastMealTime = Planetarium.GetUniversalTime() - remainingTime;
                LifeSupportScenario.Instance.KerbalsHadASnack(this.vessel, lastMealTime);
            }
        }

        private bool ResourceIsAutosupplied(string tieredResourceName)
        {
            if (this.IsMiningLanderPresent)
            {
                ColonizationResearchScenario.Instance.TryParseTieredResourceName(tieredResourceName, out var resource, out _);
                return resource == ColonizationResearchScenario.Instance.CrushInsResource;
            }
            else
            {
                return false;
            }
        }

        internal void ResourceQuantities(out Dictionary<string, double> availableResources, out Dictionary<string, double> availableStorage)
            => ResourceQuantities(this.vessel, 100 * ResourceUtilities.FLOAT_TOLERANCE, out availableResources, out availableStorage);

        internal static void ResourceQuantities(Vessel vessel, double minimumAmount, out Dictionary<string, double> availableResources, out Dictionary<string, double> availableStorage)
        {
            availableResources = new Dictionary<string, double>();
            availableStorage = new Dictionary<string, double>();
            foreach (var part in vessel.parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (resource.resourceName == "ElectricCharge")
                    {
                        continue;
                    }

                    // Be careful that we treat nearly-zero as zero, as otherwise we can get into an infinite
                    // loop when the resource calculator decides the amount is too minute to rate more than 0 time.
                    if (resource.flowState && resource.amount > minimumAmount)
                    {
                        availableResources.TryGetValue(resource.resourceName, out double amount);
                        availableResources[resource.resourceName] = amount + resource.amount;
                    }
                    if (resource.flowState && resource.maxAmount - resource.amount > minimumAmount)
                    {
                        availableStorage.TryGetValue(resource.resourceName, out double amount);
                        availableStorage[resource.resourceName] = amount + resource.maxAmount - resource.amount;
                    }
                }
            }

            // Add a magic container that has whatever stuff the planet has
            foreach (var resourceLode in ResourceLodeScenario.Instance.FindResourceLodesInRange(vessel))
            {
                availableResources.Add(ColonizationResearchScenario.LodeResource.TieredName(resourceLode.Tier), resourceLode.Quantity);
            }
        }

        public bool IsAtHome => vessel.mainBody == FlightGlobals.GetHomeBody() && vessel.altitude < 10000;

        private double GetDeltaTime()
        {
            if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
            {
                return -1;
            }

            if (Math.Abs(LastUpdateTime) < ResourceUtilities.FLOAT_TOLERANCE)
            {
                // Just started running
                LastUpdateTime = Planetarium.GetUniversalTime();
                return -1;
            }

            double now = Planetarium.GetUniversalTime();
            double deltaTime = now - LastUpdateTime;

            LastUpdateTime = now;
            return deltaTime;
        }
    }
}
