
using Experience;

namespace ProgressiveColonizationSystem.ExperienceEffects
{
    public class PksScanningSkill
         : ExperienceEffect
    {
        [KSPField]
        public string extra;

        public PksScanningSkill(ExperienceTrait parent) : base(parent)
        {
        }

        public PksScanningSkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "Experience in operating orbital scanning equipment";
        }
    }
}
