using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public interface ITieredCombiner
    {
        double ProductionRate { get; }

        string NonTieredOutputResourceName { get; }

        TieredResource TieredInput { get; }

        string NonTieredInputResourceName { get; }

        double GetRatioForTier(TechTier tier);

        bool IsProductionEnabled { get; }
    }
}
