using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nerm.Colonization
{
    public static class Extensions
    {
        public static bool TryGetValue(this ConfigNode node, string name, ref Dictionary<string,TechProgress> map)
        {
            ConfigNode agNodes = node.GetNode(name);
            if (agNodes == null)
            {
                return false;
            }

            foreach (ConfigNode childNode in agNodes.GetNodes())
            {
                TechTier tierAtBody = TechTier.Tier0;
                double progress = 0;
                if (childNode.TryGetEnum<TechTier>("tier", ref tierAtBody, TechTier.Tier0)
                  && childNode.TryGetValue("progress", ref progress))
                {
                    map[childNode.name] = new TechProgress { Progress = Math.Max(0, progress), Tier = tierAtBody };
                }
            }
            return true;
        }

        public static void SetValue(this ConfigNode node, string name, Dictionary<string, TechProgress> map)
        {
            ConfigNode agNode = node.AddNode("agriculture");
            foreach (KeyValuePair<string, TechProgress> pair in map)
            {
                ConfigNode bodyNode = agNode.AddNode(pair.Key);
                bodyNode.SetValue("tier", pair.Value.Tier.ToString());
                bodyNode.SetValue("tier", pair.Value.Progress);
            }
        }

        private static FieldInfo windowListField;

        /// <summary>
        /// Find the UIPartActionWindow for a part. Usually this is useful just to mark it as dirty.
        /// </summary>
        /// <remarks>
        /// Stolen from Firespitter Core.  If this code gives any trouble, let's try just taking an
        /// honest dependency on it.
        /// </remarks>
        public static UIPartActionWindow FindActionWindow(this Part part)
        {
            if (part == null)
                return null;

            // We need to do quite a bit of piss-farting about with reflection to 
            // dig the thing out. We could just use Object.Find, but that requires hitting a heap more objects.
            UIPartActionController controller = UIPartActionController.Instance;
            if (controller == null)
                return null;

            if (windowListField == null)
            {
                Type cntrType = typeof(UIPartActionController);
                foreach (FieldInfo info in cntrType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (info.FieldType == typeof(List<UIPartActionWindow>))
                    {
                        windowListField = info;
                        goto foundField;
                    }
                }

                Debug.LogWarning("*PartUtils* Unable to find UIPartActionWindow list");
                return null;
            }

        foundField:
            List<UIPartActionWindow> uiPartActionWindows = (List<UIPartActionWindow>)windowListField.GetValue(controller);
            if (uiPartActionWindows == null)
                return null;

            return uiPartActionWindows.FirstOrDefault(window => window != null && window.part == part);
        }
    }
}
