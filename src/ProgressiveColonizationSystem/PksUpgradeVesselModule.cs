using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    class PksUpgradeVesselModule
        : VesselModule
    {
        [KSPField(isPersistant = true)]
        private double lastTimeCheck;

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || this.vessel == null || !this.vessel.loaded)
            {
                return;
            }

            double now = Planetarium.GetUniversalTime();
            var upgradingParts = this.GetUpgradingParts();
            int numStaffedUpgrades = upgradingParts.Count(p => p.CrewRequirement.IsStaffed);
            if (numStaffedUpgrades == 0)
            {
                this.lastTimeCheck = now;
                return;
            }

            PartResourceDefinition rocketPartsResourceDefinition = PartResourceLibrary.Instance.GetDefinition("RocketParts");
            vessel.GetConnectedResourceTotals(rocketPartsResourceDefinition.id, out double availableRocketParts, out double _);

            double timeLeft = now - lastTimeCheck;
            double rocketPartsUsed = 0;
            while (timeLeft > float.Epsilon && upgradingParts.Any() && rocketPartsUsed < availableRocketParts)
            {
                var workingOnParts = upgradingParts.Where(up => up.CrewRequirement.IsStaffed).ToList();
                if (workingOnParts.Count < numStaffedUpgrades)
                {
                    int others = numStaffedUpgrades - workingOnParts.Count;
                    workingOnParts.AddRange(
                        upgradingParts
                            .Where(up => !up.CrewRequirement.IsStaffed)
                            .Take(others));
                }

                // If we're working on all the parts that are staffed at once, what's the rate of consumption?
                double ratePerSecond = workingOnParts.Sum(wp => wp.PartsUseRateInRocketPartsPerSecond);
                // Time is limited by either running out of parts
                double timeToRunOutOfParts = (availableRocketParts - rocketPartsUsed) / ratePerSecond;
                double timeToFinishSomething = workingOnParts.Select(wp => wp.remainingWork / wp.PartsUseRateInRocketPartsPerSecond).Min();
                double timeSpent = Math.Min(timeLeft, Math.Min(timeToRunOutOfParts, timeToFinishSomething));

                foreach (var workingOnPart in workingOnParts)
                {
                    double numRocketPartsThatStillNeedToBeInstalled
                        = workingOnPart.remainingWork - timeSpent * workingOnPart.PartsUseRateInRocketPartsPerSecond;
                    if (numRocketPartsThatStillNeedToBeInstalled < float.Epsilon)
                    {
                        numRocketPartsThatStillNeedToBeInstalled = 0;
                        upgradingParts.Remove(workingOnPart);
                    }

                    workingOnPart.UpdateRemainingParts(numRocketPartsThatStillNeedToBeInstalled);
                }

                rocketPartsUsed += timeSpent * ratePerSecond;
                timeLeft -= timeSpent;
            }

            ResourceConverter resourceConverter = new ResourceConverter();
            ConversionRecipe recipe = new ConversionRecipe();
            recipe.Inputs.Add(new ResourceRatio("RocketParts", rocketPartsUsed, dumpExcess: false, ResourceFlowMode.ALL_VESSEL));
            resourceConverter.ProcessRecipe(1, recipe, this.vessel.rootPart, resModule: null, efficiencyBonus: 1f);
            this.lastTimeCheck = now;
        }

        private List<PksUpgradablePart> GetUpgradingParts()
        {
            var all = this.Vessel.FindPartModulesImplementing<PksUpgradablePart>();
            all.RemoveAll(up => !up.IsUpgrading);
            return all;
        }
    }
}
