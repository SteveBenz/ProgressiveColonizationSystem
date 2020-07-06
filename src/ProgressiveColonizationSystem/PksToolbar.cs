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
    /// <remarks>
    ///   Toolbars have sense of being toggled vs not-toggled (as opposed to just a simple pushbutton).
    ///   This mod doesn't take advantage of that, principally because it adds complexity to the code
    ///   without really providing the player much value.  Additionally, I haven't found any other mods
    ///   that are doing it, so it's a consistent experience with other mods.  It's also the case that
    ///   the toolbar doesn't enforce any kind of consistent look for toggled vs. not-toggled buttons,
    ///   which would seem to be a requirement for there to be widespread adoption of the feature.
    /// </remarks>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class PksToolbar : MonoBehaviour
    {
        public static ToolbarControl ColonizationDialogToggle { get; private set; }

        public const string ModId = "PKS_NS";
        public const string ModName = "Progressive Colonization System";

        public void Start()
        {
            SetupColonizationDialogToggle();
            DontDestroyOnLoad(this);
        }

        private void SetupColonizationDialogToggle()
        {
            ColonizationDialogToggle = gameObject.AddComponent<ToolbarControl>();
            ColonizationDialogToggle.AddToAllToolbars(HandleColonizationDialogToggled, HandleColonizationDialogToggled,
                ApplicationLauncher.AppScenes.SPH |
                ApplicationLauncher.AppScenes.VAB |
                ApplicationLauncher.AppScenes.FLIGHT |
                ApplicationLauncher.AppScenes.MAPVIEW,
                nameSpace: ModId,
                toolbarId: "pksButton",
                largeToolbarIcon: "ProgressiveColonizationSystem/Textures/cupcake-38",
                smallToolbarIcon: "ProgressiveColonizationSystem/Textures/cupcake-24",
                toolTip: ModName
            );
        }

        private void HandleColonizationDialogToggled()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                LifeSupportCalculator.ToggleDialogVisibility();
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                LifeSupportStatusMonitor.ToggleDialogVisibility();
            }
        }

        private void FixedUpdate()
        {
            // Seems like FixedUpdate is a pretty heavy hammer to do this with, but it
            // needs to fire when the scene is loaded (and the toolbar is ready) and
            // other events like kerbals going EVA.
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
            ToolbarControl.RegisterMod(PksToolbar.ModId, PksToolbar.ModName);
        }
    }
}
