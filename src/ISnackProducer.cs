using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    internal interface ISnackProducer
    {
        TechTier Tier { get; }
        double Capacity { get; }
        bool IsResearchEnabled { get; }
        bool IsProductionEnabled { get; }

        /// <summary>
        ///   For the type and tier of food, what's the maximum percentage of a kerbal's diet that
        ///   this kind of food can be.
        /// </summary>
        double MaxConsumptionForProducedFood { get; }

        /// <summary>
        ///   Contribute production amount to the research stack
        /// </summary>
        /// <param name="amount">The amount of units of supplies manufactured.</param>
        /// <returns>True if there was a research breakthrough as a result of this.</returns>
        bool ContributeResearch(ColonizationResearchScenario target, double amount);
    }
}
