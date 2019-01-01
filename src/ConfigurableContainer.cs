using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Much code taken from:  https://github.com/Fengist/MPFuelSwitch/blob/master/MPFuelSwitch/MPFuelSwitch.cs

namespace Nerm.Colonization
{
    public class ModuleTieredContainer
        : PartModule
    {
        /// <summary>
        ///   This is the resource name for Tier4
        /// </summary>
        [KSPField]
        public string resource;

        [KSPField]
        public float maxAmount;

        [KSPField]
        public TechTier tier = TechTier.Tier4;

        private UIPartActionWindow tweakableUI = null;

        [KSPEvent(active = true, guiActiveEditor = true, externalToEVAOnly = true, guiName = "Change Tier", unfocusedRange = 10f)]
        public void NextTier()
        {
            tier = (TechTier)((1 + (int)this.tier) % (1 + (int)TechTier.Tier4));
            setupTankInPart(this.part, true);

            SetDisplayDirty();
        }

        private void SetDisplayDirty()
        {
            if (this.tweakableUI == null)
            {
                this.tweakableUI = this.part.FindActionWindow();
            }

            if (this.tweakableUI != null)
            {
                this.tweakableUI.displayDirty = true;
            }
            else
            {
                Debug.LogWarning("no UI to refresh");
            }
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            setupTankInPart(this.part, false);
        }

        private void setupTankInPart(Part currentPart, bool calledByPlayer)
        {
            currentPart.Resources.dict = new DictionaryValueList<int, PartResource>();

            ConfigNode newResourceNode = new ConfigNode("RESOURCE");
            newResourceNode.AddValue("name", this.tier.GetTieredResourceName(this.resource));
            newResourceNode.AddValue("maxAmount", this.maxAmount);

            if (calledByPlayer && !HighLogic.LoadedSceneIsEditor)
            {
                newResourceNode.AddValue("amount", 0.0f);
            }
            else
            {
                newResourceNode.AddValue("amount", this.maxAmount);
            }

            currentPart.AddResource(newResourceNode);
        }
    }
}
