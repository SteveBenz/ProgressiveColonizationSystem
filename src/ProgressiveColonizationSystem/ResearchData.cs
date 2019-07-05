using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{

    internal class ResearchData
    {
        private readonly TechTier currentTier;

        public ResearchData(ResearchCategory category, TechTier currentTier, double accumulatedKerbalDays, double kerbalDaysRequired)
        {
            this.Category = category;
            this.AccumulatedKerbalDays = accumulatedKerbalDays;
            this.KerbalDaysRequired = kerbalDaysRequired;
            this.KerbalDaysContributedPerDay = 0;
            this.currentTier = currentTier;
        }

        public bool HasProgress => this.AccumulatedKerbalDays > 0;
        public bool IsAtMaxTier => this.currentTier == TechTier.Tier4;

        public ResearchCategory Category { get; }
        public TechTier TierBeingResearched => (TechTier)(this.currentTier + 1);
        public double AccumulatedKerbalDays { get; }
        public double KerbalDaysRequired { get; }
        public double KerbalDaysContributedPerDay { get; set; }
        public string WhyBlocked { get; set; }
    }
}
