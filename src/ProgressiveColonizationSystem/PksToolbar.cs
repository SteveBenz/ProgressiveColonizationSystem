using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ToolbarControl_NS;
using KSP.UI.Screens;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   This class manages the mod's toolbar (with some help from <see cref="RegisterToolbar"/>.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class PksToolbar : MonoBehaviour
    {
        public static ToolbarControl ColonizationDialogToggle { get; private set; }

        public const string MODID = "PKS_NS";
        public const string MODNAME = "Progressive Colonization System";

        public void Start()
        {
            SetupColonizationDialogToggle();
            DontDestroyOnLoad(this);
        }

        private void SetupColonizationDialogToggle()
        {
            ColonizationDialogToggle = gameObject.AddComponent<ToolbarControl>();
            ColonizationDialogToggle.AddToAllToolbars(HandleColonizationDialogToggledOn, HandleColonizationDialogToggledOff,
                ApplicationLauncher.AppScenes.SPH |
                ApplicationLauncher.AppScenes.VAB |
                ApplicationLauncher.AppScenes.FLIGHT |
                ApplicationLauncher.AppScenes.MAPVIEW,
                MODID,
                "pksButton",
                "ProgressiveColonizationSystem/Textures/cupcake-38",
                "ProgressiveColonizationSystem/Textures/cupcake-24",
                MODNAME
            );
        }

        private void HandleColonizationDialogToggledOn()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                LifeSupportCalculator.ShowDialog();
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                LifeSupportStatusMonitor.ShowDialog();
            }
        }

        private void HandleColonizationDialogToggledOff()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                LifeSupportCalculator.DismissDialog();
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                LifeSupportStatusMonitor.DismissDialog();
            }
        }

        private void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                ColonizationDialogToggle.Enabled = LifeSupportStatusMonitor.IsRelevant_static;
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(PksToolbar.MODID, PksToolbar.MODNAME);
        }
    }
}
