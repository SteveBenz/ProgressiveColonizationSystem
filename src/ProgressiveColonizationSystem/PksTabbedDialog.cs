using KSP.UI.Screens;
using ProgressiveColonizationSystem.ProductionChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace ProgressiveColonizationSystem
{
    /// <summary>
    ///   This class maintains tabbed dialogs.
    /// </summary>
    public abstract class PksTabbedDialog
        : PksPersistentDialog
    {
        private string selectedTabTitle;
        private string[] allTabTitles;

        protected PksTabbedDialog(IEnumerable<string> tabs, string selectedTab = null)
        {
            this.allTabTitles = tabs.ToArray();
            this.selectedTabTitle = this.allTabTitles.Any(s => s == selectedTab) ? selectedTab : this.allTabTitles[0];
        }

        private float buttonWidth = 80f; // Shenanigans - Can't figure out how to calculate this, but these numbers work somehow.
        private float buttonHeight = 30f;

        protected DialogGUIBase DrawTabbedDialog()
        {
            DialogGUIBase[] buttons = new DialogGUIBase[this.allTabTitles.Length];
            for (int i = 0; i < buttons.Length; ++i)
            {
                var thisTab = this.allTabTitles[i];
                buttons[i] = new DialogGUIToggleButton(this.selectedTabTitle == thisTab, thisTab, (isSet) => { this.selectedTabTitle = thisTab; this.Redraw(); }, w: buttonWidth, h: buttonHeight);
            }

            return new DialogGUIVerticalLayout(new DialogGUIHorizontalLayout(buttons), this.DrawTab(this.selectedTabTitle));
        }

        protected abstract DialogGUIBase DrawTab(string tab);
    }
}
