using ContractConfigurator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    public class PksTierRequirement : ContractRequirement
    {
        private string body;
        private string researchCategory;
        private int tier;

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue(nameof(body), this.body);
            configNode.AddValue(nameof(researchCategory), this.researchCategory);
            configNode.AddValue(nameof(tier), this.tier);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            this.LoadFromConfig(configNode);
        }

        protected override string RequirementText()
        {
            return $"Must have reached tier-{this.tier} {this.researchCategory} on {this.body}";
        }

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            if (!base.LoadFromConfig(configNode))
            {
                return false;
            }

            if (!configNode.TryGetValue("body", ref this.body))
            {
                Debug.LogError($"{nameof(PksTierRequirement)} needs a '{nameof(body)}' for node: {configNode}");
                return false;
            }

            if (!configNode.TryGetValue("researchCategory", ref this.researchCategory))
            {
                Debug.LogError($"{nameof(PksTierRequirement)} needs a '{nameof(researchCategory)}' for node: {configNode}");
                return false;
            }

            if (!configNode.TryGetValue("tier", ref this.tier))
            {
                Debug.LogError($"{nameof(PksTierRequirement)} needs a '{nameof(tier)}' for node: {configNode}");
                return false;
            }

            return true;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            TieredResource resource = ColonizationResearchScenario.Instance.AllResourcesTypes.FirstOrDefault(r => r.ResearchCategory.Name == this.researchCategory);
            if (resource == null)
            {
                Debug.LogError($"Misconfigured PksTierRequirement - unknown resource '{this.researchCategory}'");
                return false;
            }                               

            TechTier tier = ColonizationResearchScenario.Instance.GetMaxUnlockedTier(resource, this.body);
            return (int)tier > this.tier;
        }
    }
}
