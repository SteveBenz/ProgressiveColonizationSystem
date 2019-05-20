using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    public enum ProductionRestriction
    {
        Space,
        OrbitOfBody,
        LandedOnBody,
    }

    public class TieredResource
    {
        private double[] effectivenessAtTier;
        private float[] reputationPerUnitAtTier;
        private TieredResource madeFrom;
        private TechTier madeFromStartsAt;


        public TieredResource(
            string name,
            string capacityUnits,
            ResearchCategory researchCategory,
            bool canBeStored,
            bool unstoredExcessCanGoToResearch,
            bool isHarvestedLocally,
            TieredResource madeFrom,
            TechTier madeFromStartsAt)
        {
            this.BaseName = name;
            this.DisplayName = name;
            this.CapacityUnits = capacityUnits;
            this.CanBeStored = canBeStored;
            this.ExcessProductionCountsTowardsResearch = unstoredExcessCanGoToResearch;
            this.ResearchCategory = researchCategory;
            this.IsHarvestedLocally = isHarvestedLocally;
            this.madeFrom = madeFrom;
            this.madeFromStartsAt = madeFromStartsAt;
            this.reputationPerUnitAtTier = name == "Shinies" ? new float[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f } : new float[5];
        }

        public TieredResource(
            string name,
            string capacityUnits,
            ResearchCategory researchCategory,
            bool canBeStored,
            bool unstoredExcessCanGoToResearch,
            bool isHarvestedLocally,
            TieredResource madeFrom,
            TechTier madeFromStartsAt,
            double effT0, double effT1, double effT2, double effT3, double effT4)
        {
            this.BaseName = name;
            this.DisplayName = name;
            this.CapacityUnits = capacityUnits;
            this.CanBeStored = canBeStored;
            this.ExcessProductionCountsTowardsResearch = unstoredExcessCanGoToResearch;
            this.ResearchCategory = researchCategory;
            this.IsHarvestedLocally = isHarvestedLocally;
            this.madeFrom = madeFrom;
            this.madeFromStartsAt = madeFromStartsAt;
            this.effectivenessAtTier = new double[5];
            this.effectivenessAtTier[0] = effT0;
            this.effectivenessAtTier[1] = effT1;
            this.effectivenessAtTier[2] = effT2;
            this.effectivenessAtTier[3] = effT3;
            this.effectivenessAtTier[4] = effT4;
            this.reputationPerUnitAtTier = new float[5];
        }


        private TieredResource(ConfigNode c, ResearchCategory researchCategory, TieredResource madeFrom, TechTier madeFromStartsAt)
        {
            this.BaseName = c.GetValue("name");
            this.DisplayName = c.GetValue("display_name");
            this.CapacityUnits = c.GetValue("capacity_units");
            bool canBeStored = false;
            c.TryGetValue("can_be_stored", ref canBeStored);
            this.CanBeStored = canBeStored;

            bool unstoredExcessCanGoToResearch = false;
            c.TryGetValue("unstored_excess_can_go_to_research", ref unstoredExcessCanGoToResearch);
            this.ExcessProductionCountsTowardsResearch = unstoredExcessCanGoToResearch;

            bool isHarvestedLocally = false;
            c.TryGetValue("is_harvested_locally", ref isHarvestedLocally);
            this.IsHarvestedLocally = isHarvestedLocally;

            this.CrewSkill = c.GetValue("crew_skill");
            researchCategory.AddCrewSkill(this.CrewSkill);

            this.ResearchCategory = researchCategory;

            // If it's edible, it'll have these set
            bool allSet = true;
            double[] effectiveness = new double[1 + (int)TechTier.Tier4];
            for (TechTier tech = TechTier.Tier0; tech <= TechTier.Tier4; ++tech)
            {
                string name = $"effectiveness_at_tier{(int)tech}";
                if (!c.TryGetValue(name, ref effectiveness[(int)tech]) || effectiveness[(int)tech] > 1 || effectiveness[(int)tech] < 0)
                {
                    allSet = false;
                    break;
                }
            }

            this.effectivenessAtTier = allSet ? effectiveness : null;
            this.madeFrom = madeFrom;
            this.madeFromStartsAt = madeFromStartsAt;

            float[] repGains = new float[1 + (int)TechTier.Tier4];
            allSet = true;
            for (TechTier tech = TechTier.Tier0; tech <= TechTier.Tier4; ++tech)
            {
                string name = $"reputation_per_unit_at_tier{(int)tech}";
                if (!c.TryGetValue(name, ref repGains[(int)tech]))
                {
                    allSet = false;
                    break;
                }
            }
            this.reputationPerUnitAtTier = allSet ? repGains : new float[1 + (int)TechTier.Tier4];
        }

        public static Dictionary<string, TieredResource> LoadAll(Dictionary<string, ResearchCategory> researchCategories)
        {
            ConfigNode[] resourceCategoryNodes = GameDatabase.Instance.GetConfigNodes("TIERED_RESOURCE_DEFINITION");
            Dictionary<string, TieredResource> result = new Dictionary<string, TieredResource>();
            while (result.Count != resourceCategoryNodes.Length)
            {
                bool madeProgress = false;
                for (int i = 0; i < resourceCategoryNodes.Length; ++i)
                {
                    ConfigNode c = resourceCategoryNodes[i];
                    if (c == null)
                    {
                        continue;
                    }

                    string researchCategory = c.GetValue("research_category");
                    if (researchCategory == null || !researchCategories.TryGetValue(researchCategory, out ResearchCategory category))
                    {
                        Debug.LogError($"TIERED_RESOURCE_DEFINITION.{c.GetValue("name")} misconfigured - missing or invalid research_category");
                        resourceCategoryNodes[i] = null;
                        madeProgress = true;
                        continue;
                    }

                    string madeFrom = c.GetValue("made_from");
                    string madeFromT2 = c.GetValue("made_from_at_tier2");
                    TieredResource tieredResource = null;
                    TieredResource madeFromResource;
                    if (!string.IsNullOrEmpty(madeFrom) && result.TryGetValue(madeFrom, out madeFromResource))
                    {
                        tieredResource = new TieredResource(c, category, madeFromResource, TechTier.Tier0);
                    }
                    else if (!string.IsNullOrEmpty(madeFromT2) && result.TryGetValue(madeFromT2, out madeFromResource))
                    {
                        tieredResource = new TieredResource(c, category, madeFromResource, TechTier.Tier2);
                    }
                    else if (string.IsNullOrEmpty(madeFrom) && string.IsNullOrEmpty(madeFromT2))
                    {
                        tieredResource = new TieredResource(c, category, null, TechTier.Tier0);
                    }

                    if (tieredResource != null)
                    {
                        result.Add(tieredResource.BaseName, tieredResource);
                        resourceCategoryNodes[i] = null;
                        madeProgress = true;
                    }
                }

                if (!madeProgress)
                {
                    throw new Exception($"Leftovers {string.Join("|", resourceCategoryNodes.Where(cn => cn != null).Select(cn => cn.GetValue("name")).ToArray())}");
                    //Debug.LogError($"TIERED_RESOURCE_DEFINITION misconfigured - either a made_from or made_from_tier2 attribute is pointing to a non-existant node or there's a circularity.");
                    //break;
                }
            }

            return result;
        }
        public ProductionRestriction ProductionRestriction => this.ResearchCategory.Type;

        public ResearchCategory ResearchCategory { get; }

        public bool LimitedTierOnEasyBodies { get; }

        public string BaseName { get; }

        public string DisplayName { get; }

        public string CrewSkill { get; }

        public double BaseRepPerUnit { get; }

        public TieredResource MadeFrom(TechTier tier)
            => tier >= this.madeFromStartsAt ? this.madeFrom : null;

        /// <summary>
        ///    Gets the name of the resource as it is in the game configuration
        /// </summary>
        /// <remarks>
        ///   The name is like "resource-Tier3" except if it's a Tier4, where it's just the resource name.  The exception
        ///   is Fertilizer, where, to avoid conflicting with the community resource kit we just do Fertilizer-Tier4.
        /// </remarks>
        public string TieredName(TechTier tier)
            => $"{this.BaseName}-{tier.ToString()}";

        public string CapacityUnits { get; }

        public bool CanBeStored { get; }

        public bool ExcessProductionCountsTowardsResearch { get; }

        public bool IsHarvestedLocally { get; }

        public bool IsEdible => effectivenessAtTier != null;

        public double GetPercentOfDietByTier(TechTier tier)
            => this.effectivenessAtTier[(int)tier];

        public float GetReputationGain(TechTier tier, float amount)
            => this.reputationPerUnitAtTier[(int)tier] * amount;
    }
}
