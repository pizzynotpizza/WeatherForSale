#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeatherForSale.Obby;
using WeatherForSale.UI;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Editor-only tool that procedurally builds the first obby test level from primitives
    /// (platforms, checkpoints, finish, lava, player, camera, HUD, Game Over panel, touch controls),
    /// creates the <see cref="ObbyLevelConfig"/> asset if missing, saves Assets/Scenes/Obby_Test.unity
    /// and adds it to the Build Settings. Run from the menu or with
    /// <c>-executeMethod WeatherForSale.EditorTools.ObbyTestSceneBuilder.BuildFromCommandLine</c>.
    /// The same layout is also built once per map (Obby_Rainforest_1, Obby_Valley_1, Obby_Desert_1,
    /// Obby_Glacier_1), recoloured for the climate and with its own config copy, so each map on the
    /// shop counter loads its own level. These are starter levels: story 004 replaces the layouts,
    /// adds the second level, the PDF obstacles and the KayKit dressing.
    /// Implements: story-001 (Files: Assets/Scenes/Obby_Test.unity); story-002 criterion 5.
    /// </summary>
    public static class ObbyTestSceneBuilder
    {
        private const string ConfigPath = "Assets/Data/ObbyLevelConfig_Level1.asset";
        private const string MaterialFolder = "Assets/Materials/Obby";

        // Player character (Meshy model, rigged at 1.4 m, Humanoid) and its clips.
        private const string SamFolder = "Assets/Art/Characters/Sam/";
        private const string SamRigPath = SamFolder + "Sam_Rigged.fbx";
        private const string SamControllerPath = SamFolder + "Sam.controller";
        private const float SamHeight = 1.4f;
        // Jump clip trimmed to just after the crouch through the landing (frames 6..46 of 56),
        // played faster so it roughly matches the ~0.7 s airtime of jumpSpeed 9 / gravity 25.
        private const float JumpFirstFrame = 6f;
        private const float JumpLastFrame = 46f;
        private const float JumpStateSpeed = 1.8f;

        // Level colours (distinct per element, for readability).
        // Checkpoint, finish and player colours stay the same on every map so they always read the same.
        private static readonly Color CheckpointColor = new Color(1.00f, 0.85f, 0.20f);
        private static readonly Color FinishColor = new Color(0.90f, 0.20f, 0.80f);
        private static readonly Color PlayerColor = new Color(1.00f, 0.45f, 0.10f);

        /// <summary>Per-level look: scene, config and the climate colours.</summary>
        private struct ObbyTheme
        {
            public string Id, ScenePath, ConfigPath, HazardName;
            public Color Platform, Start, Hazard, Sky, Ambient, Sun;
            /// <summary>Top of the hazard slab, metres. Deep (e.g. -60) reads as a bottomless chasm;
            /// the config kill height still respawns the player long before they reach it.</summary>
            public float HazardTopY;

            public ObbyTheme(string id, string scenePath, string configPath, string hazardName,
                Color platform, Color start, Color hazard, Color sky, Color ambient, Color sun, float hazardTopY = -6f)
            {
                Id = id; ScenePath = scenePath; ConfigPath = configPath; HazardName = hazardName;
                Platform = platform; Start = start; Hazard = hazard; Sky = sky; Ambient = ambient; Sun = sun;
                HazardTopY = hazardTopY;
            }
        }

        private static readonly ObbyTheme TestTheme = new ObbyTheme("Test", "Assets/Scenes/Obby_Test.unity", ConfigPath, "Lava",
            new Color(0.72f, 0.74f, 0.80f), new Color(0.20f, 0.60f, 0.70f), new Color(0.90f, 0.15f, 0.10f),
            new Color(0.53f, 0.78f, 0.95f), new Color(0.62f, 0.66f, 0.72f), Color.white);

        // Scene names must match MapCatalog sceneName (ShopSceneBuilder.Maps).
        // Colours sampled from the user's map reference paintings (2026-11-23): Rainforest = mossy
        // cliffs over a turquoise river under a misty rain sky; Valley = orange sandstone canyon over a
        // cloud-filled chasm; Desert = golden dunes and red rock under a bright sky; Glacier = snow-capped
        // grey cliffs above icy water.
        private static readonly ObbyTheme[] MapThemes =
        {
            new ObbyTheme("Rainforest", "Assets/Scenes/Obby_Rainforest_1.unity", "Assets/Data/ObbyLevelConfig_Rainforest_1.asset", "River",
                new Color(0.46f, 0.66f, 0.30f), new Color(0.44f, 0.48f, 0.50f), new Color(0.24f, 0.66f, 0.70f),
                new Color(0.68f, 0.77f, 0.84f), new Color(0.58f, 0.64f, 0.66f), new Color(0.90f, 0.95f, 1.00f)),
            new ObbyTheme("Valley", "Assets/Scenes/Obby_Valley_1.unity", "Assets/Data/ObbyLevelConfig_Valley_1.asset", "Chasm",
                new Color(0.88f, 0.52f, 0.30f), new Color(0.94f, 0.76f, 0.46f), new Color(0.80f, 0.78f, 0.88f),
                new Color(0.45f, 0.72f, 0.98f), new Color(0.72f, 0.66f, 0.64f), new Color(1.00f, 0.95f, 0.86f), -60f),
            new ObbyTheme("Desert", "Assets/Scenes/Obby_Desert_1.unity", "Assets/Data/ObbyLevelConfig_Desert_1.asset", "Quicksand",
                new Color(0.96f, 0.80f, 0.46f), new Color(0.82f, 0.52f, 0.38f), new Color(0.80f, 0.56f, 0.26f),
                new Color(0.45f, 0.72f, 0.98f), new Color(0.80f, 0.72f, 0.60f), new Color(1.00f, 0.95f, 0.80f)),
            new ObbyTheme("Glacier", "Assets/Scenes/Obby_Glacier_1.unity", "Assets/Data/ObbyLevelConfig_Glacier_1.asset", "IceWater",
                new Color(0.94f, 0.97f, 1.00f), new Color(0.56f, 0.62f, 0.70f), new Color(0.14f, 0.32f, 0.52f),
                new Color(0.50f, 0.75f, 0.98f), new Color(0.72f, 0.78f, 0.88f), new Color(1.00f, 0.98f, 0.92f)),
        };

        private struct PlatformSpec
        {
            public string Name;
            public float X, TopY, Z, SizeX, SizeZ;

            public PlatformSpec(string name, float x, float topY, float z, float sizeX, float sizeZ)
            {
                Name = name; X = x; TopY = topY; Z = z; SizeX = sizeX; SizeZ = sizeZ;
            }
        }

        // Plain stepping stones between the start pad, checkpoints and finish pad.
        // Gaps are at most 3 m and rises at most 0.5 m (moveSpeed 6 / jumpSpeed 9 / gravity 25 clears about 4 m).
        private static readonly PlatformSpec[] Stones =
        {
            new PlatformSpec("Stone_A", 0f, 0f, 7f, 3f, 3f),
            new PlatformSpec("Stone_B", 0f, 0.5f, 12f, 2.5f, 2.5f),
            new PlatformSpec("Stone_C", 3.5f, 1f, 15.5f, 2.5f, 2.5f),
            new PlatformSpec("Stone_D", 7.5f, 1f, 15.5f, 2.5f, 2.5f),
            new PlatformSpec("Stone_E", 12.5f, 1.5f, 22f, 2f, 2f),
            new PlatformSpec("Stone_F", 12.5f, 2f, 27f, 2f, 2f),
            new PlatformSpec("Stone_G", 8.5f, 2.5f, 29.5f, 2f, 2f),
            new PlatformSpec("Stone_H", 4.5f, 3f, 29.5f, 2f, 2f),
            new PlatformSpec("Stone_I", 0f, 3.5f, 35.5f, 2f, 2f),
            new PlatformSpec("Stone_J", -3.5f, 4f, 38.5f, 2f, 2f),
            new PlatformSpec("Stone_K", -3.5f, 4.5f, 43f, 2f, 2f),
            new PlatformSpec("Stone_L", 0f, 5f, 46f, 2f, 2f),
            new PlatformSpec("Stone_M", 3.5f, 5.5f, 46f, 2f, 2f),
        };

        /// <summary>Menu entry: builds and saves the obby test scene.</summary>
        [MenuItem("Weather For Sale/Build Obby Test Scene")]
        public static void BuildFromMenu()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(TestTheme);
        }

        /// <summary>Menu entry: builds one starter obby scene per map and adds them to the Build Settings.</summary>
        [MenuItem("Weather For Sale/Build Map Obby Scenes")]
        public static void BuildMapsFromMenu()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildMaps();
        }

        /// <summary>Builds every map's starter obby scene (also callable from batch mode).</summary>
        public static void BuildMaps()
        {
            foreach (ObbyTheme theme in MapThemes) Build(theme);
        }

        /// <summary>Batch-mode entry point (<c>-executeMethod</c>). Exits non-zero on failure.</summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                Build(TestTheme);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void Build(ObbyTheme theme)
        {
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Materials");
            EnsureFolder(MaterialFolder);

            // NewScene(Single) unloads unused assets, so load/create every asset reference AFTER it;
            // otherwise the config and materials loaded before it become destroyed (null) objects.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ObbyLevelConfig config = GetOrCreateConfig(theme.ConfigPath);

            string prefix = theme.Id == TestTheme.Id ? "Obby_" : "Obby_" + theme.Id + "_";
            Material platformMat = GetOrCreateMaterial(prefix + "Platform", theme.Platform);
            Material startMat = GetOrCreateMaterial(prefix + "Start", theme.Start);
            Material checkpointMat = GetOrCreateMaterial("Obby_Checkpoint", CheckpointColor);
            Material finishMat = GetOrCreateMaterial("Obby_Finish", FinishColor);
            Material lavaMat = GetOrCreateMaterial(prefix + theme.HazardName, theme.Hazard);
            Material playerMat = GetOrCreateMaterial("Obby_Player", PlayerColor);
            Material whiteMat = GetOrCreateMaterial("Obby_White", Color.white);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = theme.Ambient;

            // Light
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = theme.Sun;
            light.shadows = LightShadows.Hard;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Level geometry
            GameObject levelRoot = new GameObject("Level");

            GameObject startPlatform = CreatePlatform("StartPad", new PlatformSpec("StartPad", 0f, 0f, 0f, 6f, 6f), startMat, levelRoot.transform);

            foreach (PlatformSpec spec in Stones)
            {
                CreatePlatform(spec.Name, spec, platformMat, levelRoot.transform);
            }

            Checkpoint cp1 = CreateCheckpoint("Checkpoint_1", new PlatformSpec("Checkpoint_1", 12.5f, 1f, 15.5f, 5f, 5f), checkpointMat, levelRoot.transform);
            Checkpoint cp2 = CreateCheckpoint("Checkpoint_2", new PlatformSpec("Checkpoint_2", 0f, 3f, 29.5f, 5f, 5f), checkpointMat, levelRoot.transform);
            FinishTrigger finish = CreateFinish(new PlatformSpec("FinishPad", 9f, 5.5f, 46f, 5f, 5f), finishMat, whiteMat, levelRoot.transform);

            // Hazard (lava on the test level; swamp, river, quicksand or ice water on the maps): a trigger slab well below the platforms (top at y = -6, extends past the config kill height).
            GameObject lava = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lava.name = theme.HazardName + "_KillVolume";
            lava.transform.SetParent(levelRoot.transform, false);
            lava.transform.position = new Vector3(5f, theme.HazardTopY - 5f, 25f);
            // A deep chasm floor must reach the horizon, or the sky shows below the platforms.
            float hazardSize = theme.HazardTopY < -20f ? 700f : 200f;
            lava.transform.localScale = new Vector3(hazardSize, 10f, hazardSize);
            lava.GetComponent<MeshRenderer>().sharedMaterial = lavaMat;
            lava.GetComponent<BoxCollider>().isTrigger = true;
            KillVolume killVolume = lava.AddComponent<KillVolume>();

            // Spawn point
            GameObject startPoint = new GameObject("StartPoint");
            startPoint.transform.SetParent(levelRoot.transform, false);
            startPoint.transform.position = new Vector3(0f, 0f, -1f);
            startPoint.transform.rotation = Quaternion.identity;

            // Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = theme.Sky;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = theme.HazardTopY < -20f ? 450f : 250f;
            camGo.AddComponent<AudioListener>();
            ThirdPersonCamera thirdPersonCamera = camGo.AddComponent<ThirdPersonCamera>();

            // Player
            GameObject playerGo = new GameObject("Player");
            playerGo.transform.position = startPoint.transform.position + Vector3.up * config.respawnHeightOffset;
            CharacterController cc = playerGo.AddComponent<CharacterController>();
            cc.height = SamHeight;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, SamHeight * 0.5f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 50f;
            // A kinematic Rigidbody guarantees trigger callbacks reach the checkpoint / finish / lava volumes.
            Rigidbody body = playerGo.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            PlayerController player = playerGo.AddComponent<PlayerController>();

            AddPlayerVisual(playerGo, player, playerMat, whiteMat);

            SetRefs(player, ("config", config), ("cameraReference", camGo.transform));
            SetRefs(thirdPersonCamera, ("config", config), ("target", playerGo.transform));
            camGo.transform.position = playerGo.transform.position + new Vector3(0f, 4f, -7f);
            camGo.transform.LookAt(playerGo.transform.position + Vector3.up * 1.5f);

            // Level manager
            GameObject managerGo = new GameObject("LevelManager");
            ObbyLevelManager manager = managerGo.AddComponent<ObbyLevelManager>();
            SetRefs(manager,
                ("config", config),
                ("player", player),
                ("playerCamera", thirdPersonCamera),
                ("startPoint", startPoint.transform),
                ("finish", finish));
            SetRefArray(manager, "checkpoints", new Object[] { cp1, cp2 });
            SetRefArray(manager, "killVolumes", new Object[] { killVolume });

            // EventSystem (Input System UI module)
            GameObject esGo = new GameObject("EventSystem");
            EventSystem eventSystem = esGo.AddComponent<EventSystem>();
            eventSystem.pixelDragThreshold = 1; // so the first pixels of a stick drag are not swallowed
            InputSystemUIInputModule uiModule = esGo.AddComponent<InputSystemUIInputModule>();
            if (uiModule.actionsAsset == null) uiModule.AssignDefaultActions();

            BuildUi(manager, config, player, thirdPersonCamera);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, theme.ScenePath);
            AddToBuildSettings(theme.ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ObbyTestSceneBuilder] Built " + theme.ScenePath + " with config " + theme.ConfigPath
                      + " (startPad=" + startPlatform.name + ").");
        }

        // ------------------------------------------------------------------ level pieces

        private static GameObject CreatePlatform(string name, PlatformSpec spec, Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(spec.X, spec.TopY - 0.5f, spec.Z);
            go.transform.localScale = new Vector3(spec.SizeX, 1f, spec.SizeZ);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject CreateTriggerChild(string name, GameObject platform, PlatformSpec spec)
        {
            GameObject trigger = new GameObject(name + "_Trigger");
            trigger.transform.SetParent(platform.transform, false);
            trigger.transform.position = new Vector3(spec.X, spec.TopY, spec.Z);
            trigger.transform.rotation = Quaternion.identity;

            BoxCollider box = trigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            // Slightly smaller than the pad horizontally, 3 m tall above the surface (compensates the parent's scale).
            Vector3 parentScale = platform.transform.localScale;
            box.size = new Vector3((spec.SizeX - 0.4f) / parentScale.x, 3f / parentScale.y, (spec.SizeZ - 0.4f) / parentScale.z);
            box.center = new Vector3(0f, 1.5f / parentScale.y, 0f);
            return trigger;
        }

        private static Checkpoint CreateCheckpoint(string name, PlatformSpec spec, Material material, Transform parent)
        {
            GameObject platform = CreatePlatform(name, spec, material, parent);
            GameObject trigger = CreateTriggerChild(name, platform, spec);
            Checkpoint checkpoint = trigger.AddComponent<Checkpoint>();
            SetRefs(checkpoint, ("tintRenderer", platform.GetComponent<MeshRenderer>()));
            return checkpoint;
        }

        private static FinishTrigger CreateFinish(PlatformSpec spec, Material padMaterial, Material whiteMat, Transform parent)
        {
            GameObject platform = CreatePlatform("FinishPad", spec, padMaterial, parent);
            GameObject trigger = CreateTriggerChild("FinishPad", platform, spec);
            FinishTrigger finish = trigger.AddComponent<FinishTrigger>();

            // Simple flag so the goal is visible from a distance.
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "FinishPole";
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            pole.transform.SetParent(parent, false);
            pole.transform.position = new Vector3(spec.X, spec.TopY + 2.5f, spec.Z);
            pole.transform.localScale = new Vector3(0.2f, 2.5f, 0.2f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = whiteMat;

            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "FinishFlag";
            Object.DestroyImmediate(flag.GetComponent<Collider>());
            flag.transform.SetParent(parent, false);
            flag.transform.position = new Vector3(spec.X + 0.8f, spec.TopY + 4.2f, spec.Z);
            flag.transform.localScale = new Vector3(1.4f, 0.9f, 0.1f);
            flag.GetComponent<MeshRenderer>().sharedMaterial = padMaterial;

            return finish;
        }

        // ------------------------------------------------------------------ UI

        private static void BuildUi(ObbyLevelManager manager, ObbyLevelConfig config, PlayerController player, ThirdPersonCamera cam)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            GameObject canvasGo = new GameObject("ObbyCanvas", typeof(RectTransform));
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            ObbyHud hud = canvasGo.AddComponent<ObbyHud>();

            // --- Touch controls (drawn first = behind the HUD and panels)
            RectTransform pad = CreateRect("TouchControls", canvasGo.transform);
            Stretch(pad);
            Image padImage = pad.gameObject.AddComponent<Image>();
            padImage.color = new Color(0f, 0f, 0f, 0f);
            padImage.raycastTarget = false; // TouchControls switches it on for touch devices only
            TouchControls touch = pad.gameObject.AddComponent<TouchControls>();

            RectTransform visuals = CreateRect("Visuals", pad);
            Stretch(visuals);

            RectTransform stickBase = CreateRect("StickBase", visuals);
            SetCentered(stickBase, new Vector2(260f, 260f));
            Image baseImg = stickBase.gameObject.AddComponent<Image>();
            baseImg.sprite = knobSprite;
            baseImg.color = new Color(1f, 1f, 1f, 0.25f);
            baseImg.raycastTarget = false;

            RectTransform stickKnob = CreateRect("StickKnob", stickBase);
            SetCentered(stickKnob, new Vector2(120f, 120f));
            Image knobImg = stickKnob.gameObject.AddComponent<Image>();
            knobImg.sprite = knobSprite;
            knobImg.color = new Color(1f, 1f, 1f, 0.6f);
            knobImg.raycastTarget = false;

            RectTransform jump = CreateRect("JumpButton", visuals);
            jump.anchorMin = jump.anchorMax = new Vector2(1f, 0f);
            jump.pivot = new Vector2(0.5f, 0.5f);
            jump.anchoredPosition = new Vector2(-240f, 260f);
            jump.sizeDelta = new Vector2(240f, 240f);
            Image jumpImg = jump.gameObject.AddComponent<Image>();
            jumpImg.sprite = knobSprite;
            jumpImg.color = new Color(1f, 1f, 1f, 0.4f);
            jumpImg.raycastTarget = false;
            Text jumpLabel = CreateText("Label", jump, font, "JUMP", 48, TextAnchor.MiddleCenter, Color.white);
            Stretch(jumpLabel.rectTransform);

            SetRefs(touch,
                ("player", player),
                ("playerCamera", cam),
                ("visualsRoot", visuals.gameObject),
                ("stickBase", stickBase),
                ("stickKnob", stickKnob),
                ("jumpButton", jump));

            // --- HUD
            Text timerText = CreateText("TimerText", canvasGo.transform, font, "0:00", 80, TextAnchor.UpperCenter, Color.white);
            timerText.rectTransform.anchorMin = timerText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            timerText.rectTransform.pivot = new Vector2(0.5f, 1f);
            timerText.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            timerText.rectTransform.sizeDelta = new Vector2(500f, 110f);
            timerText.fontStyle = FontStyle.Bold;
            timerText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3f, -3f);

            Text fallsText = CreateText("FallsText", canvasGo.transform, font, "Falls: 0", 44, TextAnchor.UpperLeft, Color.white);
            fallsText.rectTransform.anchorMin = fallsText.rectTransform.anchorMax = new Vector2(0f, 1f);
            fallsText.rectTransform.pivot = new Vector2(0f, 1f);
            fallsText.rectTransform.anchoredPosition = new Vector2(32f, -32f);
            fallsText.rectTransform.sizeDelta = new Vector2(500f, 70f);
            fallsText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);

            // --- Game Over panel
            RectTransform gameOver = CreatePanel("GameOverPanel", canvasGo.transform, panelSprite, new Color(0f, 0f, 0f, 0.78f));
            Text goTitle = CreateText("Title", gameOver, font, "Game Over", 120, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.3f));
            PlaceCentered(goTitle.rectTransform, new Vector2(0f, 120f), new Vector2(1200f, 180f));
            Button goButton = CreateButton("RestartButton", gameOver, panelSprite, font, "Restart Level", new Vector2(0f, -80f), new Vector2(560f, 130f));
            gameOver.gameObject.SetActive(false);

            // --- Level complete panel
            RectTransform complete = CreatePanel("LevelCompletePanel", canvasGo.transform, panelSprite, new Color(0f, 0f, 0f, 0.7f));
            Text cTitle = CreateText("Title", complete, font, "Level Complete!", 120, TextAnchor.MiddleCenter, new Color(0.4f, 1f, 0.5f));
            PlaceCentered(cTitle.rectTransform, new Vector2(0f, 190f), new Vector2(1400f, 180f));
            Text cSummary = CreateText("Summary", complete, font, "", 56, TextAnchor.MiddleCenter, Color.white);
            PlaceCentered(cSummary.rectTransform, new Vector2(0f, 20f), new Vector2(1000f, 200f));
            Button cButton = CreateButton("PlayAgainButton", complete, panelSprite, font, "Play Again", new Vector2(0f, -170f), new Vector2(560f, 130f));
            complete.gameObject.SetActive(false);

            SetRefs(hud,
                ("level", manager),
                ("config", config),
                ("timerText", timerText),
                ("fallsText", fallsText),
                ("gameOverPanel", gameOver.gameObject),
                ("gameOverRestartButton", goButton),
                ("completePanel", complete.gameObject),
                ("completeSummaryText", cSummary),
                ("completeRestartButton", cButton));
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetCentered(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void PlaceCentered(RectTransform rt, Vector2 position, Vector2 size)
        {
            SetCentered(rt, size);
            rt.anchoredPosition = position;
        }

        private static Text CreateText(string name, Transform parent, Font font, string content, int size, TextAnchor anchor, Color color)
        {
            RectTransform rt = CreateRect(name, parent);
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rt = CreateRect(name, parent);
            Stretch(rt);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = true; // blocks the touch area behind while the panel is open
            return rt;
        }

        private static Button CreateButton(string name, Transform parent, Sprite sprite, Font font, string label, Vector2 position, Vector2 size)
        {
            RectTransform rt = CreateRect(name, parent);
            PlaceCentered(rt, position, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            Text text = CreateText("Label", rt, font, label, 56, TextAnchor.MiddleCenter, new Color(0.1f, 0.1f, 0.1f));
            Stretch(text.rectTransform);
            return button;
        }

        // ------------------------------------------------------------------ player visual

        /// <summary>
        /// Puts the rigged Sam model under the player with an Animator Controller (idle/walk/run blend +
        /// jump) driven by <see cref="PlayerAnimator"/>. Falls back to the old capsule + facing marker
        /// when the Sam assets are missing, so the level still builds.
        /// </summary>
        private static void AddPlayerVisual(GameObject playerGo, PlayerController player, Material playerMat, Material whiteMat)
        {
            GameObject samModel = AssetDatabase.LoadAssetAtPath<GameObject>(SamRigPath);
            AnimatorController controller = samModel != null ? GetOrCreateSamController() : null;
            if (samModel == null || controller == null)
            {
                Debug.LogWarning("[ObbyTestSceneBuilder] Sam model or clips missing under " + SamFolder + " — using the placeholder capsule.");
                AddPlaceholderBody(playerGo, playerMat, whiteMat);
                return;
            }

            var sam = (GameObject)PrefabUtility.InstantiatePrefab(samModel, playerGo.transform);
            sam.name = "Sam";
            // Rig root sits at the feet; drop it by the controller's skin width so the feet touch the
            // ground instead of hovering on the CharacterController's contact offset.
            CharacterController cc = playerGo.GetComponent<CharacterController>();
            sam.transform.localPosition = new Vector3(0f, cc != null ? -cc.skinWidth : 0f, 0f);
            sam.transform.localRotation = Quaternion.identity;
            Animator animator = sam.GetComponent<Animator>();
            if (animator == null) animator = sam.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            PlayerAnimator driver = playerGo.AddComponent<PlayerAnimator>();
            SetRefs(driver, ("player", player), ("animator", animator));
        }

        private static void AddPlaceholderBody(GameObject playerGo, Material playerMat, Material whiteMat)
        {
            GameObject body3d = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body3d.name = "Body";
            Object.DestroyImmediate(body3d.GetComponent<Collider>());
            body3d.transform.SetParent(playerGo.transform, false);
            body3d.transform.localPosition = new Vector3(0f, SamHeight * 0.5f, 0f);
            body3d.transform.localScale = new Vector3(0.6f, SamHeight * 0.5f, 0.6f);
            body3d.GetComponent<MeshRenderer>().sharedMaterial = playerMat;

            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "FacingMarker";
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(playerGo.transform, false);
            nose.transform.localPosition = new Vector3(0f, SamHeight * 0.75f, 0.28f);
            nose.transform.localScale = new Vector3(0.25f, 0.2f, 0.3f);
            nose.GetComponent<MeshRenderer>().sharedMaterial = whiteMat;
        }

        /// <summary>
        /// Builds (or rebuilds) Sam.controller: a Locomotion blend tree on "Speed" (0 idle, 0.35 walk,
        /// 1 run) and a Jump state entered by the "Jump" trigger or by falling, left on landing.
        /// Returns null when any clip is missing.
        /// </summary>
        private static AnimatorController GetOrCreateSamController()
        {
            TrimJumpClip();
            AnimationClip idle = LoadClip("Sam_Idle.fbx");
            AnimationClip walk = LoadClip("Sam_Walk.fbx");
            AnimationClip run = LoadClip("Sam_Run.fbx");
            AnimationClip jump = LoadClip("Sam_Jump.fbx");
            if (idle == null || walk == null || run == null || jump == null) return null;

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(SamControllerPath) != null)
                AssetDatabase.DeleteAsset(SamControllerPath); // regenerated on every build, never hand-edited
            AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(SamControllerPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ac.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            AnimatorState locomotion = ac.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 0.35f);
            tree.AddChild(run, 1f);
            sm.defaultState = locomotion;

            AnimatorState jumpState = sm.AddState("Jump");
            jumpState.motion = jump;
            jumpState.speed = JumpStateSpeed;

            AnimatorStateTransition toJump = locomotion.AddTransition(jumpState);
            toJump.hasExitTime = false;
            toJump.duration = 0.05f;
            toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

            AnimatorStateTransition toFall = locomotion.AddTransition(jumpState);
            toFall.hasExitTime = false;
            toFall.duration = 0.15f;
            toFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            toFall.AddCondition(AnimatorConditionMode.Less, -4f, "VerticalSpeed");

            AnimatorStateTransition land = jumpState.AddTransition(locomotion);
            land.hasExitTime = false;
            land.duration = 0.1f;
            land.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            land.AddCondition(AnimatorConditionMode.Less, 0f, "VerticalSpeed");

            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            return ac;
        }

        private static void TrimJumpClip()
        {
            var importer = AssetImporter.GetAtPath(SamFolder + "Sam_Jump.fbx") as ModelImporter;
            if (importer == null) return;
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips.Length == 0) clips = importer.defaultClipAnimations;
            if (clips.Length == 0) return;
            if (Mathf.Approximately(clips[0].firstFrame, JumpFirstFrame) && Mathf.Approximately(clips[0].lastFrame, JumpLastFrame)) return;
            clips[0].firstFrame = JumpFirstFrame;
            clips[0].lastFrame = JumpLastFrame;
            clips[0].loopTime = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip LoadClip(string file)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(SamFolder + file))
            {
                if (o is AnimationClip clip && !clip.name.StartsWith("__preview")) return clip;
            }
            return null;
        }

        // ------------------------------------------------------------------ assets / settings

        private static ObbyLevelConfig GetOrCreateConfig(string path)
        {
            ObbyLevelConfig config = AssetDatabase.LoadAssetAtPath<ObbyLevelConfig>(path);
            if (config != null) return config; // keep designer tuning

            // A map's first config starts as a copy of the tuned test level's (time, camera, movement).
            if (path != ConfigPath && AssetDatabase.LoadAssetAtPath<ObbyLevelConfig>(ConfigPath) != null
                && AssetDatabase.CopyAsset(ConfigPath, path))
            {
                return AssetDatabase.LoadAssetAtPath<ObbyLevelConfig>(path);
            }

            config = ScriptableObject.CreateInstance<ObbyLevelConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true)); // the Shop (ShopSceneBuilder) stays scene 0
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ serialization helpers

        private static void SetRefs(Object target, params (string field, Object value)[] values)
        {
            SerializedObject so = new SerializedObject(target);
            foreach ((string field, Object value) in values)
            {
                SerializedProperty prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogError("[ObbyTestSceneBuilder] Missing serialized field '" + field + "' on " + target.GetType().Name);
                    continue;
                }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefArray(Object target, string field, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
