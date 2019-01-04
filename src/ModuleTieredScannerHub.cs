
namespace Nerm.Colonization
{
    /// <summary>
    ///   This part doesn't have any direct role to play - it just marks out the vessel as being capable of scanning
    /// </summary>
	public class ModuleTieredScannerHub
		: BodySpecificTieredResourceConverter
    {
        public const string ScannerDataMetaResourceBaseName = "ScannerData";

        public ModuleTieredScannerHub()
        {
            this.output = ScannerDataMetaResourceBaseName;
        }

        public override bool CanStockpileProduce => true;

        public override string SourceResourceName => null;

        protected override TechTier MaxTechTierResearched
            => ColonizationResearchScenario.Instance == null ? TechTier.Tier0 : ColonizationResearchScenario.Instance.GetScanningMaxTier(this.body);

        protected override string RequiredCrewTrait => "Pilot";

        public override bool ContributeResearch(IColonizationResearchScenario target, double amount)
            => target.ContributeScanningResearch(this.body, amount);

        protected override bool CanDoProduction(out string reasonWhyNotMessage)
        {
            if (this.vessel.situation != Vessel.Situations.ORBITING || this.body != this.vessel.mainBody.name)
            {
                reasonWhyNotMessage = $"Not orbiting {this.body}";
                return false;
            }
            else
            {
                reasonWhyNotMessage = null;
                return true;
            }
        }
    }
}
