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

        private static ResearchCategory hydroponicResearchCategory = new HydroponicResearchCategory();
        private static ResearchCategory farmingResearchCategory = new FarmingResearchCategory();
        private static ResearchCategory productionResearchCategory = new ProductionResearchCategory();
        private static ResearchCategory scanningResearchCategory = new ScanningResearchCategory();
        private static ResearchCategory shiniesResearchCategory = new ShiniesResearchCategory();
        private static ResearchCategory rocketPartsResearchCategory = new RocketPartsResearchCategory();

        private static TieredResource scanningResource = new TieredResource("ScanningData", "Kerbal-Days", ProductionRestriction.OrbitOfBody, scanningResearchCategory, canBeStored: false, unstoredExcessCanGoToResearch: true, isHarvestedLocally: false);

        public static TieredResource LodeResource = new TieredResource("LooseCrushIns", "", ProductionRestriction.OrbitOfBody, scanningResearchCategory, canBeStored: false, unstoredExcessCanGoToResearch: false, isHarvestedLocally: true);
        public static TieredResource CrushInsResource = new TieredResource("CrushIns", null, ProductionRestriction.LandedOnBody, productionResearchCategory, false, false, isHarvestedLocally: true);

        TieredResource IColonizationResearchScenario.CrushInsResource => ColonizationResearchScenario.CrushInsResource;

        private static TieredResource[] AllTieredResources =
        {
            new EdibleResource("HydroponicSnacks", ProductionRestriction.Orbit, hydroponicResearchCategory, false, false, .2, .4, .55, .7, .95),
            new EdibleResource("Snacks", ProductionRestriction.LandedOnBody, farmingResearchCategory, true, false, .6, .85, .95, .98, 1.0),
            new TieredResource("Fertilizer", "Kerbal-Days", ProductionRestriction.LandedOnBody, productionResearchCategory, true, false, false),
            new TieredResource("Shinies", "Bling-per-day", ProductionRestriction.LandedOnBody, shiniesResearchCategory, true, false, false),
            new TieredResource("LocalParts", "Parts/Day", ProductionRestriction.LandedOnBody, rocketPartsResearchCategory, false, false, false),
            new TieredResource("Stuff", null, ProductionRestriction.LandedOnBody, productionResearchCategory, false, false, false),
            CrushInsResource,
            scanningResource,
            LodeResource,
        };

        public static TieredResource GetTieredResourceByName(string name)
        {
            return AllTieredResources.First(tr => tr.BaseName == name);
        }

        public TieredResource TryGetTieredResourceByName(string name)
        {
            return AllTieredResources.FirstOrDefault(tr => tr.BaseName == name);
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

            string bodyName = source.ProductionRestriction == ProductionRestriction.Orbit ? "" : atBody;
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
                return true;
            }
            else
            {
                return false;
            }
        }

        public IEnumerable<TieredResource> AllResourcesTypes => AllTieredResources;

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

            string bodyName = forResource.ProductionRestriction == ProductionRestriction.Orbit ? "" : atBody;
            if (!bodyToProgressMap.TryGetValue(bodyName, out TechProgress progress))
            {
                return TechTier.Tier0;
            }

            return progress.Tier;
        }

        public TechTier GetMaxUnlockedScanningTier(string atBody)
        {
            return this.GetMaxUnlockedTier(scanningResource, atBody);
        }

        public void GetResearchProgress(TieredResource forResource, string atBody, out double accumulatedKerbalDays, out double requiredKerbalDays)
        {
            double kerbalSecondsSoFar = 0;
            TechTier currentTier = TechTier.Tier0;
            if (this.categoryToBodyToProgressMap.TryGetValue(forResource.ResearchCategory, out Dictionary<string, TechProgress> bodyToProgressMap))
            {
                string bodyName = forResource.ProductionRestriction == ProductionRestriction.Orbit ? "" : atBody;
                if (bodyToProgressMap.TryGetValue(bodyName, out TechProgress progress))
                {
                    currentTier = progress.Tier;
                    kerbalSecondsSoFar = progress.ProgressInKerbalSeconds;
                }
            }

            accumulatedKerbalDays = KerbalSecondsToDays(kerbalSecondsSoFar);
            requiredKerbalDays = KerbalYearsToDays(forResource.ResearchCategory.KerbalYearsToNextTier(currentTier));
        }
        
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            this.categoryToBodyToProgressMap = new Dictionary<ResearchCategory, Dictionary<string, TechProgress>>();
            foreach (var pair in new KeyValuePair<string, ResearchCategory>[]
            {
                new KeyValuePair<string, ResearchCategory>("agriculture", farmingResearchCategory),
                new KeyValuePair<string, ResearchCategory>("production", productionResearchCategory),
                new KeyValuePair<string, ResearchCategory>("scanning", scanningResearchCategory),
                new KeyValuePair<string, ResearchCategory>("shinies", shiniesResearchCategory),
                new KeyValuePair<string, ResearchCategory>("construction", rocketPartsResearchCategory),
            })
            {
                ConfigNode childNode = null;
                if (node.TryGetNode(pair.Key, ref childNode) && TryCreateFromNode(childNode, out Dictionary<string, TechProgress> map))
                {
                    this.categoryToBodyToProgressMap.Add(pair.Value, map);
                }
            }

            ConfigNode hydroponicsNode = null;

            if (node.TryGetNode("hydroponics", ref hydroponicsNode) && TryCreateFromNode(hydroponicsNode, out TechProgress hydroponicsProgress))
            {
                this.categoryToBodyToProgressMap.Add(
                    hydroponicResearchCategory,
                    new Dictionary<string, TechProgress>() { { "", hydroponicsProgress } });
            }
        }

        public override void OnSave(ConfigNode node)
        {
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

            base.OnSave(node);

            foreach (var pair in new KeyValuePair<string, ResearchCategory>[]
            {
                new KeyValuePair<string, ResearchCategory>("agriculture", farmingResearchCategory),
                new KeyValuePair<string, ResearchCategory>("production", productionResearchCategory),
                new KeyValuePair<string, ResearchCategory>("scanning", scanningResearchCategory),
                new KeyValuePair<string, ResearchCategory>("shinies", shiniesResearchCategory),
                new KeyValuePair<string, ResearchCategory>("construction", rocketPartsResearchCategory),
            })
            {
                if (this.categoryToBodyToProgressMap.TryGetValue(pair.Value, out var stringToProgressMap))
                {
                    node.AddNode(pair.Key, ToNode(stringToProgressMap));
                }
            }

            if (this.categoryToBodyToProgressMap.TryGetValue(hydroponicResearchCategory, out var hydroponicProgress)
             && hydroponicProgress.TryGetValue("", out TechProgress techProgress))
            {
                node.AddNode("hydroponics", ToNode(techProgress));
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
