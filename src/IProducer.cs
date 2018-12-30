using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    internal interface IProducer
    {
        TechTier Tier { get; }
        double Capacity { get; }
        bool IsResearchEnabled { get; }
        bool IsProductionEnabled { get; }

        /// <summary>
        ///   Contribute production amount to the research stack
        /// </summary>
        /// <param name="amount">The amount of units of supplies manufactured.</param>
        /// <returns>True if there was a research breakthrough as a result of this.</returns>
        bool ContributeResearch(IColonizationResearchScenario target, double amount);

        /// <summary>
        ///   If true, the output can be stored in the ship, if false, it means that it can only
        ///   produce stuff that the kerbals eat immediately.  (Or rather, what we're really trying
        ///   to do is say that this is a zero-sum thing - it can only produce as much as the kerbals poop.)
        /// </summary>
        bool CanStockpileProduce { get; }

		string ProductResourceName { get; }
    }
}
