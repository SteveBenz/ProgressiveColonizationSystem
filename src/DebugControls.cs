using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IFILifeSupport
{
    [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, true)]
    public class DebugControls
        : MonoBehaviour
    {
        public static DebugControls Instance;

        private bool isVisible = true;

        public const float WINDOW_WIDTH_DEFAULT = 100;
        public const float WINDOW_HEIGHT = 440;
        public Rect windowPos = new Rect(180, Screen.height / 2 - WINDOW_HEIGHT / 2, WINDOW_WIDTH_DEFAULT, WINDOW_HEIGHT);
        public const float HorizontalBuffer = 3;
        public const float VerticalBuffer = 3;
        public const float VerticalSpacing = 2;

        private bool[] settings = new bool[3];

        public void Start()
        {
            Debug.Log("DebugControls - Start/Stop");
            Instance = this;
        }

        public void Awake()
        {
            Debug.Log("DebugControls - Awake/Start");
            Debug.Log("DebugControls - Awake/Stop");
        }

        private void OnGUI()
        {
            Debug.Log("DebugControls - OnGUI 1");

            if (!this.isVisible)
            {
                Debug.Log("DebugControls - Has become invisible");
                //return;
            }

            Debug.Log("DebugControls - OnGUI 2");
            //windowPos = ClickThruBlocker.GUILayoutWindow(99977, windowPos, DebugModeDialog, "Debug modes");
            windowPos = GUI.Window(GetInstanceID(), windowPos, DebugModeDialog, "Debug modes");
            Debug.Log("DebugControls - OnGUI 3");
        }

        private void DebugModeDialog(int windowId)
        {
            Debug.Log($"DebugModeDialog {windowId} - Enter");
            int i = 0;
            GUILayout.BeginVertical();
            foreach (string blob in new string[] { "Fiddly bits", "Naughty bits", "Crispy bits" })
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(this.settings[i], new GUIContent(blob)))
                {
                    this.settings[i] = !this.settings[i];
                }
                ++i;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
            Debug.Log($"DebugModeDialog {windowId} - Exit");
        }

        //private void DebugModeDialog(int windowId)
        //{
        //    Debug.Log($"DebugModeDialog {windowId}");
        //    int i = 0;
        //    float top = VerticalBuffer;
        //    foreach (string blob in new string[] { "Fiddly bits", "Naughty bits", "Crispy bits"})
        //    {
        //        var content = new GUIContent(blob);
        //        var width = this.windowPos.width - 2 * HorizontalBuffer;
        //        var height = GUI.skin.toggle.CalcHeight(content, width);
        //        var extent = new Rect(HorizontalBuffer, top, width, height);
        //        top = top + height + VerticalSpacing;

        //        if (GUI.Toggle(extent, this.settings[i], content))
        //        {
        //            this.settings[i] = !this.settings[i];
        //        }
        //        ++i;
        //    }

        //    GUI.DragWindow();
        //}
    }
}