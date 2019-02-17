using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressiveColonizationSystem.UnitTests
{
    public class StubSkilledCrewman
        : SkilledCrewman
    {
        private readonly int level1;
        private readonly string effect1;
        private readonly int level2;
        private readonly string effect2;

        public StubSkilledCrewman(string effect, int level)
            : base(null)
        {
            this.level1 = level;
            this.effect1 = effect;
        }

        public StubSkilledCrewman(string effect1, int level1, string effect2, int level2)
            : base(null)
        {
            this.level1 = level1;
            this.effect1 = effect1;
            this.level2 = level2;
            this.effect2 = effect2;
        }

        public override bool CanRunPart(string requiredEffect, int requiredLevel)
        {
            return this.effect1 == requiredEffect && this.level1 >= requiredLevel
                || this.effect2 == requiredEffect && this.level2 >= requiredLevel;
        }
    }
}
