using System.Text;

namespace ProgressiveColonizationSystem
{
    public class PksTieredCombiner
        : PksTieredResourceConverter, ITieredCombiner
    {
        [KSPField]
        public string untieredOutput;

        [KSPField]
        public string untieredInput;

        private static readonly double[] combinationRates = { .4, .65, .8, .92, .98 };

        double ITieredCombiner.ProductionRate => this.capacity;

        string ITieredCombiner.NonTieredOutputResourceName => this.untieredOutput;

        TieredResource ITieredCombiner.TieredInput => base.Output;

        double ITieredCombiner.GetRatioForTier(TechTier tier)
            => combinationRates[(int)tier];

        string ITieredCombiner.NonTieredInputResourceName => this.untieredInput;


        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            if (this.Input != null)
            {
                info.AppendLine($"{GreenInfo("Input:")} {this.Input.BaseName}");
            }

            info.AppendLine($"{GreenInfo("Capacity:")} {this.capacity} {this.Output.CapacityUnits}");
            info.AppendLine($"{GreenInfo("Output:")} {this.untieredOutput}");

            info.AppendLine($"{GreenInfo("%Local:")}");
            foreach (TechTier tier in TechTierExtensions.AllTiers)
            {
                info.AppendLine($" {tier.ToString()}: {(int)(combinationRates[(int)tier] * 100)}%");
            }

            return info.ToString();
        }
    }
}
