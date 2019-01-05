
namespace Nerm.Colonization
{
    /// <summary>
    ///   This part doesn't have any direct role to play - it just marks out the vessel as being capable of scanning
    /// </summary>
	public class ModuleTieredScannerHub
		: BodySpecificTieredResourceConverter
    {
        protected override TechTier MaxTechTierResearched
            => ColonizationResearchScenario.Instance == null ? TechTier.Tier0 : ColonizationResearchScenario.Instance.GetScanningMaxTier(this.body);

        public override bool ContributeResearch(IColonizationResearchScenario target, double amount)
            => target.ContributeScanningResearch(this.body, amount);

        protected override bool CanDoProduction(ModuleResourceConverter resourceConverter, out string reasonWhyNotMessage)
        {
            if (!base.CanDoProduction(resourceConverter, out reasonWhyNotMessage))
            {
                return false;
            }
            else if (this.vessel.situation != Vessel.Situations.ORBITING || this.body != this.vessel.mainBody.name)
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
