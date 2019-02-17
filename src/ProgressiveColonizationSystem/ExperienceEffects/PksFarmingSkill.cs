using Experience;

namespace ProgressiveColonizationSystem.ExperienceEffects
{
    public class PksFarmingSkill
         : ExperienceEffect
    {
        [KSPField]
        public string extra;

        public PksFarmingSkill(ExperienceTrait parent) : base(parent)
        {
        }

        public PksFarmingSkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "Experience in operating off-world agriculture modules";
        }
    }
}
