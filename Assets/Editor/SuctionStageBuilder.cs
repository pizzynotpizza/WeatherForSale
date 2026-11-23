#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using WeatherForSale.Core;
using WeatherForSale.Obby;
using WeatherForSale.Suction;
using WeatherForSale.UI;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Editor-only helper used by <see cref="ObbyTestSceneBuilder"/> for every map's level-2 scene: adds
    /// the in-place suction stage (SuctionSpot, weather orb, two streak particle systems, the
    /// <see cref="SuctionStage"/> and <see cref="SuctionMinigame"/>) and its UI on the obby canvas
    /// (key prompt lower-left, Collecting gauge lower-right, tap area, Weather Collected screen,
    /// Complete screen), styled with the Cozy UI pack like the shop. Creates SuctionConfig,
    /// SuctionStrings and the orb / streak materials when missing (designer tuning is kept).
    /// Replaces the earlier separate Suction scene builder.
    /// Implements: story-003 change request (suction in place at the end of level 2).
    /// </summary>
    public static class SuctionStageBuilder
    {
        private const string DataFolder = "Assets/Data/Suction";
        private const string ConfigPath = DataFolder + "/SuctionConfig.asset";
        private const string StringsPath = DataFolder + "/SuctionStrings.asset";
        private const string MaterialFolder = "Assets/Materials/Suction";
        private const string OrbMaterialPath = MaterialFolder + "/Suction_Orb.mat";
        private const string StreakMaterialPath = MaterialFolder + "/Suction_Streak.mat";
        private const string BottlePrefabPath = "Assets/Art/Props/Bottle/Bottle.prefab";

        private const string Cozy = "Assets/Cozy_UI_Pack_doboui/";
        private const string WoodPanelPath = Cozy + "Containers/WoodenContainers/WoodenContainer1.png";
        private const string HeaderPath = Cozy + "Headers/Header1.png";
        private const string LabelPath = Cozy + "Labels/Label1.png";
        private const string RoundWoodPath = Cozy + "Buttons/Round/RoundButton1_wood.png";
        private const string RoundCreamPath = Cozy + "Buttons/Round/RoundButton1_cream.png";
        private const string ButtonBluePath = Cozy + "Buttons/Inflated/InflatedButton1_blue.png";
        private const string BarBackgroundPath = Cozy + "ProgressBars/ProgressBar/ProgressBarBackground.png";
        private const string BarFillPath = Cozy + "ProgressBars/ProgressBar/ProgressBar1.png";
        private const string IconStarPath = Cozy + "Icons/IconsDefault/128px/IconDefault8_128px.png";
        private const string IconStopwatchPath = Cozy + "Icons/IconsDefault/128px/IconDefault10_128px.png";
        private const string IconGemPath = Cozy + "Icons/IconsDefault/128px/IconDefault23_128px.png";

        private static readonly Color DarkBrownText = new Color(0.30f, 0.17f, 0.09f);
        private static readonly Color OutlineColor = new Color(0.2f, 0.12f, 0.06f, 0.9f);

        /// <summary>Starting per-map orb and streak colours for a newly created SuctionConfig; tune in the asset.</summary>
        private static readonly (string id, Color orb, Color streak)[] MapColours =
        {
            ("rainforest", new Color(0.30f, 0.78f, 0.62f, 0.55f), new Color(0.75f, 1.00f, 0.90f, 0.85f)),
            ("valley", new Color(0.85f, 0.88f, 1.00f, 0.50f), new Color(1.00f, 1.00f, 1.00f, 0.85f)),
            ("desert", new Color(1.00f, 0.78f, 0.30f, 0.55f), new Color(1.00f, 0.93f, 0.60f, 0.85f)),
            ("glacier", new Color(0.70f, 0.88f, 1.00f, 0.55f), new Color(0.90f, 0.97f, 1.00f, 0.85f)),
        };

        private sealed class UiSprites
        {
            public Sprite WoodPanel, Header, Label, RoundWood, RoundCream, ButtonBlue, BarBackground, BarFill, Star, Stopwatch, Gem;
        }

        /// <summary>What the level-2 scene hands over to build the stage.</summary>
        public struct StageInputs
        {
            /// <summary>Obby canvas the suction UI is added to.</summary>
            public Transform Canvas;
            /// <summary>Where Sam stands during suction (on the finish pad).</summary>
            public Vector3 SpotPosition;
            /// <summary>Way Sam faces during suction, degrees.</summary>
            public float SpotYaw;
            /// <summary>The player.</summary>
            public PlayerController Player;
            /// <summary>The follow camera component (disabled during suction).</summary>
            public ThirdPersonCamera FollowCamera;
            /// <summary>The scene camera's transform.</summary>
            public Transform ViewCamera;
            /// <summary>The scene's loop controller.</summary>
            public GameLoopController Loop;
            /// <summary>Obby HUD pieces hidden during suction.</summary>
            public GameObject[] HideDuringSuction;
        }

        /// <summary>Builds the stage into the open scene and returns its <see cref="SuctionStage"/>.</summary>
        public static SuctionStage Build(StageInputs inputs)
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(DataFolder);
            EnsureFolder("Assets/Materials");
            EnsureFolder(MaterialFolder);

            SuctionConfig config = GetOrCreateConfig();
            SuctionStrings strings = GetOrCreateAsset<SuctionStrings>(StringsPath);
            Material orbMaterial = GetOrCreateOrbMaterial();
            Material streakMaterial = GetOrCreateStreakMaterial();
            GameObject bottlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BottlePrefabPath);
            if (bottlePrefab == null) Debug.LogWarning("[SuctionStageBuilder] Bottle prefab not found: " + BottlePrefabPath);

            // --- 3D
            GameObject root = new GameObject("SuctionStage");
            GameObject spot = new GameObject("SuctionSpot");
            spot.transform.SetParent(root.transform, false);
            spot.transform.SetPositionAndRotation(inputs.SpotPosition, Quaternion.Euler(0f, inputs.SpotYaw, 0f));

            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "WeatherOrb";
            Object.DestroyImmediate(orb.GetComponent<Collider>());
            orb.transform.SetParent(root.transform, false);
            MeshRenderer orbRenderer = orb.GetComponent<MeshRenderer>();
            orbRenderer.sharedMaterial = orbMaterial;
            orbRenderer.shadowCastingMode = ShadowCastingMode.Off;
            orbRenderer.receiveShadows = false;
            orb.SetActive(false);

            ParticleSystem surround = CreateStreaks("Streaks_Surround", root.transform, streakMaterial, config);
            ParticleSystem fromOrb = CreateStreaks("Streaks_FromOrb", root.transform, streakMaterial, config);

            SuctionStage stage = root.AddComponent<SuctionStage>();
            SuctionMinigame minigame = root.AddComponent<SuctionMinigame>();

            // --- UI
            UiSprites sprites = LoadUiSprites();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUi(inputs.Canvas, font, sprites, config, strings, inputs.Loop, minigame);

            SetRefs(stage,
                ("config", config), ("minigame", minigame),
                ("player", inputs.Player), ("followCamera", inputs.FollowCamera), ("viewCamera", inputs.ViewCamera),
                ("spot", spot.transform), ("bottlePrefab", bottlePrefab),
                ("orb", orb.transform), ("orbRenderer", orbRenderer),
                ("surroundStreaks", surround), ("orbStreaks", fromOrb));
            SetRefArray(stage, "hideDuringSuction", inputs.HideDuringSuction ?? new GameObject[0]);
            return stage;
        }

        // ------------------------------------------------------------------ particles

        private static ParticleSystem CreateStreaks(string name, Transform parent, Material material, SuctionConfig config)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startSpeed = 0f;
            main.startSize = config.streakWidth;
            main.maxParticles = config.streakMaxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f; // SuctionStage sets the rate on Begin

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radiusThickness = 0f;

            // Fade in as they leave, a little dimmer as they reach the bottle.
            ParticleSystem.ColorOverLifetimeModule colour = ps.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0.2f, 1f) });
            colour.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.12f;
            renderer.lengthScale = 2.5f;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        // ------------------------------------------------------------------ UI

        private static void BuildUi(Transform canvas, Font font, UiSprites sprites, SuctionConfig config, SuctionStrings strings,
            GameLoopController loop, SuctionMinigame minigame)
        {
            // --- HUD root (hidden until the suction begins)
            RectTransform hud = CreateRect("SuctionHud", canvas);
            Stretch(hud);

            // PC prompt: key cap + big "Press W!" in one container that SuctionMinigame moves around
            // the screen (inside a stretched area, like the tap button).
            RectTransform keyArea = CreateRect("KeyArea", hud);
            Stretch(keyArea);
            RectTransform keyPrompt = CreateRect("KeyPrompt", keyArea);
            PlaceCentered(keyPrompt, new Vector2(0f, -200f), new Vector2(680f, 235f));
            RectTransform keyCap = CreateRect("KeyCap", keyPrompt);
            PlaceCorner(keyCap, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(220f, 235f));
            AddImage(keyCap, sprites.RoundWood, Color.white, false, false).preserveAspect = true;
            Text keyText = CreateText("Key", keyCap, font, "W", 130, TextAnchor.MiddleCenter, Color.white);
            Stretch(keyText.rectTransform);
            keyText.rectTransform.offsetMin = new Vector2(0f, 20f); // the round button has a lip at the bottom
            keyText.fontStyle = FontStyle.Bold;
            keyText.gameObject.AddComponent<Outline>().effectColor = OutlineColor;

            Text prompt = CreateText("Prompt", keyPrompt, font, "", 104, TextAnchor.MiddleLeft, Color.white);
            PlaceCorner(prompt.rectTransform, new Vector2(0f, 0.5f), new Vector2(240f, 0f), new Vector2(440f, 150f));
            prompt.fontStyle = FontStyle.Bold;
            Outline promptOutline = prompt.gameObject.AddComponent<Outline>();
            promptOutline.effectColor = OutlineColor;
            promptOutline.effectDistance = new Vector2(3f, -3f);

            // Title, top centre under the obby timer (which keeps counting during suction): "Collect the Weather!"
            Text title = CreateText("Title", hud, font, strings.collectTitle, 84, TextAnchor.MiddleCenter, Color.white);
            PlaceCorner(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1400f, 120f));
            title.fontStyle = FontStyle.Bold;
            Outline titleOutline = title.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = OutlineColor;
            titleOutline.effectDistance = new Vector2(3f, -3f);

            // Gauge, top centre under the title, percent inside the bar
            RectTransform bar = CreateRect("GaugeBar", hud);
            PlaceCorner(bar, new Vector2(0.5f, 1f), new Vector2(0f, -252f), new Vector2(720f, 72f));
            AddImage(bar, sprites.BarBackground, Color.white, true, false);
            RectTransform fillArea = CreateRect("FillArea", bar);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(8f, 8f);
            fillArea.offsetMax = new Vector2(-8f, -8f);
            RectTransform fill = CreateRect("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f); // SuctionMinigame drives anchorMax.x = gauge 0..1
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            AddImage(fill, sprites.BarFill, Color.white, true, false);
            Text percent = CreateText("Percent", bar, font, "0%", 44, TextAnchor.MiddleCenter, Color.white);
            Stretch(percent.rectTransform);
            percent.fontStyle = FontStyle.Bold;
            percent.gameObject.AddComponent<Outline>().effectColor = OutlineColor;

            // Touch tap button (positioned at runtime inside the safe margins of TapArea)
            RectTransform tapArea = CreateRect("TapArea", hud);
            Stretch(tapArea);
            RectTransform tap = CreateRect("TapButton", tapArea);
            PlaceCentered(tap, Vector2.zero, new Vector2(config.tapButtonSize, config.tapButtonSize));
            Image tapImage = AddImage(tap, sprites.RoundCream, Color.white, false, true);
            tapImage.preserveAspect = true;
            Button tapButton = tap.gameObject.AddComponent<Button>(); // press tint only; input is pointer-down in SuctionMinigame
            tapButton.targetGraphic = tapImage;
            Navigation nav = tapButton.navigation;
            nav.mode = Navigation.Mode.None;
            tapButton.navigation = nav;
            Text tapLabel = CreateText("Label", tap, font, strings.tapButtonLabel, 64, TextAnchor.MiddleCenter, DarkBrownText);
            Stretch(tapLabel.rectTransform);
            tapLabel.rectTransform.offsetMin = new Vector2(0f, 16f);
            tapLabel.fontStyle = FontStyle.Bold;
            tap.gameObject.SetActive(false);
            hud.gameObject.SetActive(false);

            // --- Weather Collected screen
            RectTransform collectedHost = CreateRect("WeatherCollected", canvas);
            Stretch(collectedHost);
            WeatherCollectedScreen collected = collectedHost.gameObject.AddComponent<WeatherCollectedScreen>();
            RectTransform collectedRoot = CreateOverlay("Root", collectedHost, 0.55f);
            RectTransform collectedCard = CreateCard("Card", collectedRoot, sprites.WoodPanel, new Vector2(0f, -20f), new Vector2(900f, 860f));
            Text cTitle = CreateHeader(collectedCard, sprites.Header, font, strings.weatherCollectedTitle);
            RectTransform cBottle = CreateRect("Bottle", collectedCard);
            PlaceCentered(cBottle, new Vector2(0f, 60f), new Vector2(380f, 460f));
            Image cBottleImage = AddImage(cBottle, null, Color.white, false, false);
            cBottleImage.preserveAspect = true;
            Text cMapName = CreateText("MapName", collectedCard, font, "", 56, TextAnchor.MiddleCenter, DarkBrownText);
            PlaceCentered(cMapName.rectTransform, new Vector2(0f, -220f), new Vector2(800f, 80f));
            cMapName.fontStyle = FontStyle.Bold;
            Button continueButton = CreateTextButton("ContinueButton", collectedCard, sprites.ButtonBlue, font, strings.continueLabel,
                new Vector2(0f, -330f), new Vector2(400f, 120f), Color.white, out Text continueLabel);
            SetRefs(collected, ("root", collectedRoot.gameObject), ("titleText", cTitle), ("bottleImage", cBottleImage),
                ("mapNameText", cMapName), ("continueButton", continueButton), ("continueLabel", continueLabel));
            collectedRoot.gameObject.SetActive(false);

            // --- Complete screen
            RectTransform completeHost = CreateRect("StageComplete", canvas);
            Stretch(completeHost);
            StageCompletePanel complete = completeHost.gameObject.AddComponent<StageCompletePanel>();
            RectTransform completeRoot = CreateOverlay("Root", completeHost, 0.55f);
            RectTransform card = CreateCard("Card", completeRoot, sprites.WoodPanel, new Vector2(0f, -20f), new Vector2(1000f, 860f));
            Text sTitle = CreateHeader(card, sprites.Header, font, strings.stageCompleteTitle);

            var stars = new Object[3];
            float[] starX = { -210f, 0f, 210f };
            float[] starY = { 190f, 220f, 190f }; // middle star a little higher
            for (int i = 0; i < 3; i++)
            {
                RectTransform star = CreateRect("Star_" + (i + 1), card);
                PlaceCentered(star, new Vector2(starX[i], starY[i]), new Vector2(180f, 180f));
                Image img = AddImage(star, sprites.Star, Color.white, false, false);
                img.preserveAspect = true;
                stars[i] = img;
            }

            CreateIcon("TimeIcon", card, sprites.Stopwatch, new Vector2(-330f, 10f));
            Text timeLabel = CreateText("TimeLabel", card, font, strings.timeLabel, 48, TextAnchor.MiddleLeft, DarkBrownText);
            PlaceCentered(timeLabel.rectTransform, new Vector2(-30f, 10f), new Vector2(480f, 80f));
            timeLabel.fontStyle = FontStyle.Bold;
            Text timeValue = CreateText("TimeValue", card, font, "00:00", 52, TextAnchor.MiddleRight, DarkBrownText);
            PlaceCentered(timeValue.rectTransform, new Vector2(250f, 10f), new Vector2(300f, 80f));
            timeValue.fontStyle = FontStyle.Bold;

            Image gemIcon = CreateIcon("KeyItemIcon", card, sprites.Gem, new Vector2(-330f, -120f));
            Text keyLabel = CreateText("KeyItemLabel", card, font, strings.keyItemLabel, 48, TextAnchor.MiddleLeft, DarkBrownText);
            PlaceCentered(keyLabel.rectTransform, new Vector2(-30f, -120f), new Vector2(480f, 80f));
            keyLabel.fontStyle = FontStyle.Bold;
            Text keyValue = CreateText("KeyItemValue", card, font, "0/1", 52, TextAnchor.MiddleRight, DarkBrownText);
            PlaceCentered(keyValue.rectTransform, new Vector2(250f, -120f), new Vector2(300f, 80f));
            keyValue.fontStyle = FontStyle.Bold;

            Button nextButton = CreateTextButton("NextButton", card, sprites.ButtonBlue, font, strings.nextLabel,
                new Vector2(0f, -320f), new Vector2(400f, 120f), Color.white, out Text nextLabel);
            SetRefs(complete, ("root", completeRoot.gameObject), ("titleText", sTitle),
                ("timeLabelText", timeLabel), ("timeValueText", timeValue),
                ("keyItemLabelText", keyLabel), ("keyItemValueText", keyValue), ("keyItemIcon", gemIcon),
                ("nextButton", nextButton), ("nextLabel", nextLabel));
            SetRefArray(complete, "starImages", stars);
            completeRoot.gameObject.SetActive(false);

            // --- wire the minigame
            SetRefs(minigame,
                ("config", config), ("strings", strings), ("loop", loop), ("hudRoot", hud.gameObject),
                ("gaugeFill", fill), ("gaugePercentText", percent),
                ("keyPromptRoot", keyPrompt.gameObject), ("keyCapText", keyText), ("promptText", prompt), ("titleText", title),
                ("tapButton", tap), ("tapButtonLabel", tapLabel),
                ("collectedScreen", collected), ("completePanel", complete));
        }

        private static Text CreateHeader(RectTransform card, Sprite headerSprite, Font font, string title)
        {
            RectTransform header = CreateRect("Header", card);
            PlaceCentered(header, new Vector2(0f, 420f), new Vector2(760f, 150f));
            AddImage(header, headerSprite, Color.white, false, false);
            Text text = CreateText("Title", header, font, title, 56, TextAnchor.MiddleCenter, DarkBrownText);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(0f, 45f); // the ribbon's flat face is its upper part
            text.fontStyle = FontStyle.Bold;
            return text;
        }

        private static Image CreateIcon(string name, Transform parent, Sprite sprite, Vector2 position)
        {
            RectTransform rt = CreateRect(name, parent);
            PlaceCentered(rt, position, new Vector2(96f, 96f));
            Image img = AddImage(rt, sprite, Color.white, false, false);
            img.preserveAspect = true;
            return img;
        }

        private static Button CreateTextButton(string name, Transform parent, Sprite sprite, Font font, string labelText,
            Vector2 position, Vector2 size, Color textColor, out Text label)
        {
            RectTransform rt = CreateRect(name, parent);
            PlaceCentered(rt, position, size);
            Image img = AddImage(rt, sprite, Color.white, true, true);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            button.colors = colors;
            label = CreateText("Label", rt, font, labelText, 52, TextAnchor.MiddleCenter, textColor);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(0f, 10f); // the button art has a lip at the bottom
            label.fontStyle = FontStyle.Bold;
            return button;
        }

        /// <summary>Full-screen dim layer that also blocks clicks behind it.</summary>
        private static RectTransform CreateOverlay(string name, Transform parent, float alpha)
        {
            RectTransform rt = CreateRect(name, parent);
            Stretch(rt);
            AddImage(rt, null, new Color(0f, 0f, 0f, alpha), false, true);
            return rt;
        }

        private static RectTransform CreateCard(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            RectTransform rt = CreateRect(name, parent);
            PlaceCentered(rt, position, size);
            AddImage(rt, sprite, Color.white, true, true);
            return rt;
        }

        private static Image AddImage(RectTransform rt, Sprite sprite, Color color, bool sliced, bool raycast)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = sliced && sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = raycast;
            return img;
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

        private static void PlaceCentered(RectTransform rt, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        /// <summary>Anchored and pivoted at a screen corner (<paramref name="corner"/> 0/1 per axis), offset inward by <paramref name="offset"/>.</summary>
        private static void PlaceCorner(RectTransform rt, Vector2 corner, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = corner;
            rt.pivot = corner;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
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

        // ------------------------------------------------------------------ assets

        private static SuctionConfig GetOrCreateConfig()
        {
            SuctionConfig config = AssetDatabase.LoadAssetAtPath<SuctionConfig>(ConfigPath);
            if (config != null) return config; // keep designer tuning

            config = ScriptableObject.CreateInstance<SuctionConfig>();
            config.maps = new SuctionMapSettings[MapColours.Length];
            for (int i = 0; i < MapColours.Length; i++)
            {
                config.maps[i] = new SuctionMapSettings { mapId = MapColours[i].id, orbColor = MapColours[i].orb, streakColor = MapColours[i].streak };
            }
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset; // keep designer tuning
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>URP Lit, transparent, glossy: the see-through weather orb. Its colour is set per map at runtime.</summary>
        private static Material GetOrCreateOrbMaterial()
        {
            Material mat = GetOrCreateMaterial(OrbMaterialPath, "Universal Render Pipeline/Lit");
            MakeTransparent(mat, false);
            mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.55f));
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>URP Particles/Unlit, additive, untextured: thin glowing streaks (colour comes from the particles).</summary>
        private static Material GetOrCreateStreakMaterial()
        {
            Material mat = GetOrCreateMaterial(StreakMaterialPath, "Universal Render Pipeline/Particles/Unlit");
            MakeTransparent(mat, true);
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateMaterial(string path, string shaderName)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("[SuctionStageBuilder] Shader not found: " + shaderName + " (is URP installed?)");
                shader = Shader.Find("Sprites/Default");
            }
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        /// <summary>Sets URP's transparent surface options from script (what the material inspector would set).</summary>
        private static void MakeTransparent(Material mat, bool additive)
        {
            SetFloatIfPresent(mat, "_Surface", 1f);                // Transparent
            SetFloatIfPresent(mat, "_Blend", additive ? 2f : 0f);  // 2 = Additive, 0 = Alpha
            SetFloatIfPresent(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(mat, "_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(mat, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloatIfPresent(mat, "_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(mat, "_ZWrite", 0f);
            SetFloatIfPresent(mat, "_AlphaClip", 0f);
            SetFloatIfPresent(mat, "_Cull", (float)CullMode.Back);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void SetFloatIfPresent(Material mat, string property, float value)
        {
            if (mat.HasProperty(property)) mat.SetFloat(property, value);
        }

        private static UiSprites LoadUiSprites()
        {
            return new UiSprites
            {
                WoodPanel = LoadSprite(WoodPanelPath, new Vector4(48f, 48f, 48f, 48f)),
                Header = LoadSprite(HeaderPath, Vector4.zero),
                Label = LoadSprite(LabelPath, Vector4.zero),
                RoundWood = LoadSprite(RoundWoodPath, Vector4.zero),
                RoundCream = LoadSprite(RoundCreamPath, Vector4.zero),
                ButtonBlue = LoadSprite(ButtonBluePath, new Vector4(50f, 50f, 50f, 50f)),
                BarBackground = LoadSprite(BarBackgroundPath, new Vector4(32f, 0f, 32f, 0f)),
                BarFill = LoadSprite(BarFillPath, new Vector4(32f, 0f, 32f, 0f)),
                Star = LoadSprite(IconStarPath, Vector4.zero),
                Stopwatch = LoadSprite(IconStopwatchPath, Vector4.zero),
                Gem = LoadSprite(IconGemPath, Vector4.zero),
            };
        }

        /// <summary>Same import rules as ShopSceneBuilder (Sprite, no mipmaps, full-rect mesh for 9-slicing).</summary>
        private static Sprite LoadSprite(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (importer.spriteBorder != border) { importer.spriteBorder = border; changed = true; }
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("[SuctionStageBuilder] Sprite not found: " + path);
            return sprite;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
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
                    Debug.LogError("[SuctionStageBuilder] Missing serialized field '" + field + "' on " + target.GetType().Name);
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
