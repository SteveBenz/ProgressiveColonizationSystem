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
        [KSPField(isPersistant = true)]
        public double LastUpdateTime;

        /// <summary>
        ///   This is the vessel ID of the rover or lander that is used to automatically supply
        ///   the base.
        /// </summary>
        [KSPField(isPersistant = true)]
        public Guid supplierMinerCraftId = Guid.Empty;

        /// <summary>
        ///   The vessel ID of the rover that last pushed the minimum quantity of resources to the station.
        /// </summary>
        [KSPField(isPersistant = true)]
        public Guid lastMinerToDepositCraftId = Guid.Empty;

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
                foreach (var crewman in crew)
                {
                    LifeSupportScenario.Instance?.KerbalHasReachedHomeworld(crewman);
                }
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
                if (this.supplierMinerCraftId == Guid.Empty)
                {
                    return false;
                }
                else
                {
                    Guid vesselId = this.supplierMinerCraftId;
                    Vessel vessel = FlightGlobals.Vessels.FirstOrDefault(v => v.id == vesselId);
                    return vessel.loaded && (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED);
                }
            }
        }

        internal void MiningMissionFinished(Vessel sourceVessel)
        {
            if (this.lastMinerToDepositCraftId == sourceVessel.id)
            {
                if (this.supplierMinerCraftId != sourceVessel.id)
                {
                    // TODO: Funner messages.
                    PopupMessageWithKerbal.ShowPopup("We Got it From here!",
                        $"Looks like the {sourceVessel.name} is a fine vessel for grabbing resources.",
                        $"Looks like the {sourceVessel.name} is a fine vessel for grabbing resources.",
                        "Thanks!");
                    this.supplierMinerCraftId = sourceVessel.id;
                }
            }
            else
            {
                this.lastMinerToDepositCraftId = sourceVessel.id;
            }
        }

        /// <summary>
        ///   Calculates snacks consumption aboard the vessel.
        /// </summary>
        /// <param name="crew">The crew</param>
        /// <param name="deltaTime">The amount of time (in seconds) since the last calculation was done</param>
        /// <returns>The amount of <paramref name="deltaTime"/> in which food was supplied.</returns>
        private double ProduceAndConsume(List<ProtoCrewMember> crew, double deltaTime)
        {
            var snackProducers = this.vessel.FindPartModulesImplementing<ITieredProducer>();
			this.ResourceQuantities(out var availableResources, out var availableStorage);
            var crewPart = vessel.parts.FirstOrDefault(p => p.CrewCapacity > 0);
            double remainingTime = deltaTime;

            while (remainingTime > ResourceUtilities.FLOAT_TOLERANCE)
            {
                TieredProduction.CalculateResourceUtilization(
                    crew.Count,
                    deltaTime,
                    snackProducers,
                    ColonizationResearchScenario.Instance,
                    availableResources,
					availableStorage,
                    out double elapsedTime,
                    out List<TieredResource> breakthroughCategories,
                    out Dictionary<string,double> resourceConsumptionPerSecond,
                    out Dictionary<string,double> resourceProductionPerSecond);

                if (elapsedTime == 0)
                {
                    break;
                }

                if (resourceConsumptionPerSecond != null || resourceProductionPerSecond != null)
                {
                    ConversionRecipe consumptionRecipe = new ConversionRecipe();
                    if (resourceConsumptionPerSecond != null)
                    {
                        // ISSUE 2019/2: This isn't really ideal, since finding nearby lodes is not a cheap operation
                        //   and it gets done twice.  But perhaps it's all mute because the whole resource chain calculation
                        //   is expensive as well and perhaps there's a way to compute it less than once a frame.
                        if (ResourceLodeScenario.Instance.TryFindResourceLodeInRange(vessel, out var resourceLode)
                         && resourceConsumptionPerSecond.TryGetValue(ColonizationResearchScenario.LodeResource.TieredName(resourceLode.Tier), out double lodeConsumptionPerSecond))
                        {
                            ResourceLodeScenario.Instance.TryConsume(resourceLode, lodeConsumptionPerSecond * elapsedTime, out _);
                        }
                        else
                        {
                            consumptionRecipe.Inputs.AddRange(resourceConsumptionPerSecond
                                .Where(pair => !resourceIsAutosupplied(pair.Key))
                                .Select(pair => new ResourceRatio()
                                {
                                    ResourceName = pair.Key,
                                    Ratio = pair.Value,
                                    DumpExcess = false,
                                    FlowMode = ResourceFlowMode.ALL_VESSEL
                                }));
                        }
                    }
                    if (resourceProductionPerSecond != null)
                    {
                        consumptionRecipe.Outputs.AddRange(
                            resourceProductionPerSecond.Select(pair => new ResourceRatio()
                            {
                                ResourceName = pair.Key,
                                Ratio = pair.Value,
                                DumpExcess = true,
                                FlowMode = ResourceFlowMode.ALL_VESSEL
                            }));
                    }
                    Debug.Assert(elapsedTime > 0);
                    var consumptionResult = this.ResConverter.ProcessRecipe(elapsedTime, consumptionRecipe, crewPart, null, 1f);
                    Debug.Assert(Math.Abs(consumptionResult.TimeFactor - elapsedTime) < ResourceUtilities.FLOAT_TOLERANCE,
                        "ProgressiveColonizationSystem.SnackConsumption.CalculateSnackFlow is busted - it somehow got the consumption recipe wrong.");
                }

                foreach (TieredResource resource in breakthroughCategories)
                {
                    TechTier newTier = ColonizationResearchScenario.Instance.GetMaxUnlockedTier(resource, this.vessel.lastBody.name);
                    string title = $"{resource.ResearchCategory.DisplayName} has progressed to {newTier.DisplayName()}!";
                    string message = resource.ResearchCategory.BreakthroughMessage(newTier);
                    string boringMessage = resource.ResearchCategory.BoringBreakthroughMessage(newTier);
                    PopupMessageWithKerbal.ShowPopup(title, message, boringMessage, "That's Just Swell");
                }

                remainingTime -= elapsedTime;
            }

            if (remainingTime != deltaTime)
            {
                double lastMealTime = Planetarium.GetUniversalTime() - remainingTime;
                // Somebody got something to eat - record that.
                foreach (var crewMember in crew)
                {
                    LifeSupportScenario.Instance.KerbalHadASnack(crewMember, lastMealTime);
                }
            }

            if (remainingTime > ResourceUtilities.FLOAT_TOLERANCE)
            {
                // We ran out of food
                // TODO: Maybe we ought to have a single message for the whole crew?
                foreach (var crewMember in crew)
                {
                    LifeSupportScenario.Instance.KerbalMissedAMeal(crewMember);
                }
                return deltaTime - remainingTime;
            }
            else
            {
                return 0;
            }
        }

        private bool resourceIsAutosupplied(string tieredResourceName)
        {
            if (this.IsMiningLanderPresent)
            {
                ColonizationResearchScenario.Instance.TryParseTieredResourceName(tieredResourceName, out var resource, out _);
                return resource == ColonizationResearchScenario.CrushInsResource;
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
            if (ResourceLodeScenario.Instance.TryFindResourceLodeInRange(vessel, out var resourceLode))
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

            double maxDeltaTime = ResourceUtilities.GetMaxDeltaTime();
            double deltaTime = Math.Min(Planetarium.GetUniversalTime() - LastUpdateTime, maxDeltaTime);

            LastUpdateTime += deltaTime;
            return deltaTime;
        }
    }
}
