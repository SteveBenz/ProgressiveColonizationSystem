using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class ModuleTieredAgroponics
        : PartModule, // <-  perhaps it should be ModuleResourceConverter?
          ISnackProducer
    {
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Snack Research", isPersistant = true)]
        [UI_Toggle( scene = UI_Scene.Flight)]
        public bool isSnackResearchActive;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Snack Production", isPersistant = true)]
        [UI_Toggle(scene = UI_Scene.Flight)]
        public bool isSnackProductionActive;

        [KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Tier", isPersistant = true, guiActiveEditor = true)]
        public int tier;

        [KSPField]
        public float capacity;

        public ModuleTieredAgroponics()
        {
            // Default to the max tier for new parts - for old parts, it will be overwritten on load.
            tier = (int)(ColonizationResearchScenario.Instance != null
                ? ColonizationResearchScenario.Instance.AgroponicsMaxTier
                : TechTier.Tier0);
        }

        public TechTier Tier => (TechTier)this.tier;

        double ISnackProducer.Capacity => this.capacity;

        bool ISnackProducer.IsResearchEnabled => this.isSnackResearchActive;

        bool ISnackProducer.IsProductionEnabled => this.isSnackProductionActive;

        double ISnackProducer.MaxConsumptionForProducedFood => this.Tier.AgroponicMaxDietRatio();

        bool ISnackProducer.ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            if (target.AgroponicsMaxTier == this.Tier && this.isSnackResearchActive)
            {
                target.ContributeAgroponicResearch(amount);
                return target.AgroponicsMaxTier != this.Tier;
            }
            else
            {
                return false;
            }
        }
    }
}
