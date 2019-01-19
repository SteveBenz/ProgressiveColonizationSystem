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

        public float Amount { get; set; }

        public float MaxAmount { get; set; } = 100;
    }
}
