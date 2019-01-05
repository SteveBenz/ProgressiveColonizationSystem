using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{
	public abstract class TieredResourceCoverter
		: PartModule, IProducer
	{
        private double firstNoPowerIndicator = -1.0;

		[KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Tier", isPersistant = true, guiActiveEditor = true)]
		public int tier;

		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Tier")]
		public void ChangeTier()
		{
			this.tier = (this.tier + 1) % ((int)this.MaxTechTierResearched + 1);
		}

        /// <summary>
        ///   The name of the output resource (as a Tier4 resource)
        /// </summary>
        [KSPField]
        public string output;

        /// <summary>
        ///   The name of the input resource (as a Tier4 resource)
        /// </summary>
        [KSPField]
        public string input;

        [KSPField]
		public float capacity;

		[KSPField(guiActive = true, guiActiveEditor = false, guiName = "Research")]
		public string researchStatus;

		protected abstract TechTier MaxTechTierResearched { get; }

		protected virtual bool CanDoProduction(ModuleResourceConverter resourceConverter, out string reasonWhyNotMessage)
		{
			if (!resourceConverter.isActiveAndEnabled)
			{
				reasonWhyNotMessage = "Disabled - module is off";
				return false;
			}

			if (!this.IsPowered(resourceConverter))
			{
				reasonWhyNotMessage = "Disabled - module lacks power";
				return false;
			}

			if (!IsCrewed())
			{
				reasonWhyNotMessage = "Disabled - no qualified crew";
			}

			reasonWhyNotMessage = null;
			return true;
		}

		protected virtual bool CanDoResearch(out string reasonWhyNotMessage)
		{
			if (this.tier < (int)this.MaxTechTierResearched)
			{
				reasonWhyNotMessage = $"Disabled - Not cutting edge gear";
				return false;
			}

			reasonWhyNotMessage = null;
			return true;
		}

        public void FixedUpdate()
        {
			if (!HighLogic.LoadedSceneIsFlight)
			{
				return;
			}

            ModuleResourceConverter resourceConverter = this.GetComponent<ModuleResourceConverter>();

            if (this.CanDoProduction(resourceConverter, out string reasonWhyNotMessage))
			{
				this.IsProductionEnabled = true;

				if (this.CanDoResearch(out reasonWhyNotMessage))
				{
					this.IsResearchEnabled = true;
					this.researchStatus = "Active";
				}
				else
				{
					this.IsResearchEnabled = false;
					this.researchStatus = reasonWhyNotMessage;
				}
			}
			else
			{
				//if (resourceConverter != null && resourceConverter.IsActivated)
				//{
					//ScreenMessages.PostScreenMessage($"{this.name} is shutting down:  {reasonWhyNotMessage}", 10.0f);
                    //resourceConverter.StopResourceConverter();
				//}
				this.IsProductionEnabled = false;
				this.IsResearchEnabled = false;
				this.researchStatus = reasonWhyNotMessage;
			}
		}

		protected virtual bool IsCrewed()
		{
			if (this.vessel == null)
			{
				return false;
			}

			if (this.vessel.GetCrewCount() == 0)
			{
				return false;
			}

            var crewRequirement = this.GetComponent<CbnCrewRequirement>();
            return crewRequirement == null || crewRequirement.TryAssignCrew();
		}

        /// <summary>
        ///   Returns true if the part has electrical power
        /// </summary>
		private bool IsPowered(ModuleResourceConverter resourceConverter)
        {
            if (resourceConverter == null)
            {
                // This module doesn't have a power requirement
                return true;
            }

            // I don't see a good way to determine if a converter is running stably.
            //  lastTimeFactor seems to be the amount of the last recipe that it was able
            //  to successfully convert, which ought to be it, but lastTimeFactor is zero
            //  for several iterations after unpacking the vessel.  This code attempts to
            //  compensate for that by waiting at least 10 seconds before declaring itself
            //  unpowered.
            if (resourceConverter.lastTimeFactor == 0)
            {
                if (this.firstNoPowerIndicator < 0)
                {
                    this.firstNoPowerIndicator = Planetarium.GetUniversalTime();
                    return true;
                }
                else
                {
                    return Planetarium.GetUniversalTime() - this.firstNoPowerIndicator < 10.0;
                }
            }
            else
            {
                this.firstNoPowerIndicator = -1;
                return true;
            }
        }

		public TechTier Tier => (TechTier)this.tier;

		public bool IsResearchEnabled { get; private set; }

		public bool IsProductionEnabled { get; private set; }

		public double ProductionRate => this.capacity;

		/// <summary>
		///   The maximum-tier version of the product that this producer can produce.  The actual
		///   resource name would need to have <see cref="Tier"/> mixed into the name.
		/// </summary>
		public string ProductResourceName => this.output;

        public string SourceResourceName => this.input;

        public abstract bool ContributeResearch(IColonizationResearchScenario target, double amount);

        public static string GreenInfo(string info)
        {
            return $"<color=#99FF00>{info}</color>";
        }

        private TieredResource inputAsTieredResource;
        private TieredResource outputAsTieredResource;

        public TieredResource Input
        {
            get
            {
                if (this.inputAsTieredResource == null && string.IsNullOrEmpty(this.input))
                {
                    this.inputAsTieredResource = ColonizationResearchScenario.Instance.TryGetTieredResourceByName(this.input);
                    Debug.Assert(this.inputAsTieredResource != null, "Part is not configured correctly - input not set to a tiered resource name");
                }
                return this.inputAsTieredResource;
            }
        }

        public TieredResource Output
        {
            get
            {
                if (this.outputAsTieredResource == null && string.IsNullOrEmpty(this.output))
                {
                    this.outputAsTieredResource = ColonizationResearchScenario.Instance.TryGetTieredResourceByName(this.output);
                    Debug.Assert(this.outputAsTieredResource != null, "Part is not configured correctly - output not set to a tiered resource name");
                }
                return this.outputAsTieredResource;
            }
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            if (this.Input != null)
            {
                info.AppendLine($"{GreenInfo("Input:")} {this.Input.TieredName(this.Tier)}");
            }

            info.AppendLine($"{GreenInfo("Capacity:")} {this.capacity} {this.Output.CapacityUnits}");

            if (this.Output.CanBeStored)
            {
                info.AppendLine($"{GreenInfo("Output:")} {this.Output.TieredName(this.Tier)}");
            }

            if (this.Output is EdibleResource edible)
            {
                info.AppendLine($"{GreenInfo("Quality:")}");
                foreach (TechTier tier in TechTierExtensions.AllTiers)
                {
                    info.AppendLine($" {tier.ToString()}: {(int)(edible.GetPercentOfDietByTier(tier) * 100)}%");
                }
            }

            return info.ToString();
        }
    }
}
