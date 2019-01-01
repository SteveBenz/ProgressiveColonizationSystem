using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public abstract class BodySpecificTieredResourceConverter
        : TieredResourceCoverter
    {
        [KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Target Body", isPersistant = true, guiActiveEditor = true)]
        public string body = "<not set>";

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Body")]
        public void ChangeBody()
        {
            var validBodies = ColonizationResearchScenario.Instance.ValidBodiesForAgriculture.ToList();
            validBodies.Sort();

            if (string.IsNullOrEmpty(body) && validBodies.Count == 0)
            {
                // Shouldn't be possible without cheating...  Unless this is sandbox
                return;
            }

            if (string.IsNullOrEmpty(body))
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

        protected override bool CanDoProduction(out string reasonWhyNotMessage)
        {
            if (this.vessel.situation != Vessel.Situations.LANDED || this.body != this.vessel.mainBody.name)
            {
                reasonWhyNotMessage = $"Not landed on {this.body}";
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
