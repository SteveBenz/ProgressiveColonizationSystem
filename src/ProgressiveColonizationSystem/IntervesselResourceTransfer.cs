using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgressiveColonizationSystem
{
    public class IntervesselResourceTransfer
    {
        private double transferStartTime;
        private double expectedTransferCompleteTime;
        private double lastTransferTime;
        private ResourceConverter resourceConverter;
        private ConversionRecipe thisVesselConversionRecipe;
        private ConversionRecipe otherVesselConversionRecipe;
        private Vessel sourceVessel;

        private const double transferTimeInSeconds = 30;

        public bool IsTransferUnderway { get; private set; }

        public bool IsTransferComplete { get; private set; }

        public double TransferPercent
        {
            get
            {
                if (this.IsTransferComplete)
                {
                    return 1;
                }
                else if (this.IsTransferUnderway)
                {
                    return Math.Min(1, (Planetarium.GetUniversalTime() - transferStartTime) / (expectedTransferCompleteTime - transferStartTime));
                }
                else
                {
                    return 0;
                }
            }
        }

        public Vessel TargetVessel { get; set; }

        public void StartTransfer(IReadOnlyDictionary<string, TransferDirection> userOverrides)
        {
            if (IsTransferUnderway)
            {
                return;
            }

            var transfers = TryFindResourceToTransfer(FlightGlobals.ActiveVessel, this.TargetVessel);

            var thisVesselConversionRecipe = new ConversionRecipe();
            var otherVesselConversionRecipe = new ConversionRecipe();

            bool anythingGoingOn = false;
            foreach (var item in transfers)
            {
                if (!userOverrides.TryGetValue(item.ResourceName, out var direction))
                {
                    direction = item.SuggestedDirection;
                }

                if (direction == TransferDirection.Send)
                {
                    anythingGoingOn = true;
                    double amountPerSecond = item.MaxCanSend / transferTimeInSeconds;
                    thisVesselConversionRecipe.Inputs
                        .Add(new ResourceRatio(item.ResourceName, amountPerSecond, dumpExcess: false));
                    otherVesselConversionRecipe.Outputs
                        .Add(new ResourceRatio(item.ResourceName, amountPerSecond, dumpExcess: false));
                }
                else if (direction == TransferDirection.Receive)
                {
                    anythingGoingOn = true;
                    double amountPerSecond = item.MaxCanReceive / transferTimeInSeconds;
                    thisVesselConversionRecipe.Outputs
                        .Add(new ResourceRatio(item.ResourceName, amountPerSecond, dumpExcess: false));
                    otherVesselConversionRecipe.Inputs
                        .Add(new ResourceRatio(item.ResourceName, amountPerSecond, dumpExcess: false));
                }
            }

            if (anythingGoingOn)
            {
                this.sourceVessel = FlightGlobals.ActiveVessel;
                this.resourceConverter = new ResourceConverter();
                this.thisVesselConversionRecipe = thisVesselConversionRecipe;
                this.otherVesselConversionRecipe = otherVesselConversionRecipe;
                this.lastTransferTime = transferStartTime = Planetarium.GetUniversalTime();
                this.expectedTransferCompleteTime = transferStartTime + transferTimeInSeconds;
                this.IsTransferUnderway = true;
                this.IsTransferComplete = false;
            }
        }

        private void CheckIfSatisfiedAutoMiningRequirement()
        {
            foreach (var resource in this.thisVesselConversionRecipe.Outputs)
            {
                if (this.IsResourceSatisfyingMiningRequirement(resource))
                {
                    this.sourceVessel.vesselModules.OfType<SnackConsumption>().First()
                        .MiningMissionFinished(this.TargetVessel, resource.Ratio * transferTimeInSeconds);
                    return;
                }
            }
            foreach (var resource in this.thisVesselConversionRecipe.Inputs)
            {
                if (this.IsResourceSatisfyingMiningRequirement(resource))
                {
                    this.TargetVessel.vesselModules.OfType<SnackConsumption>().First()
                        .MiningMissionFinished(this.sourceVessel, resource.Ratio * transferTimeInSeconds);
                    return;
                }
            }
        }

        private bool IsResourceSatisfyingMiningRequirement(ResourceRatio resource)
        {
            if (!ColonizationResearchScenario.Instance.TryParseTieredResourceName(resource.ResourceName, out TieredResource tieredResource, out TechTier tier)
             || tieredResource != ColonizationResearchScenario.Instance.CrushInsResource)
            {
                return false;
            }

            // TODO: Validate that the amount is sufficient;
            return true;
        }

        public void Reset()
        {
            this.sourceVessel = null;
            this.resourceConverter = null;
            this.otherVesselConversionRecipe = null;
            this.thisVesselConversionRecipe = null;
            this.IsTransferUnderway = false;
            this.IsTransferComplete = false;
        }

        public void OnFixedUpdate()
        {
            if (FlightGlobals.ActiveVessel.situation != Vessel.Situations.LANDED)
            {
                this.Reset();
                return;
            }

            if (IsTransferComplete && FlightGlobals.ActiveVessel == this.sourceVessel)
            {
                return;
            }

            if (IsTransferUnderway && FlightGlobals.ActiveVessel == this.sourceVessel)
            {
                double now = Planetarium.GetUniversalTime();
                double elapsedTime = now - this.lastTransferTime;
                this.lastTransferTime = now;

                // Move the goodies
                var thisShipResults = this.resourceConverter.ProcessRecipe(elapsedTime, this.thisVesselConversionRecipe, this.sourceVessel.rootPart, null, 1f);
                var otherShipResults = this.resourceConverter.ProcessRecipe(elapsedTime, this.otherVesselConversionRecipe, this.TargetVessel.rootPart, null, 1f);
                if (thisShipResults.TimeFactor < elapsedTime || otherShipResults.TimeFactor < elapsedTime)
                {
                    this.CheckIfSatisfiedAutoMiningRequirement();
                    this.resourceConverter = null;
                    this.thisVesselConversionRecipe = null;
                    this.otherVesselConversionRecipe = null;
                    this.IsTransferComplete = true;
                }

                return;
            }
        }

        private static HashSet<string> GetVesselsProducers(Vessel vessel)
        {
            HashSet<string> products = new HashSet<string>();

            foreach (var tieredConverter in vessel.FindPartModulesImplementing<PksTieredResourceConverter>())
            {
                products.Add(tieredConverter.Output.TieredName(tieredConverter.Tier));
            }

            foreach (var oldSchoolConverter in vessel.FindPartModulesImplementing<ModuleResourceConverter>())
            {
                foreach (var resourceRation in oldSchoolConverter.Recipe.Outputs)
                {
                    products.Add(resourceRation.ResourceName);
                }
            }

            foreach (var combiner in vessel.FindPartModulesImplementing<PksTieredCombiner>())
            {
                products.Add(combiner.untieredOutput);
            }
            return products;
        }

        private static HashSet<string> GetVesselsConsumers(Vessel vessel)
        {
            HashSet<string> consumers = new HashSet<string>();

            // None of the tiered resources really make sense to produce in one place and
            // process in another, but the combiners do require this sort of thing.
            foreach (var combiner in vessel.FindPartModulesImplementing<PksTieredCombiner>())
            {
                consumers.Add(combiner.untieredInput);
            }
            return consumers;
        }

        public class ResourceTransferPossibility
        {
            public ResourceTransferPossibility(string resourceName, TransferDirection suggestedDirection, double maxCanSend, double maxCanRecieve)
            {
                this.ResourceName = resourceName;
                this.SuggestedDirection = suggestedDirection;
                this.MaxCanSend = maxCanSend;
                this.MaxCanReceive = maxCanRecieve;
            }

            public string ResourceName { get; }
            public TransferDirection SuggestedDirection { get; }
            public double MaxCanSend { get; }
            public double MaxCanReceive { get; }
        }

        public static IReadOnlyList<ResourceTransferPossibility> TryFindResourceToTransfer(Vessel sourceVessel, Vessel otherVessel)
        {
            SnackConsumption.ResourceQuantities(sourceVessel, 1, out Dictionary<string, double> thisShipCanSupply, out Dictionary<string, double> thisShipCanStore);
            SnackConsumption.ResourceQuantities(otherVessel, 1, out Dictionary<string, double> otherShipCanSupply, out Dictionary<string, double> otherShipCanStore);

            List<PksTieredResourceConverter> otherVesselProducers = otherVessel.FindPartModulesImplementing<PksTieredResourceConverter>();
            HashSet<string> thisShipsProducts = GetVesselsProducers(sourceVessel);
            HashSet<string> otherShipsProducts = GetVesselsProducers(otherVessel);
            HashSet<string> thisShipsConsumption = GetVesselsConsumers(sourceVessel);
            HashSet<string> otherShipsConsumption = GetVesselsConsumers(otherVessel);

            List<ResourceTransferPossibility> result = new List<ResourceTransferPossibility>();

            foreach (string resourceName in thisShipCanStore.Keys.Union(thisShipCanStore.Keys).Union(otherShipCanSupply.Keys).Union(otherShipCanStore.Keys))
            {
                double maxCanReceive;
                {
                    thisShipCanStore.TryGetValue(resourceName, out double maxCanStore);
                    otherShipCanSupply.TryGetValue(resourceName, out double maxCanBeSent);
                    maxCanReceive = Math.Min(maxCanStore, maxCanBeSent);
                }

                double maxCanSend;
                {
                    otherShipCanStore.TryGetValue(resourceName, out double maxCanStore);
                    thisShipCanSupply.TryGetValue(resourceName, out double maxCanBeSent);
                    maxCanSend = Math.Min(maxCanBeSent, maxCanStore);
                }

                if (maxCanReceive == 0 && maxCanSend == 0)
                {
                    continue;
                }

                TransferDirection transferDirection = TransferDirection.Neither;

                // if the player seems to have abandoned otherVessel
                if ((otherVessel.GetCrewCount() == 0 && otherVesselProducers.Count > 0)
                    || otherVessel.vesselType == VesselType.Debris)
                {
                    transferDirection = TransferDirection.Receive;
                }
                // If other ship has a producer for a resource and we don't
                else if (otherShipsProducts.Contains(resourceName)
                      && !thisShipsProducts.Contains(resourceName))
                {
                    transferDirection = TransferDirection.Receive;
                }
                // else if the converse...
                else if (!otherShipsProducts.Contains(resourceName)
                       && thisShipsProducts.Contains(resourceName))
                {
                    transferDirection = TransferDirection.Send;
                }
                else if (thisShipsConsumption.Contains(resourceName)
                      && !otherShipsConsumption.Contains(resourceName))
                {
                    transferDirection = TransferDirection.Receive;
                }
                else if (otherShipsConsumption.Contains(resourceName)
                      && !thisShipsConsumption.Contains(resourceName))
                {
                    transferDirection = TransferDirection.Send;
                }
                else if (resourceName == "Snacks-Tier4")
                {
                    if (sourceVessel.vesselType == VesselType.Ship && (otherVessel.vesselType == VesselType.Base || otherVessel.vesselType == VesselType.Rover || otherVessel.vesselType == VesselType.Lander)
                     || sourceVessel.vesselType == VesselType.Base && (otherVessel.vesselType == VesselType.Rover || otherVessel.vesselType == VesselType.Lander))
                    {
                        transferDirection = TransferDirection.Send;
                    }
                    else if (otherVessel.vesselType == VesselType.Ship && (sourceVessel.vesselType == VesselType.Base || sourceVessel.vesselType == VesselType.Rover || sourceVessel.vesselType == VesselType.Lander)
                          || otherVessel.vesselType == VesselType.Base && (sourceVessel.vesselType == VesselType.Rover || sourceVessel.vesselType == VesselType.Lander))
                    {
                        transferDirection = TransferDirection.Receive;
                    }
                    else if (otherVessel.GetCrewCount() > 0 && sourceVessel.GetCrewCount() == 0)
                    {
                        transferDirection = TransferDirection.Send;
                    }
                    else if (otherVessel.GetCrewCount() == 0 && sourceVessel.GetCrewCount() > 0)
                    {
                        transferDirection = TransferDirection.Receive;
                    }
                }

                if (transferDirection == TransferDirection.Send && maxCanSend == 0)
                {
                    transferDirection = TransferDirection.Neither;
                }
                else if (transferDirection == TransferDirection.Receive && maxCanReceive == 0)
                {
                    transferDirection = TransferDirection.Neither;
                }
                result.Add(new ResourceTransferPossibility(resourceName, transferDirection, maxCanSend, maxCanReceive));
            }

            return result;
        }
    }
}
