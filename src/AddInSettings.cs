using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{
    static class AddInSettings
    {
        public static bool[] DebugToggles { get; } = new bool[10];
        public static Rect DebugWindowExtent = new Rect(100, 100, 150, 330);
        public static bool DebugWindowIsVisible = true;
    }
}
