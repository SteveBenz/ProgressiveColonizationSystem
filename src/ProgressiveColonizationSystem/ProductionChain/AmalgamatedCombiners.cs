using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem.ProductionChain
{
    internal class AmalgamatedCombiners
    {
        private readonly ITieredCombiner exemplar;

        public AmalgamatedCombiners(IEnumerable<ITieredCombiner> tieredCombiner)
        {
            var l = tieredCombiner.ToList();
            this.exemplar = l.First();
            this.ProductionRate = l.Sum(c => c.ProductionRate);
        }

        public double ProductionRate { get; }

        public string NonTieredOutputResourceName => exemplar.NonTieredOutputResourceName;

        public TieredResource TieredInput => exemplar.TieredInput;

        public string NonTieredInputResourceName => exemplar.NonTieredInputResourceName;

        public double GetRatioForTier(TechTier tier) => this.exemplar.GetRatioForTier(tier);

        public double UsedCapacity { get; set; }

        public double RequiredMixins { get; set; }
    }
}
