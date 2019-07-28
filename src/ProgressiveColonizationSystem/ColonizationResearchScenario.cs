using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
    public class ColonizationResearchScenario
        : ScenarioModule, IColonizationResearchScenario
    {
        public static ColonizationResearchScenario Instance;

        private Dictionary<ResearchCategory, Dictionary<string, TechProgress>> categoryToBodyToProgressMap;
        private static Dictionary<string, ResearchCategory> researchCategories;
        private static Dictionary<string, TieredResource> resources;

        public static TieredResource LodeResource
        {
            get
            {
                ColonizationResearchScenario.LoadResourcesIfNeeded();
                return ColonizationResearchScenario.resources["LooseCrushIns"];
            }
        }

        public TieredResource CrushInsResource
        {
            get
            {
                ColonizationResearchScenario.LoadResourcesIfNeeded();
                return ColonizationResearchScenario.resources["CrushIns"];
            }
        }

        public static TieredResource GetTieredResourceByName(string name)
        {
            ColonizationResearchScenario.LoadResourcesIfNeeded();
            if (!resources.ContainsKey(name))
            {
                throw new Exception($"Key not found: {name}");
            }
            return ColonizationResearchScenario.resources[name];
        }

        public TieredResource TryGetTieredResourceByName(string name)
        {
            ColonizationResearchScenario.LoadResourcesIfNeeded();
            resources.TryGetValue(name, out var resource);
            return resource;
        }

        public bool TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier)
        {
            int dashIndex = tieredResourceName.IndexOf('-');
            if (dashIndex < 0)
            {
                resource = TryGetTieredResourceByName(tieredResourceName);
                tier = TechTier.Tier4;
                return resource != null;
            }
            else
            {
                try
                {
                    // Oh, but we do pine ever so much for .Net 4.6...
                    tier = (TechTier)Enum.Parse(typeof(TechTier), tieredResourceName.Substring(dashIndex + 1));
                    var tier4Name = tieredResourceName.Substring(0, dashIndex);
                    resource = TryGetTieredResourceByName(tier4Name);
                    return resource != null;
                }
                catch (Exception)
                {
                    resource = null;
                    tier = TechTier.Tier0;
                    return false;
                }
            }
        }

        /// <summary>
        ///   A '|' separated list of worlds where the player can do agriculture&c.
        /// </summary>
        /// <remarks>
        ///   This is stored because it comes from the ProgressTracking scenario, which isn't
        ///   loaded in the editor, which is the place we principally need this data.
        /// </remarks>
        [KSPField(isPersistant = true)]
        public string unlockedBodies = "";

        public ColonizationResearchScenario()
        {
            Instance = this;
        }

        public string[] UnlockedBodies =>
            string.IsNullOrEmpty(this.unlockedBodies) ? new string[0] : this.unlockedBodies.Split(new char[] { '|' });

        public bool ContributeResearch(TieredResource source, string atBody, double timespentInKerbalSeconds)
        {
            if (!this.categoryToBodyToProgressMap.TryGetValue(source.ResearchCategory, out Dictionary<string, TechProgress> bodyToProgressMap))
            {
                bodyToProgressMap = new Dictionary<string, TechProgress>();
                this.categoryToBodyToProgressMap.Add(source.ResearchCategory, bodyToProgressMap);
            }

            string bodyName = source.ProductionRestriction == ProductionRestriction.Space ? "" : atBody;
            if (!bodyToProgressMap.TryGetValue(bodyName, out TechProgress progress))
            {
                progress = new TechProgress() { ProgressInKerbalSeconds = 0, Tier = TechTier.Tier0 };
                bodyToProgressMap.Add(bodyName, progress);
            }

            progress.ProgressInKerbalSeconds += timespentInKerbalSeconds;
            if (progress.ProgressInKerbalSeconds > KerbalYearsToSeconds(source.ResearchCategory.KerbalYearsToNextTier(progress.Tier)))
            {
                progress.ProgressInKerbalSeconds = 0;
                ++progress.Tier;
                if (progress.Tier == TechTier.Tier1 && this.EligibleToSkipTier1(source.ResearchCategory))
                {
                    ++progress.Tier;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool EligibleToSkipTier1(ResearchCategory researchCategory)
        {
            // If we've researched Tier3 on 2 or more worlds, allow user to skip T1 for this category.
            return this.categoryToBodyToProgressMap[researchCategory].Values.Count(v => v.Tier > TechTier.Tier3) >= 2;
        }

        public IEnumerable<TieredResource> AllResourcesTypes
        {
            get
            {
                if (ColonizationResearchScenario.resources == null)
                {
                    ColonizationResearchScenario.LoadResourcesIfNeeded();
                }
                return ColonizationResearchScenario.resources.Values;
            }
        }

        public static double KerbalYearsToSeconds(double years) => KerbalDaysToSeconds(years * 426.0);
        public static double KerbalYearsToDays(double years) => years * 426.0;
        public static double SecondsToKerbalDays(double seconds) => seconds / (6.0 * 60.0 * 60.0);
        public static double KerbalDaysToSeconds(double days) => days * (6.0 * 60.0 * 60.0);
        public static double KerbalSecondsToDays(double days) => days / (6.0 * 60.0 * 60.0);

        public TechTier GetMaxUnlockedTier(TieredResource forResource, string atBody)
        {
            if (!this.categoryToBodyToProgressMap.TryGetValue(forResource.ResearchCategory, out Dictionary<string, TechProgress> bodyToProgressMap))
            {
                return TechTier.Tier0;
            }

            string bodyName = forResource.ProductionRestriction == ProductionRestriction.Space ? "" : atBody;
            if (!bodyToProgressMap.TryGetValue(bodyName, out TechProgress progress))
            {
                return TechTier.Tier0;
            }

            return progress.Tier;
        }

        public TechTier GetMaxUnlockedScanningTier(string atBody)
        {
            ColonizationResearchScenario.LoadResourcesIfNeeded();
            return this.GetMaxUnlockedTier(ColonizationResearchScenario.resources["ScanningData"], atBody);
        }

        internal ResearchData GetResearchProgress(TieredResource forResource, string atBody)
        {
            double kerbalSecondsSoFar = 0;
            TechTier currentTier = TechTier.Tier0;
            if (this.categoryToBodyToProgressMap.TryGetValue(forResource.ResearchCategory, out Dictionary<string, TechProgress> bodyToProgressMap))
            {
                string bodyName = forResource.ProductionRestriction == ProductionRestriction.Space ? "" : atBody;
                if (bodyToProgressMap.TryGetValue(bodyName, out TechProgress progress))
                {
                    currentTier = progress.Tier;
                    kerbalSecondsSoFar = progress.ProgressInKerbalSeconds;
                }
            }

            return new ResearchData(forResource.ResearchCategory, currentTier, KerbalSecondsToDays(kerbalSecondsSoFar), KerbalYearsToDays(forResource.ResearchCategory.KerbalYearsToNextTier(currentTier)));
        }

        internal ResearchData GetResearchProgress(TieredResource forResource, string atBody, TechTier tier, string whyBlocked)
        {
            double kerbalSecondsSoFar = 0;
            TechTier currentTier = TechTier.Tier0;
            if (this.categoryToBodyToProgressMap.TryGetValue(forResource.ResearchCategory, out Dictionary<string, TechProgress> bodyToProgressMap))
            {
                string bodyName = forResource.ProductionRestriction == ProductionRestriction.Space ? "" : atBody;
                if (bodyToProgressMap.TryGetValue(bodyName, out TechProgress progress))
                {
                    currentTier = progress.Tier;
                    kerbalSecondsSoFar = progress.ProgressInKerbalSeconds;
                }
            }

            var result = (currentTier == tier)
                ? new ResearchData(forResource.ResearchCategory, tier, KerbalSecondsToDays(kerbalSecondsSoFar), KerbalYearsToDays(forResource.ResearchCategory.KerbalYearsToNextTier(currentTier)))
                : new ResearchData(forResource.ResearchCategory, tier, 0, 0);
            result.WhyBlocked = whyBlocked;
            return result;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            ColonizationResearchScenario.LoadResourcesIfNeeded();
            this.categoryToBodyToProgressMap = new Dictionary<ResearchCategory, Dictionary<string, TechProgress>>();
            foreach (var category in ColonizationResearchScenario.researchCategories.Values)
            {
                if (category.Type == ProductionRestriction.Space)
                {
                    ConfigNode hydroponicsNode = null;
                    if (node.TryGetNode(category.Name, ref hydroponicsNode) && TryCreateFromNode(hydroponicsNode, out TechProgress hydroponicsProgress))
                    {
                        this.categoryToBodyToProgressMap.Add(
                            category,
                            new Dictionary<string, TechProgress>() { { "", hydroponicsProgress } });
                    }
                }
                else
                {
                    ConfigNode childNode = null;
                    if (node.TryGetNode(category.Name, ref childNode) && TryCreateFromNode(childNode, out Dictionary<string, TechProgress> map))
                    {
                        this.categoryToBodyToProgressMap.Add(category, map);
                    }
                }
            }
        }

        private static void LoadResourcesIfNeeded()
        {
            if (ColonizationResearchScenario.researchCategories == null)
            {
                ColonizationResearchScenario.researchCategories =
                    GameDatabase.Instance.GetConfigNodes("TIERED_RESEARCH_CATEGORY")
                        .Select(n => new ResearchCategory(n))
                        .ToDictionary(rc => rc.Name, rc => rc);
                ColonizationResearchScenario.resources = TieredResource.LoadAll(ColonizationResearchScenario.researchCategories);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            ColonizationResearchScenario.LoadResourcesIfNeeded();

            // Update valid bodies if possible
            if (ProgressTracking.Instance != null && ProgressTracking.Instance.celestialBodyNodes != null)
            {
                StringBuilder validBodies = new StringBuilder();
                foreach (var body in ProgressTracking.Instance.celestialBodyNodes)
                {
                    if (body.returnFromSurface != null && body.returnFromSurface.IsComplete)
                    {
                        if (validBodies.Length != 0)
                        {
                            validBodies.Append('|');
                        }
                        validBodies.Append(body.Id);
                    }
                }
                this.unlockedBodies = validBodies.ToString();
            }

            foreach (var category in ColonizationResearchScenario.researchCategories.Values)
            {
                if (category.Type == ProductionRestriction.Space
                    && this.categoryToBodyToProgressMap.TryGetValue(category, out var progress)
                    && progress.TryGetValue("", out TechProgress techProgress))
                {
                    node.AddNode(category.Name, ToNode(techProgress));
                }
                else if (category.Type != ProductionRestriction.Space
                         && this.categoryToBodyToProgressMap.TryGetValue(category, out var stringToProgressMap))
                {
                    node.AddNode(category.Name, ToNode(stringToProgressMap));
                }
            }
        }

        private static ConfigNode ToNode(Dictionary<string, TechProgress> map)
        {
            ConfigNode agNode = new ConfigNode();
            foreach (KeyValuePair<string, TechProgress> pair in map)
            {
                agNode.AddNode(pair.Key, ToNode(pair.Value));
            }
            return agNode;
        }

        private static ConfigNode ToNode(TechProgress progress)
        {
            ConfigNode bodyNode = new ConfigNode();
            bodyNode.AddValue("tier", progress.Tier.ToString());
            bodyNode.AddValue("progress", progress.ProgressInKerbalSeconds);
            return bodyNode;
        }

        private static bool TryCreateFromNode(ConfigNode node, out TechProgress progress)
        {
            TechTier tierAtBody = TechTier.Tier0;
            double progressToNext = 0;
            if (!node.TryGetEnum<TechTier>("tier", ref tierAtBody, TechTier.Tier0)
             || !node.TryGetValue("progress", ref progressToNext))
            {
                progress = null;
                return false;
            }

            progress = new TechProgress()
            {
                ProgressInKerbalSeconds = progressToNext,
                Tier = tierAtBody
            };
            return true;
        }

        private static bool TryCreateFromNode(ConfigNode node, out Dictionary<string, TechProgress> map)
        {
            map = null;
            foreach (ConfigNode childNode in node.GetNodes())
            {
                TechProgress progress;
                if (TryCreateFromNode(childNode, out progress))
                {
                    if (map == null)
                    {
                        map = new Dictionary<string, TechProgress>();
                    }
                    map[childNode.name] = progress;
                }
            }
            return map != null;
        }
    }

    // Test interface
    public interface IColonizationResearchScenario
    {
        bool ContributeResearch(TieredResource source, string atBody, double timespentInKerbalSeconds);
        TechTier GetMaxUnlockedTier(TieredResource forResource, string atBody);
        bool TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier);
        IEnumerable<TieredResource> AllResourcesTypes { get; }
        TechTier GetMaxUnlockedScanningTier(string atBody);
        TieredResource CrushInsResource { get; }
    }
}
