﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgressiveColonizationSystem.ProductionChain
{
    public static class TieredProduction
    {
        /// <summary>
        ///   Calculates production and snack consumption, using the production capacity available
        ///   on the vessel.  It takes in <paramref name="fullTimespanInSeconds"/>, but it actually
        ///   calculates only up until the first resource runs out (e.g. either fertilizer or
        ///   snacks).  The output variable, <paramref name="timePassedInSeconds"/> will be set to the
        ///   amount of time until that event happened, so you need to call this method in a
        ///   loop until you get a result where <paramref name="timePassedInSeconds"/> == <paramref name="fullTimespanInSeconds"/>
        ///   or <paramref name="timePassedInSeconds"/> == 0 (which implies that there wasn't enough food
        ///   to go around).
        /// </summary>
        /// <param name="numCrew">The number of mouths to feed.</param>
        /// <param name="fullTimespanInSeconds">The full time that has passed that we're looking to calculate for.</param>
        /// <param name="producers">The mechanisms on the vessel that can produce tiered stuff.</param>
        /// <param name="colonizationResearch">The research object (passed in for testability).</param>
        /// <param name="timePassedInSeconds">Set to the amount of <paramref name="fullTimespanInSeconds"/> that we've managed to calculate for.</param>
        /// <param name="breakthroughHappened">Set to true if the agronomy tier was eclipsed in the timespan.</param>
        /// <param name="consumptionFormula">Set to a formula for calculating the transformation of stuff.</param>
        /// <remarks>
        ///   Simplifying production chain assumptions:
        ///   <list type="bullet">
        ///     <item>Producers that require a source item require at most one of them.</item>
        ///     <item>Producers produce at exactly the same rate as they consume.</item>
        ///     <item>All producers require fed kerbals to operate, regardless of whether they have
        ///           directly to do with food or not.</item>
        ///   </list>
        ///   These simplifying assumptions not only make the game easier to code, but easier to play as well.
        /// </remarks>
        public static void CalculateResourceUtilization(
            int numCrew,
            double fullTimespanInSeconds,
            List<ITieredProducer> producers,
            List<ITieredCombiner> combiners,
            IColonizationResearchScenario colonizationResearch,
            Dictionary<string, double> availableResources,
            Dictionary<string, double> availableStorage,
            out double timePassedInSeconds,
            out List<ITieredProducer> breakthroughs,
            out Dictionary<string, double> resourceConsumptionPerSecond,
            out Dictionary<string, double> resourceProductionPerSecond,
            out IEnumerable<string> limitingFactors,
            out Dictionary<string, double> unusedProduction)
        {
            // The mechanic we're after writes up pretty simple - Kerbals will try to use renewable
            // resources first, then they start in on the on-board stocks, taking as much of the low-tier
            // stuff as they can.  Implementing that is tricky...

            // <--cacheable start
            //  The stuff from here to 'cacheable end' could be stored - we'd just have to check to
            //  see that 'availableResources.Keys' and 'availableStorage.Keys' are the same.

            // First just get a handle on what stuff we could produce.

            var limitMap = new Dictionary<string, string>();
            unusedProduction = new Dictionary<string, double>();
            List<ProducerData> producerInfos = FindProducers(producers, colonizationResearch, availableResources, availableStorage);
            SortProducerList(producerInfos);
            MatchProducersWithSourceProducers(producerInfos, limitMap, unusedProduction);

            Dictionary<TieredResource, AmalgamatedCombiners> inputToCombinerMap = combiners
                .Where(c => c.IsProductionEnabled)
                .GroupBy(c => c.TieredInput)
                .ToDictionary(pair => pair.Key, pair => new AmalgamatedCombiners(pair));

            List<FoodProducer> snackProducers = GetFoodProducers(producerInfos);
            double ratioFulfilled = 0;
            foreach (FoodProducer foodProducer in snackProducers)
            {
                if (foodProducer.MaxDietRatio > ratioFulfilled)
                {
                    double amountAskedFor = numCrew * (foodProducer.MaxDietRatio - ratioFulfilled);
                    double amountReceived = foodProducer.ProductionChain.TryToProduce(amountAskedFor, limitMap);
                    ratioFulfilled += amountReceived / numCrew;
                }
            }

            if (ratioFulfilled < (1 - AcceptableError))
            {
                List<FoodProducer> snackStorage = GetFoodStorage(producerInfos);
                foreach (FoodProducer foodProducer in snackStorage)
                {
                    if (foodProducer.MaxDietRatio > ratioFulfilled + AcceptableError)
                    {
                        double amountAskedFor = numCrew * (foodProducer.MaxDietRatio - ratioFulfilled);
                        double amountReceived = foodProducer.ProductionChain.TryToProduce(amountAskedFor, limitMap);
                        ratioFulfilled += amountReceived / numCrew;
                    }
                }
            }

            if (ratioFulfilled < (1 - AcceptableError))
            {
                // We couldn't put together a production plan that will satisfy all of the Kerbals
                // needs for any amount of time.
                timePassedInSeconds = 0;
                resourceConsumptionPerSecond = null;
                resourceProductionPerSecond = null;
                breakthroughs = null;
                limitingFactors = SquishLimitMap(limitMap);
                unusedProduction = null;
                return;
            }

            resourceProductionPerSecond = new Dictionary<string, double>();
            resourceConsumptionPerSecond = new Dictionary<string, double>();
            // Okay, now we know what the minimum plan is that will keep our kerbals fed.

            // See what goes into tiered->untiered converters
            foreach (ProducerData producerData in producerInfos)
            {
                if (inputToCombinerMap.TryGetValue(producerData.SourceTemplate.Output, out AmalgamatedCombiners combiner)
                 && availableStorage.ContainsKey(combiner.NonTieredOutputResourceName))
                {
                    if (availableResources.ContainsKey(combiner.NonTieredInputResourceName))
                    {
                        double productionRatio = combiner.GetRatioForTier(producerData.SourceTemplate.Tier);
                        double suppliesWanted = (combiner.ProductionRate - combiner.UsedCapacity) * productionRatio;
                        double applicableConverterCapacity = producerData.TryToProduce(suppliesWanted, limitMap);
                        combiner.UsedCapacity -= applicableConverterCapacity;
                        double usedResourcesRate = combiner.ProductionRate * (applicableConverterCapacity / suppliesWanted) * (1 - productionRatio);
                        combiner.RequiredMixins += usedResourcesRate;
                        double producedResourcesRate = (applicableConverterCapacity / suppliesWanted) * combiner.ProductionRate;

                        AddTo(resourceProductionPerSecond, combiner.NonTieredOutputResourceName, KerbalTime.UnitsPerDayToUnitsPerSecond(producedResourcesRate));
                        AddTo(resourceConsumptionPerSecond, combiner.NonTieredInputResourceName, KerbalTime.UnitsPerDayToUnitsPerSecond(usedResourcesRate));
                    }
                    else
                    {
                        limitMap.Add(combiner.NonTieredOutputResourceName, combiner.NonTieredInputResourceName);
                    }
                }
            }

            // Augment that with stockpiling
            foreach (ProducerData producerData in producerInfos)
            {
                string resourceName = producerData.SourceTemplate.Output.TieredName(producerData.SourceTemplate.Tier);
                if (availableStorage.ContainsKey(resourceName) && !(producerData.SourceTemplate is StorageProducer))
                {
                    double stockpiledPerDay = producerData.TryToProduce(double.MaxValue, limitMap);
                    double stockpiledPerSecond = KerbalTime.UnitsPerDayToUnitsPerSecond(stockpiledPerDay);
                    AddTo(resourceProductionPerSecond, resourceName, stockpiledPerSecond);
                }
                else if (producerData.SourceTemplate.Output.ExcessProductionCountsTowardsResearch)
                {
                    // Just run the machine
                    producerData.TryToProduce(double.MaxValue, limitMap);
                }
            }
            // <<-- end cacheable

            // Figure out how much time we can run with this production plan before the stores run out
            timePassedInSeconds = fullTimespanInSeconds;
            foreach (ProducerData producerData in producerInfos)
            {
                if (producerData.SourceTemplate is StorageProducer storage)
                {
                    // producerData.AllottedCapacity  is the amount consumed per day under our plan
                    // storage.Amount  is what we have on-hand
                    double amountUsedPerSecond = KerbalTime.UnitsPerDayToUnitsPerSecond(producerData.AllottedCapacity);
                    if (amountUsedPerSecond > 0)
                    {
                        AddTo(resourceConsumptionPerSecond, storage.Output.TieredName(storage.Tier), amountUsedPerSecond);
                    }
                }
            }

            foreach (var consumedPair in resourceConsumptionPerSecond)
            {
                double storageAmount = availableResources[consumedPair.Key];
                double secondsToEmpty = (storageAmount / consumedPair.Value);
                timePassedInSeconds = Math.Min(timePassedInSeconds, secondsToEmpty);
            }

            // ...or before the storage space is packed
            foreach (var pair in resourceProductionPerSecond)
            {
                string resourceName = pair.Key;
                double amountStockpiledPerSecond = pair.Value;
                double secondsToFilled = availableStorage[resourceName] / amountStockpiledPerSecond;
                timePassedInSeconds = Math.Min(timePassedInSeconds, secondsToFilled);
            }

            // Okay, finally now we can apply the work done towards research
            breakthroughs = new List<ITieredProducer>();
            foreach (ProducerData producerData in producerInfos)
            {
                double contributionInKerbals = Math.Min(producerData.AllottedCapacity, producerData.ProductionContributingToResearch);
                if (contributionInKerbals > 0)
                {
                    // If we have some doodads with research associated with it and some not, then
                    // what we want to do is make sure that the labs with research turned on do
                    // all the work they can.
                    if (producerData.SourceTemplate.ContributeResearch(
                        colonizationResearch,
                        timePassedInSeconds * contributionInKerbals /* convert to kerbal-seconds */))
                    {
                        // TODO: Probably add this to the data - or maybe we should push the
                        // SourceTemplate itself?
                        //   producerData.SourceTemplate.Body
                        breakthroughs.Add(producerData.SourceTemplate);
                    }
                }
            }

            limitingFactors = SquishLimitMap(limitMap);
            foreach (var pi in producerInfos
                .Where(pi => !(pi.SourceTemplate is StorageProducer))
                .Where(pi => pi.TotalProductionCapacity - pi.AllottedCapacity > AcceptableError))
            {
                unusedProduction[pi.SourceTemplate.Output.TieredName(pi.SourceTemplate.Tier)]
                    = pi.TotalProductionCapacity - pi.AllottedCapacity;
            }
        }

        public const double AcceptableError = 0.001;

        private class FoodProducer
        {
            public double MaxDietRatio;
            public ProducerData ProductionChain;
        }

        private static List<FoodProducer> GetFoodProducers(List<ProducerData> producers)
            => producers
               .Where(producer => !(producer.SourceTemplate is StorageProducer))
               .Where(producer => producer.SourceTemplate.Output.IsEdible)
               .Select(producer => new FoodProducer { ProductionChain = producer, MaxDietRatio = producer.SourceTemplate.Output.GetPercentOfDietByTier(producer.SourceTemplate.Tier) })
               .OrderBy(p => p.MaxDietRatio)
               .ToList();

        private static List<FoodProducer> GetFoodStorage(List<ProducerData> producers)
            => producers
               .Where(producer => producer.SourceTemplate is StorageProducer)
               .Where(producer => producer.SourceTemplate.Output.IsEdible)
               .Select(producer => new FoodProducer { ProductionChain = producer, MaxDietRatio = producer.SourceTemplate.Output.GetPercentOfDietByTier(producer.SourceTemplate.Tier) })
               .OrderBy(p => p.MaxDietRatio)
               .ToList();

        private static List<ProducerData> FindProducers(
            List<ITieredProducer> producers,
            IColonizationResearchScenario colonizationScenario,
            Dictionary<string, double> availableResources,
            Dictionary<string, double> availableStorage)
        {
            // First run through the producers to find out who can contribute what
            List<ProducerData> productionPossibilities = new List<ProducerData>();
            foreach (ITieredProducer producer in producers)
            {
                if (producer.IsProductionEnabled)
                {
                    // The "Body" clause is there for scanning data - it's the only one
                    //  where one ship can do research for more than one body.
                    ProducerData data = productionPossibilities.FirstOrDefault(
                        pp => pp.SourceTemplate.GetType() == producer.GetType()
                           && pp.SourceTemplate.Tier == producer.Tier
                           && pp.SourceTemplate.Body == producer.Body
                           && pp.SourceTemplate.Output == producer.Output);

                    if (data == null)
                    {
                        data = new ProducerData
                        {
                            SourceTemplate = producer,
                        };
                        productionPossibilities.Add(data);
                    }

                    data.TotalProductionCapacity += producer.ProductionRate;
                    if (producer.IsResearchEnabled)
                    {
                        data.ProductionContributingToResearch += producer.ProductionRate;
                    }
                    data.IsStockpiling |= availableStorage.ContainsKey(producer.Output.TieredName(producer.Tier));
                    data.IsStockpiling |= producer.Output.ExcessProductionCountsTowardsResearch;
                }
            }

            foreach (var pair in availableResources)
            {
                string storedResource = pair.Key;
                double amount = pair.Value;
                // TODO: Come up with a better way to filter it down to the resources that we care about.

                if (colonizationScenario.TryParseTieredResourceName(storedResource, out TieredResource resource, out TechTier tier))
                {
                    ProducerData resourceProducer = new ProducerData
                    {
                        SourceTemplate = new StorageProducer(resource, tier, amount),
                        IsStockpiling = false,
                        ProductionContributingToResearch = 0,
                        TotalProductionCapacity = double.MaxValue,
                    };
                    productionPossibilities.Add(resourceProducer);
                }
            }

            return productionPossibilities;
        }

        private static void SortProducerList(List<ProducerData> producers)
        {
            // For all consumption scenarios, we want our list sorted in a particular way
            producers.Sort((left, right) =>
            {
                // First, we always sort Storage to the bottom - we prefer to consume produced stuff before
                //  any stashed stuff
                if ((left.SourceTemplate is StorageProducer) && !(right.SourceTemplate is StorageProducer))
                {
                    return 1;
                }
                else if (!(left.SourceTemplate is StorageProducer) && (right.SourceTemplate is StorageProducer))
                {
                    return -1;
                }
                else
                {
                    // Sort by tier next - low-tier first.
                    int tierComparison = left.SourceTemplate.Tier.CompareTo(right.SourceTemplate.Tier);
                    if (tierComparison != 0)
                    {
                        return tierComparison;
                    }
                    else
                    {
                        // And finally sort Snacks before Fertilizer -- this only influences the case where
                        // we're stockpiling both snacks and fertilizer.  This choice means that if we've only
                        // got a little bit of excess fertilizer capacity, it'll go towards making extra snacks
                        // rather than stacking up the fertilizer.
                        bool leftIsSnacks = left.SourceTemplate.Output.IsEdible;
                        bool rightIsSnacks = right.SourceTemplate.Output.IsEdible;
                        if (leftIsSnacks && rightIsSnacks)
                        {
                            return 0;
                        }
                        else if (leftIsSnacks && !rightIsSnacks)
                        {
                            return -1;
                        }
                        else if (!leftIsSnacks && rightIsSnacks)
                        {
                            return 1;
                        }

                        // Okay, for the rest, it's alphabetical order, because we don't have any known preference.
                        return left.SourceTemplate.Output.BaseName.CompareTo(right.SourceTemplate.Output.BaseName);
                    }
                }
            });

        }

        private static void MatchProducersWithSourceProducers(
            List<ProducerData> producers,
            Dictionary<string,string> limitMap,
            Dictionary<string,double> unusedProduction)
        {
            List<ProducerData> producersWithNoSupply = new List<ProducerData>();
            foreach (ProducerData producer in producers.Where(p => !(p.SourceTemplate is StorageProducer)))
            {
                var input = producer.SourceTemplate.Input;
                if (input != null)
                {
                    producer.Suppliers = producers
                        // We can use anything that produces our kind of thing at our tier and up
                        .Where(potentialSupplier => potentialSupplier.SourceTemplate.Output == input
                                                 && potentialSupplier.SourceTemplate.Tier >= producer.SourceTemplate.Tier)
                        .ToList();

                    if (producer.Suppliers.Count == 0)
                    {
                        producersWithNoSupply.Add(producer);
                    }
                }
            }

            // Clean out all the producers that can't contribute because there's no source
            while (producersWithNoSupply.Count > 0)
            {
                foreach (ProducerData deadProducer in producersWithNoSupply)
                {
                    string output = deadProducer.SourceTemplate.Output.TieredName(deadProducer.SourceTemplate.Tier);
                    limitMap.Add(output, deadProducer.SourceTemplate.Input.TieredName(deadProducer.SourceTemplate.Tier));
                    unusedProduction.Add(output, deadProducer.TotalProductionCapacity);
                    producers.Remove(deadProducer);
                    foreach (ProducerData producer in producers)
                    {
                        producer.Suppliers.RemoveAll(p => p == deadProducer);
                    }
                }

                producersWithNoSupply = producers.Where(p => p.SourceTemplate.Input != null && p.Suppliers.Count == 0).ToList();
            }
        }

        private static void AddTo<T>(Dictionary<T, double> dictionary, T key, double amount)
        {
            if (amount < 0.0000000000001)
            {
                // Disregard tiny amounts
            }
            else if (dictionary.TryGetValue(key, out var existing))
            {
                dictionary[key] = existing + amount;
            }
            else
            {
                dictionary.Add(key, amount);
            }
        }

        private static IEnumerable<string> SquishLimitMap(Dictionary<string,string> map)
            => map.Values.Except(map.Keys).Distinct();
    }
}
