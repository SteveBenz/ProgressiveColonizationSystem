using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgressiveColonizationSystem
{
    public static class TieredProduction
    {
        /// <summary>
        ///   Our math is based on per-day calculations e.g. each kerbal eats one supply per day.
        ///   Each agroponic thing can eat up to one fertilizer per day and create one supply per day.
        ///   One unit of fertilizer, however, might weigh a whole lot less than a unit of supplies.
        /// </summary>
        public static double UnitsPerDayToUnitsPerSecond(double x) => x / (6.0 * 60.0 * 60.0);
        public static double UnitsPerSecondToUnitsPerDay(double x) => x * (6.0 * 60.0 * 60.0);

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
            List<ITieredProducer> producers2,
            IColonizationResearchScenario colonizationResearch,
            Dictionary<string, double> availableResources,
            Dictionary<string, double> availableStorage,
            out double timePassedInSeconds,
            out List<TieredResource> breakthroughs,
            out Dictionary<string, double> resourceConsumptionPerSecond,
            out Dictionary<string, double> resourceProductionPerSecond
            )
        {
            // The mechanic we're after writes up pretty simple - Kerbals will try to use renewable
            // resources first, then they start in on the on-board stocks, taking as much of the low-tier
            // stuff as they can.  Implementing that is tricky...

            // <--cacheable start
            //  The stuff from here to 'cacheable end' could be stored - we'd just have to check to
            //  see that 'availableResources.Keys' and 'availableStorage.Keys' are the same.

            // First just get a handle on what stuff we could produce.
            List<ProducerData> producerInfos = FindProducers(producers2, colonizationResearch, availableResources, availableStorage);
            SortProducerList(producerInfos);
            MatchProducersWithSourceProducers(producerInfos);

            List<FoodProducer> snackProducers = GetFoodProducers(producerInfos);
            double ratioFulfilled = 0;
            foreach (FoodProducer foodProducer in snackProducers)
            {
                if (foodProducer.MaxDietRatio > ratioFulfilled)
                {
                    double amountAskedFor = numCrew * (foodProducer.MaxDietRatio - ratioFulfilled);
                    double amountReceived = foodProducer.ProductionChain.TryToProduce(amountAskedFor);
                    ratioFulfilled += amountReceived / numCrew;
                }
            }

            if (ratioFulfilled < (1 - AcceptableError))
            {
                List<FoodProducer> snackStorage = GetFoodStorage(producerInfos);
                foreach (FoodProducer foodProducer in snackStorage)
                {
                    if (foodProducer.MaxDietRatio > ratioFulfilled)
                    {
                        double amountAskedFor = numCrew * (foodProducer.MaxDietRatio - ratioFulfilled);
                        double amountReceived = foodProducer.ProductionChain.TryToProduce(amountAskedFor);
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
                return;
            }

            resourceProductionPerSecond = new Dictionary<string, double>();
            // Okay, now we know what the minimum plan is that will keep our kerbals fed.
            // Augment that with stockpiling
            foreach (ProducerData producerData in producerInfos)
            {
                string resourceName = producerData.SourceTemplate.Output.TieredName(producerData.SourceTemplate.Tier);
                if (availableStorage.ContainsKey(resourceName) && !(producerData.SourceTemplate is StorageProducer))
                {
                    double stockpiledPerDay = producerData.TryToProduce(double.MaxValue);
                    double stockpiledPerSecond = UnitsPerDayToUnitsPerSecond(stockpiledPerDay);
                    if (stockpiledPerSecond > 0.0)
                    {
                        if (resourceProductionPerSecond.ContainsKey(resourceName))
                        {
                            resourceProductionPerSecond[resourceName] = resourceProductionPerSecond[resourceName] + stockpiledPerSecond;
                        }
                        else
                        {
                            resourceProductionPerSecond.Add(resourceName, stockpiledPerSecond);
                        }
                    }
                }
                else if (producerData.SourceTemplate.Output.ExcessProductionCountsTowardsResearch)
                {
                    // Just run the machine
                    producerData.TryToProduce(double.MaxValue);
                }
            }
            // <<-- end cacheable

            // Figure out how much time we can run with this production plan before the stores run out
            timePassedInSeconds = fullTimespanInSeconds;
            resourceConsumptionPerSecond = new Dictionary<string, double>();
            foreach (ProducerData producerData in producerInfos)
            {
                if (producerData.SourceTemplate is StorageProducer storage)
                {
                    // producerData.AllottedCapacity  is the amount consumed per day under our plan
                    // storage.Amount  is what we have on-hand
                    double amountUsedPerSecond = UnitsPerDayToUnitsPerSecond(producerData.AllottedCapacity);
                    if (amountUsedPerSecond > 0)
                    {
                        double secondsToEmpty = (storage.Amount / amountUsedPerSecond);
                        timePassedInSeconds = Math.Min(timePassedInSeconds, secondsToEmpty);
                        resourceConsumptionPerSecond.Add(storage.Output.TieredName(storage.Tier), amountUsedPerSecond);
                    }
                }
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
            breakthroughs = new List<TieredResource>();
            foreach (ProducerData producerData in producerInfos)
            {
                if (producerData.ProductionContributingToResearch > 0)
                {
                    // If we have some doodads with research associated with it and some not, then
                    // what we want to do is make sure that the labs with research turned on do
                    // all the work they can.
                    double contributionInKerbals = Math.Min(producerData.AllottedCapacity, producerData.ProductionContributingToResearch);
                    if (producerData.SourceTemplate.ContributeResearch(
                        colonizationResearch,
                        timePassedInSeconds * contributionInKerbals /* convert to kerbal-seconds */))
                    {
                        breakthroughs.Add(producerData.SourceTemplate.Output);
                    }
                }
            }
        }

        public const double AcceptableError = 0.001;

        private class StorageProducer
            : ITieredProducer
        {
            public StorageProducer(TieredResource resource, TechTier tier, double amount)
            {
                this.Output = resource;
                this.Tier = tier;
                this.Amount = amount;
            }

            public TechTier Tier { get; set; }

            public double ProductionRate => double.MaxValue;

            public bool IsResearchEnabled => false;

            public bool IsProductionEnabled => true;

            public bool CanStockpileProduce => false;

            public TieredResource Input => null;

            public TieredResource Output { get; }

            public bool ContributeResearch(IColonizationResearchScenario target, double amount)
                => throw new NotImplementedException();

            // Not part of IProducer

            public double Amount { get; }

            public string Body { get; set; } = null;
        }

        private class ProducerData
        {
            /// <summary>
            ///   An exemplar of the type of producer.  (That is, if you have 5 parts that all have
            ///   a module of the same type and they're configured for the same tier, then you only
            ///   get one ProducerData row for all 5 of them.)
            /// </summary>
            public ITieredProducer SourceTemplate;

            public double TotalProductionCapacity;
            public double ProductionContributingToResearch;

            /// <summary>
            ///   True if we are stockpiling the resource that this produces
            /// </summary>
            public bool IsStockpiling;

            public List<ProducerData> Suppliers = new List<ProducerData>();

            public double AllottedCapacity;
            public double WastedCapacity; // Amount that can't be used because of a lack of supply

            /// <summary>
            ///   Attempts to allocate enough production resources to satisfy the requirement
            /// </summary>
            /// <param name="resourceName">The Tier-4 name of the resource to produce</param>
            /// <param name="tier">The Tier which must be produced - this routine does not substitute Tier4 for Tier1.</param>
            /// <param name="amountPerDay">The required amount in units per day</param>
            /// <returns>The amount that this production tree can actually produce in a day</returns>
            public double TryToProduce(double amountPerDay)
            {
                if (this.WastedCapacity > 0)
                {
                    // No point in trying - this producer's maxed out.
                    return 0;
                }

                double capacityLimitedRequest = Math.Min(this.TotalProductionCapacity - this.AllottedCapacity, amountPerDay);
                if (this.SourceTemplate.Input == null)
                {
                    this.AllottedCapacity += capacityLimitedRequest;
                    return capacityLimitedRequest;
                }
                else
                {
                    double sourcesObtainedSoFar = 0;
                    foreach (ProducerData supplier in this.Suppliers)
                    {
                        sourcesObtainedSoFar += supplier.TryToProduce(capacityLimitedRequest - sourcesObtainedSoFar);
                        if (sourcesObtainedSoFar >= capacityLimitedRequest - AcceptableError)
                        {
                            // We got all we asked for
                            this.AllottedCapacity += capacityLimitedRequest;
                            return capacityLimitedRequest;
                        }
                    }

                    // We can't completely fulfil the request.
                    this.AllottedCapacity += sourcesObtainedSoFar;
                    this.WastedCapacity = this.TotalProductionCapacity - this.AllottedCapacity;
                    return sourcesObtainedSoFar;
                }
            }
        }

        private class FoodProducer
        {
            public double MaxDietRatio;
            public ProducerData ProductionChain;
        }

        private static List<FoodProducer> GetFoodProducers(List<ProducerData> producers)
            => producers
               .Where(producer => !(producer.SourceTemplate is StorageProducer))
               .Where(producer => producer.SourceTemplate.Output is EdibleResource)
               .Select(producer => new FoodProducer { ProductionChain = producer, MaxDietRatio = ((EdibleResource)(producer.SourceTemplate.Output)).GetPercentOfDietByTier(producer.SourceTemplate.Tier) })
               .OrderBy(p => p.MaxDietRatio)
               .ToList();

        private static List<FoodProducer> GetFoodStorage(List<ProducerData> producers)
            => producers
               .Where(producer => producer.SourceTemplate is StorageProducer)
               .Where(producer => producer.SourceTemplate.Output is EdibleResource)
               .Select(producer => new FoodProducer { ProductionChain = producer, MaxDietRatio = ((EdibleResource)(producer.SourceTemplate.Output)).GetPercentOfDietByTier(producer.SourceTemplate.Tier) })
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
                    ProducerData data = productionPossibilities.FirstOrDefault(
                        pp => pp.SourceTemplate.GetType() == producer.GetType()
                           && pp.SourceTemplate.Tier == producer.Tier
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
                        bool leftIsSnacks = left.SourceTemplate.Output is EdibleResource;
                        bool rightIsSnacks = right.SourceTemplate.Output is EdibleResource;
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

        private static void MatchProducersWithSourceProducers(List<ProducerData> producers)
        {
            List<ProducerData> producersWithNoSupply = new List<ProducerData>();
            foreach (ProducerData producer in producers)
            {
                if (producer.SourceTemplate.Input != null)
                {
                    producer.Suppliers = producers
                        // We can use anything that produces our kind of thing at our tier and up
                        .Where(potentialSupplier => potentialSupplier.SourceTemplate.Output == producer.SourceTemplate.Input
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
                    producers.RemoveAll(p => p == deadProducer);
                    foreach (ProducerData producer in producers)
                    {
                        producer.Suppliers.RemoveAll(p => p == deadProducer);
                    }
                }

                producersWithNoSupply = producers.Where(p => p.SourceTemplate.Input != null && p.Suppliers.Count == 0).ToList();
            }
        }
    }
}
