namespace ProgressiveColonizationSystem
{
    public class PksTieredCombiner
        : PartModule, ITieredCombiner
    {
        [KSPField]
        public string output;

        [KSPField]
        public string tieredInput;

        [KSPField]
        public string untieredInput;

        [KSPField]
        public float capacity;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Research")]
        public string researchStatus;

        private static readonly double[] combinationRates = { .4, .65, .8, .92, .98 };

        private TieredResource tieredInputAsTieredResource = null;

        double ITieredCombiner.ProductionRate => this.capacity;

        string ITieredCombiner.NonTieredOutputResourceName => this.output;

        TieredResource ITieredCombiner.TieredInput
        {
            get
            {
                if (this.tieredInputAsTieredResource == null && !string.IsNullOrEmpty(this.tieredInput))
                {
                    this.tieredInputAsTieredResource = ColonizationResearchScenario.GetTieredResourceByName(this.tieredInput);
                }
                return this.tieredInputAsTieredResource;
            }
        }

        double ITieredCombiner.GetRatioForTier(TechTier tier)
            => combinationRates[(int)tier];

        string ITieredCombiner.NonTieredInputResourceName => this.untieredInput;
    }
}
