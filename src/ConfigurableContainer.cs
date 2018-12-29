using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{

    public class ModuleTieredContainer
        : PartModule
    {
        public ModuleTieredContainer()
        {
            Debug.Log("In ModuleTieredContainer constructor.");
        }

        [KSPEvent(active = true, guiActiveEditor = true, guiName = "Change Tier", unfocusedRange = 10f)]
        public void NextTier()
        {
            Debug.Assert(this.part.Resources.Count == 1, $"{nameof(ModuleTieredContainer)} is only expecting a single resource type");
            string inputResourceName = this.part.Resources[0].resourceName;
            string newResourceName = null;
            foreach (TechTier techTier in TechTierExtensions.AllTiers)
            {
                TechTier nextTier = (TechTier)((1+(int)techTier) % (1 + (int)TechTier.Tier4));
                if (inputResourceName == techTier.SnacksResourceName())
                {
                    newResourceName = nextTier.SnacksResourceName();
                }
                else if (inputResourceName == techTier.FertilizerResourceName())
                {
                    newResourceName = nextTier.FertilizerResourceName();
                }
            }

            if (newResourceName == null)
            {
                Debug.LogError($"{this.part.name} is not configured correctly - it can only work for containers with tiered resources");
                return;
            }

            var x = this.part.Resources[0];
            var z = this.part.Resources.Remove(x);
            if (part.Events != null) part.SendEvent("resource_changed");
            // var y = this.part.Resources.Add(newResourceName, 0, x.maxAmount, x.flowState, x.isTweakable, x.hideFlow, x.isVisible, x.flowMode);

            var node = new ConfigNode("RESOURCE");
            node.AddValue("name", newResourceName);
            node.AddValue("amount", 0);
            node.AddValue("maxAmount", x.maxAmount);
            var current_resource = part.Resources.Add(node);
            if (part.Events != null) part.SendEvent("resource_changed");
            //this.part.SetupResources();
        }
    }
}
