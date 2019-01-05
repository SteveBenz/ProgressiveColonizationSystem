using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.LOADING, GameScenes.LOADINGBUFFER)]
    public class ColonizationResearchScenario
        : ScenarioModule, IColonizationResearchScenario
    {
        public static ColonizationResearchScenario Instance;

        [KSPField(isPersistant = true)]
        public float accumulatedAgroponicResearchProgressToNextTier = 0f;
        [KSPField(isPersistant = true)]
        public int agroponicsMaxTier = 0;


        static TieredResource[] AllTieredResources =
        {
            new EdibleResource("HydroponicSnacks", false, false, .2, .4, .55, .7, .95),
            new EdibleResource("Snacks", true, false, .6, .85, .95, .98, 1.0),
            new EdibleResource("Snacks", true, false, .6, .85, .95, .98, 1.0),
            new TieredResource("Fertilizer", "Kerbal-Days", true, false),
            new TieredResource("Shinies", "Bling-per-day", true, false),
            new TieredResource("Stuff", null, true, false),
            new TieredResource("ScanningData", null, false, true)
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
        ///   A '|' separated list of worlds where the player can do agriculture.
        /// </summary>
        /// <remarks>
        ///   This is stored because it comes from the ProgressTracking scenario, which isn't
        ///   loaded in the editor, which is the place we principally need this data.
        /// </remarks>
        [KSPField(isPersistant = true)]
        public string validProductionBodies = "";

		private Dictionary<string, TechProgress> bodyToAgricultureTechTierMap;
        private Dictionary<string, TechProgress> bodyToProductionTechTierMap;
        private Dictionary<string, TechProgress> bodyToScanningTechTierMap;

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

        public bool ContributeAgroponicResearch(double timespent)
        {
            this.accumulatedAgroponicResearchProgressToNextTier += (float)timespent;
            if (this.accumulatedAgroponicResearchProgressToNextTier > AgroponicsMaxTier.KerbalSecondsToResearchNextAgroponicsTier())
            {
                this.accumulatedAgroponicResearchProgressToNextTier = 0;
                ++this.AgroponicsMaxTier;
                return true;
            }
            else
            {
                return false;
            }
        }

		public bool ContributeAgricultureResearch(string bodyName, double timespent)
			=> ContributeResearch(this.bodyToAgricultureTechTierMap, bodyName, timespent, tier => tier.KerbalSecondsToResearchNextAgricultureTier());

		public bool ContributeProductionResearch(string bodyName, double timespent)
			=> ContributeResearch(this.bodyToProductionTechTierMap, bodyName, timespent, tier => tier.KerbalSecondsToResearchNextProductionTier());

		public bool ContributeScanningResearch(string bodyName, double timespent)
			=> ContributeResearch(this.bodyToScanningTechTierMap, bodyName, timespent, tier => tier.KerbalSecondsToResearchNextScanningTier());

		public static bool ContributeResearch(Dictionary<string, TechProgress> progressMap, string bodyName, double timespent, Func<TechTier,double> getTargetAmount)
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
                return true;
			}
            else
            {
                return false;
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

        public double KerbalSecondsToGoUntilNextScanningTier(string bodyName)
        {
            this.bodyToScanningTechTierMap.TryGetValue(bodyName, out TechProgress progress);
            return progress == null ? TechTier.Tier0.KerbalSecondsToResearchNextScanningTier()
                   : progress.Tier.KerbalSecondsToResearchNextScanningTier() - progress.Progress;
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
            node.SetValue("production", this.bodyToProductionTechTierMap);
            node.SetValue("scanning", this.bodyToScanningTechTierMap);
        }
    }

    // Test interface
    public interface IColonizationResearchScenario
    {
        TechTier AgroponicsMaxTier { get; }
        TechTier GetAgricultureMaxTier(string bodyName);
        TechTier GetProductionMaxTier(string bodyName);
        bool ContributeAgroponicResearch(double timespent);
        bool ContributeAgricultureResearch(string bodyName, double timespent);
        bool ContributeProductionResearch(string bodyName, double timespent);
        bool ContributeScanningResearch(string bodyName, double timespent);

        bool TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier);
    }
}
