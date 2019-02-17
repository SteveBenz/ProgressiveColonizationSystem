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
        public StubCrewRequirement(string requiredEffect, int requiredLevel)
        {
            this.RequiredEffect = requiredEffect;
            this.RequiredLevel = requiredLevel;
        }

        public bool IsRunning { get; set; }

        public bool IsStaffed { get; set; } = true;

        public float CapacityRequired { get; set; } = 1f;

        public string RequiredEffect { get; }

        public int RequiredLevel { get; }
    }
}
