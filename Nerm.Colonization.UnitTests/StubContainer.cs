using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nerm.Colonization.UnitTests
{
    public class StubContainer
        : ITieredContainer
    {
        public TechTier Tier { get; set; } = TechTier.Tier0;

        public TieredResource Content { get; set; } = StubColonizationResearchScenario.GetTieredResourceByName("Snacks");

        public double Amount { get; set; }

        public double MaxAmount { get; set; } = 100;
    }
}
