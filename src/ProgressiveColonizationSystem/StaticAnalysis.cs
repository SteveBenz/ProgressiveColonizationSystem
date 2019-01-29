using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    internal static class StaticAnalysis
    {
        internal class WarningMessage
        {
            public string Message { get; set; }
            public bool IsClearlyBroken { get; set; }
            public Action FixIt { get; set; }
        }

        internal static IEnumerable<WarningMessage> CheckBodyIsSet(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, List<ITieredContainer> containers)
        {
            // Check for body parts
            List<ITieredProducer> bodySpecific = producers.Where(c => c.Output.ProductionRestriction != ProductionRestriction.Orbit).ToList();
            var mostUsedBodyAndCount = bodySpecific
                .Where(c => c.Body != null)
                .GroupBy(c => c.Body)
                .Select(g => new { body = g.Key, count = g.Count() })
                .OrderByDescending(o => o.count)
                .FirstOrDefault();
            var mostUsedBody = mostUsedBodyAndCount?.body;
            int? numSetToMostUsed = mostUsedBodyAndCount?.count;
            int numNotSet = bodySpecific.Count(c => c.Body == null);
            Action fixIt = mostUsedBody == null ? (Action)null : () =>
            {
                foreach (var producer in producers.Where(c => c.Output.ProductionRestriction != ProductionRestriction.Orbit))
                {
                    if (producer.Body != mostUsedBody)
                    {
                        producer.Body = mostUsedBody;
                    }
                }
            };

            if (numNotSet + numSetToMostUsed < bodySpecific.Count)
            {
                yield return new WarningMessage
                {
                    Message = $"Not all of the body-specific parts are set up for {mostUsedBody}",
                    IsClearlyBroken = true,
                    FixIt = fixIt
                };
            }
            else if (numNotSet > 0)
            {
                yield return new WarningMessage
                {
                    Message = "Need to set up the target for the world-specific parts",
                    IsClearlyBroken = true,
                    FixIt = fixIt
                };
            }
        }

        internal static IEnumerable<WarningMessage> CheckTieredProduction(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, List<ITieredContainer> containers)
        {
            TechTier minimumSensibleOrbitalTechTier = colonizationResearch.AllResourcesTypes
                .Where(resource => resource.ProductionRestriction == ProductionRestriction.Orbit)
                .Min(resource => colonizationResearch.GetMaxUnlockedTier(resource, null));
            var subTierOrbitalParts = producers
                .Where(producer => producer.Output.ProductionRestriction == ProductionRestriction.Orbit)
                .Where(producer => producer.Tier < minimumSensibleOrbitalTechTier)
                .ToArray();
            if (subTierOrbitalParts.Any())
            {
                yield return new WarningMessage
                {
                    Message = $"All orbital-production parts should be set to {minimumSensibleOrbitalTechTier}",
                    IsClearlyBroken = false,
                    FixIt = () =>
                    {
                        foreach (var part in subTierOrbitalParts)
                        {
                            part.Tier = minimumSensibleOrbitalTechTier;
                        }
                    }
                };
            }

            var mostUsedBodyAndCount = producers
                .Where(c => c.Output.ProductionRestriction != ProductionRestriction.Orbit)
                .Where(c => c.Body != null)
                .GroupBy(c => c.Body)
                .Select(g => new { body = g.Key, count = g.Count() })
                .OrderByDescending(o => o.count)
                .ToArray();
            if (mostUsedBodyAndCount.Length == 1)
            {
                // Only do this test if we have a single body to speak to
                TechTier minimumAtBodyTechTier = colonizationResearch.AllResourcesTypes
                    .Where(resource => resource.ProductionRestriction != ProductionRestriction.Orbit)
                    .Min(resource => colonizationResearch.GetMaxUnlockedTier(resource, mostUsedBodyAndCount[0].body));
                var subTierPlanetaryParts = producers
                    .Where(producer => producer.Output.ProductionRestriction != ProductionRestriction.Orbit)
                    .Where(producer => producer.Tier < minimumAtBodyTechTier)
                    .ToArray();
                if (subTierPlanetaryParts.Any())
                {
                    yield return new WarningMessage
                    {
                        Message = $"All production parts should be set to {minimumAtBodyTechTier}",
                        IsClearlyBroken = false,
                        FixIt = () =>
                        {
                            foreach (var part in subTierPlanetaryParts)
                            {
                                part.Tier = minimumAtBodyTechTier;
                            }
                        }
                    };
                }
            }
            string targetBody = mostUsedBodyAndCount.Length > 0 ? mostUsedBodyAndCount[0].body : null;

            foreach (var pair in producers.GroupBy(producer => producer.Output))
            {
                TieredResource output = pair.Key;
                IEnumerable<ITieredProducer> parts = pair;

                // Parts should be set consistently
                TechTier minTier = parts.Min(p => p.Tier);
                TechTier maxTier = parts.Max(p => p.Tier);
                if (minTier != maxTier)
                {
                    yield return new WarningMessage
                    {
                        Message = $"Not all of the parts producing {output.BaseName} are set at {maxTier}",
                        IsClearlyBroken = false,
                        FixIt = () =>
                        {
                            foreach (var part in parts)
                            {
                                part.Tier = maxTier;
                            }
                        }
                    };
                    break;
                }

                // Supplier parts should be at least maxTier
                TieredResource input = parts.First().Input;
                if (input == null && output.IsSupportedByScanning && targetBody != null)
                {
                    // then it depends on scanning
                    TechTier maxScanningTier = colonizationResearch.GetMaxUnlockedScanningTier(targetBody);
                    if (maxTier > maxScanningTier)
                    {
                        yield return new WarningMessage
                        {
                            Message = $"Scanning technology at {targetBody} has not progressed beyond {maxScanningTier.DisplayName()} - scroungers won't produce if a scanner at their tier is present in-orbit.",
                            IsClearlyBroken = true,
                            FixIt = null
                        };
                    }
                }
                else if (input != null)
                {
                    // Ensure that the suppliers are all at least the same tier.
                    if (producers.Any(producer => producer.Output == input && producer.Tier < maxTier))
                    {
                        yield return new WarningMessage
                        {
                            Message = $"There are {maxTier.DisplayName()} producers of {output.BaseName}, but it requires equal-tier {input.BaseName} production in order to work.",
                            IsClearlyBroken = true,
                            FixIt = null
                        };
                    }
                }
            }
        }

        internal static IEnumerable<WarningMessage> CheckCorrectCapacity(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, List<ITieredContainer> containers)
        {
            var production = producers
                .GroupBy(p => p.Output)
                .ToDictionary(pair => pair.Key, pair => pair.Sum(p => p.ProductionRate));
            var consumption = producers
                .Where(p => p.Input != null)
                .GroupBy(p => p.Input)
                .ToDictionary(pair => pair.Key, pair => pair.Sum(p => p.ProductionRate));
            foreach (var inputPair in consumption)
            {
                TieredResource inputResource = inputPair.Key;
                double inputRequired = inputPair.Value;
                if (!production.TryGetValue(inputPair.Key, out double outputAmount))
                {
                    if (!containers.Any(c => c.Content == inputResource && c.Tier == TechTier.Tier4 && c.Amount > 0))
                    {
                        yield return new WarningMessage()
                        {
                            Message = $"The ship needs {inputResource.BaseName} to produce {producers.First(p => p.Input == inputResource).Output.BaseName}",
                            IsClearlyBroken = false,
                            FixIt = null
                        };
                    }
                    // See if there's storage
                }
                else if (outputAmount < inputRequired)
                {
                    yield return new WarningMessage()
                    {
                        Message = $"The ship needs at least {inputRequired} production of {inputResource.BaseName} but it is only producing {outputAmount}",
                        IsClearlyBroken = false,
                        FixIt = null
                    };
                }
            }
        }

        internal static IEnumerable<WarningMessage> CheckTieredProductionStorage(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, List<ITieredContainer> containers)
        {
            HashSet<string> missingStorageComplaints = new HashSet<string>();
            foreach (ITieredProducer producer in producers)
            {
                if (!containers.Any(c => c.Content == producer.Output && c.Tier == producer.Tier) && producer.Output.CanBeStored)
                {
                    // Don't complain about missing Stuff storage at Tier0 & Tier1 (the tiers before the rover requirement)
                    // because it makes no sense to store it it.
                    if (producer.Output.IsSupportedByScanning && producer.Tier < TechTier.Tier2)
                    {
                        // TODO: Perhaps after the scanning refactor we can detect this a bit better.
                        continue;
                    }

                    missingStorageComplaints.Add($"This craft is producing {producer.Output.TieredName(producer.Tier)} but there's no storage for it.");
                }
            }
            return missingStorageComplaints.OrderBy(s => s).Select(s => new WarningMessage { Message = s, FixIt = null, IsClearlyBroken = false });
        }

        internal static IEnumerable<WarningMessage> CheckExtraBaggage(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, List<ITieredContainer> containers)
        {
            HashSet<string> extraBaggageComplaints = new HashSet<string>();
            foreach (var container in containers)
            {
                if ((container.Tier != TechTier.Tier4 || container.Content.BaseName == "Shinies") && container.Amount > 0)
                {
                    extraBaggageComplaints.Add($"This vessel is carrying {container.Content.TieredName(container.Tier)}.  That kind of cargo that should just be produced - that's fine for testing mass & delta-v, but you wouldn't really want to fly this way.");
                }
            }
            return extraBaggageComplaints.OrderBy(s => s).Select(s => new WarningMessage { Message = s, FixIt = null, IsClearlyBroken = false });
        }

        internal static IEnumerable<WarningMessage> CheckHasSomeFood(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, List<ITieredContainer> containers, List<SkilledCrewman> crew)
        {
            if (crew.Count > 0
                && !containers.Any(c => c.Content.IsSnacks && c.Tier == TechTier.Tier4 && c.Amount > 0)
                && !producers.Any(p => p.Output.IsSnacks && p.Tier == TechTier.Tier4))
            {
                yield return new WarningMessage
                {
                    Message = $"There's no Snacks on this vessel - the crew will get angry after {LifeSupportScenario.DaysBeforeKerbalStarves} days",
                    IsClearlyBroken = false,
                    FixIt = null
                };
            }
        }

        internal static IEnumerable<WarningMessage> CheckHasProperCrew(List<ICbnCrewRequirement> parts, List<SkilledCrewman> crew)
        {
            List<ICbnCrewRequirement> unstaffedParts = CrewRequirementVesselModule.TestIfCrewRequirementsAreMet(parts, crew);
            if (unstaffedParts.Count > 0)
            {
                string list = string.Join(", ", unstaffedParts.SelectMany(part => part.RequiredTraits).Distinct().OrderBy(s => s).ToArray());
                yield return new WarningMessage
                {
                    Message = $"The ship doesn't have enough crew or insufficiently experienced crew to operate all its parts - add crew with these traits: {list}",
                    IsClearlyBroken = false,
                    FixIt = null
                };
            }
        }
    }
}
