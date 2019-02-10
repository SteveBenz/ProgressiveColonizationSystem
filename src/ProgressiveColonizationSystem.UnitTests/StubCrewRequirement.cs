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
        bool IPksCrewRequirement.IsRunning => throw new NotImplementedException();

        bool IPksCrewRequirement.IsStaffed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        float IPksCrewRequirement.CapacityRequired => throw new NotImplementedException();

        IEnumerable<string> IPksCrewRequirement.RequiredTraits => throw new NotImplementedException();

        bool IPksCrewRequirement.CanRunPart(SkilledCrewman crewman)
        {
            throw new NotImplementedException();
        }
    }
}
