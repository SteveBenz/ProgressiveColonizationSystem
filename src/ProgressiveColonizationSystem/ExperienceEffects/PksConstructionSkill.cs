
using Experience;

namespace ProgressiveColonizationSystem.ExperienceEffects
{
    public class PksConstructionSkill
         : ExperienceEffect
    {
        [KSPField]
        public string extra;

        public PksConstructionSkill(ExperienceTrait parent) : base(parent)
        {
        }

        public PksConstructionSkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "Experience in off-world construction";
        }
    }
}
