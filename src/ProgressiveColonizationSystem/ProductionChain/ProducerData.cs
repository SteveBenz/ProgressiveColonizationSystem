using System;
using System.Collections.Generic;

namespace ProgressiveColonizationSystem.ProductionChain
{
    internal class ProducerData
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
                    if (sourcesObtainedSoFar >= capacityLimitedRequest - TieredProduction.AcceptableError)
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
}
