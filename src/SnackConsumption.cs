using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{
    public class SnackConsumption
        : VesselModule
    {
        [KSPField(isPersistant = true)]
        public double LastUpdateTime;

        // Assumes that the time things come back as fractions of a second.
        const float SupplyConsumptionPerSecondPerKerbal = 1f / (6f * 60f * 60f);

        protected IResourceBroker _resBroker;
        public IResourceBroker ResBroker
        {
            get { return _resBroker ?? (_resBroker = new ResourceBroker()); }
        }

        protected ResourceConverter _resConverter;
        public ResourceConverter ResConverter
        {
            get { return _resConverter ?? (_resConverter = new ResourceConverter()); }
        }

        /// <summary>
        ///   This is called on each physics frame for the active vessel by reflection-magic from KSP.
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel == null || !vessel.loaded || LifeSupportScenario.Instance == null)
            {
                return;
            }

            // Compute this early, since it sets the last update time member
            double deltaTime = GetDeltaTime();
            if (deltaTime < 0)
            {
                // This is the vessel launch - nothing to do but set the update time.
                return;
            }

            if (vessel.isEVA)
            {
                // TODO: What to do here?  Should Kerbals on EVA ever go hungry?
                return;
            }

            List<ProtoCrewMember> crew = vessel.GetVesselCrew();
            if (crew.Count == 0)
            {
                // Nobody on board
                return;
            }

            if (this.IsAtHome)
            {
                // While actually on Kerbal, the Kerbals will order take-out rather than consuming
                // what's in the ship.
                foreach (var crewman in crew)
                {
                    LifeSupportScenario.Instance?.KerbalHasReachedHomeworld(crewman);
                }
            }
            else
            {
                this.ProduceAndConsumeSnacks(crew, deltaTime);
            }
        }

        /// <summary>
        ///   Calculates snacks consumption aboard the vessel.
        /// </summary>
        /// <param name="crew">The crew</param>
        /// <param name="deltaTime">The amount of time (in seconds) since the last calculation was done</param>
        /// <returns>The amount of <paramref name="deltaTime"/> in which food was supplied.</returns>
        private void ProduceAndConsumeSnacks(List<ProtoCrewMember> crew, double deltaTime)
        {
            var snackProducers = this.vessel.FindPartModulesImplementing<ISnackProducer>();
            // TODO: Make sure the Scientists have enough stars to match the tier.
            var crewPart = vessel.parts.FirstOrDefault(p => p.CrewCapacity > 0);
            double remainingTime = deltaTime;

            while (remainingTime > ResourceUtilities.FLOAT_TOLERANCE)
            {
                
                SnackConsumption.CalculateSnackflow(
                    crew.Count,
                    deltaTime,
                    snackProducers.ToArray(),
                    ColonizationResearchScenario.Instance,
                    this.ResourceQuantities(),
                    out double elapsedTime,
                    out bool agroponicsBreakthrough,
                    out Dictionary<string,double> resourceConsumptionPerSecond);

                if (elapsedTime == 0)
                {
                    break;
                }

                if (resourceConsumptionPerSecond != null)
                {
                    ConversionRecipe consumptionRecipe = new ConversionRecipe();
                    consumptionRecipe.Inputs.AddRange(
                        resourceConsumptionPerSecond.Select(pair => new ResourceRatio()
                        {
                            ResourceName = pair.Key,
                            Ratio = pair.Value,
                            DumpExcess = false,
                            FlowMode = ResourceFlowMode.ALL_VESSEL
                        }));
                    Debug.Assert(elapsedTime > 0);
                    var consumptionResult = this.ResConverter.ProcessRecipe(elapsedTime, consumptionRecipe, crewPart, null, 1f);
                    Debug.Assert(Math.Abs(consumptionResult.TimeFactor - elapsedTime) < ResourceUtilities.FLOAT_TOLERANCE,
                        "Nerm.Colonization.SnackConsumption.CalculateSnackFlow is busted - it somehow got the consumption recipe wrong.");
                }

                remainingTime -= elapsedTime;
            }

            if (remainingTime != deltaTime)
            {
                double lastMealTime = Planetarium.GetUniversalTime() - remainingTime;
                // Somebody got something to eat - record that.
                foreach (var crewMember in crew)
                {
                    LifeSupportScenario.Instance.KerbalHadASnack(crewMember, lastMealTime);
                }
            }

            if (remainingTime > ResourceUtilities.FLOAT_TOLERANCE)
            {
                // We ran out of food
                // TODO: Maybe we ought to have a single message for the whole crew?
                foreach (var crewMember in crew)
                {
                    LifeSupportScenario.Instance.KerbalMissedAMeal(crewMember);
                }
            }
        }

        private Dictionary<string, double> ResourceQuantities()
        {
            Dictionary<string, double> quantities = new Dictionary<string, double>();
            foreach (var part in this.vessel.parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (resource.amount > 0)
                    {
                        quantities.TryGetValue(resource.resourceName, out double amount);
                        quantities[resource.resourceName] = amount + resource.amount;
                    }
                }
            }

            return quantities;
        }

        private class ProducerData
        {
            // Sum up all of the snack producers of the same 
            public ISnackProducer SourceTemplate;
            public double TotalProductionCapacity;
            public string SourceResourceName;
            public double MaxResearchPerDay;

            public double SupplyFraction; // The fraction of the kerbals needs that this block is slated to take care of
        }

        /// <summary>
        ///   Calculates the consumption of snacks, using the production capacity available
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
        /// <param name="snackProducers">The mechanisms on the vessel that can produce snacks.</param>
        /// <param name="colonizationResearch">The research object (passed in for testability).</param>
        /// <param name="timePassedInSeconds">Set to the amount of <paramref name="fullTimespanInSeconds"/> that we've managed to calculate for.</param>
        /// <param name="agroponicsBreakthroughHappened">Set to true if the agronomy tier was eclipsed in the timespan.</param>
        /// <param name="consumptionFormula">Set to a formula for calculating the transformation of stuff.</param>
        internal static void CalculateSnackflow(
            int numCrew,
            double fullTimespanInSeconds,
            ISnackProducer[] snackProducers,
            IColonizationResearchScenario colonizationResearch,
            Dictionary<string,double> availableResources,
            out double timePassedInSeconds,
            out bool agroponicsBreakthroughHappened,
            out Dictionary<string,double> resourceConsumptionPerSecond
            )
        {
            // The mechanic we're after writes up pretty simple - Kerbals will try to use renewable
            // resources first, then they start in on the on-board stocks, taking as much of the low-tier
            // stuff as they can.  Implementing that is tricky...

            // First just get a handle on what stuff we could produce.
            List<ProducerData> producers = FindProducers(snackProducers, availableResources);
            // Put it in order of increasing desirability
            producers.Sort((left, right) => left.SourceTemplate.MaxConsumptionForProducedFood.CompareTo(right.SourceTemplate.MaxConsumptionForProducedFood));

            // Now see what amount of the kerbals needs can be fulfilled with each.
            double fractionSatisfiedByProduction = 0;
            foreach (ProducerData producerData in producers)
            {
                // This producer can satisfy either...
                producerData.SupplyFraction = Math.Min(
                    // ...the maximum amount the kerbals will be willing to eat or...
                    producerData.SourceTemplate.MaxConsumptionForProducedFood - fractionSatisfiedByProduction,
                    // ...the amount that the thing can produce
                    (double)producerData.TotalProductionCapacity / (double)numCrew
                );
                // ...whichever is smaller.

                fractionSatisfiedByProduction += producerData.SupplyFraction;
            }

            // Now raid the ships' stores - again in least-desirable to most
            double fractionSatisfied = fractionSatisfiedByProduction;
            for (TechTier tier = TechTier.Tier0; tier <= TechTier.Tier4; ++tier)
            {
                string resourceName = tier.SnacksResourceName();
                double maxDietRatio = tier.AgricultureMaxDietRatio();
                // If we have it and can eat it...
                if (availableResources.ContainsKey(resourceName) && fractionSatisfied < maxDietRatio)
                {
                    // ...Stick it onto our production plan
                    producers.Add(new ProducerData() {
                        SourceResourceName = resourceName,
                        SourceTemplate = null,
                        TotalProductionCapacity = numCrew,
                        SupplyFraction = maxDietRatio - fractionSatisfied
                    });
                    fractionSatisfied = maxDietRatio;
                }

                // Note that this isn't be optimum - suppose we have all tiers of food on
                // board and a mid-tier agroponic module that can only supply 80% of the ship's
                // capacity - we could use the low-tier food for some of that 80% and still be
                // within the rules, but with this algorithm, it'll insist on high quality fare
                // for all of that 80%.
            }

            if (fractionSatisfied < 0.999)
            {
                // We couldn't put together a production plan that will satisfy all of the Kerbals
                // needs for any amount of time.  (The .999 is 1.0 with a generous precision error).
                timePassedInSeconds = 0;
                resourceConsumptionPerSecond = null;
                agroponicsBreakthroughHappened = false;
            }
            else
            {
                // Get a weighted average of all the stuff we're gonna use with this plan.
                resourceConsumptionPerSecond = new Dictionary<string, double>();
                foreach (ProducerData producerData in producers)
                {
                    double contribution = UnitsPerDayToUnitsPerSecond(producerData.SupplyFraction * numCrew);
                    if (resourceConsumptionPerSecond.TryGetValue(producerData.SourceResourceName, out double existingConsumption))
                    {
                        existingConsumption += contribution;
                        resourceConsumptionPerSecond[producerData.SourceResourceName] = existingConsumption;
                    }
                    else
                    {
                        resourceConsumptionPerSecond.Add(producerData.SourceResourceName, contribution);
                    }
                }

                // Okay, with that in our hands, we can calculate the runtime, which again is the maximum
                // part of the input time we can run with the formula that we concocted.  So it's the
                // maximum time unless we're limited by supplies or fertilizer on one of our segments.
                timePassedInSeconds = fullTimespanInSeconds;
                foreach (var pair in resourceConsumptionPerSecond)
                {
                    string resourceName = pair.Key;
                    double consumedPerSecond = pair.Value;
                    double availableAmount = availableResources[resourceName];
                    double maxPossibleRuntime = availableAmount / consumedPerSecond;
                    timePassedInSeconds = Math.Min(timePassedInSeconds, maxPossibleRuntime);
                }

                // Okay, based on the actual runtime, we can contribute it to the agroponics research
                // and see if a breakthrough happened.
                agroponicsBreakthroughHappened = false;
                foreach (ProducerData producerData in producers)
                {
                    if (producerData.SourceTemplate != null)
                    {
                        double contributionInUnitsPerDay = producerData.SupplyFraction * numCrew;
                        agroponicsBreakthroughHappened |= producerData.SourceTemplate.ContributeResearch(
                            colonizationResearch,
                            timePassedInSeconds*UnitsPerDayToUnitsPerSecond(Math.Min(contributionInUnitsPerDay, producerData.MaxResearchPerDay)));
                    }
                }
            }
        }

        private static List<ProducerData> FindProducers(ISnackProducer[] snackProducers, Dictionary<string, double> availableResources)
        {
            // First run through the producers to find out who can contribute what
            List<ProducerData> productionPossibilities = new List<ProducerData>();
            foreach (ISnackProducer producer in snackProducers)
            {
                // We don't check if the dieticians are qualified here - we assume they are.  The part
                //   itself should be responsible for ensuring that and setting IsProductionEnabled
                //   appropriately.  That way there can be UI showing that the part is not functioning
                //   on the part itself.
                if (producer.IsProductionEnabled)
                {
                    ProducerData existing = productionPossibilities.FirstOrDefault(
                        pp => pp.SourceTemplate.GetType() == producer.GetType()
                           && pp.SourceTemplate.Tier == producer.Tier);

                    if (existing != null)
                    {
                        // Same kinda part as one we've already resolved -- add its capacity.
                        existing.TotalProductionCapacity += producer.Capacity;
                        if (producer.IsResearchEnabled)
                        {
                            existing.MaxResearchPerDay += producer.Capacity;
                        }
                    }
                    else
                    {
                        // Hunt for the best fertilizer match
                        string fertilizerResource = null;
                        for (TechTier tier = producer.Tier; tier <= TechTier.Tier4; ++tier)
                        {
                            if (availableResources.ContainsKey(tier.FertilizerResourceName()))
                            {
                                fertilizerResource = tier.FertilizerResourceName();
                                break;
                            }
                        }

                        if (fertilizerResource != null)
                        {
                            productionPossibilities.Add(new ProducerData()
                            {
                                SourceTemplate = producer,
                                SourceResourceName = fertilizerResource,
                                TotalProductionCapacity = producer.Capacity,
                                MaxResearchPerDay = (producer.IsResearchEnabled ? producer.Capacity : 0)
                            });
                        }
                    }
                }
            }
            return productionPossibilities;
        }

        private bool IsAtHome => vessel.mainBody == FlightGlobals.GetHomeBody() && vessel.altitude < 10000;

        private double GetDeltaTime()
        {
            if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
            {
                return -1;
            }

            if (Math.Abs(LastUpdateTime) < ResourceUtilities.FLOAT_TOLERANCE)
            {
                // Just started running
                LastUpdateTime = Planetarium.GetUniversalTime();
                return -1;
            }

            double maxDeltaTime = ResourceUtilities.GetMaxDeltaTime();
            double deltaTime = Math.Min(Planetarium.GetUniversalTime() - LastUpdateTime, maxDeltaTime);

            LastUpdateTime += deltaTime;
            return deltaTime;
        }

        /// <summary>
        ///   Our math is based on per-day calculations e.g. each kerbal eats one supply per day.
        ///   Each agroponic thing can eat up to one fertilizer per day and create one supply per day.
        ///   One unit of fertilizer, however, might weigh a whole lot less than a unit of supplies.
        /// </summary>
        private static double UnitsPerDayToUnitsPerSecond(double x) => x / (6.0 * 60.0 * 60.0);
    }
}
