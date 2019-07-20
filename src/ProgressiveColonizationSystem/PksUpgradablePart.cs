using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public class PksUpgradablePart
        : PartModule
    {
        private PksTieredResourceConverter tieredConverterCache = null;
        private PksCrewRequirement crewRequirementCache = null;

        /// <summary>
        ///   The crew skill required to make an upgrade
        /// </summary>
        const string requiredEffect = "PksConstructionSkill";
        const string rocketPartsResourceName = "RocketParts";

        /// <summary>
        ///   The cost, in rocket parts, to upgrade the part
        /// </summary>
        [KSPField]
        public float upgradeCost;

        /// <summary>
        ///   The amount of time it takes
        /// </summary>
        [KSPField]
        public float upgradeTimeInKerbalDays;

        /// <summary>
        ///   If greater than zero, then an upgrade is in progress and remainingWork is how
        ///   many rocket parts need to be placed.
        /// </summary>
        /// <remarks>
        ///   Stored by the game, not set in the CFG.
        /// </remarks>
        [KSPField(isPersistant = true)]
        public float remainingWork;

        /// <summary>
        ///   To avoid overworking the CPU looking for a rare event, we only check whether or not
        ///   we can upgrade every few seconds or so.  This holds the last time we looked.
        /// </summary>
        private double lastCanUpgradeCheck;

        private const double SecondsBetweenCanUpgradeChecks = 2.0;

        /// <summary>
        ///   Displayed to the user
        /// </summary>
        [KSPField(isPersistant = false, guiName = "Upgrade Progress", guiUnits = "%", guiActive = true, guiFormat = "N2", guiActiveEditor = false)]
        public float remainingWorkAsPercentage;

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Upgrade")]
        public void OnUpgrade()
        {
            if (!this.CanUpgrade)
            {
                return;
            }

            PartResourceDefinition rocketPartsResourceDefinition = PartResourceLibrary.Instance.GetDefinition(rocketPartsResourceName);
            vessel.GetConnectedResourceTotals(rocketPartsResourceDefinition.id, out double rocketPartsOnHand, out double _);
            string kerbalFixerDescription = PksCrewRequirement.DescribeKerbalsWithEffect(requiredEffect, 1 + this.TieredConverter.Tier);

            if (rocketPartsOnHand == 0)
            {
                PopupMessageWithKerbal.ShowPopup("No can do boss!", $"This job requires {rocketPartsResourceDefinition.displayName}; there are none aboard.", "K, I'll go get some");
                return;
            }

            int numUpgraders = this.NumberOfUpgraders();
            if (numUpgraders == 0)
            {
                PopupMessageWithKerbal.ShowPopup("No can do boss!", $"This job requires a {kerbalFixerDescription}; there are none aboard", "K, I'll go get some");
                return;
            }

            PopupMessageWithKerbal.ShowOkayCancel(
                title: "Moar Boosters!",
                content: $"Upgrading this part will take it offline for {this.upgradeTimeInKerbalDays:N} days "
                        +$"and require the full-time attention of a {kerbalFixerDescription} (you have {numUpgraders} on board).  "
                        +$"It will take {this.upgradeCost} {rocketPartsResourceDefinition.displayName} (you have "
                        +$"{rocketPartsOnHand:N} on-hand at the moment).\r\n\r\n"
                        +"But moreover, in order for this part to function once it gets upgraded, the whole "
                        +"supply chain that leads up to this part needs to be updated as well.  This operation needs "
                        +"to be approached with some care; if your kerbals run out of snacks during the upgrade, "
                        +"the whole operation can go to pieces.",
                okayButton: "Let's Do This!",
                cancelButton: "Err, nope.",
                onOkay: this.StartUpgrade);
        }

        private int NumberOfUpgraders()
        {
            List<ProtoCrewMember> kspCrew = this.vessel.GetVesselCrew();
            return kspCrew.Select(c => new SkilledCrewman(c)).Count(c => c.CanRunPart(requiredEffect, (int)this.TieredConverter.Tier + 1));
        }


        private void StartUpgrade()
        {
            this.remainingWork = this.upgradeCost;
            // Tier+2 because:
            //  +1 because it's converting from "tier" to "stars"
            //  +1 more because we want to ask for an equivolent engineer to what it would take to run it at the next tier.
            this.CrewRequirement.OverrideRequirement(requiredEffect, this.TieredConverter.Tier + 2, 1.0f);
            this.UpdateFields();
        }

        public void UpdateRemainingParts(double newValue)
        {
            this.remainingWork = (float)newValue;
            if (this.remainingWork == 0)
            {
                this.CrewRequirement.CancelOverride();
                ++this.TieredConverter.tier;
            }
            this.UpdateFields();
        }

        public double PartsUseRateInRocketPartsPerSecond => this.upgradeCost / ColonizationResearchScenario.KerbalDaysToSeconds(this.upgradeTimeInKerbalDays);

        private PksTieredResourceConverter TieredConverter
        {
            get
            {
                if (this.tieredConverterCache == null)
                {
                    this.tieredConverterCache = this.part.FindModuleImplementing<PksTieredResourceConverter>();
                }

                return this.tieredConverterCache;
            }
        }

        public PksCrewRequirement CrewRequirement
        {
            get
            {
                if (this.crewRequirementCache == null)
                {
                    this.crewRequirementCache = this.vessel.FindPartModuleImplementing<PksCrewRequirement>();
                }

                return this.crewRequirementCache;
            }
        }

        public bool IsUpgrading
        {
            get
            {
                return this.remainingWork > 0;
            }
        }

        public void FixedUpdate()
        {
            double now = Planetarium.GetUniversalTime();
            if (now > this.lastCanUpgradeCheck + SecondsBetweenCanUpgradeChecks)
            {
                UpdateFields();
                this.lastCanUpgradeCheck = now;
            }
        }

        private void UpdateFields()
        {
            if (this.IsUpgrading)
            {
                this.Events[nameof(this.OnUpgrade)].guiActive = false;
                this.Fields[nameof(this.remainingWorkAsPercentage)].guiActive = true;
                this.remainingWorkAsPercentage = 100.0f * (this.upgradeCost - this.remainingWork) / this.upgradeCost;
            }
            else
            {
                this.Events[nameof(this.OnUpgrade)].guiName = $"Upgrade to Tier {this.TieredConverter.tier+1}";
                this.Events[nameof(this.OnUpgrade)].guiActive = this.CanUpgrade;
                this.Fields[nameof(this.remainingWorkAsPercentage)].guiActive = false;
            }
        }

        private bool CanUpgrade
        {
            get
            {
                if (this.TieredConverter == null)
                {
                    return false;
                }

                TechTier maxTier = ColonizationResearchScenario.Instance.GetMaxUnlockedTier(this.TieredConverter.Output, this.TieredConverter.Body);
                if (this.TieredConverter.Tier >= maxTier)
                {
                    return false;
                }

                // TODO: Validate that rest of the resource chain is available at this tier
                return true;
            }
        }
    }
}