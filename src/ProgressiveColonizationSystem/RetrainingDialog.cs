using Experience;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProgressiveColonizationSystem
{
    internal class RetrainingDialog
    {
        private readonly List<PksRetrainingEntry> currentStatus;
        private readonly IReadOnlyList<ProtoCrewMember> kerbalsInPart;
        private readonly double trainingCostInSeconds;
        private PopupDialog dialog;
        private static readonly Vector2 defaultPosition = new Vector2(.5f, .6f);

        private RetrainingDialog(
            List<PksRetrainingEntry> currentStatus,
            IReadOnlyList<ProtoCrewMember> kerbalsInPart,
            double trainingCostInSeconds)
        {
            this.currentStatus = currentStatus;
            this.kerbalsInPart = kerbalsInPart;
            this.trainingCostInSeconds = trainingCostInSeconds;
        }

        // Shows the dialog for the indicated kerbals.  The 
        public static RetrainingDialog Show(
            List<PksRetrainingEntry> currentStatus,
            IReadOnlyList<ProtoCrewMember> kerbalsInPart,
            double trainingCostInSeconds)
        {
            var d = new RetrainingDialog(currentStatus, kerbalsInPart, trainingCostInSeconds);
            d.Draw(defaultPosition);
            return d;
        }

        private void Draw(Vector2 position)
        {
            DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout();

            foreach (var kerbal in kerbalsInPart.Where(k => k.trait != "Tourist").OrderBy(n => n.name))
            {
                var entry = this.currentStatus.FirstOrDefault(s => s.KerbalName == kerbal.name);
                if (entry != null)
                {
                    vertical.AddChild(new DialogGUIHorizontalLayout(
                        new DialogGUILabel(() => entry.IsComplete
                            ? $"{entry.KerbalName} is a fully trained {entry.FutureTrait}!"
                            : $"{entry.KerbalName} will be a {entry.FutureTrait} in {BuildTimeString(entry.RemainingTrainingTime)}"),
                        new DialogGUIFlexibleSpace(),
                        new DialogGUIButton("Change", () => this.OnChangeClicked(entry, kerbal), w: 70, h: 16, dismissOnSelect: false)
                    ));
                }
                else
                {
                    vertical.AddChild(new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                        new DialogGUILabel($"{kerbal.name} is currently a {kerbal.experienceTrait.TypeName}"),
                        new DialogGUIFlexibleSpace(),
                        new DialogGUIButton("Change", () => this.OnChangeClicked(null, kerbal), w: 70, h: 16, dismissOnSelect: false)
                    ));
                }
            }

            foreach (var entry in currentStatus)
            {
                if (!kerbalsInPart.Any(k => k.name == entry.KerbalName))
                {
                    vertical.AddChild(new DialogGUIHorizontalLayout(
                        new DialogGUILabel($"{entry.KerbalName} is not in class!")
                    ));
                }
            }

            vertical.AddChild(new DialogGUISpace(10));
            vertical.AddChild(
                new DialogGUIHorizontalLayout(
                    new DialogGUIFlexibleSpace(),
                    new DialogGUIButton("Close", () => { }, dismissOnSelect: true),
                    new DialogGUIFlexibleSpace()));

            this.dialog = PopupDialog.SpawnPopupDialog(
                new Vector2(.5f, .5f),
                new Vector2(.5f, .5f),
                new MultiOptionDialog(
                    "RetrainingView",
                    "",
                    $"Training",
                    HighLogic.UISkin,
                    new Rect(position,new Vector2(340, 1)),
                    vertical),
                persistAcrossScenes: false,
                skin: HighLogic.UISkin,
                isModal: false,
                titleExtra: "TITLE EXTRA!");
        }

        public void Dismiss()
        {
            if (this.dialog != null)
            {
                this.dialog.Dismiss();
            }
        }

        public void Redraw()
        {
            Vector2 position = defaultPosition;
            if (this.dialog != null)
            {
                Vector3 rt = dialog.GetComponent<RectTransform>().position;
                position = new Vector2(rt.x / GameSettings.UI_SCALE / Screen.width + 0.5f,
                                       rt.y / GameSettings.UI_SCALE / Screen.height + 0.5f);
                this.dialog.Dismiss();
            }

            this.Draw(position);
        }

        public bool IsVisible => this.dialog != null;

        private static string BuildTimeString(double totalSeconds)
        {
            int t = (int)totalSeconds;
            int seconds = t % 60;
            t = t / 60;
            int minutes = t % 60;
            t = t / 60;
            int hours = t % 6;
            t = t / 6;
            int days = t;
            return $"{days}:{hours}:{minutes:D2}:{seconds:D2}";
        }

        private void StartTraining(PksRetrainingEntry entry, ProtoCrewMember kerbal, ExperienceTraitConfig config, double actualTime)
        {
            if (entry == null)
            {
                this.currentStatus.Add(new PksRetrainingEntry(kerbal.name, actualTime, config.Name));
                Redraw();
            }
            else
            {
                entry.ChangeFutureTrait(actualTime, config.Name);
            }
            KerbalRoster.SetExperienceTrait(kerbal, "Trainee");
        }

        private void OnChangeClicked(PksRetrainingEntry entry, ProtoCrewMember kerbal)
        {
            var leftColumn = new DialogGUIVerticalLayout();
            var rightColumn = new DialogGUIVerticalLayout();

            ExperienceSystemConfig fullConfig = new ExperienceSystemConfig();
            PopupDialog traitChooserDialog = null;
            ExperienceTraitConfig selectedConfig = null;
            bool isLeft = false;
            foreach (var config in fullConfig.Categories.Where(c => c.Name != "Tourist" && c.Name != "Trainee" && c.Name != kerbal.trait).OrderBy(c => c.Name))
            {
                (isLeft ? leftColumn : rightColumn).AddChild(new DialogGUIToggle(
                    () => config == selectedConfig, config.Name, (b) => { selectedConfig = config; }));
                isLeft = !isLeft;
            }

            double actualTime = trainingCostInSeconds * (1 + kerbal.stupidity);
            DialogGUIVerticalLayout dialogBody = new DialogGUIVerticalLayout(
                new DialogGUIHorizontalLayout(leftColumn, rightColumn),
                new DialogGUISpace(4),
                new DialogGUILabel($"{kerbal.name} will need {((int)actualTime) / (6 * 60 * 60):D} days to retrain"),
                new DialogGUIHorizontalLayout(
                    new DialogGUIFlexibleSpace(),
                    new DialogGUIButton(
                        "OK",
                        () => this.StartTraining(entry, kerbal, selectedConfig, actualTime),
                        () => selectedConfig != null,
                        dismissOnSelect: true
                    ),
                    new DialogGUISpace(8),
                    new DialogGUIButton("Cancel", () => { }),
                    new DialogGUIFlexibleSpace())
            ) ;

            Vector3 rt = dialog.GetComponent<RectTransform>().position;
            var sz = dialog.GetComponent<RectTransform>().sizeDelta;
            var xPosition = (rt.x + 2f*sz.x) / GameSettings.UI_SCALE / Screen.width + 0.5f;
            var yPosition = rt.y / GameSettings.UI_SCALE / Screen.height + 0.5f;

            traitChooserDialog = PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog(
                    "TraitChooser",
                    "",
                    $"Choose {kerbal.name}'s new career",
                    HighLogic.UISkin,
                    new Rect(new Vector2(xPosition, yPosition), new Vector2(340, 1)),
                    dialogBody),
                persistAcrossScenes: false,
                skin: HighLogic.UISkin,
                isModal: true,
                titleExtra: "TITLE EXTRA!");
        }
    }
}
