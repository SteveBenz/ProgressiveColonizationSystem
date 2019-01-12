using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                            makeInstructor(),
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

        private static KerbalInstructor instructor = null;
        private static RenderTexture instructorTexture;
        private static GameObject lightGameObject = null;
        private static float offset = 0.0f;
        private static string characterName = "Frodo";
        private static GUIStyle labelStyle;

        private static Material _portraitRenderMaterial = null;
        public static Material PortraitRenderMaterial
        {
            get
            {
                if (_portraitRenderMaterial == null)
                {
                    _portraitRenderMaterial = AssetBase.GetPrefab("Instructor_Gene").GetComponent<KerbalInstructor>().PortraitRenderMaterial;
                }
                return _portraitRenderMaterial;
            }
        }

        private static List<CharacterAnimationState> initialAnimations;
        private static List<CharacterAnimationState> vampingAnimations;
        private static float nextAnimTime = float.MaxValue;
        private static bool doneFirstYet = false;

        private static System.Random random = new System.Random();


        private static DialogGUIImage makeInstructor()
        {
            if (instructor == null)
            {
                GameObject o = AssetBase.GetPrefab("Instructor_Gene");
                var i = UnityEngine.Object.Instantiate(o);
                instructor = i.GetComponent<KerbalInstructor>();
                //instructor = ((GameObject)UnityEngine.Object.Instantiate(AssetBase.GetPrefab(name))).GetComponent<KerbalInstructor>();

                instructorTexture = new RenderTexture(128, 128, 8);
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

                offset += 25f;
                instructor.gameObject.transform.Translate(offset, 0.0f, 0.0f);

                // Add a light
                lightGameObject = new GameObject("Dialog Box Light");
                Light lightComp = lightGameObject.AddComponent<Light>();
                lightComp.color = new Color(0.4f, 0.4f, 0.4f);
                lightGameObject.transform.position = instructor.instructorCamera.transform.position;

                //if (string.IsNullOrEmpty(characterName))
                //{
                //    characterName = Localizer.GetStringByTag(instructor.CharacterName);
                //}

                instructor.SetupAnimations();

                initialAnimations = new List<CharacterAnimationState>()
                    {
                        instructor.anim_true_thumbsUp,
                        instructor.anim_true_thumbUp,
                        instructor.anim_true_nodA,
                        instructor.anim_true_nodB,
                        instructor.anim_true_smileA,
                        instructor.anim_true_smileB,
                    };
                vampingAnimations = new List<CharacterAnimationState>()
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
                nextAnimTime = Time.fixedTime + 0.3f;
            }

            DialogGUIImage box = new DialogGUIImage(new Vector2(128, 128), new Vector2(0, 0), Color.gray, instructorTexture);
            box.OnUpdate = () =>
            {
                // Play the animation
                if (nextAnimTime <= Time.fixedTime)
                {
                    CharacterAnimationState nowPlaying;
                    if (doneFirstYet)
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
                    //animState.audioClip = null;
                    nextAnimTime = Time.fixedTime + nowPlaying.clip.length + 1.0f;
                }
            };

            return box;
        }
    }
}
