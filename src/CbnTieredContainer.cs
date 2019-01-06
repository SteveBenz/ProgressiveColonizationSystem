using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Much code taken from:  https://github.com/Fengist/MPFuelSwitch/blob/master/MPFuelSwitch/MPFuelSwitch.cs

namespace Nerm.Colonization
{
    public class CbnTieredContainer
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

        [KSPEvent(active = true, guiActiveEditor = true, guiActive = true, externalToEVAOnly = true, guiName = "Change Tier", unfocusedRange = 10f)]
        public void NextTier()
        {
            tier = (TechTier)((1 + (int)this.tier) % (1 + (int)TechTier.Tier4));
            assignResourcesToPart();
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
            if (this.part.Resources.Count == 0)
            {
                assignResourcesToPart();
            }
        }

        private void assignResourcesToPart()
        {
            // destroying a resource messes up the gui in editor, but not in flight.
            setupTankInPart(part);
            if (HighLogic.LoadedSceneIsEditor)
            {
                for (int s = 0; s < part.symmetryCounterparts.Count; s++)
                {
                    setupTankInPart(part.symmetryCounterparts[s]);
                }
            }
            SetDisplayDirty();
        }

        private void setupTankInPart(Part currentPart)
        {
            double oldAmount = (currentPart.Resources.Count > 0) ? currentPart.Resources[0].amount : -1;
            currentPart.Resources.dict = new DictionaryValueList<int, PartResource>();

            TieredResource tieredResource = ColonizationResearchScenario.Instance.TryGetTieredResourceByName(this.resource);
            Debug.Assert(tieredResource != null, "Tank is not configured correctly - resource is not a tiered resource");

            ConfigNode newResourceNode = new ConfigNode("RESOURCE");
            newResourceNode.AddValue("name", tieredResource.TieredName(this.tier));
            newResourceNode.AddValue("maxAmount", this.maxAmount);
            newResourceNode.AddValue("amount", HighLogic.LoadedSceneIsEditor ? (oldAmount < 0 ? this.maxAmount : (float)oldAmount) : 0.0f);

            currentPart.AddResource(newResourceNode);
        }
    }
}
