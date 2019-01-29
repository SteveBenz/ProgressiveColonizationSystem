using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public interface ITieredContainer
    {
        TechTier Tier { get; }
		TieredResource Content { get; }
        double Amount { get; set; }
        double MaxAmount { get; }
    }
}
