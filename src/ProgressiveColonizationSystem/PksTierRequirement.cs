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
        private string researchCategory;
        private int tier;

        public override void OnSave(ConfigNode configNode)
        {
            // Theory: After the contract gets generated, these things all get specific values

            configNode.AddValue(nameof(researchCategory), this.researchCategory);
            configNode.AddValue(nameof(tier), this.tier);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            this.LoadFromConfig(configNode);
        }

        protected override string RequirementText()
        {
            return $"Must have reached tier-{this.tier} {this.researchCategory} on {this.targetBody.name}";
        }

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            bool valid = base.LoadFromConfig(configNode);

            // TODO: write a validator for researchCategory
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, nameof(researchCategory), x => researchCategory = x, this, null, Validation.NotNull);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, nameof(tier), x => tier = x, this, -1, x => Validation.BetweenInclusive(x, 0, 4));

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            TieredResource resource = ColonizationResearchScenario.Instance.AllResourcesTypes.FirstOrDefault(r => r.ResearchCategory.Name == this.researchCategory);
            if (resource == null)
            {
                Debug.LogError($"Misconfigured PksTierRequirement - unknown resource '{this.researchCategory}'");
                return false;
            }

            TechTier tier = ColonizationResearchScenario.Instance.GetMaxUnlockedTier(resource, this.targetBody.name);
            return (int)tier > this.tier;
        }
    }
}
