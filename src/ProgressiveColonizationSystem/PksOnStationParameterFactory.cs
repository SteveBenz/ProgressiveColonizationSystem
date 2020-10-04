using ContractConfigurator;
using ContractConfigurator.Parameters;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///  Use this if it seems like a good idea to require that the kerbal make it all the way
    ///  to a base.  Otherwise use the ReachState configuration.
    /// </summary>
    public class PksOnStationParameterFactory
        : ParameterFactory
    {
        private CelestialBody body;
        private string researchCategory;
        private int tier;
        private ContractConfigurator.Kerbal kerbal;

        public override bool Load(ConfigNode configNode)
        {
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, nameof(body), x => body = x, this, null, Validation.NotNull);

            // TODO: write a validator for researchCategory
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, nameof(researchCategory), x => researchCategory = x, this, null, Validation.NotNull);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, nameof(tier), x => tier = x, this, -1, x => Validation.BetweenInclusive(x, 0, 4));
            valid &= ConfigNodeUtil.ParseValue<ContractConfigurator.Kerbal>(configNode, nameof(kerbal), x => kerbal = x, this, null, Validation.NotNull);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new PksOnStationParameter(this.body, this.researchCategory, this.tier, this.kerbal);
        }
    }

    internal class PksOnStationParameter
        : ContractConfiguratorParameter
    {
        private string rescuedKerbal;
        private string body;
        private string researchCategory;
        private int tier;

        public PksOnStationParameter(CelestialBody body, string researchCategory, int tier, ContractConfigurator.Kerbal kerbal)
        {
            this.body = body.name;
            this.researchCategory = researchCategory;
            this.tier = tier;
            this.rescuedKerbal = kerbal.name;
        }

        public PksOnStationParameter()
        {
        }

        protected override string GetParameterTitle()
        {
            return this.state == ParameterState.Complete
                ? $"{this.rescuedKerbal} is at home on {this.body}"
                : $"Bring {this.rescuedKerbal} to a station on {this.body}";
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue(nameof(body), body);
            node.AddValue(nameof(researchCategory), researchCategory);
            node.AddValue(nameof(tier), tier);
            node.AddValue(nameof(rescuedKerbal), rescuedKerbal);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            if (!node.TryGetValue(nameof(body), ref this.body))
            {
                Debug.LogError($"PksOnStationParameter.OnLoad didn't find a '{nameof(body)}' node");
            }
            if (!node.TryGetValue(nameof(researchCategory), ref this.researchCategory))
            {
                Debug.LogError($"PksOnStationParameter.OnLoad didn't find a '{nameof(researchCategory)}' node");
            }
            if (!node.TryGetValue(nameof(tier), ref this.tier))
            {
                Debug.LogError($"PksOnStationParameter.OnLoad didn't find a '{nameof(tier)}' node");
            }
            if (!node.TryGetValue(nameof(rescuedKerbal), ref this.rescuedKerbal))
            {
                Debug.LogError($"PksOnStationParameter.OnLoad didn't find a '{nameof(rescuedKerbal)}' node");
            }
        }

        protected override void OnUpdate()
        {
            // Are we on any kind of a vessel?
            var activeVessel = FlightGlobals.ActiveVessel;
            if (activeVessel == null || HighLogic.LoadedScene != GameScenes.FLIGHT)
            {
                return;
            }

            // Is this a vessel on the target world?
            if ( activeVessel.orbit?.referenceBody.name != this.body
                 || !(activeVessel.situation == Vessel.Situations.LANDED
                 || activeVessel.situation == Vessel.Situations.SPLASHED))
            {
                return;
            }

            // Does it have our kerbal on board?
            if (!activeVessel.GetVesselCrew().Any(k => k.name == this.rescuedKerbal))
            {
                return;
            }

            // Are we in a base with the required kit?
            var converters = activeVessel.FindPartModulesImplementing<PksTieredResourceConverter>();
            if (!converters.Any(m => (int)m.tier >= this.tier && m.Output.ResearchCategory.Name == this.researchCategory))
            {
                return;
            }

            base.SetState(ParameterState.Complete);
        }
    }
}
