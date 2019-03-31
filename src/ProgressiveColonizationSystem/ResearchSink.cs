using System.Collections.Generic;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   A <see cref="IColonizationResearchScenario"/> to use when just measuring the potential impact on research
    ///   from a simulated production run.
    /// </summary>
    internal class ResearchSink
        : IColonizationResearchScenario
    {
        public Dictionary<ResearchCategory, ResearchData> Data { get; } = new Dictionary<ResearchCategory, ResearchData>();

        internal class ResearchData
        {
            public ResearchData(double contributionInKerbalDaysPerDay, double accumulatedKerbalDays, double kerbalDaysRequired)
            {
                this.KerbalDaysContributedPerDay = contributionInKerbalDaysPerDay;
                this.AccumulatedKerbalDays = accumulatedKerbalDays;
                this.KerbalDaysRequired = kerbalDaysRequired;
            }

            public double KerbalDaysContributedPerDay { get; }
            public double AccumulatedKerbalDays { get; }
            public double KerbalDaysRequired { get; }
        }

        IEnumerable<TieredResource> IColonizationResearchScenario.AllResourcesTypes => ColonizationResearchScenario.Instance.AllResourcesTypes;

        TieredResource IColonizationResearchScenario.CrushInsResource => ColonizationResearchScenario.CrushInsResource;

        bool IColonizationResearchScenario.ContributeResearch(TieredResource source, string atBody, double researchInKerbalsecondsPerSecond)
        {
            // KerbalDaysContributedPerDay is equal to Kerbals.
            // timeSpentInKerbalSeconds works out to be time spent in a kerbal second (because that's the timespan
            // we passed into the production engine), so it's really kerbalSecondsContributedPerKerbalSecond.
            if (this.Data.TryGetValue(source.ResearchCategory, out ResearchData data))
            {
                this.Data[source.ResearchCategory] = new ResearchData(
                    researchInKerbalsecondsPerSecond + data.KerbalDaysContributedPerDay,
                    data.AccumulatedKerbalDays,
                    data.KerbalDaysRequired);
            }
            else
            {
                ColonizationResearchScenario.Instance.GetResearchProgress(source, atBody, out double accumulated, out double required);
                data = new ResearchData(researchInKerbalsecondsPerSecond, accumulated, required);
                this.Data.Add(source.ResearchCategory, data);
            }

            return false;
        }

        TechTier IColonizationResearchScenario.GetMaxUnlockedScanningTier(string atBody)
            => ColonizationResearchScenario.Instance.GetMaxUnlockedScanningTier(atBody);

        TechTier IColonizationResearchScenario.GetMaxUnlockedTier(TieredResource forResource, string atBody)
            => ColonizationResearchScenario.Instance.GetMaxUnlockedTier(forResource, atBody);

        bool IColonizationResearchScenario.TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier)
            => ColonizationResearchScenario.Instance.TryParseTieredResourceName(tieredResourceName, out resource, out tier);
    }
}
