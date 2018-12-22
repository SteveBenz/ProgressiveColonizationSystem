using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nerm.Colonization
{
    public static class Extensions
    {
        //private static readonly Regex RectConverter = new Regex(
        //    @"\[(?<x>d+),(?<y>d+),(?<width>d+),(?<height>d+)",
        //    RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

        //static bool TryGetValue(this ConfigNode _this, string name, out Rect rect)
        //{
        //    string asString = _this.GetValue(name);
        //    if (string.IsNullOrEmpty(asString))
        //    {
        //        rect = new Rect();
        //        return false;
        //    }

        //    Match result = RectConverter.Match(asString);
        //    if (!result.Success)
        //    {
        //        rect = new Rect();
        //        return false;
        //    }

        //    rect = new Rect(
        //        int.Parse(result.Groups["x"].Value),
        //        int.Parse(result.Groups["y"].Value),
        //        int.Parse(result.Groups["width"].Value),
        //        int.Parse(result.Groups["height"].Value));
        //    return true;
        //}

    }
}
