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
    }
}
