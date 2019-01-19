using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public interface ITieredContainer
    {
        TechTier Tier { get; }
		TieredResource Content { get; }
        float Amount { get; set; }
        float MaxAmount { get; }
    }
}
