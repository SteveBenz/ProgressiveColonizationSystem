using FinePrint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    public static class Waypoints
    {
        public static bool TryFindWaypointById(string id, out Waypoint waypoint)
        {
            waypoint = FinePrint.WaypointManager.Instance().Waypoints.FirstOrDefault(wp => wp.navigationId.ToString() == id);
            return waypoint != null;
        }

        public static Waypoint CreateWaypointNear(
            string name,
            Vessel near,
            double minDistanceInMeters,
            double maxDistanceInMeters,
            double scannerNetQuality,
            bool waterAllowed,
            string detail1 = null,
            string detail2 = null,
            string detail3 = null)
        {
            Waypoint bestWaypoint = null;
            double bestWaypointDistance = 0;
            double currentMaxDistance = maxDistanceInMeters;
            for (int i = 0; i <= 3*scannerNetQuality; ++i)
            {
                var waypoint = new Waypoint()
                {
                    celestialName = near.mainBody.name,
                    name = name,
                    nodeCaption1 = detail1,
                    nodeCaption2 = detail2,
                    nodeCaption3 = detail3,
                    id = "custom",
                    seed = 269,
                };

                waypoint.RandomizeNear(near.latitude, near.longitude, minDistanceInMeters, currentMaxDistance, waterAllowed: false, generator: new System.Random());
                waypoint.height = WaypointHeight(waypoint);
                waypoint.altitude = 0;

                double thisWaypointDistance = StraightLineDistanceInMeters(near, waypoint);

                if (i == 0 || thisWaypointDistance < bestWaypointDistance )
                {
                    bestWaypoint = waypoint;
                    bestWaypointDistance = thisWaypointDistance;
                }

                currentMaxDistance = minDistanceInMeters + (currentMaxDistance - minDistanceInMeters) * .75;
            }

            ScenarioCustomWaypoints.AddWaypoint(bestWaypoint);
            return bestWaypoint;
        }

        public static bool RemoveWaypoint(string id)
        {
            if (TryFindWaypointById(id, out var waypoint))
            {
                ScenarioCustomWaypoints.RemoveWaypoint(waypoint);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Waypoint CreateWaypointAt(
            string name,
            CelestialBody body,
            double latitude,
            double longitude,
            string detail1 = null,
            string detail2 = null,
            string detail3 = null)
        {
            var waypoint = new Waypoint()
            {
                celestialName = body.name,
                name = name,
                nodeCaption1 = detail1,
                nodeCaption2 = detail2,
                nodeCaption3 = detail3,
                latitude = latitude,
                longitude = longitude,
                id = "custom",
                seed = 269,
                height = TerrainHeight(latitude, longitude, body),
                altitude = 0
            };

            ScenarioCustomWaypoints.AddWaypoint(waypoint);
            return waypoint;
        }
        /// <summary>
        ///   Calculates a straight-line distance between a vessel and a waypoint - only really accurate at short-range
        ///   or when the waypoint is on another body.
        /// </summary>
        public static double StraightLineDistanceInMeters(Vessel vessel, Waypoint waypoint)
        {
            Vector3 wpPosition = waypoint.celestialBody.GetWorldSurfacePosition(waypoint.latitude, waypoint.longitude, waypoint.height + waypoint.altitude);
            return Vector3.Distance(wpPosition, vessel.transform.position);
        }

        public static double StraightLineDistanceInMeters(Vessel vessel, Vessel otherVessel)
        {
            return Vector3.Distance(otherVessel.transform.position, vessel.transform.position);
        }

        private static double WaypointHeight(Waypoint w)
        {
            return TerrainHeight(w.latitude, w.longitude, w.celestialBody);
        }

        private static double TerrainHeight(double latitude, double longitude, CelestialBody body)
        {
            // Not sure when this happens - for Sun and Jool?
            if (body.pqsController == null)
            {
                return 0;
            }

            // Figure out the terrain height
            double latRads = Math.PI / 180.0 * latitude;
            double lonRads = Math.PI / 180.0 * longitude;
            Vector3d radialVector = new Vector3d(Math.Cos(latRads) * Math.Cos(lonRads), Math.Sin(latRads), Math.Cos(latRads) * Math.Sin(lonRads));
            return Math.Max(body.pqsController.GetSurfaceHeight(radialVector) - body.pqsController.radius, 0.0);
        }
    }
}
