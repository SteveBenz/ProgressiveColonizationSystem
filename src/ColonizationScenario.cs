using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{
    //[KSPScenario(ScenarioCreationOptions.AddToAllGames)]
    public class ColonizationScenario
        : ScenarioModule
    {
        //public override void OnLoad(ConfigNode config)
        //{
        //    Debug.Log("ColonizationScenario.OnLoad - start");
        //    var myNode = config.GetNode("Nerm.Colonization");
        //    if (myNode == null)
        //    {
        //        Debug.Log("ColonizationScenario.OnLoad - bailed because Nerm.Colonization node is missing");
        //    }

        //    string rectAsString = myNode.GetValue("DebugWindow");

        //    myNode.TryGetValue(nameof(AddInSettings.DebugWindowExtent), ref AddInSettings.DebugWindowExtent);
        //    myNode.TryGetValue(nameof(AddInSettings.DebugWindowIsVisible), ref AddInSettings.DebugWindowIsVisible);
        //    for (int i = 0; i < AddInSettings.DebugToggles.Length; ++i)
        //    {
        //        myNode.TryGetValue($"{nameof(AddInSettings.DebugToggles)}_{i}", ref AddInSettings.DebugToggles[i]);
        //    }
        //    Debug.Log("ColonizationScenario.OnLoad - succeeded");
        //}

        //public override void OnSave(ConfigNode node)
        //{
        //    Debug.Log("ColonizationScenario.OnSave - start");
        //    ConfigNode myStuff = new ConfigNode("Nerm.Colonization");
        //    myStuff.AddValue(nameof(AddInSettings.DebugWindowExtent), AddInSettings.DebugWindowExtent);
        //    myStuff.AddValue(nameof(AddInSettings.DebugWindowIsVisible), AddInSettings.DebugWindowIsVisible);
        //    for (int i = 0; i < AddInSettings.DebugToggles.Length; ++i)
        //    {
        //        myStuff.AddValue($"{nameof(AddInSettings.DebugToggles)}_{i}", AddInSettings.DebugToggles[i]);
        //    }

        //    node.AddNode(myStuff);
        //    Debug.Log("ColonizationScenario.OnSave - succeeded");
        //}
    }
}
