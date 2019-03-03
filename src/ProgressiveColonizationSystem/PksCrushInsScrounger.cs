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
        protected override bool CanDoProduction(ModuleResourceConverter resourceConverter, out string reasonWhyNotMessage)
        {
            if (!base.CanDoProduction(resourceConverter, out reasonWhyNotMessage))
            {
                return false;
            }

            if (!ResourceLodeScenario.Instance.TryFindResourceLodeInRange(this.vessel, out _))
            {
                reasonWhyNotMessage = "Not at the spot";
                return false;
            }

            return true;
        }
    }
}
