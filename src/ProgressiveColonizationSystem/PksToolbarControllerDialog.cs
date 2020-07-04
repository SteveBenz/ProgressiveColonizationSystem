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
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class PksToolbarControllerDialog : MonoBehaviour
    {
        static public PksToolbarControllerDialog instance;
        internal ToolbarControl toolbarControl = null;

        internal const string MODID = "PKS_NS";
        internal const string MODNAME = "Progressive Colonization System";


        public void Start()
        {
            Debug.Log("PksToolbarControllerDialog.Start");
            instance = this;
            SetUpToolbarIcon();
            DontDestroyOnLoad(this);
        }

        private void SetUpToolbarIcon()
        {
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(ShowDialog, HideDialog,
                ApplicationLauncher.AppScenes.SPH |
                ApplicationLauncher.AppScenes.VAB |
                ApplicationLauncher.AppScenes.FLIGHT |
                ApplicationLauncher.AppScenes.MAPVIEW,
                MODID,
                "pksButton",
                "ProgressiveColonizationSystem/Textures/cupcake-n-38",
                "ProgressiveColonizationSystem/Textures/cupcake-n-24",
                "ProgressiveColonizationSystem/Textures/cupcake-s-38",
                "ProgressiveColonizationSystem/Textures/cupcake-s-24",
                MODNAME
            );
        }

        private void ShowDialog()
        {
            Debug.Log("PksToolbarControllerDialog.ShowDialog");
            if (!PksToolbarDialog.instance) return;
            PksToolbarControllerDialog.instance.SetTexture("ProgressiveColonizationSystem/Textures/cupcake-s-38",
                                                           "ProgressiveColonizationSystem/Textures/cupcake-s-24");

            PksToolbarDialog.instance.isVisible = true;
            if (PksToolbarDialog.instance.dialog == null)
            {
                PksToolbarDialog.instance.dialog = PopupDialog.SpawnPopupDialog(
                    new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f),
                    PksToolbarDialog.instance.DrawDialog(new Rect(PksToolbarDialog.instance.xPosition, PksToolbarDialog.instance.yPosition, width: 430f, height: 300f)),
                    persistAcrossScenes: false,
                    skin: HighLogic.UISkin,
                    isModal: false,
                    titleExtra: "TITLE EXTRA!"); // <- no idea what that does.
            }
        }
        private void HideDialog()
        {
            Debug.Log("PksToolbarControllerDialog.HideDialog");
            if (!PksToolbarDialog.instance) return;
            PksToolbarDialog.instance.isVisible = false;
            PksToolbarDialog.instance.dialog?.Dismiss();
            PksToolbarDialog.instance.dialog = null;

            PksToolbarControllerDialog.instance.SetTexture("ProgressiveColonizationSystem/Textures/cupcake-n-38",
                                                           "ProgressiveColonizationSystem/Textures/cupcake-n-24");
        }

        public void SetTexture(string large, string small)
        {
            if (toolbarControl != null)
                toolbarControl.SetTexture(large, small);
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(PksToolbarControllerDialog.MODID, PksToolbarControllerDialog.MODNAME);
        }
    }
}
