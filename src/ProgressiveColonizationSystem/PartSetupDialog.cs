using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public class PartSetupDialog
    {
        private string[] tierLabels = new string[1 + (int)TechTier.Tier4];
        private DialogGUIToggle[] tierToggles = new DialogGUIToggle[1 + (int)TechTier.Tier4];

        private PartSetupDialog(TieredResource product, string body, TechTier tier, TechTier maxTierForPart)
        {
            this.Body = body;
            this.Tier = tier;
            this.Product = product;
            this.MaxTierForPart = maxTierForPart;
            this.RiskLevel = StaticAnalysis.GetTierSuitability(ColonizationResearchScenario.Instance, this.Product, tier, maxTierForPart, this.Body);
        }

        public enum DecisionImpact
        {
            ThisPart,
            FutureParts,
            AllParts,
        }

        public static void Show(TieredResource product, string body, TechTier tier, TechTier maxTierForPart, Action<PartSetupDialog> onSet)
        {
            if (product.ResearchCategory.Type == ProductionRestriction.LandedOnBody && !ColonizationResearchScenario.Instance.UnlockedBodies.Any())
            {
                PopupMessageWithKerbal.ShowPopup("No Bodies Explored", "You need to land and return from a body in order to start a colony on it", "Got it");
            }
            else
            {
                new PartSetupDialog(product, body, tier, maxTierForPart).Show(onSet);
            }
        }

        private void Show(Action<PartSetupDialog> onSet)
        {
            this.CalculateTierLabels(false);
            var tierSelector = new DialogGUIVerticalLayout(this.tierToggles);

            DialogGUIBase mainForm;
            if (this.Product.ResearchCategory.Type == ProductionRestriction.Space)
            {
                mainForm = tierSelector;
            }
            else
            {
                var bodySelector = new DialogGUIVerticalLayout(
                        ColonizationResearchScenario.Instance.UnlockedBodies
                            .Select(b => new DialogGUIToggle(() => b == this.Body, b, isSelected => { if (isSelected) this.SetBody(b); })).ToArray());

                mainForm = new DialogGUIHorizontalLayout(
                    tierSelector,
                    new DialogGUISpace(50),
                    bodySelector);
            }

            PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog(
                    "Whatsthisdoeven",
                    "", // This actually shows up on the screen as a sort of a wierd-looking subtitle
                    "Tiered Part Configuration",
                    HighLogic.UISkin,
                    new DialogGUIVerticalLayout(
                        mainForm,
                        new DialogGUIHorizontalLayout(
                            new DialogGUIButton("Setup This Part", () => { this.Applicability = DecisionImpact.ThisPart; onSet(this); }, dismissOnSelect: true),
                            new DialogGUIFlexibleSpace(),
                            new DialogGUIButton("Setup All Parts", () => { this.Applicability = DecisionImpact.AllParts; onSet(this); }, dismissOnSelect: true),
                            new DialogGUIFlexibleSpace(),
                            new DialogGUIButton("Cancel", () => { }, dismissOnSelect: true)))),
                persistAcrossScenes: false,
                skin: HighLogic.UISkin,
                isModal: true,
                titleExtra: "TITLE EXTRA!");
        }

        private void SetBody(string body)
        {
            this.Body = body;
            this.CalculateTierLabels(true);
        }

        public TieredResource Product { get; }

        public string Body { get; private set; }

        public TechTier Tier { get; private set; }

        public TechTier MaxTierForPart { get; private set; }

        public DecisionImpact Applicability { get; private set; }

        public TierSuitability RiskLevel { get; private set; }

        private void CalculateTierLabels(bool setTier)
        {
            for (TechTier tier = TechTier.Tier0; tier <= TechTier.Tier4; ++tier)
            {
                var suitability = StaticAnalysis.GetTierSuitability(ColonizationResearchScenario.Instance, this.Product, tier, this.MaxTierForPart, this.Body);
                bool isEnabled;
                Func<string, string> transform;
                string toolTipTag;
                switch (suitability)
                {
                    default:
                    case TierSuitability.Ideal:
                        transform = TextEffects.Green;
                        toolTipTag = "#LOC_KPBS_IDEAL_TIER";
                        isEnabled = true;
                        if (setTier)
                        {
                            this.RiskLevel = TierSuitability.Ideal;
                            this.Tier = tier;
                        }
                        break;
                    case TierSuitability.LacksScanner:
                        transform = TextEffects.Yellow;
                        toolTipTag = "#LOC_KPBS_SCANNING_SKILL_LAGS";
                        isEnabled = true;
                        break;
                    case TierSuitability.LacksSubordinateResearch:
                        transform = TextEffects.Red;
                        toolTipTag = "#LOC_KPBS_SUBORDINATE_SKILL_LAGS";
                        isEnabled = true;
                        break;
                    case TierSuitability.UnderTier:
                        transform = TextEffects.Yellow;
                        toolTipTag = "#LOC_KPBS_UNDER_TIER";
                        isEnabled = true;
                        break;
                    case TierSuitability.NotResearched:
                        transform = s => s;
                        toolTipTag = "#LOC_KPBS_NOT_RESEARCHED";
                        isEnabled = false;
                        break;
                    case TierSuitability.BodyNotSelected:
                        transform = s => s;
                        toolTipTag = "#LOC_KPBS_CHOOSE_A_BODY_FIRST";
                        isEnabled = false;
                        break;
                    case TierSuitability.PartDoesntSupportTier:
                        transform = s => s;
                        toolTipTag = "#LOC_KPBS_PART_DOES_NOT_SUPPORT_TIER";
                        isEnabled = false;
                        break;
                }

                TechTier techTierCopy = tier;
                string labelGetter() => transform(techTierCopy.DisplayName());
                void onToggled(bool isSelected)
                {
                    if (isSelected && isEnabled)
                    {
                        this.Tier = techTierCopy;
                        this.RiskLevel = suitability;
                    }
                }

                if (this.tierToggles[(int)tier] == null)
                {
                    this.tierToggles[(int)tier] = new DialogGUIToggle(() => techTierCopy == this.Tier, labelGetter, onToggled);
                }
                else
                {
                    this.tierToggles[(int)tier].setLabel = labelGetter;
                    this.tierToggles[(int)tier].onToggled = onToggled;
                }

                this.tierToggles[(int)tier].OptionInteractableCondition = () => isEnabled;
                this.tierToggles[(int)tier].tooltipText = Localizer.GetStringByTag(toolTipTag);
            }
        }
    }
}
