using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nerm.Colonization
{
    public static class PopupMessageWithKerbal
    {
        public static void ShowPopup(string title, string content)
        {
            // .25,.5 x .5,.75  yielded a placement around .75-1.2x by .3-.5y
            var menu = PopupDialog.SpawnPopupDialog(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new MultiOptionDialog(
                    "TierUpAlert",
                    content,
                    title,
                    HighLogic.UISkin,
                    new DialogGUIVerticalLayout(
                        new DialogGUIHorizontalLayout(
                            new DialogGUIFlexibleSpace(),
                            makePictureOfAKerbal(160,160),
                            new DialogGUIButton("Sprinkles!", () =>
                            {
                                Debug.Log("Sprinkles happened");
                            }),
                            new DialogGUIFlexibleSpace()
                        ))),
                persistAcrossScenes: false,
                skin: HighLogic.UISkin,
                isModal: true,
                titleExtra: "TITLE EXTRA!");
        }

        private static DialogGUIImage makePictureOfAKerbal(int width, int height)
        {
            GameObject genesPrefab = AssetBase.GetPrefab("Instructor_Gene");
            var instantiatedGene = UnityEngine.Object.Instantiate(genesPrefab);
            KerbalInstructor instructor = instantiatedGene.GetComponent<KerbalInstructor>();

            RenderTexture instructorTexture = new RenderTexture(width, height, 8);
            instructor.instructorCamera.targetTexture = instructorTexture;
            instructor.instructorCamera.ResetAspect();

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

            DialogGUIImage box = new DialogGUIImage(new Vector2(width, height), new Vector2(0, 0), Color.gray, instructorTexture);
            box.OnUpdate = () =>
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

            return box;
        }
    }
}
