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
        public float accumulatedAgroponicResearchProgressToNextTier = 0f;
        [KSPField(isPersistant = true)]
        public int agroponicsMaxTier = 0;

        /// <summary>
        ///   A '|' separated list of worlds where the player can do agriculture.
        /// </summary>
        /// <remarks>
        ///   This is stored because it comes from the ProgressTracking scenario, which isn't
        ///   loaded in the editor, which is the place we principally need this data.
        /// </remarks>
        [KSPField(isPersistant = true)]
        public string validAgricultureBodies = "";

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

        public string[] ValidBodiesForAgriculture => this.validAgricultureBodies.Split(new char[] { '|' });

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
                this.validAgricultureBodies = validBodies.ToString();
            }

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
