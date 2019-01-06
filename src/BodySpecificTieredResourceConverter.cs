using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public abstract class BodySpecificTieredResourceConverter
        : TieredResourceCoverter
    {

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Body")]
        public void ChangeBody()
        {
            var validBodies = ColonizationResearchScenario.Instance.UnlockedBodies.ToList();
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

            this.tier = (int)ColonizationResearchScenario.Instance.GetMaxUnlockedTier(this.Output, this.body);
        }
    }
}
