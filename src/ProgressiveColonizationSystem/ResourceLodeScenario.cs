using FinePrint;
using ProgressiveColonizationSystem.ExperienceEffects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgressiveColonizationSystem
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    public class ResourceLodeScenario
        : ScenarioModule
    {
        public static ResourceLodeScenario Instance { get; private set; }

        private readonly List<ResourceLode> activeLodes = new List<ResourceLode>();

        public ResourceLodeScenario()
        {
            Instance = this;
        }

        public ResourceLode GetOrCreateResourceLoad(Vessel nearVessel, TechTier tier, double scannerNetQuality)
        {
            // There's only allowed one resource load - you have to harvest it until it's gone
            // So find the thing first.
            var lode = this.activeLodes.FirstOrDefault(rl => rl.bodyName == nearVessel.mainBody.name && rl.Tier == tier);

            if (lode != null)
            {
                // Ensure that there's a waypoint
                if (Waypoints.TryFindWaypointById(lode.Identifier, out _))
                {
                    ScreenMessages.PostScreenMessage("A lode has already been identified - look for a waypoint on the surface");
                }
                else
                {
                    Waypoints.CreateWaypointAt($"Loose Crushins ({tier.DisplayName()})", nearVessel.mainBody, lode.Latitude, lode.Longitude);
                    ScreenMessages.PostScreenMessage("A lode has already been identified - the waypoint was recreated");
                }
            }
            else
            {
                var waypoint = Waypoints.CreateWaypointNear(
                    $"Loose Crushins ({tier.DisplayName()})", nearVessel, 10000, 500000, 
                    scannerNetQuality, nearVessel.situation == Vessel.Situations.SPLASHED);
                lode = new ResourceLode(waypoint, tier);
                activeLodes.Add(lode);

                PopupMessageWithKerbal.ShowPopup(
                    "Lookie What I Found!",
                    CrewBlurbs.CreateMessage(
                        scannerNetQuality < PksScanner.BadScannerNetQualityThreshold ? "LOC_KPBS_SCANNER_FIND_NOSATS" : "LOC_KPBS_SCANNER_FIND_SATS",
                        new string[] { nameof(PksScanningSkill) }, tier),
                        "A waypoint has been created - you need to land a ship or drive a rover with a portable digger "
                        + "to within 150m of the waypoint, deploy the drill, fill your tanks with CrushIns, haul the "
                        + "load back to the base and unload it using the resource-transfer mechanism on the colony "
                        + "status screen (the cupcake button).  After you've dumped two loads with the same craft, "
                        + "the crew at the base will be able to automatically gather resources in the future."
                        + "\r\n\r\n"
                        + "The more scanner satellites you have in polar orbit, the more likely you are to get a location "
                        + "near your base.",
                        "On it");
            }

            return lode;
        }

        public bool TryFindResourceLodeInRange(Vessel vessel, out ResourceLode resourceLode)
        {
            // There's only allowed one resource load - you have to harvest it until it's gone
            // So find the thing first.
            resourceLode = this.activeLodes.FirstOrDefault(rl => rl.bodyName == vessel.mainBody.name);
            if (resourceLode == null)
            {
                return false;
            }

            // Ensure that there's a waypoint
            if (!Waypoints.TryFindWaypointById(resourceLode.Identifier, out Waypoint waypoint))
            {
                waypoint = Waypoints.CreateWaypointAt("Resource Lode", vessel.mainBody, resourceLode.Latitude, resourceLode.Longitude);
                resourceLode.WaypointRecreated(waypoint);
            }

            return Waypoints.StraightLineDistanceInMeters(vessel, waypoint) < 150.0;
        }

        public bool TryConsume(ResourceLode resourceLode, double amountRequested, out double amountReceived)
        {
            if (resourceLode.Quantity <= amountRequested)
            {
                this.activeLodes.Remove(resourceLode);
                Waypoints.RemoveWaypoint(resourceLode.Identifier);
                amountReceived = resourceLode.Quantity;
                return amountRequested == resourceLode.Quantity;
            }
            else
            {
                resourceLode.Quantity -= amountRequested;
                amountReceived = amountRequested;
                return true;
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            this.activeLodes.Clear();
            foreach (var childNode in node.GetNodes())
            {
                if (ResourceLode.TryLoad(childNode, out ResourceLode lode))
                {
                    this.activeLodes.Add(lode);
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            foreach (var lode in this.activeLodes)
            {
                node.AddNode(lode.Serialize());
            }
        }

        public class ResourceLode
        {
            internal ResourceLode(Waypoint waypoint, TechTier tier)
            {
                this.Latitude = waypoint.latitude;
                this.Longitude = waypoint.longitude;
                this.bodyName = waypoint.celestialBody.name;
                this.Identifier = waypoint.navigationId.ToString();
                this.DiscoveryTime = Planetarium.GetUniversalTime();
                this.Quantity = 500;
                this.Tier = tier;
            }

            internal ResourceLode(string bodyName, double latitude, double longitude, string identifier, double discoveryTime, double quantity, TechTier tier)
            {
                this.Latitude = latitude;
                this.Longitude =longitude;
                this.bodyName = bodyName;
                this.Identifier = identifier;
                this.DiscoveryTime = discoveryTime;
                this.Quantity = quantity;
                this.Tier = tier;
            }

            public static bool TryLoad(ConfigNode configNode, out ResourceLode resourceLode)
            {
                double latitude = 0;
                double longitude = 0;
                double discoveryTime = 0;
                double quantity = 0;
                int tier = 0;
                if (!configNode.TryGetValue("latitude", ref latitude)
                 || !configNode.TryGetValue("longitude", ref longitude)
                 || !configNode.TryGetValue("discoveryTime", ref discoveryTime)
                 || !configNode.TryGetValue("quantity", ref quantity)
                 || !configNode.TryGetValue("tier", ref tier))
                {
                    resourceLode = null;
                    return false;
                }

                string bodyName = configNode.GetValue("body");
                string identifier = configNode.GetValue("id");
                resourceLode = new ResourceLode(bodyName, latitude, longitude, identifier, discoveryTime, quantity, (TechTier)tier);
                return true;
            }

            public ConfigNode Serialize()
            {
                ConfigNode node = new ConfigNode("Lode");
                node.AddValue("id", this.Identifier);
                node.AddValue("body", this.bodyName);
                node.AddValue("latitude", this.Latitude);
                node.AddValue("longitude", this.Longitude);
                node.AddValue("discoveryTime", this.DiscoveryTime);
                node.AddValue("quantity", this.Quantity);
                node.AddValue("tier", (int)this.Tier);
                return node;
            }

            internal void WaypointRecreated(Waypoint waypoint)
            {
                this.Identifier = waypoint.navigationId.ToString();
            }

            public double Latitude { get; }
            public double Longitude { get; }
            public string bodyName { get; }
            public string Identifier { get; private set; }
            public double DiscoveryTime { get; }
            public double Quantity { get; set; }
            public TechTier Tier { get; set; }
        }
    }
}
