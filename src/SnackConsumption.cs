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

        // Assumes that the time things come back as fractions of a second.
        const float SupplyConsumptionPerDayPerKerbal = 1f / (6f * 60f * 60f);

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

            // Compute this early, since it sets the last update time member
            double deltaTime = GetDeltaTime();
            if (deltaTime < 0)
            {
                // This is the first update - don't eat anything.
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

            if (this.IsAtHome)
            {
                // While actually on Kerbal, the Kerbals will order take-out rather than consuming
                // what's in the ship.
                foreach (var crewman in crew)
                {
                    LifeSupportScenario.Instance?.KerbalHasReachedHomeworld(crewman);
                }
            }
            else
            {
                double timeWithFood = this.CalculateSnackflow(crew, deltaTime);
                // TODO: result.TimeFactor should be used as the value for 'lastMeal', which should be passed in.
                //  Then you can call missed-a-meal.

                bool gotAMeal = timeWithFood >= deltaTime - ResourceUtilities.FLOAT_TOLERANCE;
                foreach (var crewman in crew)
                {
                    if (gotAMeal)
                    {
                        LifeSupportScenario.Instance?.KerbalHadASnack(crewman);
                    }
                    else
                    {
                        LifeSupportScenario.Instance?.KerbalMissedAMeal(crewman);
                    }
                }
            }
        }

        /// <summary>
        ///   Calculates snacks consumption aboard the vessel.
        /// </summary>
        /// <param name="crew">The crew</param>
        /// <param name="deltaTime">The amount of time (in seconds) since the last calculation was done</param>
        /// <returns>The amount of <paramref name="deltaTime"/> in which food was supplied.</returns>
        private double CalculateSnackflow(List<ProtoCrewMember> crew, double deltaTime)
        {
            ConversionRecipe conversionRecipe = new ConversionRecipe();
            conversionRecipe.Inputs.Add(new ResourceRatio()
            {
                FlowMode = ResourceFlowMode.ALL_VESSEL,
                Ratio = SupplyConsumptionPerDayPerKerbal * crew.Count,
                ResourceName = "Snacks",
                DumpExcess = true
            });
            conversionRecipe.Outputs.Add(new ResourceRatio()
            {
                FlowMode = ResourceFlowMode.ALL_VESSEL,
                Ratio = SupplyConsumptionPerDayPerKerbal * crew.Count,
                ResourceName = "Stinkies",
                DumpExcess = true
            });
            var crewPart = vessel.parts.FirstOrDefault(p => p.CrewCapacity > 0);
            ConverterResults result = this.ResConverter.ProcessRecipe(deltaTime, conversionRecipe, crewPart, null, 1f);

            return result.TimeFactor;
        }

        private bool IsAtHome => vessel.mainBody == FlightGlobals.GetHomeBody() && vessel.altitude < 10000;

        private double GetDeltaTime()
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
