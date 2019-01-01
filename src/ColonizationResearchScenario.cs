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
        public string validProductionBodies = "";

		Dictionary<string, TechProgress> bodyToAgricultureTechTierMap;
		Dictionary<string, TechProgress> bodyToProductionTechTierMap;
		Dictionary<string, TechProgress> bodyToScanningTechTierMap;

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
			ContributeResearch(this.bodyToAgricultureTechTierMap, bodyName, timespent, tier => tier.KerbalSecondsToResearchNextAgricultureTier());
		}

		public void ContributeProductionResearch(string bodyName, double timespent)
		{
			ContributeResearch(this.bodyToProductionTechTierMap, bodyName, timespent, tier => tier.KerbalSecondsToResearchNextProductionTier());
		}

		public void ContributeScanningResearch(string bodyName, double timespent)
		{
			ContributeResearch(this.bodyToScanningTechTierMap, bodyName, timespent, tier => tier.KerbalSecondsToResearchNextScanningTier());
		}

		public static void ContributeResearch(Dictionary<string, TechProgress> progressMap, string bodyName, double timespent, Func<TechTier,double> getTargetAmount)
		{
			if (progressMap.TryGetValue(bodyName, out TechProgress progress))
			{
				progress.Progress += timespent;
			}
			else
			{
				progress = new TechProgress() { Tier = TechTier.Tier0, Progress = timespent };
				progressMap.Add(bodyName, progress);
			}

			if (progress.Progress > getTargetAmount(progress.Tier))
			{
				progress.Progress = 0;
				++progress.Tier;
			}
		}

		public string[] ValidBodiesForAgriculture =>
			string.IsNullOrEmpty(this.validProductionBodies) ? new string[0] : this.validProductionBodies.Split(new char[] { '|' });

        public double KerbalSecondsToGoUntilNextAgroponicsTier => AgroponicsMaxTier.KerbalSecondsToResearchNextAgroponicsTier() - this.accumulatedAgroponicResearchProgressToNextTier;

        public double KerbalSecondsToGoUntilNextAgricultureTier(string bodyName)
        {
            this.bodyToAgricultureTechTierMap.TryGetValue(bodyName, out TechProgress progress);
            return progress == null ? TechTier.Tier0.KerbalSecondsToResearchNextAgricultureTier()
                   : progress.Tier.KerbalSecondsToResearchNextAgricultureTier() - progress.Progress;
        }

        public double KerbalSecondsToGoUntilNextProductionTier(string bodyName)
        {
            this.bodyToProductionTechTierMap.TryGetValue(bodyName, out TechProgress progress);
            return progress == null ? TechTier.Tier0.KerbalSecondsToResearchNextProductionTier()
                   : progress.Tier.KerbalSecondsToResearchNextProductionTier() - progress.Progress;
        }

        public TechTier GetAgricultureMaxTier(string bodyName)
		{
			this.bodyToAgricultureTechTierMap.TryGetValue(bodyName, out TechProgress progress);
			return progress?.Tier ?? TechTier.Tier0;
		}

		public TechTier GetProductionMaxTier(string bodyName)
		{
			this.bodyToProductionTechTierMap.TryGetValue(bodyName, out TechProgress progress);
			return progress?.Tier ?? TechTier.Tier0;
		}

		public TechTier GetScanningMaxTier(string bodyName)
		{
			this.bodyToScanningTechTierMap.TryGetValue(bodyName, out TechProgress progress);
			return progress?.Tier ?? TechTier.Tier0;
		}

		public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
			this.bodyToAgricultureTechTierMap = new Dictionary<string, TechProgress>();
			node.TryGetValue("agriculture", ref this.bodyToAgricultureTechTierMap);
			this.bodyToProductionTechTierMap = new Dictionary<string, TechProgress>();
			node.TryGetValue("production", ref this.bodyToProductionTechTierMap);
			this.bodyToScanningTechTierMap = new Dictionary<string, TechProgress>();
			node.TryGetValue("scanning", ref this.bodyToScanningTechTierMap);
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
                this.validProductionBodies = validBodies.ToString();
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
        TechTier GetProductionMaxTier(string bodyName);
        void ContributeAgroponicResearch(double timespent);
		void ContributeAgricultureResearch(string bodyName, double timespent);
		void ContributeProductionResearch(string bodyName, double timespent);
		void ContributeScanningResearch(string bodyName, double timespent);
	}
}
