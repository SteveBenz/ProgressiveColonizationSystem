using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{
    /// <summary>
    ///   This class maintains a toolbar button and a GUI display that allows the user to see
    ///   into the life support status of the active vessel.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    class LifeSupportStatusMonitor
        : ScenarioModule
    {
        [KSPField(isPersistant = true)]
        public Rect extent = new Rect(100, 100, 800, 300);

        [KSPField(isPersistant = true)]
        public bool isVisible = false;

        // If simulating + or - crewman, this becomes positive or negative.
        private int crewDelta = 0;

        // CrewDelta gets reset when lastActiveVessel no longer equals the current vessel.
        private Vessel lastActiveVessel;

        private ApplicationLauncherButton toolbarButton;

        private bool toolbarStateMatchedToIsVisible;

        public override void OnAwake()
        {
            base.OnAwake();

            AttachToToolbar();
        }

        private void AttachToToolbar()
        {
            if (this.toolbarButton != null)
            {
                // defensive
                return;
            }

            Texture2D texture2D = new Texture2D(36, 36, TextureFormat.ARGB32, false);
            texture2D.LoadImage(Properties.Resources.AppLauncherIcon);

            Debug.Assert(ApplicationLauncher.Ready, "ApplicationLauncher is not ready - can't add the toolbar button.  Is this possible, really?  If so maybe we could do it later?");
            this.toolbarButton = ApplicationLauncher.Instance.AddModApplication(
                () => {
                    isVisible = true;
                }, () => {
                    isVisible = false;
                }, null, null, null, null,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                texture2D);
            toolbarStateMatchedToIsVisible = false;
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

        public void OnGUI()
        {
            if (!this.toolbarStateMatchedToIsVisible)
            {
                this.toolbarButton.toggleButton.Value = this.isVisible;
                this.toolbarStateMatchedToIsVisible = true;
            }

            if (!this.isVisible)
            {
                return;
            }

            //windowPos = ClickThruBlocker.GUILayoutWindow(99977, windowPos, DebugModeDialog, "Debug modes");
            this.extent = GUI.Window(GetInstanceID(), this.extent, WindowCallback, "Life Support Status");
        }

        private void WindowCallback(int windowId)
        {
            var activeSnackConsumption = FlightGlobals.ActiveVessel?.GetComponent<SnackConsumption>();
            if (activeSnackConsumption == null)
            {
                GUILayout.Label("No active vessel.");
                return;
            }

            if (activeSnackConsumption.Vessel != this.lastActiveVessel)
            {
                this.crewDelta = 0;
                this.lastActiveVessel = activeSnackConsumption.Vessel;
            }

            GUILayout.BeginHorizontal();

            activeSnackConsumption.ResourceQuantities(out var availableResources, out var availableStorage);
            List<IProducer> snackProducers = activeSnackConsumption.Vessel.FindPartModulesImplementing<IProducer>();
            int crewCount = activeSnackConsumption.Vessel.GetCrewCount();

            // If there are no top-tier supplies, no producers, and no crew
            if (crewCount == 0
             && snackProducers.Count == 0
             && !availableResources.ContainsKey("Snacks"))
            {
                GUILayout.Label("Oy!  Robots don't eat!");
                // "This ship apparently ate all its crew"
            }
            else
            {
                DrawDialog(activeSnackConsumption, availableResources, availableStorage, snackProducers, crewCount);
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
        }


        private void DrawDialog(
            SnackConsumption activeSnackConsumption,
            Dictionary<string, double> resources,
            Dictionary<string, double> storage,
            List<IProducer> snackProducers,
            int crewCount)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("POP!"))
            {
                ShowPopup();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("What if we ");
            if (GUILayout.Button("add"))
            {
                ++crewDelta;
            }
            if (crewCount + crewDelta > 1)
            {
                GUILayout.Label(" / ");
                if (GUILayout.Button("remove"))
                {
                    --crewDelta;
                }
                GUILayout.Label(" a kerbal?");
            }
            GUILayout.EndHorizontal();

            if (crewCount + crewDelta == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("With no crew aboard, not much is going on life-support wise...");
                GUILayout.EndHorizontal();
            }
            else
            {
                ResearchSink researchSink = new ResearchSink();
                TieredProduction.CalculateResourceUtilization(
                    crewCount + crewDelta, 1, snackProducers, researchSink, resources, storage,
                    out double timePassed, out bool _, out Dictionary<string, double> resourcesConsumed,
                    out Dictionary<string, double> resourcesProduced);
                if (timePassed == 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("There aren't enough supplies or producers here to feed any kerbals.");
                    GUILayout.EndHorizontal();

                    Debug.Assert(LifeSupportScenario.Instance != null);
                    if (LifeSupportScenario.Instance != null)
                    {
                        // TODO: Somehow bucketize this, since all the crew are likely in the same state.
                        foreach (var crew in activeSnackConsumption.Vessel.GetVesselCrew())
                        {
                            GUILayout.BeginHorizontal();
                            var kerbalIsKnown = LifeSupportScenario.Instance.TryGetStatus(crew, out double daysSinceMeal, out double daysToGrouchy, out bool isGrouchy);
                            if (!kerbalIsKnown)
                            {
                                // TODO: Maybe if ! on kerban we complain about this?
                                // Debug.LogError($"Couldn't find a life support record for {crew.name}");
                            }

                            if (isGrouchy)
                            {
                                GUILayout.Label($"{crew.name} hasn't eaten in {(int)daysSinceMeal} days and is too grouchy to work");
                            }
                            else if (daysToGrouchy > 5)
                            {
                                GUILayout.Label($"{crew.name} is secretly munching a smuggled bag of potato chips");
                            }
                            else if (daysToGrouchy < 2)
                            {
                                GUILayout.Label($"{crew.name} hasn't eaten in {(int)daysSinceMeal} days and will quit working in a couple of days if this keeps up.");
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (crewDelta == 0)
                    {
                        GUILayout.Label($"To sustain its crew of {crewCount + crewDelta}, this vessel is using:");
                    }
                    else
                    {
                        GUILayout.Label($"To sustain a crew of {crewCount + crewDelta} this vessel would use:");
                    }
                    GUILayout.EndHorizontal();


                    // TODO:
                    //  ((**If the crew has active producers but no fertilizer consumption for the given tier:))
                    //      The Nom-o-tron(Tier3) is offline for lack of appropriate fertilizer
                    //  


                    foreach (var resourceName in resourcesConsumed.Keys.OrderBy(n => n))
                    {
                        double perDay = TieredProduction.UnitsPerSecondToUnitsPerDay(resourcesConsumed[resourceName]);
                        double daysLeft = resources[resourceName] / perDay;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{perDay:N1} {resourceName} per day ({daysLeft:N1} days left)");
                        GUILayout.EndHorizontal();
                    }

                    if (resourcesProduced != null && resourcesProduced.Count > 0)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("The crew is also producing:");
                        GUILayout.EndHorizontal();
                        foreach (var resourceName in resourcesProduced.Keys.OrderBy(n => n))
                        {
                            double perDay = TieredProduction.UnitsPerSecondToUnitsPerDay(resourcesProduced[resourceName]);
                            double daysLeft = resources[resourceName] / perDay;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label($"{perDay:N1} {resourceName} per day");
                            GUILayout.EndHorizontal();
                        }
                    }

                    foreach (var pair in researchSink.Data)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"This vessel {(crewDelta == 0 ? "is contributing" : "would contribute")} {pair.Value.KerbalDaysContributedPerDay:N1} units of {pair.Key.DisplayName} research per day.  ({pair.Value.KerbalDaysUntilNextTier:N} are needed to reach the next tier).");
                        GUILayout.EndHorizontal();
                    }

                    // TODO: <IN VAB>
                    // Balance supply load for a trip duration of:
                    // [---------------------------*------------] kerbal days
                }
            }
            GUILayout.EndVertical();
        }

        private void ShowPopup()
        {
            // .25,.5 x .5,.75  yielded a placement around .75-1.2x by .3-.5y
            var menu = PopupDialog.SpawnPopupDialog(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new MultiOptionDialog(
                    "TierUpAlert",
                    "Tier Up Baby!  what if it's super long and has a lot of lines adnf;lkjsdaf lkjdalf;k jads;lkjfda;lkfdaf\r\n"
                    + "I mean a lotta lotta lines.\r\n"
                    + "I mean a lotta lotta lines.\r\n"
                    + "I mean a lotta lotta lines.\r\n"
                    + "I mean a lotta lotta lines.\r\n"
                    + "I mean a lotta lotta lines.\r\n"
                    + "I mean a lotta lotta lines.\r\n"
                    + "sfal;jdasf lksadfj;lfdas lkf;asdlk fdj l;fdas jklfd; ;fjkdlj;fkd fjdkj lfkd lkf jksdfl kfsdljlksdf dfkjsdl kjsfdljfsd lk fsdklf sdjklfdjs fdsjkl fsdlkfdsljf sdlfdl sjkfds ljk",
                    "Whoah a title!",
                    HighLogic.UISkin,
                    new DialogGUIVerticalLayout(
                        new DialogGUIHorizontalLayout(
                            new DialogGUIFlexibleSpace(),
                            this.makeInstructor(),
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

        private KerbalInstructor instructor = null;
        RenderTexture instructorTexture;
        GameObject lightGameObject = null;
        static float offset = 0.0f;
        string characterName = "Frodo";
        GUIStyle labelStyle;

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


        protected void DisplayName(float width)
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(UnityEngine.GUI.skin.label);
                labelStyle.alignment = TextAnchor.UpperCenter;
                //labelStyle.normal.textColor = textColor;
                labelStyle.fontStyle = FontStyle.Bold;
            }

            GUILayout.Label(characterName, labelStyle, GUILayout.Width(width));
        }

        private List<CharacterAnimationState> initialAnimations;
        private List<CharacterAnimationState> vampingAnimations;
        float nextAnimTime = float.MaxValue;
        bool doneFirstYet = false;

        System.Random random = new System.Random();


        private DialogGUIImage makeInstructor()
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

            DialogGUIImage box = new DialogGUIImage(new Vector2(128,128), new Vector2(0,0), Color.gray, instructorTexture);
            box.OnUpdate = () =>
            {

                // Play the animation
                if (nextAnimTime <= Time.fixedTime)
                {
                    CharacterAnimationState nowPlaying;
                    if (this.doneFirstYet)
                    {
                        nowPlaying = initialAnimations[this.random.Next(initialAnimations.Count)];
                        instructor.PlayEmote(nowPlaying);
                        this.doneFirstYet = true;
                    }
                    else
                    {
                        nowPlaying = vampingAnimations[this.random.Next(vampingAnimations.Count)];
                        instructor.PlayEmote(nowPlaying, instructor.anim_idle, playSound: false);
                    }
                    //animState.audioClip = null;
                    nextAnimTime = Time.fixedTime + nowPlaying.clip.length + 1.0f;
                }
            };

            return box;
        }


        private class ResearchData
        {
            public double KerbalDaysContributedPerDay;
            public double KerbalDaysUntilNextTier;
        }


        private class ResearchSink
            : IColonizationResearchScenario
        {
            public Dictionary<ResearchCategory, ResearchData> Data { get; } = new Dictionary<ResearchCategory, ResearchData>();

            bool IColonizationResearchScenario.ContributeResearch(TieredResource source, string atBody, double timespentInKerbalSeconds)
            {
                if (!this.Data.TryGetValue(source.ResearchCategory, out ResearchData data))
                {
                    data = new ResearchData();
                    this.Data.Add(source.ResearchCategory, data);
                    data.KerbalDaysUntilNextTier = ColonizationResearchScenario.Instance.GetKerbalDaysUntilNextTier(source, atBody);
                }

                // KerbalDaysContributedPerDay is equal to Kerbals.
                // timeSpentInKerbalSeconds works out to be time spent in a kerbal second (because that's the timespan
                // we passed into the production engine), so it's really kerbalSecondsContributedPerKerbalSecond.
                data.KerbalDaysContributedPerDay = timespentInKerbalSeconds;
                return false;
            }

            TechTier IColonizationResearchScenario.GetMaxUnlockedTier(TieredResource forResource, string atBody)
                => ColonizationResearchScenario.Instance.GetMaxUnlockedTier(forResource, atBody);

            bool IColonizationResearchScenario.TryParseTieredResourceName(string tieredResourceName, out TieredResource resource, out TechTier tier)
                => ColonizationResearchScenario.Instance.TryParseTieredResourceName(tieredResourceName, out resource, out tier);
        }
    }
}
