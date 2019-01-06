using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
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

        private static TieredResource[] AllTieredResources =
        {
            new EdibleResource("HydroponicSnacks", ProductionRestriction.Orbit, hydroponicResearchCategory, false, false, .2, .4, .55, .7, .95),
            new EdibleResource("Snacks", ProductionRestriction.Orbit, farmingResearchCategory, true, false, .6, .85, .95, .98, 1.0),
            new TieredResource("Fertilizer", "Kerbal-Days", ProductionRestriction.LandedOnBody, productionResearchCategory, true, false),
            new TieredResource("Shinies", "Bling-per-day", ProductionRestriction.LandedOnBody, shiniesResearchCategory, true, false),
            new TieredResource("Stuff", null, ProductionRestriction.LandedOnBody, productionResearchCategory, true, false),
            new TieredResource("ScanningData", "Kerbal-Days", ProductionRestriction.OrbitOfBody, scanningResearchCategory, false, true)
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
            if (progress.ProgressInKerbalSeconds > KerbalYearsToKerbalSeconds(source.ResearchCategory.KerbalYearsToNextTier(progress.Tier)))
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

        public static double KerbalYearsToKerbalSeconds(double years) => years * 426.0 * 6.0 * 60.0 * 60.0;
        public static double KerbalSecondsToKerbalDays(double seconds) => seconds / (6.0 * 60.0 * 60.0);

        public string GetBreakthroughMessage(TieredResource resource, TechTier newTier)
        {
            return resource.ResearchCategory.BreakthroughMessage(newTier);
        }

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

        public double GetKerbalDaysUntilNextTier(TieredResource forResource, string atBody)
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

            double kerbalSecondsToGo = KerbalYearsToKerbalSeconds(forResource.ResearchCategory.KerbalYearsToNextTier(currentTier));
            return KerbalSecondsToKerbalDays(kerbalSecondsToGo);
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
            })
            {
                var map = new Dictionary<string, TechProgress>();
                if (node.TryGetValue(pair.Key, ref map))
                {
                    this.categoryToBodyToProgressMap.Add(pair.Value, map);
                }
            }

            int agroponicsTier = 0;
            double progressInKerbalSeconds = 0;
            node.TryGetValue("agroponicsMaxTier", ref agroponicsTier);
            node.TryGetValue("accumulatedAgroponicResearchProgressToNextTier", ref progressInKerbalSeconds);
            categoryToBodyToProgressMap.Add(hydroponicResearchCategory,
                new Dictionary<string, TechProgress>() {
                    { "", new TechProgress() {
                        Tier = (TechTier)agroponicsTier,
                        ProgressInKerbalSeconds = progressInKerbalSeconds } } });
        }

        public override void OnSave(ConfigNode node)
        {
            // Update valid bodies if possible
            if (ProgressTracking.Instance != null && ProgressTracking.Instance.celestialBodyNodes != null)
            {
                StringBuilder validBodies = new StringBuilder();
                foreach (var cbn in ProgressTracking.Instance.celestialBodyNodes)
                {
                    if (cbn.returnFromSurface != null && cbn.returnFromSurface.IsComplete)
                    {
                        if (validBodies.Length != 0)
                        {
                            validBodies.Append('|');
                        }
                        validBodies.Append(cbn.Id);
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
            })
            {
                if (this.categoryToBodyToProgressMap.TryGetValue(pair.Value, out var stringToProgressMap))
                {
                    node.SetValue(pair.Key, stringToProgressMap);
                }
            }

            if (this.categoryToBodyToProgressMap.TryGetValue(hydroponicResearchCategory, out var hydroponicProgress)
              && hydroponicProgress.TryGetValue("", out TechProgress techProgress))
            {
                node.AddValue("agroponicsMaxTier", (int)techProgress.Tier);
                node.AddValue("accumulatedAgroponicResearchProgressToNextTier", techProgress.ProgressInKerbalSeconds);
            }
        }
    }

    // Test interface
    public interface IColonizationResearchScenario
    {
        bool ContributeResearch(TieredResource source, string atBody, double timespentInKerbalSeconds);
        TechTier GetMaxUnlockedTier(TieredResource forResource, string atBody);
        bool TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier);
    }
}
