using KSP.UI.Screens;
using UnityEngine;

// Bon Voyage has a good example of how to make these dialogs.
//   https://github.com/jarosm/KSP-BonVoyage/blob/master/BonVoyage/gui/MainWindowView.cs



namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   This class maintains a GUI display that has a persistent display status and position
    /// </summary>
    public abstract class PksPersistentDialog
        : ScenarioModule
    {
        internal PopupDialog dialog = null;

        [KSPField(isPersistant = true)]
        public bool isVisible = false;
        [KSPField(isPersistant = true)]
        public float xPosition = .5f; // .5 => the middle
        [KSPField(isPersistant = true)]
        public float yPosition = .5f; // .5 => the middle

        public override void OnAwake()
        {
            base.OnAwake();
        }

        protected abstract ApplicationLauncher.AppScenes VisibleInScenes { get; }

        protected abstract MultiOptionDialog DrawDialog(Rect rect);

        public void ToggleVisibility()
        {
            if (this.isVisible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }

        public void Show()
        {
            isVisible = true;
            if (this.dialog == null)
            {
                this.dialog = PopupDialog.SpawnPopupDialog(
                    new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f),
                    DrawDialog(new Rect(this.xPosition, this.yPosition, width: 430f, height: 300f)),
                    persistAcrossScenes: false,
                    skin: HighLogic.UISkin,
                    isModal: false,
                    titleExtra: "TITLE EXTRA!"); // <- no idea what that does.
            }
        }

        public void Hide()
        {
            this.isVisible = false;
            this.dialog?.Dismiss();
            this.dialog = null;
        }

        protected void Redraw()
        {
            if (this.dialog != null)
            {
                this.dialog.Dismiss();
                this.dialog = null;
            }
        }

        protected virtual bool IsRelevant { get; } = true;

        private void FixedUpdate()
        {
            if (!this.IsRelevant)
            {
                if (this.dialog != null)
                {
                    this.dialog.Dismiss();
                    this.dialog = null;
                    // But leave isVisible set, as that's the persistent view.
                }
                return;
            }

            this.OnFixedUpdate();

            if (this.isVisible && this.dialog != null)
            {
                Vector3 rt = dialog.GetComponent<RectTransform>().position;
                this.xPosition = rt.x / GameSettings.UI_SCALE / Screen.width + 0.5f;
                this.yPosition = rt.y / GameSettings.UI_SCALE / Screen.height + 0.5f;
            }

            if (this.isVisible && this.dialog == null)
            {
                this.Show();
            }
        }

        protected virtual void OnFixedUpdate() { }
    }
}
