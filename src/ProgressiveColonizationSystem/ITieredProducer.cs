using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public interface ITieredProducer
    {
        TechTier Tier { get; set; }
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
    }

    public static class TieredProducerExtensions
    {
        public static TechTier GetOptimumTier(this ITieredProducer _this, IColonizationResearchScenario colonizationResearchScenario)
        {
            throw new NotImplementedException();
        }

        public static TechTier GetMaximumTier(this ITieredProducer _this, IColonizationResearchScenario colonizationResearchScenario)
        {
            throw new NotImplementedException();
        }

        public static TechTier GetOptimumTier(this ITieredProducer _this)
            => _this.GetOptimumTier(ColonizationResearchScenario.Instance);

        public static TechTier GetMaximumTier(this ITieredProducer _this)
            => _this.GetMaximumTier(ColonizationResearchScenario.Instance);
    }
}
