using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Much code taken from:  https://github.com/Fengist/MPFuelSwitch/blob/master/MPFuelSwitch/MPFuelSwitch.cs

namespace ProgressiveColonizationSystem
{
    public class CbnTieredContainer
        : PartModule, IPartCostModifier, ITieredContainer
    {
        /// <summary>
        ///   This is the resource name for Tier4
        /// </summary>
        [KSPField(isPersistant = true)]
        public string resource;

        [KSPField]
        public float maxAmount;

        [KSPField(isPersistant = true)]
        public int tier = (int)TechTier.Tier4;

        private UIPartActionWindow tweakableUI = null;

        public TechTier Tier => (TechTier)this.tier;

        TieredResource ITieredContainer.Content => ColonizationResearchScenario.Instance.TryGetTieredResourceByName(this.resource);

        double ITieredContainer.Amount
        {
            get
            {
                return this.part.Resources.Count == 0 ? 0.0 : this.part.Resources[0].amount;
            }
            set
            {
                this.part.Resources[0].amount = Math.Min(this.part.Resources[0].amount, this.part.Resources[0].amount);
            }
        }

        double ITieredContainer.MaxAmount => this.maxAmount;

        [KSPEvent(active = true, guiActiveEditor = true, guiActive = true, externalToEVAOnly = true, guiName = "Change Tier", unfocusedRange = 10f)]
        public void NextTier()
        {
            tier = ((1 + this.tier) % (1 + (int)TechTier.Tier4));
            assignResourcesToPart();
        }

        public override string GetModuleDisplayName()
        {
            // This is factored into the detail panel for the part - it's the headline right above the GetInfo data
            return $"{char.ToUpper(resource[0])}{resource.Substring(1)}";
        }

        public override string GetInfo()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Amount: {this.maxAmount}");

            string tier4name = $"{this.resource}-{TechTier.Tier4}";

            var resourceModel = PartResourceLibrary.Instance.resourceDefinitions.OfType<PartResourceDefinition>().FirstOrDefault(prd => prd.name == tier4name);
            if (resourceModel != null)
            {
                builder.AppendLine($"Mass: {this.maxAmount * resourceModel.density}");
            }
            else
            {
                Debug.LogError($"CbnTieredContainer: Resource definition for {this.resource} is missing");
            }
            return builder.ToString();
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
            newResourceNode.AddValue("name", tieredResource.TieredName(this.Tier));
            newResourceNode.AddValue("maxAmount", this.maxAmount);
            newResourceNode.AddValue("amount", HighLogic.LoadedSceneIsEditor ? (oldAmount < 0 ? this.maxAmount : (float)oldAmount) : 0.0f);

            currentPart.AddResource(newResourceNode);
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            double total = 0;
            foreach (var resource in this.part.Resources)
            {
                var resourceModel = PartResourceLibrary.Instance.resourceDefinitions.OfType<PartResourceDefinition>().FirstOrDefault(prd => prd.name == resource.resourceName);
                if (resourceModel != null)
                {
                    total += resource.maxAmount * resourceModel.unitCost;
                }
                else
                {
                    Debug.LogError($"CbnTieredContainer: Resource definition for {resource.resourceName} is missing");
                }
            }
            return (float)total;
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }
    }
}
