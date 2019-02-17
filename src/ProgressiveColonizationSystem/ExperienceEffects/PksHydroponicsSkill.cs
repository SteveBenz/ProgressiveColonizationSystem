
using Experience;

namespace ProgressiveColonizationSystem.ExperienceEffects
{
    public class PksHydroponicsSkill
         : ExperienceEffect
    {
        [KSPField]
        public string extra;

        public PksHydroponicsSkill(ExperienceTrait parent) : base(parent)
        {
        }

        public PksHydroponicsSkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "Experience in operating equipment that extends the crew's ability to operate in space";
        }
    }
}
