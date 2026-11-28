#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeatherForSale.Shop;
using WeatherForSale.UI;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Editor-only tool that builds the first-person shop scene: crops the map and bottle art into
    /// sprites, creates the shop data assets (customers, maps, session, settings, strings), places
    /// the counter props, customers' stand points, a placeholder shop shell, light KayKit dressing
    /// and the full UI (Dialog, map panel, Achievement book, Pause, Loading, End of day), then saves
    /// Assets/Scenes/Shop.unity and appends it to Build Settings without reordering.
    /// Run from the menu or with
    /// <c>-executeMethod WeatherForSale.EditorTools.ShopSceneBuilder.BuildFromCommandLine</c>.
    /// Implements: story-002 (Files: Assets/Scenes/Shop.unity).
    /// </summary>
    public static class ShopSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Shop.unity";
        private const string DataFolder = "Assets/Data/Shop";
        private const string MaterialFolder = "Assets/Materials/Shop";
        private const string MapsFolder = "Assets/Art/UI/Maps";
        private const string BottlesFolder = "Assets/Art/UI/Bottles";
        private const string PropsFolder = "Assets/Art/Props/";
        private const string CharactersFolder = "Assets/Art/Characters/";
        private const string Sheet = "Assets/Art/UI/Sheet/"; // the game's own UI sheet (sliced from design/ref)
        private const string KayKitFolder = "Assets/KayKit_Forest_Nature_Pack_1.0_EXTRA/Assets/fbx(unity)/Color1/";
        private const string MapBoardSource = "design/ref/props/MapBoard/MapBoard_Front.png";
        private const string BottleSource = "design/ref/All Bottle.png";
        // Each map loads its one merged obby scene (ObbyTestSceneBuilder.MapThemes): Obby_<Name>.
        private static string MapSceneName(string displayName) => "Obby_" + displayName;

        // Camera: the player's eye behind the counter (Sam is a child, ~1.4 m tall).
        private const float EyeHeight = 1.25f;
        private const float CameraBehindCounter = 0.55f;  // metres behind the counter's back edge
        private const float CameraPitch = 8f;             // degrees down
        private const float CameraFov = 60f;
        private const float CustomerBeyondCounter = 1.0f; // metres past the counter's front edge
        private const float CustomerSlideOffset = 4.5f;   // metres to the side where customers enter/leave
        private const float MapBoardYaw = 200f;           // 180 = map faces the player, +20 = turned inward
        // Counter-top layout (counter-local metres; +X right, +Z toward customers), as placed by the user in
        // the editor (2026-11-23): map board right and crystal ball left, both near the player's edge; book in front of the ball.
        private static readonly Vector3 MapBoardPos = new Vector3(0.602f, 0.009f, -0.232f); // user-placed 2026-11-23
        private const float MapBoardScale = 0.713f;        // 0.5 m → ~0.38 m tall, 0.72 m → ~0.54 m wide
        private static readonly Vector3 CrystalBallPos = new Vector3(-0.75f, 0f, -0.24f);
        private const float CrystalBallScale = 0.7f;      // 0.45 m → ~0.32 m tall
        private const float CrystalBallYaw = 135f;        // front (+Z) turned 45° from the player-facing 180° toward the counter centre
        private static readonly Vector3 BottlePos = new Vector3(-0.05f, 0f, -0.2f);
        private static readonly Vector3 BookPos = new Vector3(-0.48f, 0f, -0.28f);
        private const float BookYaw = 20f;
        private const float CounterYaw = 0f;              // decorated front faces +Z (customers)
        private const float CustomerFaceYaw = 180f;       // models face +Z; turned to face the player

        private static readonly Color SkyColor = new Color(0.55f, 0.78f, 0.96f);
        private static readonly Color FloorColor = new Color(0.55f, 0.36f, 0.22f);
        private static readonly Color WallColor = new Color(0.86f, 0.74f, 0.56f);
        private static readonly Color BeamColor = new Color(0.42f, 0.26f, 0.15f);
        private static readonly Color AwningBlue = new Color(0.20f, 0.33f, 0.68f);
        private static readonly Color AwningWhite = new Color(0.96f, 0.95f, 0.90f);
        private static readonly Color StreetColor = new Color(0.78f, 0.72f, 0.62f);
        private static readonly Color BookCoverColor = new Color(0.18f, 0.27f, 0.58f);
        private static readonly Color BookPageColor = new Color(0.98f, 0.93f, 0.80f);
        private static readonly Color GoldColor = new Color(0.95f, 0.72f, 0.20f);
        private static readonly Color DarkBrownText = new Color(0.30f, 0.17f, 0.09f);
        private static readonly Color LanternColor = new Color(1f, 0.72f, 0.42f);

        // ------------------------------------------------------------------ crops

        private struct CropRegion
        {
            public string Name;
            public float X0, Y0, X1, Y1; // fractions of the source image, top-left origin

            public CropRegion(string name, float x0, float y0, float x1, float y1)
            {
                Name = name; X0 = x0; Y0 = y0; X1 = x1; Y1 = y1;
            }
        }

        // MapBoard_Front.png (963 x 963): top-left rainforest, top-right glacier, bottom-left valley, bottom-right desert.
        // Map Select pictures (MapArt_*, 1.6 : 1): smaller than the grid cell and a little lower, clear of the header and frame.
        internal static readonly Vector2 MapPictureSize = new Vector2(480f, 300f);

        // MapSelectBoard.png (1512 x 975) on the Map Select card, under the header, and where each map is painted on it
        // (centre and size in card units; measured on the picture: rainforest TL, valley TR, desert BL, glacier BR).
        internal static readonly Vector2 MapBoardArtCentre = new Vector2(0f, -55f);
        internal static readonly Vector2 MapBoardArtSize = new Vector2(1147f, 740f);

        internal static Rect PaintedMapRect(string mapId)
        {
            switch (mapId)
            {
                case "rainforest": return new Rect(-264f, 106f, 493f, 292f);
                case "valley": return new Rect(261f, 102f, 497f, 300f);
                case "desert": return new Rect(-266f, -226f, 497f, 311f);
                case "glacier": return new Rect(265f, -220f, 504f, 330f);
                default: return new Rect(0f, 0f, 480f, 300f);
            }
        }
        internal static readonly Vector2 MapPictureNudge = new Vector2(0f, -18f);

        /// <summary>
        /// Gives the counter bottle one generated model per weather (Assets/Art/Props/Bottles/Bottle{Rain,Wind,Sun,Snow}.fbx),
        /// the same height as the old model, and a <see cref="CounterBottleVariants"/> that shows the one for the map
        /// just played. The old model stays as the fallback; the click collider stays on the bottle. Run again safely.
        /// </summary>
        public static void AddCounterBottleVariants(GameObject bottle, ShopSession session)
        {
            Transform old = bottle.transform.Find("Models");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var fallback = new List<Renderer>();
            var bounds = new Bounds(bottle.transform.position, Vector3.zero);
            bool any = false;
            foreach (Renderer r in bottle.GetComponentsInChildren<Renderer>(true))
            {
                fallback.Add(r);
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
            }
            float height = any ? Mathf.Max(0.05f, bounds.size.y) : 0.3f;
            Vector3 bottomWorld = any ? new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) : bottle.transform.position;

            var models = new GameObject("Models").transform;
            models.SetParent(bottle.transform, false);
            Vector3 ls = bottle.transform.lossyScale;
            models.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z); // children sized in metres
            models.position = bottomWorld;
            models.rotation = Quaternion.identity;

            string[,] map = { { "rainforest", "BottleRain" }, { "valley", "BottleWind" }, { "desert", "BottleSun" }, { "glacier", "BottleSnow" } };
            var variants = new List<CounterBottleVariants.Variant>();
            for (int i = 0; i < map.GetLength(0); i++)
            {
                MeshyPropBaker.PropMesh prop = MeshyPropBaker.BakeProp("Bottles", map[i, 1], MeshyPropBaker.ZUp);
                if (prop == null) { Debug.LogWarning("[ShopSceneBuilder] Missing bottle model " + map[i, 1]); continue; }
                GameObject go = MeshyPropBaker.PlaceProp(prop, map[i, 1], models, Vector3.zero, new Vector3(0f, height, 0f));
                go.SetActive(false);
                variants.Add(new CounterBottleVariants.Variant { mapId = map[i, 0], model = go });
            }
            if (variants.Count == 0) { Object.DestroyImmediate(models.gameObject); return; }

            // The old model becomes the fallback (its renderers grouped under one switch).
            GameObject fallbackRoot = null;
            foreach (Renderer r in fallback)
            {
                if (r.gameObject == bottle) { r.enabled = false; continue; } // a renderer on the root itself: hide for good
                fallbackRoot ??= r.gameObject;
            }
            var comp = bottle.GetComponent<CounterBottleVariants>() ?? bottle.AddComponent<CounterBottleVariants>();
            var so = new SerializedObject(comp);
            so.FindProperty("session").objectReferenceValue = session;
            so.FindProperty("fallback").objectReferenceValue = fallbackRoot;
            SerializedProperty list = so.FindProperty("variants");
            list.arraySize = variants.Count;
            for (int i = 0; i < variants.Count; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("mapId").stringValue = variants[i].mapId;
                list.GetArrayElementAtIndex(i).FindPropertyRelative("model").objectReferenceValue = variants[i].model;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal const string PausePrefabPath = "Assets/Prefabs/UI/PauseMenu.prefab";

        /// <summary>Saves the shop's menu button + Pause panel as a prefab so every gameplay scene gets the same one.</summary>
        internal static void SavePausePrefab(GameObject pauseHost)
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(pauseHost, PausePrefabPath);
        }

        /// <summary>
        /// Adds the shared menu button + Pause panel (<see cref="PausePrefabPath"/>, saved by the shop build) on top of a
        /// gameplay canvas. Logs a warning and does nothing when the prefab is missing (build the Shop scene first).
        /// Usage: <c>ShopSceneBuilder.AddPauseMenu(canvasGo.transform);</c>
        /// </summary>
        public static void AddPauseMenu(Transform canvas)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[ShopSceneBuilder] " + PausePrefabPath + " missing; build the Shop scene first for the gameplay menu button.");
                return;
            }
            var pause = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            var rt = (RectTransform)pause.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling(); // above the HUD and touch controls
        }

        private static readonly CropRegion[] MapCrops =
        {
            new CropRegion("Map_Rainforest", 0.175f, 0.345f, 0.505f, 0.545f),
            new CropRegion("Map_Glacier", 0.505f, 0.345f, 0.825f, 0.545f),
            new CropRegion("Map_Valley", 0.175f, 0.525f, 0.505f, 0.735f),
            new CropRegion("Map_Desert", 0.475f, 0.515f, 0.825f, 0.725f),
        };

        // All Bottle.png (1536 x 1024): row 1 col 3 rain; row 2 cols 1-3 wind, sun, snow.
        // Row-2 split columns (403, 781, 1158) are the least-opaque columns between touching bottles.
        private static readonly CropRegion[] BottleCrops =
        {
            new CropRegion("Bottle_Rain", 1015f / 1536f, 8f / 1024f, 1440f / 1536f, 496f / 1024f),
            new CropRegion("Bottle_Wind", 8f / 1536f, 510f / 1024f, 403f / 1536f, 1000f / 1024f),
            new CropRegion("Bottle_Sun", 403f / 1536f, 510f / 1024f, 781f / 1536f, 1000f / 1024f),
            new CropRegion("Bottle_Snow", 781f / 1536f, 510f / 1024f, 1158f / 1536f, 1000f / 1024f),
        };

        // ------------------------------------------------------------------ data

        private struct MapSpec
        {
            public string Id, DisplayName, Thumbnail, Bottle;
            public Vector2 GridPosition; // panel layout: rainforest TL, valley TR, desert BL, glacier BR

            public MapSpec(string id, string displayName, string thumbnail, string bottle, Vector2 grid)
            {
                Id = id; DisplayName = displayName; Thumbnail = thumbnail; Bottle = bottle; GridPosition = grid;
            }
        }

        private static readonly MapSpec[] Maps =
        {
            new MapSpec("rainforest", "Rainforest", "MapArt_Rainforest", "Bottle_Rain", new Vector2(-300f, 190f)),
            new MapSpec("valley", "Valley", "MapArt_Valley", "Bottle_Wind", new Vector2(300f, 190f)),
            new MapSpec("desert", "Desert", "MapArt_Desert", "Bottle_Sun", new Vector2(-300f, -210f)),
            new MapSpec("glacier", "Glacier", "MapArt_Glacier", "Bottle_Snow", new Vector2(300f, -210f)),
        };

        private struct CustomerSpec
        {
            public string Name, Folder, Weather, MapId, Problem, Thanks;
            public string[][] Hints; // three tiers: vague, narrow, clear

            public CustomerSpec(string name, string folder, string weather, string mapId, string problem, string[][] hints, string thanks)
            {
                Name = name; Folder = folder; Weather = weather; MapId = mapId; Problem = problem; Hints = hints; Thanks = thanks;
            }
        }

        // Problem lines: design/game-brief.md character block (verbatim). Hints never name the weather or
        // the map (no rain/forest/sun/desert/snow/ice/wind/valley...): problem -> a place -> a story detail.
        private static readonly CustomerSpec[] Customers =
        {
            new CustomerSpec("Bob", "Bob", "rain", "rainforest",
                "My garden's gone bone-dry and nothing will grow. Got anything that'll make it lush and green again?",
                new[]
                {
                    new[]
                    {
                        "My plants look up at the sky every morning, waiting for a drink that never comes.",
                        "Even my watering can gave up. The whole garden needs a good soaking from above.",
                        "The soil cracks when I step on it. Everything in this town is dry as a biscuit.",
                    },
                    new[]
                    {
                        "My grandpa grew up somewhere so damp his clothes never dried and the trees grew taller than houses.",
                        "He said you could hear frogs sing from dawn until dusk, and moss grew on everything, even the roofs.",
                        "Where he lived, the air felt like a warm towel, and the leaves were as big as umbrellas.",
                    },
                    new[]
                    {
                        "He said the sky opened up like a bucket every day at three.",
                        "Look for the place with a tall, thundering white ribbon of water pouring off a cliff.",
                        "It's the greenest corner of your map, with giant leaves and a river snaking through.",
                        "Grandpa's postcard showed palm trees, a roaring waterfall and a sky full of grey clouds. That's the place!",
                    },
                },
                "Rain at last! My garden will be lush and green again. Thank you!"),
            new CustomerSpec("Sprout", "Sprout", "wind", "valley",
                "My kite just won't fly! I need something to lift it back up into the sky!",
                new[]
                {
                    new[]
                    {
                        "The air here is so still, my kite just flops on the grass.",
                        "I ran all the way down the street and it still wouldn't lift. It needs something to push it up!",
                        "Even the flags in town hang down like wet socks. Nothing moves at all.",
                    },
                    new[]
                    {
                        "Grandpa says there's a place where tall orange rocks stand in rows, and the air rushes between them so hard your hat flies off.",
                        "He says the air squeezes through the gaps between the cliffs there and comes out whistling like a kettle.",
                        "His kite once flew so high there it took him a whole afternoon to reel it back in!",
                    },
                    new[]
                    {
                        "Look for tall orange rock towers with little trees on top, standing over a deep drop full of clouds.",
                        "On your map there are white swirly lines curling between the cliffs. That's air that never sits still!",
                        "Grandpa says you can't even see the bottom there, just soft fluffy mist far below the rock towers.",
                        "On your map it's the bottom-left corner, with flat-topped stone towers and twisting white streaks.",
                    },
                },
                "Whoosh! Look at my kite go! Thank you so much!"),
            new CustomerSpec("Auntie Skye", "AuntieSkye", "sun", "desert",
                "My laundry's been soaking wet for three whole days! Do you know how hard that makes my life?",
                new[]
                {
                    new[]
                    {
                        "If only something warm and bright would beam down on my clothesline for a few hours.",
                        "Three days of grey drizzle and my socks still squelch. I'd give anything for some toasty weather.",
                        "Even my towels have started growing little green fuzzies. Ew!",
                    },
                    new[]
                    {
                        "I'd love a place so hot and dry that a puddle vanishes before you can step in it.",
                        "A friend told me about somewhere that hasn't seen a cloud in years. Their laundry dries before they finish hanging it!",
                        "Lizards nap on the rocks there, and the ground is so warm you could bake bread on it.",
                    },
                    new[]
                    {
                        "Somewhere the clouds forget to visit, where the heat and dust have carved stones into giant arches.",
                        "Look for the big stone arch standing alone in the orange land on your map.",
                        "Golden hills, spiky green plants with pink flowers, and a sky that's always blue. That's what my friend described.",
                        "The brightest, driest corner of your map. That's where my laundry's dream lives!",
                    },
                },
                "Sunshine in a bottle! My laundry will be dry by lunchtime. You're a lifesaver!"),
            new CustomerSpec("Granny Ruby", "GrannyRuby", "snow", "glacier",
                "My grandchild wants to feel real winter cold just once. In all my years, I've never seen snow either.",
                new[]
                {
                    new[]
                    {
                        "Something soft and white that floats down and makes the whole town quiet... that's all we wish for.",
                        "My grandchild keeps asking what it feels like when the sky sends down little white feathers.",
                        "I have knitted three woolly scarves and never once had a reason to wear them.",
                    },
                    new[]
                    {
                        "When I was a girl, I heard of peaks so cold that what lands on them never melts, not even in summer.",
                        "Travellers say the mountains there glitter blue and white, and the air bites your nose.",
                        "They say your breath turns into little clouds the moment you speak there.",
                    },
                    new[]
                    {
                        "My grandchild wants to build a little man with a carrot nose. Only the coldest, highest place will do.",
                        "Look for the tallest pointy mountains on your map, the ones with pale blue tops.",
                        "Pine trees wearing thick white coats, and tall cliffs that sparkle in the light.",
                        "On your map it's the top-right corner, where everything shimmers pale and blue.",
                    },
                },
                "Real snow... My grandchild will be so happy. Thank you, dear."),
        };

        // ------------------------------------------------------------------ dressing

        private struct DressingSpec
        {
            public string File;
            public float X, Z, Yaw;

            public DressingSpec(string file, float x, float z, float yaw)
            {
                File = file; X = x; Z = z; Yaw = yaw;
            }
        }

        // Light KayKit dressing across the street (kept small for the WebGL budget).
        // The town square replaced the forest (cover / opening CGI); two trees stay for green.
        private static readonly DressingSpec[] Nature =
        {
            new DressingSpec("Tree_2_B_Color1", -13f, 19f, 40f),
            new DressingSpec("Tree_4_A_Color1", 13.5f, 18f, 200f),
        };

        // ------------------------------------------------------------------ UI sprites (game UI sheet)

        private const string WoodPanelPath = Sheet + "PanelLarge.png";
        private const string DialogFramePath = Sheet + "DialogFrame.png"; // the first-sheet frame, kept for the dialog box
        private const string HeaderPath = Sheet + "RibbonBlue.png";
        private const string LabelPath = Sheet + "CapsuleWood.png";
        private const string RoundWoodPath = Sheet + "RoundWood.png";
        private const string KnobPath = Sheet + "KeyCap.png";
        private const string ButtonBluePath = Sheet + "BtnBlueA.png"; // UI v2: leaf green (the file name is from the old sheet)
        private const string ButtonWoodPath = Sheet + "BtnWoodA.png";
        private const string BookPath = Sheet + "BookOpen.png";
        private const string BarBackgroundPath = "Assets/Art/UI/Boss/PlayerFrame.png";
        private const string BarFillPath = "Assets/Art/UI/Boss/PlayerFillGreen.png";
        private const string ArrowPath = Sheet + "IconBack.png";
        private const string IconMenuPath = Sheet + "IconMenu.png";
        private const string IconClosePath = Sheet + "IconClose.png";
        private const string IconLockPath = Sheet + "IconLock.png";
        private const string IconTrophyPath = Sheet + "IconTrophy.png";

        private sealed class UiSprites
        {
            public Sprite WoodPanel, DialogFrame, Header, Label, RoundWood, Knob, ButtonBlue, ButtonWood, Book,
                BarBackground, BarFill, Arrow, IconMenu, IconClose, IconLock, IconTrophy;
        }

        // ------------------------------------------------------------------ entry points

        /// <summary>Menu entry: builds and saves the shop scene.</summary>
        // Shop.unity is now hand-arranged by the user: the builder never overwrites an existing Shop scene.
        // To rebuild from scratch on purpose, delete or rename Assets/Scenes/Shop.unity first.
        private const string LockedScenePath = "Assets/Scenes/Shop.unity";

        private static bool SceneLocked()
        {
            if (!System.IO.File.Exists(LockedScenePath)) return false;
            Debug.LogWarning("[ShopSceneBuilder] " + LockedScenePath + " already exists and is hand-arranged; not rebuilding it. "
                             + "Delete or rename the scene first to build a new one.");
            return true;
        }

        [MenuItem("Weather For Sale/Build Shop Scene (only when Shop.unity is missing)")]
        public static void BuildFromMenu()
        {
            if (SceneLocked()) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        /// <summary>Batch-mode entry point (<c>-executeMethod</c>). Exits non-zero on failure.</summary>
        public static void BuildFromCommandLine()
        {
            if (SceneLocked()) return;
            try
            {
                Build();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void Build()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(DataFolder);
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Materials");
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/UI");
            EnsureFolder(MapsFolder);
            EnsureFolder(BottlesFolder);

            // Writing the cropped PNGs does not hold references, so it can happen before NewScene.
            CropImage(MapBoardSource, MapsFolder, MapCrops);
            CropImage(BottleSource, BottlesFolder, BottleCrops);

            // NewScene(Single) unloads unused assets, so load/create every asset reference AFTER it.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- data
            ShopUiConfig uiConfig = GetOrCreateAsset<ShopUiConfig>(DataFolder + "/ShopUiConfig.asset");
            ShopStrings strings = GetOrCreateAsset<ShopStrings>(DataFolder + "/ShopStrings.asset");
            ShopAudioSettings audioSettings = GetOrCreateAsset<ShopAudioSettings>(DataFolder + "/ShopAudioSettings.asset");
            ShopSession session = GetOrCreateAsset<ShopSession>(DataFolder + "/ShopSession.asset");
            MapCatalog catalog = GetOrCreateMapCatalog();
            CustomerRoster roster = GetOrCreateRoster();
            UiSprites sprites = LoadUiSprites();

            // --- environment and lighting
            SetupLighting();
            GameObject world = new GameObject("World");
            GameObject counter = PlacePrefab(PropsFolder + "Counter/Counter.prefab", Vector3.zero, CounterYaw, world.transform);
            Bounds counterBounds = counter != null ? WorldBounds(counter) : new Bounds(new Vector3(0f, 0.49f, 0f), new Vector3(2.8f, 0.98f, 1.1f));
            float top = counterBounds.max.y;
            float frontZ = counterBounds.max.z;
            float backZ = counterBounds.min.z;

            BuildShell(world.transform, frontZ, backZ);
            BuildNature(world.transform);
            BuildTown(world.transform);

            // --- counter props
            GameObject props = new GameObject("CounterProps");
            props.transform.SetParent(world.transform, false);
            GameObject crystalBall = PlacePrefab(PropsFolder + "CrystalBall/CrystalBall.prefab", CrystalBallPos + Vector3.up * top, CrystalBallYaw, props.transform);
            if (crystalBall != null) crystalBall.transform.localScale = Vector3.one * CrystalBallScale;

            GameObject mapBoard = PlacePrefab(PropsFolder + "MapBoard/MapBoard.prefab", MapBoardPos + Vector3.up * top, MapBoardYaw, props.transform);
            if (mapBoard != null) mapBoard.transform.localScale = Vector3.one * MapBoardScale; // before the collider is fitted
            BoxCollider mapCollider = mapBoard != null ? AddFittedCollider(mapBoard, 1.05f) : null;

            GameObject bottle = PlacePrefab(PropsFolder + "Bottle/Bottle.prefab", BottlePos + Vector3.up * top, 0f, props.transform);
            BoxCollider bottleCollider = bottle != null ? AddFittedCollider(bottle, 1.6f) : null;
            if (bottle != null) AddCounterBottleVariants(bottle, AssetDatabase.LoadAssetAtPath<ShopSession>(DataFolder + "/ShopSession.asset"));
            if (bottle != null) bottle.SetActive(false);

            GameObject book = BuildBookStandIn(props.transform, BookPos + Vector3.up * top, BookYaw);
            BoxCollider bookCollider = book.GetComponent<BoxCollider>();

            PlacePrefab(PropsFolder + "Bookshelf/Bookshelf.prefab", new Vector3(-1.75f, 0f, -0.1f), 90f, world.transform);

            // --- camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            cam.fieldOfView = CameraFov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 120f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetPositionAndRotation(new Vector3(0f, EyeHeight, backZ - CameraBehindCounter), Quaternion.Euler(CameraPitch, 0f, 0f));

            // --- customers
            GameObject customersGo = new GameObject("Customers");
            float standZ = frontZ + CustomerBeyondCounter;
            Transform stand = CreatePoint("StandPoint", customersGo.transform, new Vector3(0f, 0f, standZ));
            Transform enter = CreatePoint("EnterFrom", customersGo.transform, new Vector3(CustomerSlideOffset, 0f, standZ));
            Transform exit = CreatePoint("ExitTo", customersGo.transform, new Vector3(-CustomerSlideOffset, 0f, standZ));
            CustomerController customerController = customersGo.AddComponent<CustomerController>();
            SetRefs(customerController,
                ("config", uiConfig), ("settings", audioSettings),
                ("standPoint", stand), ("enterFrom", enter), ("exitTo", exit));
            SetFloat(customerController, "faceCounterYaw", CustomerFaceYaw);

            // --- EventSystem (Input System UI module: mouse + touch)
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            InputSystemUIInputModule uiModule = esGo.AddComponent<InputSystemUIInputModule>();
            if (uiModule.actionsAsset == null) uiModule.AssignDefaultActions();

            // --- systems
            GameObject systems = new GameObject("ShopSystems");
            ShopPointerInput pointer = systems.AddComponent<ShopPointerInput>();
            SetRefs(pointer, ("viewCamera", cam), ("config", uiConfig));
            SetPointerTargets(pointer,
                (ShopClickTarget.MapBoard, mapCollider, mapBoard != null ? mapBoard.transform : null),
                (ShopClickTarget.Bottle, bottleCollider, bottle != null ? bottle.transform : null),
                (ShopClickTarget.AchievementBook, bookCollider, book.transform));
            ShopFlowController flow = systems.AddComponent<ShopFlowController>();
            ShopDebugInput debugInput = systems.AddComponent<ShopDebugInput>();
            SetRefs(debugInput, ("session", session), ("flow", flow));

            BuildUi(flow, pointer, customerController, bottle, catalog, roster, session, strings, uiConfig, audioSettings, sprites);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AppendToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ShopSceneBuilder] Built " + ScenePath + " (counter top " + top.ToString("0.00")
                      + " m, front z " + frontZ.ToString("0.00") + ", back z " + backZ.ToString("0.00") + ").");
        }

        // ------------------------------------------------------------------ data assets

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset; // keep designer tuning
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static MapCatalog GetOrCreateMapCatalog()
        {
            string path = DataFolder + "/MapCatalog.asset";
            MapCatalog catalog = AssetDatabase.LoadAssetAtPath<MapCatalog>(path);
            bool created = catalog == null;
            if (created)
            {
                catalog = ScriptableObject.CreateInstance<MapCatalog>();
                catalog.maps = new MapEntry[Maps.Length];
                for (int i = 0; i < Maps.Length; i++)
                {
                    catalog.maps[i] = new MapEntry
                    {
                        id = Maps[i].Id,
                        displayName = Maps[i].DisplayName,
                        sceneName = MapSceneName(Maps[i].DisplayName),
                    };
                }
                catalog.minLoadingSeconds = 1.5f;
                AssetDatabase.CreateAsset(catalog, path);
            }

            // Sprites are (re)linked every build so freshly cropped art is always used.
            foreach (MapEntry entry in catalog.maps)
            {
                if (entry == null) continue;
                foreach (MapSpec spec in Maps)
                {
                    if (spec.Id != entry.id) continue;
                    // The old board crops (Map_*) give way to the painted map art (MapArt_*).
                    if (entry.thumbnail == null || entry.thumbnail.name.StartsWith("Map_")) entry.thumbnail = LoadSprite(MapsFolder + "/" + spec.Thumbnail + ".png", Vector4.zero);
                    if (entry.bottleSprite == null) entry.bottleSprite = LoadSprite(BottlesFolder + "/" + spec.Bottle + ".png", Vector4.zero);
                }
            }
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static CustomerRoster GetOrCreateRoster()
        {
            var list = new List<CustomerData>();
            foreach (CustomerSpec spec in Customers)
            {
                string path = DataFolder + "/Customer_" + spec.Folder + ".asset";
                CustomerData data = AssetDatabase.LoadAssetAtPath<CustomerData>(path);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<CustomerData>();
                    data.displayName = spec.Name;
                    data.weatherWord = spec.Weather;
                    data.correctMapId = spec.MapId;
                    data.problemLine = spec.Problem;
                    data.hintsVague = (string[])spec.Hints[0].Clone();
                    data.hintsNarrow = (string[])spec.Hints[1].Clone();
                    data.hintsClear = (string[])spec.Hints[2].Clone();
                    data.thankYouLine = spec.Thanks;
                    AssetDatabase.CreateAsset(data, path);
                }
                if (data.prefab == null)
                {
                    data.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharactersFolder + spec.Folder + "/" + spec.Folder + ".prefab");
                    if (data.prefab == null) Debug.LogWarning("[ShopSceneBuilder] Missing customer prefab for " + spec.Name);
                }
                EditorUtility.SetDirty(data);
                list.Add(data);
            }

            string rosterPath = DataFolder + "/CustomerRoster.asset";
            CustomerRoster roster = AssetDatabase.LoadAssetAtPath<CustomerRoster>(rosterPath);
            if (roster == null)
            {
                roster = ScriptableObject.CreateInstance<CustomerRoster>();
                roster.customers = list.ToArray();
                AssetDatabase.CreateAsset(roster, rosterPath);
            }
            else if (roster.Count == 0)
            {
                roster.customers = list.ToArray();
            }
            EditorUtility.SetDirty(roster);
            return roster;
        }

        // ------------------------------------------------------------------ image crops / sprites

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        private static void CropImage(string sourceRelative, string outFolder, CropRegion[] regions)
        {
            string full = Path.Combine(ProjectRoot, sourceRelative);
            if (!File.Exists(full))
            {
                Debug.LogWarning("[ShopSceneBuilder] Source image not found: " + full);
                return;
            }
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(File.ReadAllBytes(full)))
            {
                Debug.LogWarning("[ShopSceneBuilder] Could not decode " + full);
                Object.DestroyImmediate(source);
                return;
            }
            int width = source.width;
            int height = source.height;

            foreach (CropRegion region in regions)
            {
                int x0 = Mathf.Clamp(Mathf.RoundToInt(region.X0 * width), 0, width - 1);
                int x1 = Mathf.Clamp(Mathf.RoundToInt(region.X1 * width), x0 + 1, width);
                int top0 = Mathf.Clamp(Mathf.RoundToInt(region.Y0 * height), 0, height - 1);
                int top1 = Mathf.Clamp(Mathf.RoundToInt(region.Y1 * height), top0 + 1, height);
                int w = x1 - x0;
                int h = top1 - top0;
                int yFromBottom = height - top1; // Texture2D rows start at the bottom

                var crop = new Texture2D(w, h, TextureFormat.RGBA32, false);
                crop.SetPixels(source.GetPixels(x0, yFromBottom, w, h));
                crop.Apply();
                string assetPath = outFolder + "/" + region.Name + ".png";
                File.WriteAllBytes(Path.Combine(ProjectRoot, assetPath), crop.EncodeToPNG());
                Object.DestroyImmediate(crop);

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                ConfigureSprite(assetPath, Vector4.zero);
            }
            Object.DestroyImmediate(source);
        }

        private static void ConfigureSprite(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

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
                settings.spriteMeshType = SpriteMeshType.FullRect; // required for 9-slicing
                importer.SetTextureSettings(settings);
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string path, Vector4 border)
        {
            // The game UI sheet keeps the borders set by UiSheetImporter.
            if (!path.StartsWith("Assets/Art/UI/")) ConfigureSprite(path, border);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("[ShopSceneBuilder] Sprite not found: " + path);
            return sprite;
        }

        private static UiSprites LoadUiSprites()
        {
            return new UiSprites
            {
                WoodPanel = LoadSprite(WoodPanelPath, new Vector4(48f, 48f, 48f, 48f)),
                DialogFrame = LoadSprite(DialogFramePath, Vector4.zero),
                Header = LoadSprite(HeaderPath, Vector4.zero),
                Label = LoadSprite(LabelPath, Vector4.zero),
                RoundWood = LoadSprite(RoundWoodPath, Vector4.zero),
                Knob = LoadSprite(KnobPath, Vector4.zero),
                ButtonBlue = LoadSprite(ButtonBluePath, new Vector4(50f, 50f, 50f, 50f)),
                ButtonWood = LoadSprite(ButtonWoodPath, new Vector4(50f, 50f, 50f, 50f)),
                Book = LoadSprite(BookPath, Vector4.zero),
                BarBackground = LoadSprite(BarBackgroundPath, new Vector4(32f, 0f, 32f, 0f)),
                BarFill = LoadSprite(BarFillPath, new Vector4(32f, 0f, 32f, 0f)),
                Arrow = LoadSprite(ArrowPath, Vector4.zero),
                IconMenu = LoadSprite(IconMenuPath, Vector4.zero),
                IconClose = LoadSprite(IconClosePath, Vector4.zero),
                IconLock = LoadSprite(IconLockPath, Vector4.zero),
                IconTrophy = LoadSprite(IconTrophyPath, Vector4.zero),
            };
        }

        // ------------------------------------------------------------------ world

        private static void SetupLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.64f, 0.57f, 0.50f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = SkyColor;
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 90f;

            // Warm sun from behind the player so customers' faces are lit; the shell casts no shadows.
            GameObject sunGo = new GameObject("Sun");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.75f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(35f, 20f, 0f);

            // Two warm lantern lights inside the shop front.
            CreatePointLight("LanternLight_L", new Vector3(-1.5f, 2.1f, 0.4f));
            CreatePointLight("LanternLight_R", new Vector3(1.5f, 2.1f, 0.4f));
        }

        private static void CreatePointLight(string name, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = LanternColor;
            light.intensity = 2f;
            light.range = 4.5f;
            light.shadows = LightShadows.None;
        }

        /// <summary>Placeholder shop building: floor, walls, ceiling, front posts, header beam, striped awning, street.</summary>
        private static void BuildShell(Transform parent, float frontZ, float backZ)
        {
            Material floor = GetOrCreateMaterial("Shop_Floor", FloorColor);
            Material wall = GetOrCreateMaterial("Shop_Wall", WallColor);
            Material beam = GetOrCreateMaterial("Shop_Beam", BeamColor);
            Material blue = GetOrCreateMaterial("Shop_AwningBlue", AwningBlue);
            Material white = GetOrCreateMaterial("Shop_AwningWhite", AwningWhite);
            Material street = GetOrCreateMaterial("Shop_Street", StreetColor);

            GameObject shell = new GameObject("ShopShell_Placeholder");
            shell.transform.SetParent(parent, false);
            Transform t = shell.transform;

            float shopFront = frontZ + 0.25f;
            float shopBack = backZ - 1.9f;
            float depth = shopFront - shopBack;
            float midZ = (shopFront + shopBack) * 0.5f;
            const float halfWidth = 2.2f;
            const float height = 2.8f;

            Box("Floor", t, new Vector3(0f, -0.05f, midZ), new Vector3(halfWidth * 2f, 0.1f, depth), floor);
            Box("BackWall", t, new Vector3(0f, height * 0.5f, shopBack), new Vector3(halfWidth * 2f, height, 0.15f), wall);
            Box("SideWall_L", t, new Vector3(-halfWidth, height * 0.5f, midZ), new Vector3(0.15f, height, depth), wall);
            Box("SideWall_R", t, new Vector3(halfWidth, height * 0.5f, midZ), new Vector3(0.15f, height, depth), wall);
            Box("Ceiling", t, new Vector3(0f, height, midZ), new Vector3(halfWidth * 2f, 0.1f, depth), beam);
            Box("FrontPost_L", t, new Vector3(-halfWidth + 0.15f, height * 0.5f, shopFront), new Vector3(0.25f, height, 0.25f), beam);
            Box("FrontPost_R", t, new Vector3(halfWidth - 0.15f, height * 0.5f, shopFront), new Vector3(0.25f, height, 0.25f), beam);
            Box("HeaderBeam", t, new Vector3(0f, height - 0.15f, shopFront), new Vector3(halfWidth * 2f, 0.3f, 0.3f), beam);

            // Blue-and-white striped awning sloping down toward the street.
            // Wider than the shop so it reaches both screen edges, and raised so it frames the top without hiding the street.
            const int stripes = 12;
            const float awningHalfWidth = 4.1f;
            const float awningLift = 0.2f;
            float stripeWidth = awningHalfWidth * 2f / stripes;
            const float awningLength = 1.3f;
            const float awningDrop = 0.5f;
            float pitch = Mathf.Atan2(awningDrop, awningLength) * Mathf.Rad2Deg;
            Vector3 awningCentre = new Vector3(0f, height - 0.3f - awningDrop * 0.5f + awningLift, shopFront + awningLength * 0.5f);
            for (int i = 0; i < stripes; i++)
            {
                float x = -awningHalfWidth + stripeWidth * (i + 0.5f);
                GameObject stripe = Box("AwningStripe_" + i, t, new Vector3(x, awningCentre.y, awningCentre.z),
                    new Vector3(stripeWidth, 0.04f, Mathf.Sqrt(awningLength * awningLength + awningDrop * awningDrop)),
                    i % 2 == 0 ? blue : white);
                stripe.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            // Street: the v2 cobblestone texture tiled every 4 m when it is in the project, else the flat colour.
            Texture2D cobble = AssetDatabase.LoadAssetAtPath<Texture2D>(CobblestonePath);
            if (cobble != null)
            {
                street = GetOrCreateMaterial("Shop_Cobblestone", Color.white);
                street.SetTexture("_BaseMap", cobble);
                street.SetTextureScale("_BaseMap", new Vector2(60f / CobbleTileMetres, 40f / CobbleTileMetres));
                EditorUtility.SetDirty(street);
            }
            Box("Street", t, new Vector3(0f, -0.06f, shopFront + 20f), new Vector3(60f, 0.1f, 40f), street, true);
        }

        private const string CobblestonePath = "Assets/Art/Shop/ShopPaving.png"; // sunny town-square paving (Meshy text-to-image)
        /// <summary>One cobblestone texture repeat on the street, metres.</summary>
        private const float CobbleTileMetres = 4f;

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool receiveOnly = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return go;
        }

        private static void BuildNature(Transform parent)
        {
            GameObject root = new GameObject("Nature");
            root.transform.SetParent(parent, false);
            Material kaykit = GetOrCreateKayKitMaterial();

            foreach (DressingSpec spec in Nature)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(KayKitFolder + spec.File + ".fbx");
                if (model == null)
                {
                    Debug.LogWarning("[ShopSceneBuilder] KayKit model not found: " + spec.File);
                    continue;
                }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
                go.transform.SetParent(root.transform, false);
                go.transform.SetPositionAndRotation(new Vector3(spec.X, 0f, spec.Z), Quaternion.Euler(0f, spec.Yaw, 0f));
                if (kaykit == null) continue;
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = kaykit;
                    r.sharedMaterials = mats;
                }
            }
        }

        private static Material GetOrCreateKayKitMaterial()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(KayKitFolder + "forest_texture.png");
            if (texture == null) return null; // keep the FBX's own materials
            Material mat = GetOrCreateMaterial("Shop_KayKitNature", Color.white);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// The storybook town across the square, seen over the counter (cover / opening CGI): a fountain in the
        /// middle, lamps, flower barrels and market stalls near the shop, blue-roofed houses round the square and
        /// castle towers behind. Meshy models from Assets/Art/Props/Town; missing ones are skipped.
        /// </summary>
        private static void BuildTown(Transform parent)
        {
            TownDressing.Piece[] pieces =
            {
                // Square furniture kept inside the counter view (between the crystal ball and the map board).
                new TownDressing.Piece("Fountain", 0.5f, 0f, 19f, 180f, 3.8f),
                new TownDressing.Piece("StreetLamp", -3.4f, 0f, 10.5f, 200f, 3.4f),
                new TownDressing.Piece("StreetLamp", 3.6f, 0f, 11f, 160f, 3.4f),
                new TownDressing.Piece("FlowerBarrel", -2.3f, 0f, 9.5f, 0f, 0.9f),
                new TownDressing.Piece("FlowerBarrel", 2.6f, 0f, 10f, 60f, 0.9f),
                new TownDressing.Piece("MarketStall", -5.6f, 0f, 14f, 155f, 2.8f),
                new TownDressing.Piece("MarketStall", 6f, 0f, 14.5f, 205f, 2.8f),
                new TownDressing.Piece("FlowerBarrel", -4.2f, 0f, 15.5f, 30f, 0.9f),
                new TownDressing.Piece("FlowerBarrel", 4.6f, 0f, 16f, 200f, 0.9f),
                new TownDressing.Piece("StreetLamp", -8.5f, 0f, 18f, 170f, 3.6f),
                new TownDressing.Piece("StreetLamp", 9f, 0f, 18.5f, 190f, 3.6f),
                new TownDressing.Piece("TownHouseA", -19f, 0f, 24f, 165f, 9f),
                new TownDressing.Piece("TownHouseB", -9.5f, 0f, 27f, 180f, 12f),
                new TownDressing.Piece("TownHouseA", 1f, 0f, 29f, 180f, 9.5f),
                new TownDressing.Piece("TownHouseB", 11f, 0f, 26.5f, 185f, 12.5f),
                new TownDressing.Piece("TownHouseA", 20f, 0f, 23f, 200f, 9f),
                new TownDressing.Piece("TownHouseB", -21f, 0f, 11f, 100f, 11f),
                new TownDressing.Piece("TownHouseA", 21.5f, 0f, 12f, 260f, 9f),
                new TownDressing.Piece("CastleTower", -13f, 0f, 44f, 180f, 24f),
                new TownDressing.Piece("CastleTower", 15f, 0f, 48f, 180f, 28f),
                new TownDressing.Piece("CastleTower", 2f, 0f, 60f, 180f, 34f),
            };
            TownDressing.Place(parent, "Town", pieces);
        }

        /// <summary>The Achievement book on the counter (the user kept this stand-in): blue cover, cream pages, gold star.</summary>
        private static GameObject BuildBookStandIn(Transform parent, Vector3 position, float yaw)
        {

            Material cover = GetOrCreateMaterial("Shop_BookCover", BookCoverColor);
            Material pages = GetOrCreateMaterial("Shop_BookPages", BookPageColor);
            Material gold = GetOrCreateMaterial("Shop_Gold", GoldColor);

            GameObject book = new GameObject("AchievementBook_Placeholder");
            book.transform.SetParent(parent, false);
            book.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            BookPart("BackCover", book.transform, new Vector3(0f, 0.006f, 0f), new Vector3(0.26f, 0.012f, 0.2f), cover);
            BookPart("Pages", book.transform, new Vector3(0.004f, 0.027f, 0f), new Vector3(0.25f, 0.03f, 0.19f), pages);
            BookPart("FrontCover", book.transform, new Vector3(0f, 0.048f, 0f), new Vector3(0.26f, 0.012f, 0.2f), cover);
            GameObject star = BookPart("Star", book.transform, new Vector3(0f, 0.056f, 0f), new Vector3(0.05f, 0.004f, 0.05f), gold);
            star.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

            BoxCollider box = book.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.04f, 0f);
            box.size = new Vector3(0.32f, 0.1f, 0.26f); // a little bigger than the book for easy tapping
            return book;
        }

        private static GameObject BookPart(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject PlacePrefab(string path, Vector3 position, float yaw, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[ShopSceneBuilder] Prefab not found: " + path);
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        private static Bounds WorldBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.2f);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        /// <summary>Adds a BoxCollider on the root that wraps all child renderers, scaled by <paramref name="padding"/>.</summary>
        private static BoxCollider AddFittedCollider(GameObject go, float padding)
        {
            Quaternion rotation = go.transform.rotation;
            go.transform.rotation = Quaternion.identity; // measure axis-aligned in the root's own frame
            Bounds bounds = WorldBounds(go);
            go.transform.rotation = rotation;

            Vector3 scale = go.transform.lossyScale;
            BoxCollider box = go.AddComponent<BoxCollider>();
            Vector3 offset = bounds.center - go.transform.position;
            box.center = new Vector3(offset.x / scale.x, offset.y / scale.y, offset.z / scale.z);
            Vector3 size = bounds.size * padding;
            box.size = new Vector3(size.x / scale.x, size.y / scale.y, size.z / scale.z);
            return box;
        }

        private static Transform CreatePoint(string name, Transform parent, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go.transform;
        }

        // ------------------------------------------------------------------ UI

        private static void BuildUi(ShopFlowController flow, ShopPointerInput pointer, CustomerController customers, GameObject bottle,
            MapCatalog catalog, CustomerRoster roster, ShopSession session, ShopStrings strings, ShopUiConfig uiConfig,
            ShopAudioSettings audioSettings, UiSprites sprites)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasGo = new GameObject("ShopCanvas", typeof(RectTransform));
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform c = canvasGo.transform;

            // --- prompt (top centre)
            Text prompt = CreateText("Prompt", c, font, strings.idlePrompt, 40, TextAnchor.MiddleCenter, Color.white);
            prompt.rectTransform.anchorMin = prompt.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            prompt.rectTransform.pivot = new Vector2(0.5f, 1f);
            prompt.rectTransform.anchoredPosition = new Vector2(0f, -48f);
            prompt.rectTransform.sizeDelta = new Vector2(1300f, 70f);
            prompt.fontStyle = FontStyle.Bold;
            prompt.gameObject.AddComponent<Outline>().effectColor = new Color(0.2f, 0.12f, 0.06f, 0.9f);
            prompt.gameObject.SetActive(false);

            // --- dialog box (bottom)
            RectTransform dialogRt = CreateRect("DialogBox", c);
            dialogRt.anchorMin = dialogRt.anchorMax = new Vector2(0.5f, 0f);
            dialogRt.pivot = new Vector2(0.5f, 0f);
            dialogRt.anchoredPosition = new Vector2(0f, 24f);
            dialogRt.sizeDelta = new Vector2(1560f, 240f);
            AddImage(dialogRt, sprites.DialogFrame != null ? sprites.DialogFrame : sprites.WoodPanel, Color.white, true, true);
            Text dialogText = CreateText("Body", dialogRt, font, "", 42, TextAnchor.UpperLeft, DarkBrownText);
            Stretch(dialogText.rectTransform);
            dialogText.rectTransform.offsetMin = new Vector2(64f, 36f);
            dialogText.rectTransform.offsetMax = new Vector2(-64f, -36f);
            dialogText.horizontalOverflow = HorizontalWrapMode.Wrap;
            dialogText.supportRichText = true;
            RectTransform arrow = CreateRect("ContinueArrow", dialogRt);
            arrow.anchorMin = arrow.anchorMax = new Vector2(1f, 0f);
            arrow.pivot = new Vector2(0.5f, 0.5f);
            arrow.anchoredPosition = new Vector2(-56f, 36f);
            arrow.sizeDelta = new Vector2(64f, 40f);
            AddImage(arrow, sprites.Arrow, Color.white, false, false);
            DialogBox dialog = dialogRt.gameObject.AddComponent<DialogBox>();
            SetRefs(dialog, ("config", uiConfig), ("strings", strings), ("root", dialogRt.gameObject),
                ("bodyText", dialogText), ("continueIndicator", arrow.gameObject));
            dialogRt.gameObject.SetActive(false);

            // --- end of day placeholder
            RectTransform endRoot = CreateOverlay("EndOfDayPanel", c, 0.55f);
            RectTransform endCard = CreateCard("Card", endRoot, sprites.WoodPanel, new Vector2(0f, 0f), new Vector2(900f, 380f));
            Text endTitle = CreateText("Title", endCard, font, strings.endOfDayTitle, 80, TextAnchor.MiddleCenter, DarkBrownText);
            PlaceCentered(endTitle.rectTransform, new Vector2(0f, 70f), new Vector2(820f, 110f));
            endTitle.fontStyle = FontStyle.Bold;
            Text endBody = CreateText("Body", endCard, font, strings.endOfDayBody, 44, TextAnchor.MiddleCenter, DarkBrownText);
            PlaceCentered(endBody.rectTransform, new Vector2(0f, -60f), new Vector2(820f, 90f));
            endRoot.gameObject.SetActive(false);

            // --- map select panel
            RectTransform mapHost = CreateRect("MapSelect", c);
            Stretch(mapHost);
            MapSelectPanel mapPanel = mapHost.gameObject.AddComponent<MapSelectPanel>();
            RectTransform mapRoot = CreateOverlay("MapPanelRoot", mapHost, 0.7f);
            RectTransform mapCard = CreateCard("Card", mapRoot, sprites.WoodPanel, new Vector2(0f, -10f), new Vector2(1360f, 960f));
            // The four maps painted on one parchment (MapSelectBoard.png); the buttons are clear areas over the paintings.
            Sprite boardArt = LoadSprite(MapsFolder + "/MapSelectBoard.png", Vector4.zero);
            if (boardArt != null)
            {
                RectTransform artRt = CreateRect("BoardArt", mapCard);
                PlaceCentered(artRt, MapBoardArtCentre, MapBoardArtSize);
                AddImage(artRt, boardArt, Color.white, false, true).raycastTarget = false;
            }
            RectTransform mapHeader = CreateRect("Header", mapCard);
            PlaceCentered(mapHeader, new Vector2(0f, 450f), SuctionStageBuilder.HeaderSize);
            AddImage(mapHeader, sprites.Header, Color.white, false, false);
            // White with a dark outline (readable on the blue ribbon), on the ribbon's flat face (its upper part),
            // clear of the star ends; long titles shrink to fit.
            Text mapTitle = CreateText("Title", mapHeader, font, strings.mapPanelTitle, 50, TextAnchor.MiddleCenter, Color.white);
            Stretch(mapTitle.rectTransform);
            SuctionStageBuilder.PlaceOnHeaderFace(mapTitle.rectTransform);
            mapTitle.fontStyle = FontStyle.Bold;
            mapTitle.resizeTextForBestFit = true;
            mapTitle.resizeTextMinSize = 28;
            mapTitle.resizeTextMaxSize = 50;
            Outline mapTitleOutline = mapTitle.gameObject.AddComponent<Outline>();
            mapTitleOutline.effectColor = new Color(0.2f, 0.12f, 0.06f, 0.9f);
            mapTitleOutline.effectDistance = new Vector2(3f, -3f);

            var mapButtons = new Object[Maps.Length];
            var mapImages = new Object[Maps.Length];
            var mapLabels = new Object[Maps.Length];
            for (int i = 0; i < Maps.Length; i++)
            {
                MapSpec spec = Maps[i];
                MapEntry entry = catalog.Count > i ? catalog.maps[i] : null;
                RectTransform buttonRt = CreateRect("Map_" + spec.DisplayName, mapCard);
                Rect painted = boardArt != null ? PaintedMapRect(spec.Id) : new Rect(spec.GridPosition + MapPictureNudge, MapPictureSize);
                PlaceCentered(buttonRt, painted.position, painted.size);
                // Over the board art the picture is invisible (the painting shows); without the art it is the thumbnail.
                Image picture = AddImage(buttonRt, entry != null ? entry.thumbnail : null, boardArt != null ? new Color(1f, 1f, 1f, 0f) : Color.white, false, true);
                Button button = buttonRt.gameObject.AddComponent<Button>();
                button.targetGraphic = picture;
                ColorBlock colors = button.colors;
                colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
                colors.pressedColor = new Color(0.85f, 0.8f, 0.7f);
                colors.selectedColor = Color.white;
                button.colors = colors;

                RectTransform labelRt = CreateRect("Label", buttonRt);
                PlaceCentered(labelRt, new Vector2(0f, -painted.height * 0.5f + 52f), new Vector2(330f, 84f)); // near the painting's bottom edge
                AddImage(labelRt, sprites.Label, Color.white, false, false);
                Text label = CreateText("Text", labelRt, font, spec.DisplayName, 44, TextAnchor.MiddleCenter, DarkBrownText);
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(36f, 6f); // inside the capsule, clear of its rivets
                label.rectTransform.offsetMax = new Vector2(-36f, -6f);
                label.fontStyle = FontStyle.Bold;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 26;
                label.resizeTextMaxSize = 44;

                mapButtons[i] = button;
                mapImages[i] = picture;
                mapLabels[i] = label;
            }
            Button mapClose = CreateIconButton("CloseButton", mapCard, sprites.IconClose, new Vector2(640f, 440f), new Vector2(110f, 110f));
            SetRefs(mapPanel, ("catalog", catalog), ("strings", strings), ("root", mapRoot.gameObject),
                ("titleText", mapTitle), ("closeButton", mapClose));
            SetRefArray(mapPanel, "mapButtons", mapButtons);
            SetRefArray(mapPanel, "mapImages", mapImages);
            SetRefArray(mapPanel, "mapLabels", mapLabels);
            mapRoot.gameObject.SetActive(false);

            // --- achievement book
            RectTransform bookHost = CreateRect("AchievementBook", c);
            Stretch(bookHost);
            AchievementBookPanel bookPanel = bookHost.gameObject.AddComponent<AchievementBookPanel>();
            RectTransform bookRoot = CreateOverlay("BookRoot", bookHost, 0.7f);
            RectTransform bookImage = CreateRect("Book", bookRoot);
            PlaceCentered(bookImage, Vector2.zero, new Vector2(1400f, 753f));
            AddImage(bookImage, sprites.Book, Color.white, false, true).preserveAspect = true;
            Text bookTitle = CreateText("Title", bookImage, font, strings.achievementsTitle, 64, TextAnchor.MiddleCenter, DarkBrownText);
            PlaceCentered(bookTitle.rectTransform, new Vector2(-290f, 190f), new Vector2(480f, 100f)); // centred on the v2 left page (x -569..-12)
            bookTitle.fontStyle = FontStyle.Bold;
            RectTransform trophy = CreateRect("Trophy", bookImage);
            PlaceCentered(trophy, new Vector2(-290f, -50f), new Vector2(220f, 220f));
            AddImage(trophy, sprites.IconTrophy, Color.white, false, false).preserveAspect = true;

            var rowLabels = new Object[4];
            var rowLocks = new Object[4];
            AchievementId[] ids = { AchievementId.Die10Times, AchievementId.Earn12Stars, AchievementId.NormalEnding, AchievementId.TrueEnding };
            for (int i = 0; i < ids.Length; i++)
            {
                float y = 200f - i * 105f; // the last row stays clear of the bookmark
                RectTransform lockRt = CreateRect("Lock_" + i, bookImage);
                PlaceCentered(lockRt, new Vector2(110f, y), new Vector2(72f, 72f));
                AddImage(lockRt, sprites.IconLock, Color.white, false, false).preserveAspect = true;
                Text row = CreateText("Row_" + i, bookImage, font, strings.AchievementLabel(ids[i]), 44, TextAnchor.MiddleLeft, new Color(DarkBrownText.r, DarkBrownText.g, DarkBrownText.b, 0.55f));
                PlaceCentered(row.rectTransform, new Vector2(350f, y), new Vector2(380f, 80f));
                rowLabels[i] = row;
                rowLocks[i] = lockRt.gameObject;
            }
            Button bookClose = CreateIconButton("CloseButton", bookImage, sprites.IconClose, new Vector2(650f, 330f), new Vector2(100f, 100f));
            SetRefs(bookPanel, ("strings", strings), ("root", bookRoot.gameObject), ("titleText", bookTitle), ("closeButton", bookClose));
            SetRefArray(bookPanel, "rowLabels", rowLabels);
            SetRefArray(bookPanel, "rowLocks", rowLocks);
            bookRoot.gameObject.SetActive(false);

            // --- pause (menu button top-left + panel)
            RectTransform pauseHost = CreateRect("Pause", c);
            Stretch(pauseHost);
            PauseMenu pauseMenu = pauseHost.gameObject.AddComponent<PauseMenu>();
            RectTransform menuRt = CreateRect("MenuButton", pauseHost);
            menuRt.anchorMin = menuRt.anchorMax = new Vector2(0f, 1f);
            menuRt.pivot = new Vector2(0f, 1f);
            menuRt.anchoredPosition = new Vector2(32f, -32f);
            menuRt.sizeDelta = new Vector2(120f, 136f);
            // UI v2: one sprite with the bars on the round button; the old button + icon when it is missing.
            Sprite menuSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Sheet + "MenuButton.png");
            Image menuImg = AddImage(menuRt, menuSprite != null ? menuSprite : sprites.RoundWood, Color.white, false, true);
            menuImg.preserveAspect = true;
            Button menuButton = menuRt.gameObject.AddComponent<Button>();
            menuButton.targetGraphic = menuImg;
            if (menuSprite == null)
            {
                RectTransform menuIcon = CreateRect("Icon", menuRt);
                PlaceCentered(menuIcon, new Vector2(0f, 8f), new Vector2(72f, 72f));
                AddImage(menuIcon, sprites.IconMenu, Color.white, false, false).preserveAspect = true;
            }

            RectTransform pauseRoot = CreateOverlay("PauseRoot", pauseHost, 0.7f);
            RectTransform pauseCard = CreateCard("Card", pauseRoot, sprites.WoodPanel, Vector2.zero, new Vector2(980f, 760f));
            Text pauseTitle = CreateText("Title", pauseCard, font, strings.pauseTitle, 72, TextAnchor.MiddleCenter, DarkBrownText);
            PlaceCentered(pauseTitle.rectTransform, new Vector2(0f, 185f), new Vector2(800f, 100f)); // below the v2 panel crest
            pauseTitle.fontStyle = FontStyle.Bold;
            Text allLabel; Text musicLabel; Text sfxLabel;
            Slider allSlider = CreateVolumeRow("All", pauseCard, font, sprites, strings.volumeAllLabel, 85f, out allLabel);
            Slider musicSlider = CreateVolumeRow("Music", pauseCard, font, sprites, strings.volumeMusicLabel, -20f, out musicLabel);
            Slider sfxSlider = CreateVolumeRow("SFX", pauseCard, font, sprites, strings.volumeSfxLabel, -125f, out sfxLabel);
            Text backLabel;
            Button backButton = CreateTextButton("BackButton", pauseCard, sprites.ButtonBlue, font, strings.backLabel, new Vector2(-200f, -240f), new Vector2(340f, 110f), Color.white, out backLabel);
            Text titleMenuLabel;
            Button titleMenuButton = CreateTextButton("MenuButton", pauseCard, sprites.ButtonWood, font, strings.menuLabel, new Vector2(200f, -240f), new Vector2(340f, 110f), DarkBrownText, out titleMenuLabel);
            CanvasGroup titleMenuGroup = titleMenuButton.gameObject.AddComponent<CanvasGroup>();
            titleMenuGroup.alpha = 0.45f;
            titleMenuButton.interactable = false; // Title scene arrives in story 006
            SetRefs(pauseMenu,
                ("settings", audioSettings), ("config", uiConfig), ("strings", strings),
                ("menuButton", menuButton), ("root", pauseRoot.gameObject), ("titleText", pauseTitle),
                ("allSlider", allSlider), ("musicSlider", musicSlider), ("sfxSlider", sfxSlider),
                ("allLabel", allLabel), ("musicLabel", musicLabel), ("sfxLabel", sfxLabel),
                ("backButton", backButton), ("backLabel", backLabel),
                ("titleMenuButton", titleMenuButton), ("titleMenuLabel", titleMenuLabel), ("titleMenuGroup", titleMenuGroup));
            pauseRoot.gameObject.SetActive(false);
            SavePausePrefab(pauseHost.gameObject);

            // --- loading screen (topmost)
            RectTransform loadingHost = CreateRect("Loading", c);
            Stretch(loadingHost);
            LoadingScreen loading = loadingHost.gameObject.AddComponent<LoadingScreen>();
            RectTransform loadingRoot = CreateRect("LoadingRoot", loadingHost);
            Stretch(loadingRoot);
            AddImage(loadingRoot, null, Color.black, false, true);
            // Sam runs on the spot on an off-screen stage; its camera renders into a RenderTexture shown here.
            RenderTexture samTexture = GetOrCreateLoadingTexture();
            GameObject samStage = BuildLoadingStage(samTexture, out Animator samAnimator);
            RectTransform samRt = CreateRect("SamRunning", loadingRoot);
            PlaceCentered(samRt, new Vector2(0f, 150f), new Vector2(580f, 580f));
            RawImage samImage = samRt.gameObject.AddComponent<RawImage>();
            samImage.texture = samTexture;
            samImage.raycastTarget = false;
            Text mapName = CreateText("MapName", loadingRoot, font, "", 84, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.3f));
            PlaceCentered(mapName.rectTransform, new Vector2(0f, -205f), new Vector2(1200f, 120f));
            mapName.fontStyle = FontStyle.Bold;
            Text loadingText = CreateText("LoadingText", loadingRoot, font, strings.loadingLabel, 52, TextAnchor.MiddleCenter, Color.white);
            PlaceCentered(loadingText.rectTransform, new Vector2(0f, -305f), new Vector2(800f, 80f));
            SetRefs(loading, ("config", uiConfig), ("settings", audioSettings), ("strings", strings),
                ("root", loadingRoot.gameObject), ("samStage", samStage), ("samAnimator", samAnimator), ("mapNameText", mapName), ("loadingText", loadingText));
            loadingRoot.gameObject.SetActive(false);
            SetRefs(pauseMenu, ("loadingScreen", loading));

            // --- wire the flow controller
            SetRefs(flow,
                ("roster", roster), ("catalog", catalog), ("session", session), ("strings", strings),
                ("customers", customers), ("pointer", pointer), ("bottleOnCounter", bottle),
                ("dialog", dialog), ("mapPanel", mapPanel), ("loadingScreen", loading),
                ("achievementBook", bookPanel), ("pauseMenu", pauseMenu), ("promptText", prompt),
                ("endOfDayPanel", endRoot.gameObject), ("endOfDayTitle", endTitle), ("endOfDayBody", endBody),
                ("progress", GetOrCreateAsset<WeatherForSale.Progress.ProgressService>("Assets/Data/ProgressService.asset")));

            // Story Mode (story 006): the last customer served leads to an ending.
            var story = flow.gameObject.AddComponent<WeatherForSale.Story.StoryModeController>();
            SetRefs(story, ("flow", flow), ("state", FrontEndSceneBuilder.GetOrCreateModeState()),
                ("progress", GetOrCreateAsset<WeatherForSale.Progress.ProgressService>("Assets/Data/ProgressService.asset")),
                ("strings", FrontEndSceneBuilder.GetOrCreateStrings()), ("customers", customers), ("dialog", dialog),
                ("uncleJohn", GetOrCreateUncleJohn()));

            // The sky grows greyer with each customer served (story 006).
            var mood = flow.gameObject.AddComponent<ShopSkyMood>();
            GameObject mainCam = GameObject.Find("Main Camera");
            SetRefs(mood, ("session", session), ("flow", flow), ("viewCamera", mainCam != null ? mainCam.GetComponent<Camera>() : null),
                ("sun", GameObject.Find("Sun") != null ? GameObject.Find("Sun").GetComponent<Light>() : null));
        }

        /// <summary>Uncle John as a CustomerData (his model; the Story controller walks him to the counter).</summary>
        private static CustomerData GetOrCreateUncleJohn()
        {
            const string path = "Assets/Data/Shop/Customer_UncleJohn.asset";
            var data = AssetDatabase.LoadAssetAtPath<CustomerData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CustomerData>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.displayName = "Uncle John";
            data.prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/UncleJohn/UncleJohn.prefab");
            EditorUtility.SetDirty(data);
            return data;
        }

        private static Slider CreateVolumeRow(string name, Transform parent, Font font, UiSprites sprites, string labelText, float y, out Text label)
        {
            label = CreateText(name + "Label", parent, font, labelText, 48, TextAnchor.MiddleLeft, DarkBrownText);
            PlaceCentered(label.rectTransform, new Vector2(-265f, y), new Vector2(200f, 80f)); // clear of the v2 frame
            label.fontStyle = FontStyle.Bold;

            RectTransform sliderRt = CreateRect(name + "Slider", parent);
            PlaceCentered(sliderRt, new Vector2(110f, y), new Vector2(540f, 56f));

            RectTransform background = CreateRect("Background", sliderRt);
            Stretch(background);
            // The track takes taps and drags too, not just the small knob (touch target).
            AddImage(background, sprites.BarBackground, Color.white, true, true);

            RectTransform fillArea = CreateRect("Fill Area", sliderRt);
            Stretch(fillArea);
            // Inset past the track's rounded ends so the fill never pokes out of it.
            fillArea.offsetMin = new Vector2(14f, 9f);
            fillArea.offsetMax = new Vector2(-14f, -9f);
            RectTransform fill = CreateRect("Fill", fillArea);
            Stretch(fill);
            AddImage(fill, sprites.BarFill, Color.white, true, false);

            RectTransform handleArea = CreateRect("Handle Slide Area", sliderRt);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(28f, 0f);
            handleArea.offsetMax = new Vector2(-28f, 0f);
            RectTransform handle = CreateRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(64f, 56f);
            Image handleImage = AddImage(handle, sprites.Knob, Color.white, false, true);
            handleImage.preserveAspect = true;

            Slider slider = sliderRt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.value = 100f;
            return slider;
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

        private static Button CreateIconButton(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            RectTransform rt = CreateRect(name, parent);
            PlaceCentered(rt, position, size);
            Image img = AddImage(rt, sprite, Color.white, false, true);
            img.preserveAspect = true;
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            return button;
        }

        /// <summary>Full-screen dim layer that also blocks clicks to the 3D scene behind it.</summary>
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

        // ------------------------------------------------------------------ assets / settings

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        /// <summary>Adds the scene at the end of Build Settings if missing; never reorders existing scenes.</summary>
        private static void AppendToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            // The shop is where the game starts, so it must be scene 0; SampleScene is the template's.
            scenes.RemoveAll(s => s.path == scenePath || s.path == "Assets/Scenes/SampleScene.unity");
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }


        // ------------------------------------------------------------------ loading screen: Sam on a stage

        private const string LoadingTexturePath = "Assets/Art/UI/LoadingSam.renderTexture";
        private const string SamRigPath = CharactersFolder + "Sam/Sam_Rigged.fbx";
        private const string SamControllerPath = CharactersFolder + "Sam/Sam.controller";

        private static RenderTexture GetOrCreateLoadingTexture()
        {
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(LoadingTexturePath);
            if (rt != null) return rt;
            // RGBA so the area around Sam is transparent over the Loading background.
            rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "LoadingSam" };
            AssetDatabase.CreateAsset(rt, LoadingTexturePath);
            return rt;
        }

        /// <summary>
        /// Builds the stage far below the shop (the stage camera's short far plane keeps the shop out of its
        /// picture): Sam in the rig, turned to face screen-left, and a side-on camera. The shop's sun lights him.
        /// </summary>
        private static GameObject BuildLoadingStage(RenderTexture target, out Animator animator)
        {
            animator = null;
            GameObject stage = new GameObject("LoadingStage");
            stage.transform.position = new Vector3(0f, -500f, 0f);

            GameObject samPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SamRigPath);
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SamControllerPath);
            if (samPrefab == null || controller == null)
            {
                Debug.LogWarning("[ShopSceneBuilder] Sam model or controller missing: Loading screen has no runner.");
            }
            else
            {
                GameObject sam = (GameObject)PrefabUtility.InstantiatePrefab(samPrefab, stage.transform);
                sam.name = "Sam";
                sam.transform.localPosition = Vector3.zero;
                // The camera looks along +Z, so screen-left is -X: turn Sam to face -X (a side view, not the back).
                sam.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                animator = sam.GetComponent<Animator>();
                if (animator == null) animator = sam.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            GameObject camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(stage.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0.72f, -3.2f);
            camGo.transform.localRotation = Quaternion.identity;
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 8f;
            cam.targetTexture = target;
            stage.SetActive(false);
            return stage;
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
                    Debug.LogError("[ShopSceneBuilder] Missing serialized field '" + field + "' on " + target.GetType().Name);
                    continue;
                }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError("[ShopSceneBuilder] Missing serialized field '" + field + "' on " + target.GetType().Name);
                return;
            }
            prop.floatValue = value;
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

        private static void SetPointerTargets(ShopPointerInput pointer, params (ShopClickTarget id, Collider collider, Transform visual)[] targets)
        {
            SerializedObject so = new SerializedObject(pointer);
            SerializedProperty array = so.FindProperty("targets");
            array.arraySize = targets.Length;
            for (int i = 0; i < targets.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").enumValueIndex = (int)targets[i].id;
                element.FindPropertyRelative("collider").objectReferenceValue = targets[i].collider;
                element.FindPropertyRelative("visual").objectReferenceValue = targets[i].visual;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
