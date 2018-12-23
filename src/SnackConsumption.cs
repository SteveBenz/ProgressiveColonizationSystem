using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{
    public class SnackConsumption
        : VesselModule
    {
        [KSPField(isPersistant = true)]
        public double LastUpdateTime;

        protected IResourceBroker _resBroker;
        public IResourceBroker ResBroker
        {
            get { return _resBroker ?? (_resBroker = new ResourceBroker()); }
        }

        protected ResourceConverter _resConverter;
        public ResourceConverter ResConverter
        {
            get { return _resConverter ?? (_resConverter = new ResourceConverter()); }
        }

        /// <summary>
        ///   This is called on each physics frame for the active vessel by reflection-magic from KSP.
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel == null || !vessel.loaded)
            {
                return;
            }

            if (vessel.isEVA)
            {
                // TODO: What to do here?  Should Kerbals on EVA ever go hungry?
                return;
            }

            List<ProtoCrewMember> crew = vessel.GetVesselCrew();
            if (crew.Count == 0)
            {
                // Nobody on board
                return;
            }

            double deltaTime = GetDeltaTime();
            ConversionRecipe conversionRecipe = new ConversionRecipe();
            conversionRecipe.Inputs.Add(new ResourceRatio()
            {
                FlowMode = ResourceFlowMode.ALL_VESSEL,
                Ratio = 1.0 * crew.Count,
                ResourceName = "Snacks",
                DumpExcess = true
            });
            conversionRecipe.Outputs.Add(new ResourceRatio()
            {
                FlowMode = ResourceFlowMode.ALL_VESSEL,
                Ratio = 1.0,
                ResourceName = "Stinkies",
                DumpExcess = true
            });
            var crewPart = vessel.parts.FirstOrDefault(p => p.CrewCapacity > 0);
            ConverterResults result = this.ResConverter.ProcessRecipe(deltaTime, conversionRecipe, crewPart, null, 1f);
            Debug.Log($"deltaTime={deltaTime} result.TimeFactor={result.TimeFactor} result.Status='{result.Status}'");
        }

        protected double GetDeltaTime()
        {
            if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
            {
                return -1;
            }

            if (Math.Abs(LastUpdateTime) < ResourceUtilities.FLOAT_TOLERANCE)
            {
                // Just started running
                LastUpdateTime = Planetarium.GetUniversalTime();
                return -1;
            }

            double maxDeltaTime = ResourceUtilities.GetMaxDeltaTime();
            double deltaTime = Math.Min(Planetarium.GetUniversalTime() - LastUpdateTime, maxDeltaTime);

            LastUpdateTime += deltaTime;
            return deltaTime;
        }

    }
}
