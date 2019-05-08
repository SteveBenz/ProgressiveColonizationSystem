using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FinePrint;
using UnityEngine;

namespace ProgressiveColonizationSystem
{

    public class PksCrushInsScrounger
        : PksTieredResourceConverter
    {
        private double timeAtFirstDisableAttempt = 0;

        protected override bool CanDoProduction(ModuleResourceConverter resourceConverter, out string reasonWhyNotMessage)
        {
            if (!base.CanDoProduction(resourceConverter, out reasonWhyNotMessage))
            {
                return false;
            }

            if (!ResourceLodeScenario.Instance.TryFindResourceLodeInRange(this.vessel, this.Tier, out _))
            {
                if (this.isEnabled)
                {
                    // Shenanigans!  When the scene is first loaded in, the waypoint distance calculation is
                    // broken and reports us as a couple kilometers away from the waypoint.  After 3 or 4
                    // physics frames, that seems to square itself.  So this thing basically just holds its
                    // water for 3 seconds.  I suppose this could get into trouble if you had some kind of
                    // mod that allows you to warp into a zone with time-warp on.  We could fix that,  but
                    // folks who run mods like that probably like a little excitement in their lives, so
                    // let's leave it in this way.
                    double now = Planetarium.GetUniversalTime();
                    if (timeAtFirstDisableAttempt == 0)
                    {
                        timeAtFirstDisableAttempt = now;
                    }
                    else if (now > timeAtFirstDisableAttempt + 3.0)
                    {
                        ScreenMessages.PostScreenMessage("There are no crushins to be found here!  Go to your oribiting scanner and find a resource lode.", duration: 20.0f);
                        var converter = this.part.FindModuleImplementing<BaseConverter>();
                        converter?.StopResourceConverter();
                        var animation = this.part.FindModuleImplementing<ModuleAnimationGroup>();
                        animation?.RetractModule();
                    }
                }

                reasonWhyNotMessage = "No loose crushins nearby";
                return false;
            }

            timeAtFirstDisableAttempt = 0;
            return true;
        }
    }
}
