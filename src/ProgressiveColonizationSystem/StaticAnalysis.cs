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

        internal static TierSuitability GetTierSuitability(
            IColonizationResearchScenario colonizationResearchScenario,
            TieredResource tieredResource,
            TechTier tier,
            string body)
        {
            if (body == null && tieredResource.ResearchCategory.Type != ProductionRestriction.Space)
            {
                return TierSuitability.BodyNotSelected;
            }

            var maxTier = colonizationResearchScenario.GetMaxUnlockedTier(tieredResource, body);
            if (tier > maxTier)
            {
                return TierSuitability.NotResearched;
            }

            bool subordinateTechIsCapping = false;
            for (TieredResource requiredResource = tieredResource.MadeFrom(tier); requiredResource != null; requiredResource = requiredResource.MadeFrom(tier))
            {
                if (requiredResource.ResearchCategory.Type != tieredResource.ResearchCategory.Type)
                {
                    // This would be a case where the made-from is produced in one situation (e.g. landed) and consumed
                    // in another (in space).  We don't know where the stuff is produced, so we'll just have to assume
                    // we can get the stuff from somewhere.
                    break;
                }

                var t = colonizationResearchScenario.GetMaxUnlockedTier(requiredResource, body);
                if (tier > t)
                {
                    return TierSuitability.LacksSubordinateResearch;
                }
                else if (tier == t)
                {
                    subordinateTechIsCapping = true;
                }
            }

            TechTier maxScanningTier = string.IsNullOrEmpty(body) ? TechTier.Tier4 : colonizationResearchScenario.GetMaxUnlockedScanningTier(body);
            if (tier > maxScanningTier)
            {
                return TierSuitability.LacksScanner;
            }

            if (tier < maxTier && !subordinateTechIsCapping && (string.IsNullOrEmpty(body) || tier < maxScanningTier))
            {
                return TierSuitability.UnderTier;
            }

            return TierSuitability.Ideal;
        }

        internal static IEnumerable<WarningMessage> CheckBodyIsSet(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, Dictionary<string, double> amountAvailable, Dictionary<string, double> storageAvailable)
        {
            // Check for body parts
            List<ITieredProducer> bodySpecific = producers.Where(c => c.Output.ProductionRestriction != ProductionRestriction.Space).ToList();
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
                foreach (var producer in producers.Where(c => c.Output.ProductionRestriction != ProductionRestriction.Space))
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

        private static void SetToIdealTier(IColonizationResearchScenario colonizationResearch, ITieredProducer producer)
        {
            for (TechTier tier = TechTier.Tier4; tier >= TechTier.Tier0; --tier)
            {
                var suitability = StaticAnalysis.GetTierSuitability(colonizationResearch, producer.Output, tier, producer.Body);
                if (suitability == TierSuitability.Ideal)
                {
                    producer.Tier = tier;
                    break;
                }
            }
        }

        private static void SetToIdealTier(IColonizationResearchScenario colonizationResearch, IEnumerable<ITieredProducer> producers)
        {
            foreach (var producer in producers)
            {
                StaticAnalysis.SetToIdealTier(colonizationResearch, producer);
            }
        }

        internal static IEnumerable<WarningMessage> CheckTieredProduction(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, Dictionary<string, double> amountAvailable, Dictionary<string, double> storageAvailable)
        {
            List<ITieredProducer> noScannerParts = new List<ITieredProducer>();
            List<ITieredProducer> noSubResearchParts = new List<ITieredProducer>();
            List<ITieredProducer> underTier = new List<ITieredProducer>();

            foreach (var producer in producers)
            {
                var suitability = StaticAnalysis.GetTierSuitability(colonizationResearch, producer.Output, producer.Tier, producer.Body);
                switch (suitability)
                {
                    case TierSuitability.LacksScanner:
                        noScannerParts.Add(producer);
                        break;
                    case TierSuitability.LacksSubordinateResearch:
                        noSubResearchParts.Add(producer);
                        break;
                    case TierSuitability.UnderTier:
                        underTier.Add(producer);
                        break;
                    default:
                        break;
                }
            }

            if (noScannerParts.Any())
            {
                var examplePart = noScannerParts[0];
                yield return new WarningMessage
                {
                    Message = $"Scanning technology at {examplePart.Body} has not kept up with production technologies - {examplePart.Tier.DisplayName()} parts will not function until you deploy an equal-tier scanner to orbit around {examplePart.Body}.",
                    IsClearlyBroken = true,
                    FixIt = () => SetToIdealTier(colonizationResearch, noScannerParts)
                };
            }

            if (noSubResearchParts.Any())
            {
                var examplePart = noSubResearchParts[0];
                yield return new WarningMessage
                {
                    Message = $"Not all the products in the production chain for {examplePart.Output.DisplayName} have advanced to {examplePart.Tier}.",
                    IsClearlyBroken = true,
                    FixIt = () => SetToIdealTier(colonizationResearch, noSubResearchParts)
                };
            }

            if (underTier.Any())
            {
                var examplePart = underTier[0];
                yield return new WarningMessage
                {
                    Message = $"This base is not taking advantage of the latest tech for producing {examplePart.Output.DisplayName}",
                    IsClearlyBroken = true,
                    FixIt = () => SetToIdealTier(colonizationResearch, underTier)
                };
            }

            var mostUsedBodyAndCount = producers
                .Where(c => c.Output.ProductionRestriction != ProductionRestriction.Space)
                .Where(c => c.Body != null)
                .GroupBy(c => c.Body)
                .Select(g => new { body = g.Key, count = g.Count() })
                .OrderByDescending(o => o.count)
                .ToArray();
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
                var firstPart = parts.First();
                TieredResource input = firstPart.Input;
                if (parts.First().Input == null && output.IsHarvestedLocally && targetBody != null)
                {
                    // then it depends on scanning
                    TechTier maxScanningTier = colonizationResearch.GetMaxUnlockedScanningTier(targetBody);
                    if (maxTier > maxScanningTier)
                    {
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

        internal static IEnumerable<WarningMessage> CheckCorrectCapacity(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, Dictionary<string, double> amountAvailable, Dictionary<string, double> storageAvailable)
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
                if (inputResource.IsHarvestedLocally)
                {
                    // Crush-ins -- there are other things that ensure this works.
                }
                else if (!production.TryGetValue(inputPair.Key, out double outputAmount))
                {
                    // Okay, there's no producer for this - complain if there's no storage that either contains the
                    // required tier or could contain it if it's gathered locally.
                    TechTier requiredTier = producers.Where(p => p.Input == inputResource).Select(p => p.Tier).Min();
                    bool anyInStorage = Enumerable.Range((int)requiredTier, 1+(int)TechTier.Tier4)
                        .Any(i => amountAvailable.TryGetValue(inputResource.TieredName((TechTier)i), out var amount) && amount > 0);
                    if (!inputResource.IsHarvestedLocally && !anyInStorage)
                    {
                        yield return new WarningMessage()
                        {
                            Message = $"The ship needs {inputResource.BaseName} to produce {producers.First(p => p.Input == inputResource).Output.BaseName}",
                            IsClearlyBroken = false,
                            FixIt = null
                        };
                    }
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

        internal static IEnumerable<WarningMessage> CheckTieredProductionStorage(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, Dictionary<string, double> amountAvailable, Dictionary<string, double> storageAvailable)
        {
            HashSet<string> missingStorageComplaints = new HashSet<string>();
            foreach (ITieredProducer producer in producers)
            {
                if (producer.Output.CanBeStored &&
                    (!storageAvailable.TryGetValue(producer.Output.TieredName(producer.Tier), out var available)
                  || available == 0))
                {
                    missingStorageComplaints.Add($"This craft is producing {producer.Output.TieredName(producer.Tier)} but there's no storage for it.");
                }
            }
            return missingStorageComplaints.OrderBy(s => s).Select(s => new WarningMessage { Message = s, FixIt = null, IsClearlyBroken = false });
        }

        internal static IEnumerable<WarningMessage> CheckExtraBaggage(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, Dictionary<string, double> amountAvailable, Dictionary<string, double> storageAvailable)
        {
            HashSet<string> bannedBaggageComplaints = new HashSet<string>();
            HashSet<string> extraBaggageComplaints = new HashSet<string>();
            foreach (var pair in amountAvailable)
            {
                if (pair.Value == 0)
                {
                    continue;
                }

                if (pair.Value > 0 && colonizationResearch.TryParseTieredResourceName(pair.Key, out var resource, out var tier))
                {
                    if (resource.GetReputationGain(tier, 100) != 0 || resource.IsHarvestedLocally)
                    {
                        bannedBaggageComplaints.Add(pair.Key);
                    }
                    else if (tier != TechTier.Tier4)
                    {
                        extraBaggageComplaints.Add(pair.Key);
                    }
                }
            }
            return extraBaggageComplaints
                .OrderBy(s => s).Select(s => new WarningMessage
                {
                    Message = $"This vessel is carrying {s}.  Usually that kind of cargo is produced, so likely there's no point in carrying it into orbit with you.  You should probably empty those containers.",
                    FixIt = () => UnloadCargo(s),
                    IsClearlyBroken = false
                })
                .Union(bannedBaggageComplaints.OrderBy(s => s)
                    .Select(s => new WarningMessage
                    {
                        Message = $"This vessel is carrying {s}.  That kind of cargo needs to be produced locally and can't be produced on Kerbin",
                        FixIt = () => UnloadCargo(s),
                        IsClearlyBroken = true
                    }));
        }

        internal static IEnumerable<WarningMessage> CheckHasSomeFood(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, Dictionary<string, double> amountAvailable, Dictionary<string, double> storageAvailable, List<SkilledCrewman> crew)
        {
            bool needsFood = crew.Any() || producers.Any();
            bool hasStoredFood = (amountAvailable.TryGetValue("Snacks-Tier4", out var amount) && amount > 0);
            bool producesFood = producers.Any(p => p.Output.IsEdible && p.Output.GetPercentOfDietByTier(p.Tier) == 1);

            if (needsFood && !hasStoredFood && !producesFood)
            {
                yield return new WarningMessage
                {
                    Message = $"There's no Snacks on this vessel - the crew will get angry after {LifeSupportScenario.DaysBeforeKerbalStarves} days",
                    IsClearlyBroken = false,
                    FixIt = null
                };
            }
        }

        internal static IEnumerable<WarningMessage> CheckHasProperCrew(List<IPksCrewRequirement> parts, List<SkilledCrewman> crew)
        {
            List<IPksCrewRequirement> unstaffedParts = CrewRequirementVesselModule.FindUnderstaffedParts(parts, crew).ToList();
            if (unstaffedParts.Count > 0)
            {
                string list = string.Join(", ", unstaffedParts
                    .Select(part => part.RequiredEffect)
                    .Distinct()
                    .SelectMany(effect => GameDatabase.Instance.ExperienceConfigs.GetTraitsWithEffect(effect))
                    .Distinct()
                    .OrderBy(s => s)
                    .ToArray());
                yield return new WarningMessage
                {
                    Message = $"The ship doesn't have enough crew or insufficiently experienced crew to operate all its parts - add crew with these traits: {list}",
                    IsClearlyBroken = false,
                    FixIt = null
                };
            }
        }

        internal static IEnumerable<WarningMessage> CheckHasCrushinStorage(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, Dictionary<string, double> amountAvailable, Dictionary<string, double> storageAvailable)
        {
            var drills = producers
                .Where(p => p.Input == colonizationResearch.CrushInsResource)
                .ToArray();
            if (drills.Length > 0)
            {
                double totalDrillCapacity = drills.Sum(p => p.ProductionRate);
                double crushinsRequired = totalDrillCapacity * SnackConsumption.DrillCapacityMultiplierForAutomaticMiningQualification;
                storageAvailable.TryGetValue(drills[0].Input.TieredName(drills[0].Tier), out double totalCrushinStorage);

                if (totalCrushinStorage < crushinsRequired)
                {
                    // not enough storage
                    yield return new WarningMessage
                    {
                        Message = $"To ensure you can use automated mining (via a separate mining craft), you need to have "
                                + $"storage for at least {crushinsRequired} {colonizationResearch.CrushInsResource.BaseName}.  "
                                + "You will also need to send a craft capable of mining it (which will be found in "
                                + "scattered locations around the body using your orbital scanner) and bringing them "
                                + "back to the base.",
                        IsClearlyBroken = false,
                        FixIt = null
                    };
                }
                else
                {
                    yield return new WarningMessage
                    {
                        Message = $"To ensure you can use automated mining (via a separate mining craft), you need to have "
                                + $"a craft capable of mining and delivering {crushinsRequired} {colonizationResearch.CrushInsResource.BaseName}.",
                        IsClearlyBroken = false,
                        FixIt = null
                    };
                }
            }
        }

        internal static IEnumerable<WarningMessage> CheckHasRoverPilot(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, Dictionary<string, double> amountAvailable, Dictionary<string, double> storageAvailable, List<SkilledCrewman> crew)
        {
            bool needsRoverPilot = producers.Any(p => p.Input == colonizationResearch.CrushInsResource);
            if (needsRoverPilot && !crew.Any(c => c.CanPilotRover()))
            {
                yield return new WarningMessage
                {
                    Message = $"To ensure you can use automated mining (via a separate mining craft), you need to have "
                            + $"a pilot at the base to drive it.",
                    IsClearlyBroken = false,
                    FixIt = null
                };
            }
        }

        internal static IEnumerable<WarningMessage> CheckRoverHasTwoSeats(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, Dictionary<string, double> amountAvailable, Dictionary<string, double> storageAvailable, int maxCrewCapacity)
        {
            bool isCrushinRover = producers.Any(p => p.Output == colonizationResearch.CrushInsResource);
            if (isCrushinRover && maxCrewCapacity < 2)
            {
                yield return new WarningMessage
                {
                    Message = "For this craft to be useable as an automated miner, it needs at least two seats -- "
                             + "one for a miner and one for a pilot.",
                    IsClearlyBroken = false,
                    FixIt = null
                };
            }
        }

        internal static IEnumerable<WarningMessage> CheckCombiners(IColonizationResearchScenario colonizationResearch, List<ITieredProducer> producers, List<ITieredCombiner> combiners, Dictionary<string, double> amountAvailable, Dictionary<string, double> storageAvailable)
        {
            foreach (ITieredCombiner combinerWithMissingInput in combiners
                .GroupBy(c => c.NonTieredInputResourceName + c.NonTieredOutputResourceName)
                .Select(pair => pair.First())
                .Where(n => !amountAvailable.TryGetValue(n.NonTieredInputResourceName, out var amount) || amount == 0))
            {
                yield return new WarningMessage
                {
                    Message = $"To produce {combinerWithMissingInput.NonTieredOutputResourceName} you will need to bring some {combinerWithMissingInput.NonTieredInputResourceName}.",
                    IsClearlyBroken = false,
                    FixIt = null
                };
            }

            foreach (ITieredCombiner combinerWithNoOutputStorage in combiners
                .GroupBy(c => c.NonTieredOutputResourceName)
                .Select(pair => pair.First())
                .Where(n => !storageAvailable.TryGetValue(n.NonTieredOutputResourceName, out var amount) || amount == 0))
            {
                yield return new WarningMessage
                {
                    Message = $"There's no place to put the {combinerWithNoOutputStorage.NonTieredOutputResourceName} this base is producing.",
                    IsClearlyBroken = false,
                    FixIt = null
                };
            }

            foreach (ITieredCombiner combinerWithNoTieredInput in combiners
                .GroupBy(c => c.NonTieredInputResourceName + c.NonTieredOutputResourceName)
                .Select(pair => pair.First())
                .Where(c => !producers.Any(p => p.Output == c.TieredInput)))
            {
                yield return new WarningMessage
                {
                    Message = $"To produce {combinerWithNoTieredInput.NonTieredOutputResourceName}, you need produce {combinerWithNoTieredInput.TieredInput.BaseName} as input.",
                    IsClearlyBroken = false,
                    FixIt = null
                };
            }
        }

        private static void ForeachResource(Action<TieredResource, TechTier, PartResource> action)
        {
            List<Part> parts = EditorLogic.fetch.ship.Parts;
            foreach (PartResource partResource in parts.SelectMany(p => p.Resources))
            {
                if (ColonizationResearchScenario.Instance.TryParseTieredResourceName(partResource.resourceName, out var resource, out var tier))
                {
                    action(resource, tier, partResource);
                }
            }
        }

        private static void UnloadCargo(string resourceName)
        {
            List<Part> parts = EditorLogic.fetch.ship.Parts;
            foreach (PartResource partResource in parts.SelectMany(p => p.Resources).Where(pr => pr.resourceName == resourceName))
            {
                partResource.amount = 0;
            }
        }

        public static void FixBannedCargos()
        {
            ForeachResource((resource, tier, partResource) =>
            {
                if (resource.GetReputationGain(TechTier.Tier0, 1) > 0 || resource.IsHarvestedLocally)
                {
                    partResource.amount = 0;
                }
            });
        }
    }
}