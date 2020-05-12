using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public interface ITieredProducer
    {
        TechTier Tier { get; set; }
        TechTier MaximumTier { get; }
        double ProductionRate { get; }
        bool IsResearchEnabled { get; }
        bool IsProductionEnabled { get; }

        /// <summary>
        ///   Contribute production amount to the research stack
        /// </summary>
        /// <param name="amount">The amount of units of supplies manufactured.</param>
        /// <returns>True if there was a research breakthrough as a result of this.</returns>
        bool ContributeResearch(IColonizationResearchScenario target, double amount);

        TieredResource Output { get; }

        TieredResource Input { get; }

        string Body { get; set; }

        string ReasonWhyResearchIsDisabled { get; }
    }
}
