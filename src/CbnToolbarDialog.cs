using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// TODO: Boy Voyage has a less bad way of doing positioning:
//   https://github.com/jarosm/KSP-BonVoyage/blob/master/BonVoyage/gui/MainWindowView.cs

namespace Nerm.Colonization
{
    /// <summary>
    ///   This class maintains a toolbar button and a GUI display that has a persistent display status and position
    /// </summary>
    public abstract class CbnToolbarDialog
        : ScenarioModule
    {
        private ApplicationLauncherButton toolbarButton = null;
        private PopupDialog dialog = null;
        private bool toolbarStateMatchedToIsVisible;

        [KSPField(isPersistant = true)]
        public bool isVisible = false;
        [KSPField(isPersistant = true)]
        public float x;
        [KSPField(isPersistant = true)]
        public float y;

        public override void OnAwake()
        {
            base.OnAwake();

            AttachToToolbar();
        }

        protected abstract ApplicationLauncher.AppScenes VisibleInScenes { get; }

        private void AttachToToolbar()
        {
            if (this.toolbarButton != null)
            {
                // defensive
                return;
            }

            Debug.Assert(ApplicationLauncher.Ready, "ApplicationLauncher is not ready - can't add the toolbar button.  Is this possible, really?  If so maybe we could do it later?");
            this.toolbarButton = ApplicationLauncher.Instance.AddModApplication(ShowDialog, HideDialog, null, null, null, null,
                this.VisibleInScenes, this.GetButtonTexture());
            this.toolbarStateMatchedToIsVisible = false;
        }

        protected virtual Texture2D GetButtonTexture()
        {
            Texture2D appLauncherTexture = new Texture2D(36, 36, TextureFormat.ARGB32, false);
            appLauncherTexture.LoadImage(Properties.Resources.AppLauncherIcon);
            return appLauncherTexture;
        }

        protected abstract MultiOptionDialog DrawDialog();

        private void ShowDialog()
        {
            isVisible = true;
            if (this.dialog == null)
            {
                this.dialog = PopupDialog.SpawnPopupDialog(
                    new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f),
                    DrawDialog(),
                    persistAcrossScenes: false,
                    skin: HighLogic.UISkin,
                    isModal: false,
                    titleExtra: "TITLE EXTRA!"); // <- no idea what that does.
            }
        }

        protected void ForceRebuild()
        {
            if (this.dialog != null)
            {
                this.dialog.Dismiss();
                this.dialog = null;
            }
        }

        private void HideDialog()
        {
            isVisible = false;
            this.dialog?.Dismiss();
            this.dialog = null;
        }

        private void OnDestroy()
        {
            if (this.toolbarButton != null)
            {
                if (ApplicationLauncher.Instance != null)
                {
                    ApplicationLauncher.Instance.RemoveModApplication(this.toolbarButton);
                }
                this.toolbarButton = null;
            }
        }

        protected virtual bool IsRelevant { get; } = true;

        protected virtual void FixedUpdate()
        {
            if (!this.IsRelevant)
            {
                if (this.dialog != null)
                {
                    this.dialog.Dismiss();
                    this.dialog = null;
                    // But leave isVisible set, as that's the persistent view.
                }
                this.toolbarButton.Disable();
                return;
            }

            this.toolbarButton.Enable();

            // Shenanigans!  This gets around the apparent fact that you can't tell the toolbar what state to start in.
            if (!this.toolbarStateMatchedToIsVisible)
            {
                this.toolbarButton.toggleButton.Value = this.isVisible;
                this.toolbarStateMatchedToIsVisible = true;
            }

            // Shenanigans!  This hack gets around the apparent fact that you can't tell the window where to position itself.
            // Unity Shenanigans!  this.dialog?.dialog?.popupwindow can throw a null reference exception...  huh?
            if (this.dialog != null && this.dialog.popupWindow?.transform?.localPosition != null)
            {
                if ((x > 1f || y > 1f || x < 1f || y < 1f)
                    && this.dialog.popupWindow.transform.localPosition.x == 0 && this.dialog.popupWindow.transform.localPosition.y == 0)
                {
                    if (x > 1f || y > 1f || x < 1f || y < 1f)
                    {
                        // Re-apply the previous translation - adjusting for UI Scale
                        this.dialog.popupWindow.transform.Translate(x * GameSettings.UI_SCALE, y * GameSettings.UI_SCALE, 0f);
                        // If we have to persist this hack, we should detect whether the thing is pushed off the screen.
                    }
                }
                else
                {
                    // Record the translation for the future.
                    x = this.dialog.popupWindow.transform.localPosition.x;
                    y = this.dialog.popupWindow.transform.localPosition.y;
                }
            }

            this.OnFixedUpdate();

            if (this.isVisible && this.dialog == null)
            {
                this.ShowDialog();
            }
        }

        protected virtual void OnFixedUpdate() { }
    }
}
