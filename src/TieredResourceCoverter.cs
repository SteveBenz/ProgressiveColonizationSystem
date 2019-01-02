using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
	public abstract class TieredResourceCoverter
		: ModuleResourceConverter, IProducer
	{
        private double firstNoPowerIndicator = -1.0;

		[KSPField(advancedTweakable = false, category = "Nermables", guiActive = true, guiName = "Tier", isPersistant = true, guiActiveEditor = true)]
		public int tier;

		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change Tier")]
		public void ChangeTier()
		{
			this.tier = (this.tier + 1) % ((int)this.MaxTechTierResearched + 1);
		}

		[KSPField]
		public string output;

		[KSPField]
		public float capacity;

		[KSPField(guiActive = true, guiActiveEditor = false, guiName = "Research")]
		public string researchStatus;

		protected abstract TechTier MaxTechTierResearched { get; }

		protected virtual bool CanDoProduction(out string reasonWhyNotMessage)
		{
			if (!this.IsActivated)
			{
				reasonWhyNotMessage = "Disabled - module is off";
				return false;
			}

			if (!this.IsPowered)
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

		public override void FixedUpdate()
		{
			base.FixedUpdate();

			if (!HighLogic.LoadedSceneIsFlight)
			{
				return;
			}

			if (this.CanDoProduction(out string reasonWhyNotMessage))
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
				if (this.IsActivated)
				{
					ScreenMessages.PostScreenMessage($"{this.name} is shutting down:  {reasonWhyNotMessage}", 10.0f);
					this.StopResourceConverter();
				}
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

			if (this.vessel.GetVesselCrew() == null)
			{
				return false;
			}

			// We might want to make this check more elaborate someday - to encourage bigger crews
			// amont other things.
			return this.vessel.GetVesselCrew().Any(crew => crew.trait == RequiredCrewTrait && crew.experienceLevel >= this.tier);
		}

		protected abstract string RequiredCrewTrait { get; }

        /// <summary>
        ///   Returns true if the part has electrical power
        /// </summary>
		private bool IsPowered
        {
            get
            {
                // I don't see a good way to determine if a converter is running stably.
                //  lastTimeFactor seems to be the amount of the last recipe that it was able
                //  to successfully convert, which ought to be it, but lastTimeFactor is zero
                //  for several iterations after unpacking the vessel.  This code attempts to
                //  compensate for that by waiting at least 10 seconds before declaring itself
                //  unpowered.
                if (this.lastTimeFactor == 0)
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
        }

		public TechTier Tier => (TechTier)this.tier;

		public bool IsResearchEnabled { get; private set; }

		public bool IsProductionEnabled { get; private set; }

		public abstract bool CanStockpileProduce { get; }

		public double ProductionRate => this.capacity;

		/// <summary>
		///   The maximum-tier version of the product that this producer can produce.  The actual
		///   resource name would need to have <see cref="Tier"/> mixed into the name.
		/// </summary>
		public string ProductResourceName => this.output;

        public abstract string SourceResourceName { get; }

        public abstract bool ContributeResearch(IColonizationResearchScenario target, double amount);

        public static string GreenInfo(string info)
        {
            return $"<color=#99FF00>{info}</color>";
        }
    }
}
