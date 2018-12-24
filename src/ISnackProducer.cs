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
    }
}
