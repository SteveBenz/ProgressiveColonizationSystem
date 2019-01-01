using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
	public class ModuleTieredFactory
		: TieredResourceCoverter
	{
		public override bool CanStockpileProduce => true;

		protected override TechTier MaxTechTierResearched
			=> ColonizationResearchScenario.Instance.GetProductionMaxTier(this.body);

		protected override string RequiredCrewTrait => "Engineer";

        /// <summary>
        ///   The name of the input resource (as a Tier4 resource)
        /// </summary>
        [KSPField]
        public string input;

		public override bool ContributeResearch(IColonizationResearchScenario target, double amount)
		{
			if (this.IsResearchEnabled)
			{
				target.ContributeProductionResearch(this.body, amount);
				return target.GetProductionMaxTier(this.body) != this.Tier;
			}
			else
			{
				return false;
			}
		}

        public override string SourceResourceName => this.input == "" ? null : this.input;

        #region TODO: Copy/pasted -- Maybe a Body-specific TieredResourceCoverter needs to happen
        [KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Target Body", isPersistant = true, guiActiveEditor = true)]
		public string body = "<not set>";

		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Body")]
		public void ChangeBody()
		{
			var validBodies = ColonizationResearchScenario.Instance.ValidBodiesForAgriculture.ToList();
			validBodies.Sort();

			if (body == null && validBodies.Count == 0)
			{
				// Shouldn't be possible without cheating...  Unless this is sandbox
				return;
			}

			if (body == null)
			{
				body = validBodies[0];
			}
			else
			{
				int i = validBodies.IndexOf(this.body);
				i = (i + 1) % validBodies.Count;
				body = validBodies[i];
			}

			this.tier = (int)this.MaxTechTierResearched;
		}
		#endregion
	}
}
