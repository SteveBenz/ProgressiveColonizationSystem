using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public static class TextEffects
    {
        public static string Green(string info)
        {
            return $"<color=#99FF00>{info}</color>";
        }

        public static string Red(string info)
        {
            return $"<color=#FF2222>{info}</color>";
        }

        public static string Yellow(string info)
        {
            return $"<color=#F0F000>{info}</color>";
        }
    }
}
