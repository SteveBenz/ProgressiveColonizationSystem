using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
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

            if (!TryFindResourceToTransfer(this.TargetVessel, out Dictionary<string,double> toSend, out Dictionary<string,double> toReceive))
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

            foreach (var tieredConverter in vessel.FindPartModulesImplementing<CbnTieredResourceConverter>())
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
            => TryFindResourceToTransfer(otherVessel, out _, out _);


        private bool TryFindResourceToTransfer(Vessel otherVessel, out Dictionary<string, double> toSend, out Dictionary<string, double> toReceive)
        {
            // If the other ship is unmanned and has producers, take everything and give nothing

            SnackConsumption.ResourceQuantities(FlightGlobals.ActiveVessel, 1, out Dictionary<string, double> thisShipCanSupply, out Dictionary<string, double> thisShipCanUse);
            SnackConsumption.ResourceQuantities(otherVessel, 1, out Dictionary<string, double> otherShipCanSupply, out Dictionary<string, double> otherShipCouldUse);

            List<string> couldSend = thisShipCanSupply.Keys.Intersect(otherShipCouldUse.Keys).ToList();
            List<string> couldTake = otherShipCanSupply.Keys.Intersect(thisShipCanUse.Keys).ToList();

            List<CbnTieredResourceConverter> otherVesselProducers = otherVessel.FindPartModulesImplementing<CbnTieredResourceConverter>();

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
            HashSet<string> thisShipsProducts = GetVesselsProducers(FlightGlobals.ActiveVessel);
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
                       && otherShipCouldUse.ContainsKey(takeableStuff)
                       && thisShipCanSupply.ContainsKey(takeableStuff))
                {
                    toSend.Add(takeableStuff, Math.Min(otherShipCouldUse[takeableStuff], thisShipCanSupply[takeableStuff]));
                }
            }

            // Push MaxTier Snacks to bases and rovers
            if (thisShipCanSupply.ContainsKey("Snacks") && otherShipCouldUse.ContainsKey("Snacks")
             && (otherVessel.vesselType == VesselType.Base || otherVessel.vesselType == VesselType.Rover)
             && !otherShipsProducts.Contains("Snacks"))
            {
                toSend.Add("Snacks", Math.Min(thisShipCanSupply["Snacks"], otherShipCouldUse["Snacks"]));
            }

            // Likewise, pull snacks when you are the base.
            if (otherShipCanSupply.ContainsKey("Snacks") && thisShipCanUse.ContainsKey("Snacks")
             && (FlightGlobals.ActiveVessel.vesselType == VesselType.Base || FlightGlobals.ActiveVessel.vesselType == VesselType.Rover)
             && !thisShipsProducts.Contains("Snacks"))
            {
                toReceive.Add("Snacks", Math.Min(thisShipCanUse["Snacks"], otherShipCanSupply["Snacks"]));
            }

            return toReceive.Any() || toSend.Any();
        }
    }
}
