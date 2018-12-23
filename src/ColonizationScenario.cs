using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{
    // LEARNINGS
    //  - there's something called a KSPField that supplants this.
    //  - This doesn't need a node - the node you're given is specific to the scenario.
    //  - ScenarioModule inherits from MonoBehevior - maybe this class could support the GUI too?

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER)]
    public class ColonizationScenario
        : ScenarioModule
    {
        public override void OnLoad(ConfigNode config)
        {
            base.OnLoad(config);
            Debug.Log("ColonizationScenario.OnLoad - start");
            var myNode = config.GetNode("Nerm.Colonization");
            if (myNode == null)
            {
                Debug.Log("ColonizationScenario.OnLoad - bailed because Nerm.Colonization node is missing");
                return;
            }

            string rectAsString = myNode.GetValue("DebugWindow");

            myNode.TryGetValue(nameof(AddInSettings.DebugWindowExtent), ref AddInSettings.DebugWindowExtent);
            Debug.Log($"DebugWindowExtent loaded: {AddInSettings.DebugWindowExtent.x},{AddInSettings.DebugWindowExtent.y}");
            myNode.TryGetValue(nameof(AddInSettings.DebugWindowIsVisible), ref AddInSettings.DebugWindowIsVisible);
            for (int i = 0; i < AddInSettings.DebugToggles.Length; ++i)
            {
                myNode.TryGetValue($"{nameof(AddInSettings.DebugToggles)}_{i}", ref AddInSettings.DebugToggles[i]);
            }
            Debug.Log("ColonizationScenario.OnLoad - succeeded");
        }

        public override void OnSave(ConfigNode node)
        {
            Debug.Log("ColonizationScenario.OnSave - start");
            ConfigNode myStuff = new ConfigNode("Nerm.Colonization");
            myStuff.AddValue(nameof(AddInSettings.DebugWindowExtent), AddInSettings.DebugWindowExtent);
            myStuff.AddValue(nameof(AddInSettings.DebugWindowIsVisible), AddInSettings.DebugWindowIsVisible);
            for (int i = 0; i < AddInSettings.DebugToggles.Length; ++i)
            {
                myStuff.AddValue($"{nameof(AddInSettings.DebugToggles)}_{i}", AddInSettings.DebugToggles[i]);
            }

            Debug.Log($"DebugWindowExtent saved: {AddInSettings.DebugWindowExtent.x},{AddInSettings.DebugWindowExtent.y}");
            node.AddNode(myStuff);
            base.OnSave(node);
            Debug.Log("ColonizationScenario.OnSave - succeeded");
        }
    }
}
