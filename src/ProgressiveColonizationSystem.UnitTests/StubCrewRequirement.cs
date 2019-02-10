using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressiveColonizationSystem.UnitTests
{
    public class StubCrewRequirement
        : IPksCrewRequirement
    {
        public int CountCanRunParts { get; set; } = 0;

        public HashSet<SkilledCrewman> ValidCrew { get; set; } = new HashSet<SkilledCrewman>();

        public bool IsRunning { get; set; }

        public bool IsStaffed { get; set; } = true;

        public float CapacityRequired { get; set; } = 1f;

        public IEnumerable<string> RequiredTraits { get; set; } = new string[] { "Test1", "Test2" };

        bool IPksCrewRequirement.CanRunPart(SkilledCrewman crewman)
        {
            ++CountCanRunParts;
            return ValidCrew.Contains(crewman);
        }
    }
}
