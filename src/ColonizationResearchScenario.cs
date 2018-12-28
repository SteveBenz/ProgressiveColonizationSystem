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

        [KSPField(isPersistant = true)]
        private float accumulatedAgroponicResearchProgressToNextTier = 0f;
        [KSPField(isPersistant = true)]
        private int agroponicsMaxTier = 0;

        Dictionary<string, TechProgress> bodyToAgricultureTechTierMap;

        public ColonizationResearchScenario()
        {
            Instance = this;
        }

        public TechTier AgroponicsMaxTier
        {
            get => (TechTier)this.agroponicsMaxTier;
            private set => this.agroponicsMaxTier = (int)value;
        }

        public double AgroponicsResearchProgress
            => this.accumulatedAgroponicResearchProgressToNextTier / AgroponicsMaxTier.KerbalSecondsToResearchNextAgroponicsTier();

        public void ContributeAgroponicResearch(double timespent)
        {
            this.accumulatedAgroponicResearchProgressToNextTier += (float)timespent;
            if (this.accumulatedAgroponicResearchProgressToNextTier > AgroponicsMaxTier.KerbalSecondsToResearchNextAgroponicsTier())
            {
                this.accumulatedAgroponicResearchProgressToNextTier = 0;
                ++this.AgroponicsMaxTier;
            }
        }
        public void ContributeAgricultureResearch(string bodyName, double timespent)
        {
            if (this.bodyToAgricultureTechTierMap.TryGetValue(bodyName, out TechProgress progress))
            {
                progress.Progress += timespent;
            }
            else
            {
                progress = new TechProgress() { Tier = TechTier.Tier0, Progress = timespent };
                this.bodyToAgricultureTechTierMap.Add(bodyName, progress);
            }

            if (progress.Progress > progress.Tier.KerbalSecondsToResearchNextAgricultureTier())
            {
                progress.Progress = 0;
                ++progress.Tier;
            }
        }

        public double KerbalSecondsToGoUntilNextAgroponicsTier => AgroponicsMaxTier.KerbalSecondsToResearchNextAgroponicsTier() - this.accumulatedAgroponicResearchProgressToNextTier;

        public TechTier GetAgricultureMaxTier(string bodyName)
        {
            this.bodyToAgricultureTechTierMap.TryGetValue(bodyName, out TechProgress progress);
            return progress?.Tier ?? TechTier.Tier0;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            this.bodyToAgricultureTechTierMap = new Dictionary<string, TechProgress>();
            node.TryGetValue("agriculture", ref this.bodyToAgricultureTechTierMap);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.SetValue("agriculture", this.bodyToAgricultureTechTierMap);
        }
    }

    // Test interface
    public interface IColonizationResearchScenario
    {
        TechTier AgroponicsMaxTier { get; }
        TechTier GetAgricultureMaxTier(string bodyName);
        void ContributeAgroponicResearch(double timespent);
        void ContributeAgricultureResearch(string bodyName, double timespent);
    }
}
