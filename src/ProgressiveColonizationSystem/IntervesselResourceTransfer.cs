using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            

        public Vessel TargetVessel { get; private set; }

        public void StartTransfer()
        {
            if (IsTransferUnderway)
            {
                return;
            }

            if (!TryFindResourceToTransfer(FlightGlobals.ActiveVessel, this.TargetVessel, out Dictionary<string,double> toSend, out Dictionary<string,double> toReceive))
            {
                return;
            }

            this.sourceVessel = FlightGlobals.ActiveVessel;
            this.resourceConverter = new ResourceConverter();
            this.thisVesselConversionRecipe = new ConversionRecipe();
            this.otherVesselConversionRecipe = new ConversionRecipe();
            this.lastTransferTime = transferStartTime = Planetarium.GetUniversalTime();
            this.expectedTransferCompleteTime = transferStartTime + transferTimeInSeconds;
            this.IsTransferUnderway = true;
            this.IsTransferComplete = false;
            foreach (string resource in toSend.Keys.Union(toReceive.Keys))
            {
                bool isSending;
                double amount;
                isSending = toSend.TryGetValue(resource, out amount);
                if (!isSending)
                {
                    amount = toReceive[resource];
                }

                double amountPerSecond = amount / transferTimeInSeconds;
                (isSending ? thisVesselConversionRecipe.Inputs : thisVesselConversionRecipe.Outputs)
                    .Add(new ResourceRatio(resource, amountPerSecond, dumpExcess: false));
                (isSending ? otherVesselConversionRecipe.Outputs : otherVesselConversionRecipe.Inputs)
                    .Add(new ResourceRatio(resource, amountPerSecond, dumpExcess: false));
            }
        }

        private void CheckIfSatisfiedAutoMiningRequirement()
        {
            foreach (var resource in this.thisVesselConversionRecipe.Outputs)
            {
                if (this.IsResourceSatisfyingMiningRequirement(resource))
                {
                    this.TargetVessel.vesselModules.OfType<SnackConsumption>().First()
                        .MiningMissionFinished(this.sourceVessel);
                }
            }
            foreach (var resource in this.thisVesselConversionRecipe.Inputs)
            {
                if (this.IsResourceSatisfyingMiningRequirement(resource))
                {
                    this.sourceVessel.vesselModules.OfType<SnackConsumption>().First()
                        .MiningMissionFinished(this.TargetVessel);
                }
            }
        }

        private bool IsResourceSatisfyingMiningRequirement(ResourceRatio resource)
        {
            if (!ColonizationResearchScenario.Instance.TryParseTieredResourceName(resource.ResourceName, out TieredResource tieredResource, out TechTier tier)
             || tieredResource != ColonizationResearchScenario.CrushInsResource)
            {
                return false;
            }

            // TODO: Validate that the amount is sufficient;
            return true;
        }

        private void Reset()
        {
            this.TargetVessel = null;
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

            // Abort any other activity
            Reset();

            // Hunt for another vessel to trade with
            List<Vessel> candidates = FlightGlobals.VesselsLoaded.Where(v => v != FlightGlobals.ActiveVessel && v.situation == Vessel.Situations.LANDED && HasStuffToTrade(v)).ToList();
            if (!candidates.Any())
            {
                // no takers
                this.TargetVessel = null;
                return;
            }

            // Don't swap the target vessel if it's still a candidate
            if (this.TargetVessel == null || !candidates.Contains(this.TargetVessel))
            {
                this.TargetVessel = candidates[0];
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
            return products;
        }

        private bool HasStuffToTrade(Vessel otherVessel)
            => TryFindResourceToTransfer(FlightGlobals.ActiveVessel, otherVessel, out _, out _);

        private enum SnacksDirection
        {
            Send,
            Receive,
            Neither,
        }

        private static bool TryFindResourceToTransfer(Vessel sourceVessel, Vessel otherVessel, out Dictionary<string, double> toSend, out Dictionary<string, double> toReceive)
        {
            SnackConsumption.ResourceQuantities(sourceVessel, 1, out Dictionary<string, double> thisShipCanSupply, out Dictionary<string, double> thisShipCanUse);
            SnackConsumption.ResourceQuantities(otherVessel, 1, out Dictionary<string, double> otherShipCanSupply, out Dictionary<string, double> otherShipCanUse);

            List<string> couldSend = thisShipCanSupply.Keys.Intersect(otherShipCanUse.Keys).ToList();
            List<string> couldTake = otherShipCanSupply.Keys.Intersect(thisShipCanUse.Keys).ToList();

            List<PksTieredResourceConverter> otherVesselProducers = otherVessel.FindPartModulesImplementing<PksTieredResourceConverter>();

            toSend = new Dictionary<string, double>();
            toReceive = new Dictionary<string, double>();
            if (otherVessel.GetCrewCount() == 0 && otherVesselProducers.Count > 0)
            {
                // The player's trying to abandon the base so we'll take everything and give nothing.
                foreach (var otherShipPair in otherShipCanSupply)
                {
                    string resourceName = otherShipPair.Key;
                    if (thisShipCanUse.ContainsKey(resourceName))
                    {
                        toReceive.Add(resourceName, Math.Min(thisShipCanUse[resourceName], otherShipPair.Value));
                    }
                }
                return toReceive.Count > 0;
            }

            // If other ship has a producer for a resource, take it
            // and if this ship has a producer for a resource (and the other doesn't), give it
            HashSet<string> thisShipsProducts = GetVesselsProducers(sourceVessel);
            HashSet<string> otherShipsProducts = GetVesselsProducers(otherVessel);
            foreach (string takeableStuff in otherShipCanSupply.Keys.Union(thisShipCanSupply.Keys))
            {
                if (otherShipsProducts.Contains(takeableStuff)
                 && !thisShipsProducts.Contains(takeableStuff)
                 && thisShipCanUse.ContainsKey(takeableStuff)
                 && otherShipCanSupply.ContainsKey(takeableStuff))
                {
                    toReceive.Add(takeableStuff, Math.Min(thisShipCanUse[takeableStuff], otherShipCanSupply[takeableStuff]));
                }
                else if (!otherShipsProducts.Contains(takeableStuff)
                       && thisShipsProducts.Contains(takeableStuff)
                       && otherShipCanUse.ContainsKey(takeableStuff)
                       && thisShipCanSupply.ContainsKey(takeableStuff))
                {
                    toSend.Add(takeableStuff, Math.Min(otherShipCanUse[takeableStuff], thisShipCanSupply[takeableStuff]));
                }
            }

            SnacksDirection snackDirectionBasedOnVesselType;
            if (sourceVessel.vesselType == VesselType.Ship && (otherVessel.vesselType == VesselType.Base || otherVessel.vesselType == VesselType.Rover || otherVessel.vesselType == VesselType.Lander)
             || sourceVessel.vesselType == VesselType.Base && (otherVessel.vesselType == VesselType.Rover || otherVessel.vesselType == VesselType.Lander))
            {
                snackDirectionBasedOnVesselType = SnacksDirection.Send;
            }
            else if (otherVessel.vesselType == VesselType.Ship && (sourceVessel.vesselType == VesselType.Base || sourceVessel.vesselType == VesselType.Rover || sourceVessel.vesselType == VesselType.Lander)
                  || otherVessel.vesselType == VesselType.Base && (sourceVessel.vesselType == VesselType.Rover || sourceVessel.vesselType == VesselType.Lander))
            {
                snackDirectionBasedOnVesselType = SnacksDirection.Receive;
            }
            else
            {
                snackDirectionBasedOnVesselType = SnacksDirection.Neither;
            }

            // Send snacks?
            if (thisShipCanSupply.ContainsKey("Snacks-Tier4")
             && otherShipCanUse.ContainsKey("Snacks-Tier4")
             && ( thisShipsProducts.Contains("Snacks-Tier4")   // Always send if we produce it
               || snackDirectionBasedOnVesselType == SnacksDirection.Send))
            {
                toSend.Add("Snacks-Tier4", Math.Min(thisShipCanSupply["Snacks-Tier4"], otherShipCanUse["Snacks-Tier4"]));
            }
            else if (otherShipCanSupply.ContainsKey("Snacks-Tier4")
             && thisShipCanUse.ContainsKey("Snacks-Tier4")
             && (otherShipsProducts.Contains("Snacks-Tier4")   // Always take if the other guy produces it
               || snackDirectionBasedOnVesselType == SnacksDirection.Receive))
            {
                toReceive.Add("Snacks-Tier4", Math.Min(thisShipCanUse["Snacks-Tier4"], otherShipCanSupply["Snacks-Tier4"]));
            }

            return toReceive.Any() || toSend.Any();
        }
    }
}
