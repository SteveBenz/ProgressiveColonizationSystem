
using Experience;

namespace ProgressiveColonizationSystem.ExperienceEffects
{
    public class PksScroungingSkill
         : ExperienceEffect
    {
        [KSPField]
        public string extra;

        public PksScroungingSkill(ExperienceTrait parent) : base(parent)
        {
        }

        public PksScroungingSkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "Experience in operating gear that collects resources off-world";
        }
    }
}
