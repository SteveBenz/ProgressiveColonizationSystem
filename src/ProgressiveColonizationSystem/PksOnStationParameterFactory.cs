using ContractConfigurator;
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
        private string body;
        private string researchCategory;
        private int tier;
        private string kerbal;

        public override bool Load(ConfigNode configNode)
        {
            if (!base.Load(configNode))
            {
                return false;
            }

            if (!configNode.TryGetValue(nameof(body), ref this.body))
            {
                Debug.LogError($"{nameof(PksOnStationParameter)} needs a '{nameof(body)}' for node: {configNode}");
                return false;
            }

            if (!configNode.TryGetValue(nameof(researchCategory), ref this.researchCategory))
            {
                Debug.LogError($"{nameof(PksOnStationParameter)} needs a '{nameof(researchCategory)}' for node: {configNode}");
                return false;
            }

            if (!configNode.TryGetValue(nameof(tier), ref this.tier))
            {
                Debug.LogError($"{nameof(PksOnStationParameter)} needs a '{nameof(tier)}' for node: {configNode}");
                return false;
            }

            if (!configNode.TryGetValue(nameof(kerbal), ref this.kerbal))
            {
                Debug.LogError($"{nameof(PksOnStationParameter)} needs a '{nameof(kerbal)}' for node: {configNode}");
                return false;
            }

            return true;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new PksOnStationParameter(this.body, this.researchCategory, this.tier, this.kerbal);
        }
    }

    internal class PksOnStationParameter
        : ContractParameter
    {
        private readonly string rescuedKerbalName;
        private readonly string body;
        private readonly string researchCategory;
        private readonly int tier;

        public PksOnStationParameter(string body, string researchCategory, int tier, string kerbalName)
        {
            this.body = body;
            this.researchCategory = researchCategory;
            this.tier = tier;
            this.rescuedKerbalName = kerbalName;
        }

        protected override void OnUpdate()
        {
            // Is this a vessel on the target world?
            var activeVessel = FlightGlobals.ActiveVessel;
            if ( activeVessel.orbit?.referenceBody.name != this.body
                 || !(activeVessel.situation == Vessel.Situations.LANDED
                 || activeVessel.situation == Vessel.Situations.SPLASHED))
            {
                return;
            }

            // Does it have our kerbal on board?
            if (!activeVessel.GetVesselCrew().Any(k => k.name == this.rescuedKerbalName))
            {
                return;
            }

            // Are we in a base with the required kit?
            var converters = activeVessel.FindPartModulesImplementing<PksTieredResourceConverter>();
            if (!converters.Any(m => (int)m.tier >= this.tier && m.Output.ResearchCategory.Name == this.researchCategory))
            {
                return;
            }

            base.SetComplete();
        }
    }
}
