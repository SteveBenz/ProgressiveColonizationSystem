using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class CbnShiniesContainer
        : CbnTieredContainer
    {
        [KSPField(isPersistant = true)]
        public bool isFromKerbin = true;

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (this.isFromKerbin && HighLogic.LoadedSceneIsFlight && this.part.Resources.Any(r => r.amount == 0))
            {
                this.isFromKerbin = false;
            }
        }
    }
}
