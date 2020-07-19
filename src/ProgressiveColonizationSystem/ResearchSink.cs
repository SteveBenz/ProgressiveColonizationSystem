using System.Collections.Generic;
using System.Linq;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   A <see cref="IColonizationResearchScenario"/> to use when just measuring the potential impact on research
    ///   from a simulated production run.
    /// </summary>
    internal class ResearchSink
        : IColonizationResearchScenario
    {
        private Dictionary<string, ResearchData> Data = new Dictionary<string, ResearchData>();

        IEnumerable<TieredResource> IColonizationResearchScenario.AllResourcesTypes => ColonizationResearchScenario.Instance.AllResourcesTypes;

        TieredResource IColonizationResearchScenario.CrushInsResource => ColonizationResearchScenario.Instance.CrushInsResource;

        bool IColonizationResearchScenario.ContributeResearch(TieredResource source, string atBody, double researchInKerbalsecondsPerSecond)
        {
            string key = atBody == null ? source.ResearchCategory.Name : $"{source.ResearchCategory.Name}-{atBody.ToLowerInvariant()}";
            // KerbalDaysContributedPerDay is equal to Kerbals.
            // timeSpentInKerbalSeconds works out to be time spent in a kerbal second (because that's the timespan
            // we passed into the production engine), so it's really kerbalSecondsContributedPerKerbalSecond.
            if (!this.Data.TryGetValue(key, out ResearchData data))
            {
                data = ColonizationResearchScenario.Instance.GetResearchProgress(source, atBody);
                this.Data.Add(key, data);
            }

            data.KerbalDaysContributedPerDay += researchInKerbalsecondsPerSecond;
            return false;
        }

        public List<ResearchData> ResearchData
            => this.Data.Values.ToList();

        TechTier IColonizationResearchScenario.GetMaxUnlockedScanningTier(string atBody)
            => ColonizationResearchScenario.Instance.GetMaxUnlockedScanningTier(atBody);

        TechTier IColonizationResearchScenario.GetMaxUnlockedTier(TieredResource forResource, string atBody)
            => ColonizationResearchScenario.Instance.GetMaxUnlockedTier(forResource, atBody);

        bool IColonizationResearchScenario.TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier)
            => ColonizationResearchScenario.Instance.TryParseTieredResourceName(tieredResourceName, out resource, out tier);
    }
}
