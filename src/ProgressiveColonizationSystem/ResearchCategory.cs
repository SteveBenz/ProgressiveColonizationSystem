using System.Collections.Generic;

namespace ProgressiveColonizationSystem
{
    public class ResearchCategory
    {
        private readonly ResearchLimit[] researchLimits;
        private readonly double kerbalYearsToTier1, kerbalYearsToTier2, kerbalYearsToTier3, kerbalYearsToTier4;
        private readonly List<string> crewSkills = new List<string>();
        private readonly string breakThroughMessageTag;
        private readonly string breakThroughExplanationTag;

        /// <summary>
        ///   Test constructor
        /// </summary>
        public ResearchCategory(string name, ProductionRestriction productionRestriction)
        {
            this.Name = name;
            this.DisplayName = name;
            this.Type = productionRestriction;
            this.kerbalYearsToTier1 = 1;
            this.kerbalYearsToTier2 = 2;
            this.kerbalYearsToTier3 = 3;
            this.kerbalYearsToTier4 = 4;
        }

        public ResearchCategory(ConfigNode n)
        {
            this.Name = n.GetValue("name");
            this.DisplayName = n.GetValue("display_name");
            switch(n.GetValue("type")?.ToLowerInvariant())
            {
                case "space":
                    this.Type = ProductionRestriction.Space;
                    break;
                case "orbit":
                    this.Type = ProductionRestriction.OrbitOfBody;
                    break;
                default:
                    this.Type = ProductionRestriction.LandedOnBody;
                    break;
            }

            n.TryGetValue("kerbal_years_to_tier1", ref this.kerbalYearsToTier1);
            n.TryGetValue("kerbal_years_to_tier2", ref this.kerbalYearsToTier2);
            n.TryGetValue("kerbal_years_to_tier3", ref this.kerbalYearsToTier3);
            n.TryGetValue("kerbal_years_to_tier4", ref this.kerbalYearsToTier4);

            this.researchLimits = new ResearchLimit[1 + (int)TechTier.Tier4];
            for (TechTier tier = TechTier.Tier0; tier < TechTier.Tier4; ++tier)
            {
                this.researchLimits[(int)tier] = new ResearchLimit(n.GetValue($"tier{(int)tier}_limit"));
            }
            this.researchLimits[(int)TechTier.Tier4] = ResearchLimit.MaxTierLimit;

            this.breakThroughMessageTag = n.GetValue("breakthrough_messages");
            this.breakThroughExplanationTag = n.GetValue("breakthrough_message_boring");
        }

        public string Name { get; }

        public string DisplayName { get; }

        public ProductionRestriction Type { get; }

        public double KerbalYearsToNextTier(TechTier tier)
        {
            switch(tier)
            {
                case TechTier.Tier0: return this.kerbalYearsToTier1;
                case TechTier.Tier1: return this.kerbalYearsToTier2;
                case TechTier.Tier2: return this.kerbalYearsToTier3;
                case TechTier.Tier3: return this.kerbalYearsToTier4;
                case TechTier.Tier4: default: return double.MaxValue;
            }
        }

        public bool CanDoResearch(Vessel vessel, TechTier currentTier, out string reasonWhyNot)
        {
            return this.researchLimits[(int)currentTier].IsResearchAllowed(vessel, out reasonWhyNot);
        }

        public void AddCrewSkill(string crewSkill)
        {
            this.crewSkills.Add(crewSkill);
        }

        public string BreakthroughMessage(TechTier newTier)
            => CrewBlurbs.CreateMessage(this.breakThroughMessageTag, this.crewSkills, newTier);

        public string BoringBreakthroughMessage(TechTier newTier)
            => CrewBlurbs.CreateMessage(this.breakThroughExplanationTag, this.crewSkills, newTier);
    }
}
