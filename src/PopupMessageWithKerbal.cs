using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nerm.Colonization
{
    public static class PopupMessageWithKerbal
    {
        // The documentation for SpawnPopupDialog gives me not the slightest clue what the two vectors
        // it takes as its arguments are.  These numbers seem to work okay - at least on my monitor.
        // Leaving them here, like this, so they're easy to tweak in the debugger if needed.
        public static float x1 = 0.55f, y1 = 0.4f;
        public static float x2 = 0.62f, y2 = 0.4f;

        public static void ShowPopup(string title, string content, string okayButton)
        {
            var menu = PopupDialog.SpawnPopupDialog(
                new Vector2(x1,y1),
                new Vector2(x2,y2),
                new MultiOptionDialog(
                    "TierUpAlert",  // <- no idea what this does.
                    "",
                    title,
                    HighLogic.UISkin,
                    new DialogGUIVerticalLayout(
                        new DialogGUIHorizontalLayout(
                            new DialogGUIVerticalLayout(
                                new DialogGUIFlexibleSpace(),
                                makePictureOfAKerbal(160, 160),
                                new DialogGUIFlexibleSpace()),
                            new DialogGUILabel(content, true, true)),
                        new DialogGUIHorizontalLayout(
                            new DialogGUIFlexibleSpace(),
                            new DialogGUIButton(okayButton, () => { }),
                            new DialogGUIFlexibleSpace()
                        ))),
                persistAcrossScenes: false,
                skin: HighLogic.UISkin,
                isModal: true,
                titleExtra: "TITLE EXTRA!"); // <- no idea what that does.
        }

        // This object persists, whether we store it or not.
        private static KerbalInstructor instructor;

        private static DialogGUIImage makePictureOfAKerbal(int width, int height)
        {
            if (instructor == null)
            {
                GameObject genesPrefab = AssetBase.GetPrefab("Instructor_Gene");
                var instantiatedGene = UnityEngine.Object.Instantiate(genesPrefab);
                instructor = instantiatedGene.GetComponent<KerbalInstructor>();

                // Remove the lights for Gene/Wernher
                Light mainlight = instructor.GetComponentsInChildren<Light>(true).Where(l => l.name == "mainlight").FirstOrDefault();
                if (mainlight != null)
                {
                    UnityEngine.Object.Destroy(mainlight);
                }
                Light backlight = instructor.GetComponentsInChildren<Light>(true).Where(l => l.name == "backlight").FirstOrDefault();
                if (backlight != null)
                {
                    UnityEngine.Object.Destroy(backlight);
                }

                instructor.gameObject.transform.Translate(25f, 0.0f, 0.0f);

                // Add a light
                GameObject lightGameObject = new GameObject("Dialog Box Light");
                Light lightComp = lightGameObject.AddComponent<Light>();
                lightComp.color = new Color(0.4f, 0.4f, 0.4f);
                lightGameObject.transform.position = instructor.instructorCamera.transform.position;

                instructor.SetupAnimations();
            }

            var instructorTexture = new RenderTexture(width, height, 8);
            instructor.instructorCamera.targetTexture = instructorTexture;
            instructor.instructorCamera.ResetAspect();

            var initialAnimations = new List<CharacterAnimationState>()
                {
                    instructor.anim_true_thumbsUp,
                    instructor.anim_true_thumbUp,
                    instructor.anim_true_nodA,
                    instructor.anim_true_nodB,
                    instructor.anim_true_smileA,
                    instructor.anim_true_smileB,
                };
            var vampingAnimations = new List<CharacterAnimationState>()
                {
                    instructor.anim_idle_lookAround,
                    instructor.anim_idle_sigh,
                    instructor.anim_idle_wonder,
                    instructor.anim_true_nodA,
                    instructor.anim_true_nodB,
                    instructor.anim_true_smileA,
                    instructor.anim_true_smileB,
                };

            // Give a short delay before playing the animation
            float nextAnimTime = Time.fixedTime + 0.3f;
            bool doneFirstYet = false;
            var random = new System.Random();

            var guiImage = new DialogGUIImage(new Vector2(width, height), new Vector2(0, 0), Color.gray, instructorTexture);
            guiImage.OnUpdate = () =>
            {
                // Play the animation
                if (nextAnimTime <= Time.fixedTime)
                {
                    CharacterAnimationState nowPlaying;
                    if (!doneFirstYet)
                    {
                        nowPlaying = initialAnimations[random.Next(initialAnimations.Count)];
                        instructor.PlayEmote(nowPlaying);
                        doneFirstYet = true;
                    }
                    else
                    {
                        nowPlaying = vampingAnimations[random.Next(vampingAnimations.Count)];
                        instructor.PlayEmote(nowPlaying, instructor.anim_idle, playSound: false);
                    }
                    nextAnimTime = Time.fixedTime + nowPlaying.clip.length + 1.0f;
                }
            };

            return guiImage;
        }
    }
}
