
using Experience;

namespace ProgressiveColonizationSystem.ExperienceEffects
{
    public class PksManufacturingSkill
         : ExperienceEffect
    {
        [KSPField]
        public string extra;

        public PksManufacturingSkill(ExperienceTrait parent) : base(parent)
        {
        }

        public PksManufacturingSkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "Experience in operating equipment that generates fertilizer & shinies off of Kerban";
        }
    }
}
