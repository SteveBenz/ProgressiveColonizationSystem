using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FinePrint;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   A module for finding Crush-Ins on the planet
    /// </summary>
    /// <remarks>
    ///   Right now this is stuck onto the scanner, and yet it's not completely glued to the
    ///   scanner's resource-converter mechanism.  The intended effect is that the Tier1 scanner
    ///   finds Tier1 resources for your Tier1 base.  (And ensures that you've actually got the
    ///   right tier scanner).  If the idea of the planet-wide research takes a footing, these
    ///   modules could be stuck onto base control parts.
    /// </remarks>
    public class PksScanner
        : PartModule
    {
        /// <summary>
        ///  The number below which it's safe to say the user doesn't know how to create a scanner network.
        /// </summary>
        public const double BadScannerNetQualityThreshold = 1.0;

        /// <summary>
        ///   The minimum tier where grabbing crushins is required.  See also <see cref="PksTieredResourceConverter.inputRequirementStartingTier"/>
        ///   which is set for drills
        /// </summary>
        [KSPField]
        int minimumTier = 2;

        [KSPEvent(guiActive = true, guiName = "Find Loose Crush-Ins")]
        public void FindResource()
        {
            if (this.vessel.situation != Vessel.Situations.ORBITING)
            {
                ScreenMessages.PostScreenMessage("The vessel is not in a stable orbit");
                return;
            }

            GetTargetInfo(out TechTier tier, out string targetBody);
            if (this.vessel.mainBody.name != targetBody)
            {
                ScreenMessages.PostScreenMessage($"The vessel is not in a stable orbit around {targetBody}");
                return;
            }

            if ((int)tier < this.minimumTier)
            {
                ScreenMessages.PostScreenMessage($"Crushins are not required for production at {tier.DisplayName()}.");
                return;
            }

            Vessel onPlanetBase = TryFindBase();
            if (onPlanetBase == null)
            {
                ScreenMessages.PostScreenMessage($"There doesn't seem to be a base on {targetBody}");
                return;
            }

            var crewRequirement = this.part.FindModuleImplementing<PksCrewRequirement>();
            if (crewRequirement != null && !crewRequirement.IsRunning)
            {
                ScreenMessages.PostScreenMessage($"Finding resources with this scanner is proving really difficult.  Maybe we should turn it on?");
                return;
            }

            if (crewRequirement != null && !crewRequirement.IsStaffed)
            {
                ScreenMessages.PostScreenMessage($"This part requires a qualified Kerbal to run it.");
                return;
            }

            ResourceLodeScenario.Instance.GetOrCreateResourceLoad(this.vessel, onPlanetBase, tier, this.ScannerNetQuality());
        }

        private void GetTargetInfo(out TechTier tier, out string targetBody)
        {
            var scanner = this.part.GetComponent<PksTieredResourceConverter>();
            Debug.Assert(scanner != null, "GetTargetInfo couldn't find a PksTieredResourceConverter - probably the part is misconfigured");
            if (scanner == null)
            {
                throw new Exception($"Misconfigured part - {part.name} should have a PksTieredResourceConverter");
            }

            tier = scanner.Tier;
            targetBody = scanner.Body;
        }

        /// <summary>
        ///   Attempts to locate a base on the surface of the body the current vessel is on.
        ///   It first looks for any vessel at all that's landed.  It fines down that list to
        ///   the list of vessels that are marked as a "Base" - unless there are none and then it
        ///   does nothing.  Then it fines it down to vessels that have crew.  Then it picks
        ///   one (effectively) at random.
        /// </summary>
        private Vessel TryFindBase()
        {
            var candidates = FlightGlobals.Vessels.Where(v => v.mainBody == this.vessel.mainBody && v.situation == Vessel.Situations.LANDED || v.situation == Vessel.Situations.SPLASHED).ToArray();
            var baseCandidates = candidates.Where(v => v.vesselType == VesselType.Base).ToArray();
            if (baseCandidates.Length > 0)
            {
                candidates = baseCandidates;
            }

            var crewedCandidates = candidates.Where(v => v.GetCrewCount() > 0).ToArray();
            if (crewedCandidates.Length > 0)
            {
                candidates = crewedCandidates;
            }

            return candidates.FirstOrDefault();
        }

        private double ScannerNetQuality()
        {
            var scansats = FlightGlobals.Vessels.Where(v => v.mainBody == this.vessel.mainBody && v.GetCrewCapacity() == 0 && v.situation == Vessel.Situations.ORBITING);
            scansats = scansats.ToArray();
            return scansats.Sum(v => (v.orbit.inclination > 80.0 && v.orbit.inclination  < 100.0) ? 1 : .3);
        }
    }
}
