using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WeatherForSale.Boss;
using WeatherForSale.Obby;
using WeatherForSale.Suction;
using WeatherForSale.UI;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/Boss_Tempest.unity: the flat boss arena over a swirling storm sky, Sam, Tempest,
    /// the purple bottle, the boss HUD and the touch controls, plus the BossConfig and attack pattern assets
    /// (existing assets keep their tuning). Menu: Weather For Sale / Build Boss Scene.
    /// Implements: story-008 criteria 1 and the scene part of the Implementation Notes.
    /// </summary>
    public static class BossSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Boss_Tempest.unity";
        private const string DataFolder = "Assets/Data/Boss";
        private const string ConfigPath = DataFolder + "/BossConfig.asset";
        private const string PlayerConfigPath = DataFolder + "/BossPlayerConfig.asset";
        private const string SourcePlayerConfig = "Assets/Data/ObbyLevelConfig_Valley.asset";
        private const string MaterialFolder = "Assets/Materials/Boss";
        private const string TempestPrefab = "Assets/Art/Characters/Tempest/Tempest.prefab";
        private const string BottlePrefab = "Assets/Art/Props/Bottle/Bottle.prefab";
        private const string Sheet = "Assets/Art/UI/Sheet/";
        private const float BossHeightMetres = 13f;

        /// <summary>Menu entry: builds and saves the boss scene.</summary>
        [MenuItem("Weather For Sale/Build Boss Scene")]
        public static void BuildFromMenu()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        /// <summary>Batch-mode entry point (-executeMethod). Exits non-zero on failure.</summary>
        public static void BuildFromCommandLine()
        {
            try { Build(); }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void Build()
        {
            ObbyTestSceneBuilder.EnsureFolder("Assets/Data");
            ObbyTestSceneBuilder.EnsureFolder(DataFolder);
            ObbyTestSceneBuilder.EnsureFolder("Assets/Materials");
            ObbyTestSceneBuilder.EnsureFolder(MaterialFolder);
            ObbyTestSceneBuilder.EnsureFolder("Assets/Scenes");
            AssetDatabase.Refresh();

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            arenaDisc = GetOrCreateDiscMesh();
            BossConfig config = CreateConfigAssets();
            ObbyLevelConfig playerConfig = GetOrCopyPlayerConfig();
            Material skyMat = GetOrCreateSkyMaterial();
            Material playerMat = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_Player", new Color(1f, 0.45f, 0.1f));
            Material whiteMat = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_White", Color.white);

            // Night storm over the town square (the "Tempest appears" CGI): purple ambient, dim violet moonlight,
            // purple fog swallowing the far town.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = StormAmbient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = StormFog;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 190f;

            // --- light and sky
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = StormMoon;
            sun.intensity = 0.8f;
            sun.shadows = LightShadows.Soft;

            var skyRoot = new GameObject("Sky");
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "SkySphere";
            sphere.transform.SetParent(skyRoot.transform, false);
            sphere.transform.localScale = Vector3.one * 600f;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            var sphereRenderer = sphere.GetComponent<MeshRenderer>();
            sphereRenderer.sharedMaterial = skyMat;
            sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sphereRenderer.receiveShadows = false;
            var sky = skyRoot.AddComponent<BossSky>();
            ObbyTestSceneBuilder.SetRefs(sky, ("skyRenderer", sphereRenderer), ("sun", sun));
            var skySo = new SerializedObject(sky);
            SerializedProperty stormSunProp = skySo.FindProperty("stormSun");
            if (stormSunProp != null) { stormSunProp.colorValue = StormMoon; skySo.ApplyModifiedPropertiesWithoutUndo(); }

            // --- arena (top surface at y = 0) and the town square around it
            BuildArena(config.arenaRadius);
            BuildStormTown(config.arenaRadius);
            var center = new GameObject("ArenaCenter").transform;
            center.position = Vector3.zero;
            var start = new GameObject("PlayerStart").transform;
            start.position = new Vector3(0f, 0.25f, -config.arenaRadius * 0.3f);
            start.rotation = Quaternion.identity;

            // --- camera
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.3f; // a larger near plane keeps the floor inlays from flickering (depth precision)
            cam.farClipPlane = 800f;
            camGo.AddComponent<AudioListener>();
            var thirdPerson = camGo.AddComponent<ThirdPersonCamera>();

            // --- player
            var playerGo = new GameObject("Player");
            playerGo.transform.position = start.position;
            var cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.7f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.85f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 50f;
            var body = playerGo.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var player = playerGo.AddComponent<PlayerController>();
            ObbyTestSceneBuilder.AddPlayerVisual(playerGo, player, playerMat, whiteMat);
            ObbyTestSceneBuilder.SetRefs(player, ("config", playerConfig), ("cameraReference", camGo.transform));
            ObbyTestSceneBuilder.SetRefs(thirdPerson, ("config", playerConfig), ("target", playerGo.transform));
            camGo.transform.position = playerGo.transform.position + new Vector3(0f, 4f, -7f);
            camGo.transform.LookAt(playerGo.transform.position + Vector3.up * 1.5f);

            // --- bottle (hidden until the boss is drawn in)
            var bottleRoot = new GameObject("PurpleBottle");
            bottleRoot.transform.position = new Vector3(3.2f, 0f, start.position.z + 1.5f);
            MeshyPropBaker.PropMesh purpleBottle = MeshyPropBaker.BakeProp("Bottles", "BottlePurple", MeshyPropBaker.ZUp);
            GameObject bottleAsset = purpleBottle != null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(BottlePrefab);
            if (purpleBottle != null)
            {
                // The purple bottle from design/ref/All Bottle.png (Meshy), about 1 m tall.
                MeshyPropBaker.PlaceProp(purpleBottle, "BottlePurple", bottleRoot.transform, Vector3.zero, new Vector3(0f, 1.0f, 0f));
            }
            else if (bottleAsset != null)
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(bottleAsset, bottleRoot.transform);
                b.transform.localPosition = Vector3.zero;
                bottleRoot.transform.localScale = Vector3.one * 1.8f;
            }
            var glowGo = new GameObject("PurpleGlow");
            glowGo.transform.SetParent(bottleRoot.transform, false);
            glowGo.transform.localPosition = Vector3.up * 0.6f;
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.7f, 0.35f, 1f);
            glow.intensity = 3f;
            glow.range = 6f;

            // --- boss
            var bossRoot = new GameObject("Tempest");
            bossRoot.transform.position = new Vector3(0f, config.bossHoverHeight, 0f);
            Transform visual = null;
            GameObject tempestAsset = AssetDatabase.LoadAssetAtPath<GameObject>(TempestPrefab);
            if (tempestAsset != null)
            {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(tempestAsset, bossRoot.transform);
                t.name = "TempestModel";
                t.transform.localPosition = Vector3.zero;
                FitHeight(t, BossHeightMetres);
                CentreOn(t, bossRoot.transform.position);
                visual = t.transform;
            }
            else
            {
                Debug.LogWarning("[BossSceneBuilder] " + TempestPrefab + " not found; using a grey sphere for the boss.");
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.transform.SetParent(bossRoot.transform, false);
                s.transform.localScale = Vector3.one * 3f;
                Object.DestroyImmediate(s.GetComponent<Collider>());
                visual = s.transform;
            }
            var boss = bossRoot.AddComponent<TempestBoss>();
            var suctionPoint = new GameObject("SuctionPoint").transform;
            suctionPoint.SetParent(bossRoot.transform, false);
            suctionPoint.localPosition = new Vector3(0f, -BossHeightMetres * 0.2f, 0f);

            // --- suction pose: the same Sam pose and streaks as the map suction, aimed at the boss
            SuctionStage pose = BuildSuctionPose(player, thirdPerson, camGo.transform);

            // --- UI
            UiRefs ui = BuildUi(config, player, thirdPerson);

            var suction = bossRoot.AddComponent<BossSuction>();
            ObbyTestSceneBuilder.SetRefs(suction,
                ("config", config), ("suctionConfig", SuctionStageBuilder.GetOrCreateConfig()), ("root", ui.SuctionRoot), ("gaugeFill", ui.SuctionFill),
                ("titleText", ui.SuctionTitle), ("keyPromptRoot", ui.KeyPromptRoot), ("keyCapText", ui.KeyCapText),
                ("promptText", ui.PromptText), ("tapButton", ui.TapButton), ("tapLabel", ui.TapLabel),
                ("pose", pose), ("player", player), ("target", suctionPoint));
            ObbyTestSceneBuilder.SetRefs(boss, ("config", config), ("visual", visual), ("suction", suction), ("absorbTarget", bottleRoot.transform));

            // --- fight controller
            GameObject crystalPrefab = GetOrCreateCrystalPrefab();
            var fightGo = new GameObject("BossFight");
            var fight = fightGo.AddComponent<BossFightController>();
            ObbyTestSceneBuilder.SetRefs(fight,
                ("config", config), ("player", player), ("boss", boss), ("sky", sky),
                ("arenaCenter", center), ("playerStart", start), ("bottle", bottleRoot), ("crystalPrefab", crystalPrefab), ("suction", suction));
            ObbyTestSceneBuilder.SetRefs(ui.Hud, ("fight", fight), ("boss", boss));
            // Story 006/007: after Boss Sealed, Story goes to the True Ending, Freeplay back to Level Select.
            var exit = fightGo.AddComponent<WeatherForSale.Story.BossFlowExit>();
            ObbyTestSceneBuilder.SetRefs(exit, ("fight", fight), ("state", FrontEndSceneBuilder.GetOrCreateModeState()));

            // --- event system
            var es = new GameObject("EventSystem");
            var eventSystem = es.AddComponent<EventSystem>();
            eventSystem.pixelDragThreshold = 1;
            var module = es.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            ObbyTestSceneBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BossSceneBuilder] Built " + ScenePath);
        }

        // ------------------------------------------------------------------ assets

        private static Mesh arenaDisc;

        private static Mesh GetOrCreateDiscMesh()
        {
            const string path = DataFolder + "/ArenaDiscUV.asset"; // with UVs, so the paving texture shows
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = BossShapes.BuildDisc(BossShapes.Segments);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>
        /// The Storm Crystal: a long six-sided purple crystal made from code (a bipyramid with flat faces), a glowing
        /// emissive material, a purple light and a pale beam so it can be seen from across the arena.
        /// </summary>
        private static GameObject GetOrCreateCrystalPrefab()
        {
            const string folder = "Assets/Art/Props/Boss";
            MeshyPropBaker.PropMesh crystalModel = MeshyPropBaker.BakeProp("Crystal", "ShardCrystal", MeshyPropBaker.ZUp);
            string prefabPath = folder + (crystalModel != null ? "/StormCrystal_Meshy.prefab" : "/StormCrystal.prefab");
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;
            ObbyTestSceneBuilder.EnsureFolder("Assets/Art");
            ObbyTestSceneBuilder.EnsureFolder("Assets/Art/Props");
            ObbyTestSceneBuilder.EnsureFolder(folder);

            var mesh = BuildCrystalMesh(6, 0.5f, 1.15f, 0.55f);
            AssetDatabase.CreateAsset(mesh, folder + "/StormCrystal_Mesh.asset");
            Material mat = Mat("Boss_Crystal", new Color(0.62f, 0.32f, 1f), 1.6f);
            var beamMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Boss_CrystalBeam.mat");
            if (beamMat == null)
            {
                beamMat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.7f, 0.45f, 1f, 0.22f) };
                AssetDatabase.CreateAsset(beamMat, MaterialFolder + "/Boss_CrystalBeam.mat");
            }

            var root = new GameObject("StormCrystal");
            var body = new GameObject("Crystal", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * 1.3f;
            body.GetComponent<MeshFilter>().sharedMesh = mesh;
            body.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (crystalModel != null)
            {
                // The Meshy Shard crystal, about 3 m tall, centred where the code-built one was.
                body.SetActive(false);
                MeshyPropBaker.PlaceProp(crystalModel, "CrystalModel", root.transform, new Vector3(0f, -1.5f, 0f), new Vector3(0f, 3f, 0f));
            }

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(root.transform, false);
            var glow = lightGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.75f, 0.5f, 1f);
            glow.intensity = 4f;
            glow.range = 9f;

            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "Beam";
            beam.GetComponent<MeshFilter>().sharedMesh = arenaDisc;
            Object.DestroyImmediate(beam.GetComponent<Collider>());
            beam.transform.SetParent(root.transform, false);
            beam.transform.localPosition = new Vector3(0f, 5f, 0f);
            beam.transform.localScale = new Vector3(1.1f, 6f, 1.1f);
            beam.GetComponent<MeshRenderer>().sharedMaterial = beamMat;
            beam.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            root.AddComponent<SphereCollider>();
            root.AddComponent<SuctionBoostItem>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>A flat-shaded double pyramid: <paramref name="sides"/> corners, half-height <paramref name="halfHeight"/>.</summary>
        private static Mesh BuildCrystalMesh(int sides, float radius, float halfHeight, float shoulder)
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            Vector3 top = Vector3.up * halfHeight, bottom = Vector3.down * halfHeight;
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = 2f * Mathf.PI * i / sides;
                ring[i] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            }
            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                // Wind every face so its normal points away from the centre (outwards).
                bool outward = Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f) >= 0f;
                int i = verts.Count;
                verts.Add(a); verts.Add(outward ? b : c); verts.Add(outward ? c : b);
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            }
            // The widest ring sits at the shoulder (shoulder x halfHeight above the middle) so the crystal is not a plain diamond.
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                Vector3 a = ring[i] + Vector3.up * (halfHeight * shoulder * 0.4f);
                Vector3 b = ring[j] + Vector3.up * (halfHeight * shoulder * 0.4f);
                Vector3 a2 = ring[i] - Vector3.up * (halfHeight * shoulder * 0.4f);
                Vector3 b2 = ring[j] - Vector3.up * (halfHeight * shoulder * 0.4f);
                Tri(top, b, a);          // upper facet
                Tri(a, b, b2);           // band, upper triangle
                Tri(a, b2, a2);          // band, lower triangle
                Tri(bottom, a2, b2);     // lower facet
            }
            var mesh = new Mesh { name = "StormCrystal" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static BossConfig CreateConfigAssets()
        {
            var config = AssetDatabase.LoadAssetAtPath<BossConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BossConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            if (config.patterns == null || config.patterns.Length == 0)
            {
                config.patterns = new BossAttackPattern[]
                {
                    GetOrCreatePattern<LightningStrikePattern>("Pattern_Lightning", "Lightning"),
                    GetOrCreatePattern<WindSweepPattern>("Pattern_WindSweep", "Wind sweep"),
                    GetOrCreatePattern<HailRingPattern>("Pattern_HailRing", "Hail rings"),
                };
                EditorUtility.SetDirty(config);
            }
            // Phase 1 plays one attack only: the lightning circles (the easiest to read).
            if (config.phase1Patterns == null || config.phase1Patterns.Length == 0)
            {
                foreach (BossAttackPattern existing in config.patterns)
                {
                    if (existing is LightningStrikePattern) { config.phase1Patterns = new[] { existing }; EditorUtility.SetDirty(config); }
                }
            }
            bool hasCharge = false;
            foreach (BossAttackPattern existing in config.patterns) hasCharge |= existing is ChargeDashPattern;
            if (!hasCharge)
            {
                var list = new System.Collections.Generic.List<BossAttackPattern>(config.patterns)
                {
                    GetOrCreatePattern<ChargeDashPattern>("Pattern_ChargeDash", "Charge!"),
                };
                config.patterns = list.ToArray();
                EditorUtility.SetDirty(config);
            }
            AssetDatabase.SaveAssets();
            return config;
        }

        private static T GetOrCreatePattern<T>(string file, string displayName) where T : BossAttackPattern
        {
            string path = DataFolder + "/" + file + ".asset";
            var pattern = AssetDatabase.LoadAssetAtPath<T>(path);
            if (pattern != null) return pattern;
            pattern = ScriptableObject.CreateInstance<T>();
            pattern.displayName = displayName;
            AssetDatabase.CreateAsset(pattern, path);
            return pattern;
        }

        private static ObbyLevelConfig GetOrCopyPlayerConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<ObbyLevelConfig>(PlayerConfigPath);
            if (cfg != null) return cfg;
            if (!AssetDatabase.CopyAsset(SourcePlayerConfig, PlayerConfigPath))
            {
                cfg = ScriptableObject.CreateInstance<ObbyLevelConfig>();
                AssetDatabase.CreateAsset(cfg, PlayerConfigPath);
                return cfg;
            }
            AssetDatabase.Refresh();
            cfg = AssetDatabase.LoadAssetAtPath<ObbyLevelConfig>(PlayerConfigPath);
            cfg.moveSpeed = 7f; // a bigger arena
            // The boss arena is a wide open floor: the camera sits higher and further back than in the obby courses.
            cfg.cameraDistance = 10f;
            cfg.cameraMaxDistance = 22f;
            cfg.cameraStartPitch = 14f;
            cfg.cameraMinPitch = 0f;
            cfg.cameraMaxPitch = 70f;
            EditorUtility.SetDirty(cfg);
            return cfg;
        }

        // Storm night colours (the "Tempest appears" CGI).
        private static readonly Color StormAmbient = new Color(0.34f, 0.27f, 0.46f);
        private static readonly Color StormMoon = new Color(0.66f, 0.55f, 0.92f);
        private static readonly Color StormFog = new Color(0.20f, 0.12f, 0.30f);
        private const string StormSkyPath = "Assets/Art/Boss/StormSky.png";
        private const string ArenaPavingPath = "Assets/Art/Boss/ArenaPaving.png";
        /// <summary>One cobblestone texture repeat on the arena floor, metres.</summary>
        private const float PavingTileMetres = 5f;

        private static Material GetOrCreateSkyMaterial()
        {
            const string path = MaterialFolder + "/Boss_Sky.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("WeatherForSale/BossSkySwirl");
                if (shader == null) Debug.LogError("[BossSceneBuilder] Shader WeatherForSale/BossSkySwirl not found.");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            // The storm painting, projected overhead; mirrored at its edges so no seam shows.
            Texture2D storm = LoadTexture(StormSkyPath, TextureWrapMode.Mirror);
            if (storm != null) mat.SetTexture("_StormTex", storm);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Texture2D LoadTexture(string path, TextureWrapMode wrap)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.wrapMode != wrap)
            {
                importer.wrapMode = wrap;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// The town square around the arena, after the "Tempest appears" CGI: street lamps on the arena rim (no
        /// colliders: decoration only), a ring of blue-roofed houses down below the plaza, castle towers further out
        /// in the purple fog, and a dark paved ground far under the edge (no collider: a fall still ends the fight).
        /// </summary>
        private static void BuildStormTown(float radius)
        {
            var pieces = new System.Collections.Generic.List<TownDressing.Piece>();
            const int lamps = 10;
            for (int i = 0; i < lamps; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 2f / lamps;
                float r = radius + 0.45f; // on the gold lip
                pieces.Add(new TownDressing.Piece("StreetLamp", Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r, a * Mathf.Rad2Deg + 180f, 4.2f));
            }
            const float groundY = -9f;
            string[] houses = { "TownHouseA", "TownHouseB", "TownHouseA", "MarketStall", "TownHouseB", "TownHouseA", "TownHouseB", "Fountain" };
            const int houseCount = 16;
            for (int i = 0; i < houseCount; i++)
            {
                float a = i * Mathf.PI * 2f / houseCount + 0.12f;
                string model = houses[i % houses.Length];
                bool tall = model == "TownHouseA" || model == "TownHouseB";
                float r = tall ? 40f + (i % 3) * 3f : 33f;
                float h = model == "TownHouseB" ? 22f : model == "TownHouseA" ? 17f : model == "Fountain" ? 6f : 5f;
                // Faces the arena.
                pieces.Add(new TownDressing.Piece(model, Mathf.Sin(a) * r, groundY, Mathf.Cos(a) * r, a * Mathf.Rad2Deg + 180f, h));
            }
            float[] towerAngles = { 20f, 95f, 160f, 230f, 300f };
            for (int i = 0; i < towerAngles.Length; i++)
            {
                float a = towerAngles[i] * Mathf.Deg2Rad;
                float r = 70f + (i % 2) * 14f;
                pieces.Add(new TownDressing.Piece("CastleTower", Mathf.Sin(a) * r, groundY, Mathf.Cos(a) * r, towerAngles[i] + 180f, 38f + (i % 3) * 6f));
            }
            GameObject town = TownDressing.Place(null, "StormTown", pieces.ToArray(), lampLights: 5, lampLightIntensity: 3f, lampLightRange: 11f);

            // Wet paving far below the plaza edge (visual only).
            Material paving = PavingMaterial("Boss_TownGround", 240f / 8f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "TownGround";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.GetComponent<MeshFilter>().sharedMesh = arenaDisc;
            ground.transform.SetParent(town.transform, false);
            ground.transform.localPosition = new Vector3(0f, groundY - 0.05f, 0f);
            ground.transform.localScale = new Vector3(240f, 0.05f, 240f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = paving;
        }

        /// <summary>The wet night cobblestone material, repeated <paramref name="tiles"/> times across a unit disc.</summary>
        private static Material PavingMaterial(string name, float tiles)
        {
            Material mat = ObbyTestSceneBuilder.GetOrCreateMaterial(name, Color.white);
            Texture2D tex = LoadTexture(ArenaPavingPath, TextureWrapMode.Repeat);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", new Vector2(tiles, tiles));
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.55f); // rain-soaked shine
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void FitHeight(GameObject model, float height)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            if (b.size.y > 0.001f) model.transform.localScale *= height / b.size.y;
        }

        private static void CentreOn(GameObject model, Vector3 worldCentre)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            model.transform.position += worldCentre - b.center;
        }

        private static SuctionStage BuildSuctionPose(PlayerController player, ThirdPersonCamera followCamera, Transform viewCamera)
        {
            SuctionConfig suctionConfig = SuctionStageBuilder.GetOrCreateConfig();
            var root = new GameObject("SuctionPose");
            Material streakMat = SuctionStageBuilder.GetOrCreateStreakMaterial();
            ParticleSystem surround = SuctionStageBuilder.CreateStreaks("Streaks_Surround", root.transform, streakMat, suctionConfig);
            ParticleSystem fromBoss = SuctionStageBuilder.CreateStreaks("Streaks_FromBoss", root.transform, streakMat, suctionConfig);
            GameObject bottlePrefab = SuctionStageBuilder.GetOrCreateOpenBottlePrefab();
            var stage = root.AddComponent<SuctionStage>();
            ObbyTestSceneBuilder.SetRefs(stage,
                ("config", suctionConfig), ("player", player), ("followCamera", followCamera), ("viewCamera", viewCamera),
                ("bottlePrefab", bottlePrefab), ("surroundStreaks", surround), ("orbStreaks", fromBoss));
            return stage;
        }

        private static Material Mat(string name, Color color, float emission = 0f)
        {
            Material m = ObbyTestSceneBuilder.GetOrCreateMaterial(name, color);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * emission);
                EditorUtility.SetDirty(m);
            }
            return m;
        }

        /// <summary>
        /// The floating stone arena: a pale stone floor with gold ring inlays, an open gold-lipped edge,
        /// a plain stone base under it. Everything is plain primitives; a
        /// generated floor model can replace the "Floor" objects later.
        /// </summary>
        private static void BuildArena(float radius)
        {
            // Floor: the wet night cobblestones of the town square (the CGI), the old stone colour when the texture is missing.
            Material stone = AssetDatabase.LoadAssetAtPath<Texture2D>(ArenaPavingPath) != null
                ? PavingMaterial("Boss_Paving", radius * 2f / PavingTileMetres)
                : Mat("Boss_StoneV3", new Color(0.80f, 0.74f, 0.64f));
            Material stoneDark = Mat("Boss_StoneDarkV3", new Color(0.62f, 0.57f, 0.52f));
            Material gold = Mat("Boss_Gold", new Color(0.98f, 0.76f, 0.26f));
            Material lantern = Mat("Boss_Lantern", new Color(1f, 0.85f, 0.45f), 2.2f);

            var arena = new GameObject("Arena");
            float d = radius * 2f;

            // Floor: one stone disc (the only collider) with the inlays laid as flat, non-overlapping rings 2 cm above it.
            // Stacked discs used to rise up to 40 cm over the collider and Sam's feet sank into them.
            Disc("Floor", arena.transform, 0f, d, 0.6f, stone, true);
            Inlay("BandDark", arena.transform, radius * 0.90f, radius * 0.94f, stoneDark);
            Inlay("BandGold", arena.transform, radius * 0.885f, radius * 0.90f, gold);
            Inlay("MidGold", arena.transform, radius * 0.585f, radius * 0.60f, gold);
            // (No blue centre: the cobblestones show, like the square in the CGI.)
            Inlay("CentreGold", arena.transform, radius * 0.31f, radius * 0.34f, gold);

            // A plain thick stone base under the floor (no hanging rocks and no clouds: those come from generated models).
            Cylinder("Base", arena.transform, new Vector3(0f, -1.9f, 0f), new Vector3(d * 0.98f, 1.3f, d * 0.98f), stoneDark, false);

            // No wall and no pillars: Sam can be thrown off the edge. A gold lip marks the edge.
            // The lip is a ring that starts exactly at the floor edge, so it never shares a surface with the floor top
            // (a disc under the floor made the two flicker against each other along the rim).
            Ring("EdgeLip", arena.transform, radius, radius + 0.9f, 0.5f, -0.48f, gold);

            // Only the big Floor disc has a collider.
        }

        /// <summary>A flat floor inlay: a ring (or a disc when <paramref name="inner"/> is 0) whose top is 2 cm above the floor.</summary>
        private static void Inlay(string name, Transform parent, float inner, float outer, Material mat)
        {
            if (inner <= 0.001f)
            {
                Cylinder(name, parent, new Vector3(0f, 0.02f - 0.02f, 0f), new Vector3(outer * 2f, 0.02f, outer * 2f), mat, false);
                return;
            }
            string path = DataFolder + "/Inlay_" + name + ".asset";
            AssetDatabase.DeleteAsset(path);
            Mesh mesh = BossShapes.BuildRing(inner, outer, 0.04f, 0f, 0f, 128);
            AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -0.02f, 0f); // bottom 2 cm under, top 2 cm over the floor surface
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void Ring(string name, Transform parent, float inner, float outer, float height, float y, Material mat)
        {
            string path = DataFolder + "/ArenaLip.asset";
            AssetDatabase.DeleteAsset(path);
            Mesh mesh = BossShapes.BuildRing(inner, outer, height, 0f, 0f, 128);
            AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void Disc(string name, Transform parent, float y, float diameter, float thickness, Material mat, bool collider)
        {
            Cylinder(name, parent, new Vector3(0f, y - thickness * 0.5f, 0f), new Vector3(diameter, thickness * 0.5f, diameter), mat, collider);
        }

        private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.GetComponent<MeshFilter>().sharedMesh = arenaDisc; // a many-sided disc: round, with no corners
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            // The primitive cylinder ships with a capsule collider, which is the wrong shape for a wide flat disc.
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = arenaDisc;
            return go;
        }

        private static void Cube(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void Ellipsoid(string name, Transform parent, Vector3 position, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        // ------------------------------------------------------------------ UI

        private sealed class UiRefs
        {
            public BossHud Hud;
            public GameObject SuctionRoot, KeyPromptRoot;
            public Image SuctionFill;
            public Text SuctionTitle, KeyCapText, PromptText, TapLabel;
            public RectTransform TapButton;
        }

        /// <summary>The secret stage's special star (generate it with design/ref/chatgpt-prompts-secret-star.md).</summary>
        internal const string SecretStarPath = "Assets/Art/UI/Story/StarSecret.png";
        internal const string SecretStarEmptyPath = "Assets/Art/UI/Story/StarSecretEmpty.png";

        private static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Sheet + name + ".png");

        private static Sprite B(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Boss/" + name + ".png");

        /// <summary>A child rect covering the given fractions (xMin, yMin, xMax, yMax) of its parent.</summary>
        private static RectTransform FractionRect(string name, RectTransform parent, Vector4 f)
        {
            RectTransform rt = ObbyTestSceneBuilder.CreateRect(name, parent);
            rt.anchorMin = new Vector2(f.x, f.y);
            rt.anchorMax = new Vector2(f.z, f.w);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Image ImageStretch(string name, RectTransform parent, Sprite sprite, Image.Type type)
        {
            RectTransform rt = ObbyTestSceneBuilder.CreateRect(name, parent);
            ObbyTestSceneBuilder.Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = type;
            img.raycastTarget = false;
            return img;
        }

        private static UiRefs BuildUi(BossConfig config, PlayerController player, ThirdPersonCamera cam)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var refs = new UiRefs();

            var canvasGo = new GameObject("BossCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            refs.Hud = canvasGo.AddComponent<BossHud>();
            Transform root = canvasGo.transform;

            // --- touch controls (behind everything)
            BuildTouchControls(root, font, player, cam);

            // --- flashes
            Image hurt = FullScreenImage("HurtFlash", root);
            Image white = FullScreenImage("WhiteFlash", root);

            // --- clock plate (top centre): the same small plate as every obby level
            RectTransform plate = ImageRect("TimerPlate", root, S("CapsuleWood"), Image.Type.Sliced, new Vector2(0.5f, 1f), new Vector2(0f, -12f), ObbyTestSceneBuilder.TimerPlateSize);
            Text timer = ObbyTestSceneBuilder.CreateText("TimerText", plate, font, "3:00", ObbyTestSceneBuilder.TimerFontSize, TextAnchor.MiddleCenter, Color.white);
            ObbyTestSceneBuilder.Stretch(timer.rectTransform);
            timer.fontStyle = FontStyle.Bold;
            timer.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);

            Text banner = ObbyTestSceneBuilder.CreateText("Banner", root, font, "", 56, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.4f));
            Anchor(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -290f), new Vector2(1500f, 90f));
            banner.fontStyle = FontStyle.Bold;
            banner.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);

            // --- boss HP: top centre, right under the clock plate. Frame with a dark channel; the empty, trail and fill
            // layers sit inside the channel (fractions measured on the generated frame sprite).
            const float barW = 600f;
            const float barH = barW * 162f / 1327f;
            const float barTop = -172f;
            RectTransform bar = ImageRect("BossBar", root, B("BossFrame"), Image.Type.Simple, new Vector2(0.5f, 1f), new Vector2(0f, barTop), new Vector2(barW, barH));
            Vector4 channel = new Vector4(0.0535f, 0.21f, 0.991f, 0.747f); // xMin, yMin, xMax, yMax of the channel inside the frame
            RectTransform channelRect = FractionRect("Channel", bar, channel);
            ImageStretch("Empty", channelRect, B("BossEmpty"), Image.Type.Simple);
            Image bossTrail = ImageStretch("Trail", channelRect, B("BossFillYellow"), Image.Type.Filled);
            bossTrail.fillMethod = Image.FillMethod.Horizontal;
            Image bossFill = ImageStretch("Fill", channelRect, B("BossFillPurple"), Image.Type.Filled);
            bossFill.fillMethod = Image.FillMethod.Horizontal;
            for (int i = 1; i < config.bossMaxHp; i++)
            {
                float f = (float)i / config.bossMaxHp;
                RectTransform tick = ObbyTestSceneBuilder.CreateRect("Tick" + i, channelRect);
                tick.anchorMin = new Vector2(f, -0.1f);
                tick.anchorMax = new Vector2(f, 1.1f);
                tick.pivot = new Vector2(0.5f, 0.5f);
                tick.sizeDelta = new Vector2(14f, 0f);
                tick.anchoredPosition = Vector2.zero;
                var tickImg = tick.gameObject.AddComponent<Image>();
                tickImg.sprite = B("Tick1");
                tickImg.raycastTarget = false;
            }
            // Emblem over the left end of the bar.
            // The secret slot's look (Level Select): the violet stone frame with Tempest inside, no star.
            Sprite slotFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Story/SecretSlotFrame.png");
            Sprite tempestFace = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Story/Portrait_Tempest.png");
            if (slotFrame != null && tempestFace != null)
            {
                // Over the bar's round left end cap, covering it.
                RectTransform emblem = ImageRect("BossEmblem", root, slotFrame, Image.Type.Simple, new Vector2(0.5f, 1f), new Vector2(-barW * 0.5f + 20f, barTop - barH * 0.5f), new Vector2(124f, 124f), Color.white, new Vector2(0.5f, 0.5f));
                emblem.GetComponent<Image>().preserveAspect = true;
                RectTransform face = ImageRect("Tempest", emblem, tempestFace, Image.Type.Simple, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(78f, 70f));
                face.GetComponent<Image>().preserveAspect = true;
            }
            else
            {
                ImageRect("BossEmblem", root, B("Emblem"), Image.Type.Simple, new Vector2(0.5f, 1f), new Vector2(-barW * 0.5f + 14f, barTop - barH * 0.5f), new Vector2(112f, 113f), Color.white, new Vector2(0.5f, 0.5f));
            }
            // Boss name: plain text between the clock and the bar.
            Text bossName = ObbyTestSceneBuilder.CreateText("BossName", root, font, config.bossName, 52, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.8f));
            Anchor(bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(600f, 58f));
            bossName.fontStyle = FontStyle.Bold;
            bossName.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);

            // --- suction HUD
            var suctionRoot = new RectTransform[1];
            suctionRoot[0] = ObbyTestSceneBuilder.CreateRect("SuctionHud", root);
            ObbyTestSceneBuilder.Stretch(suctionRoot[0]);
            refs.SuctionRoot = suctionRoot[0].gameObject;
            Sprite gaugeSprite = S("GaugeCloud");
            ImageRect("GaugeBack", suctionRoot[0], gaugeSprite, Image.Type.Simple, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(480f, 109f), new Color(0.4f, 0.4f, 0.4f, 1f));
            RectTransform gFill = ImageRect("GaugeFill", suctionRoot[0], gaugeSprite, Image.Type.Filled, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(480f, 109f), Color.white);
            refs.SuctionFill = gFill.GetComponent<Image>();
            refs.SuctionFill.fillMethod = Image.FillMethod.Horizontal;
            refs.SuctionFill.fillAmount = 0f;
            refs.SuctionTitle = ObbyTestSceneBuilder.CreateText("Title", suctionRoot[0], font, "", 42, TextAnchor.MiddleCenter, new Color(0.85f, 0.65f, 1f));
            Anchor(refs.SuctionTitle.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(1200f, 64f));
            refs.SuctionTitle.fontStyle = FontStyle.Bold;
            refs.SuctionTitle.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);

            RectTransform keyRoot = ObbyTestSceneBuilder.CreateRect("KeyPrompt", suctionRoot[0]);
            // Off to the side so it never covers Sam or the boss.
            Anchor(keyRoot, new Vector2(0.5f, 0f), new Vector2(520f, 260f), new Vector2(400f, 250f));
            refs.KeyPromptRoot = keyRoot.gameObject;
            RectTransform cap = ImageRect("KeyCap", keyRoot, S("KeyCap"), Image.Type.Simple, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(140f, 136f));
            refs.KeyCapText = ObbyTestSceneBuilder.CreateText("Letter", cap, font, "F", 78, TextAnchor.MiddleCenter, new Color(0.25f, 0.15f, 0.1f));
            ObbyTestSceneBuilder.Stretch(refs.KeyCapText.rectTransform);
            refs.KeyCapText.fontStyle = FontStyle.Bold;
            refs.PromptText = ObbyTestSceneBuilder.CreateText("Prompt", keyRoot, font, "Press F!", 44, TextAnchor.MiddleCenter, Color.white);
            Anchor(refs.PromptText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(500f, 60f));
            refs.PromptText.fontStyle = FontStyle.Bold;
            refs.PromptText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);

            RectTransform tap = ImageRect("TapButton", suctionRoot[0], S("TapBtn"), Image.Type.Simple, new Vector2(0.5f, 0f), new Vector2(430f, 340f), new Vector2(190f, 178f));
            tap.GetComponent<Image>().raycastTarget = true;
            refs.TapButton = tap;
            refs.TapLabel = ObbyTestSceneBuilder.CreateText("Label", tap, font, "TAP!", 44, TextAnchor.MiddleCenter, Color.white);
            ObbyTestSceneBuilder.Stretch(refs.TapLabel.rectTransform);
            refs.TapLabel.fontStyle = FontStyle.Bold;
            refs.TapLabel.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            tap.gameObject.SetActive(false);
            suctionRoot[0].gameObject.SetActive(false);

            // --- result panel
            RectTransform result = ObbyTestSceneBuilder.CreatePanel("ResultPanel", root, null, new Color(0f, 0f, 0f, 0.6f));
            RectTransform card = ImageRect("Card", result, S("PanelLarge"), Image.Type.Sliced, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 700f));
            Text resultTitle = ObbyTestSceneBuilder.CreateText("Title", card, font, "Game Over", 110, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.35f));
            // Inside the frame's inner edge (the card's border is ~90 px), shrinking to fit long titles like "Boss Sealed".
            Anchor(resultTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(760f, 150f));
            resultTitle.resizeTextForBestFit = true;
            resultTitle.resizeTextMinSize = 60;
            resultTitle.resizeTextMaxSize = 96;
            // The secret stage's three stars (their own violet art), lit by the fight's time, popping in on a win.
            Sprite secretStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SecretStarPath);
            Sprite secretStarEmptySprite = AssetDatabase.LoadAssetAtPath<Sprite>(SecretStarEmptyPath);
            RectTransform specialStar = ObbyTestSceneBuilder.CreateRect("SpecialStars", card);
            Anchor(specialStar, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(480f, 150f));
            var starImages = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform one = ImageRect("Star_" + i, specialStar, secretStarSprite != null ? secretStarSprite : S("StarBig"), Image.Type.Simple,
                    new Vector2(0.5f, 0.5f), new Vector2(-150f + i * 150f, i == 1 ? 12f : 0f), new Vector2(140f, 140f));
                var img = one.GetComponent<Image>();
                img.preserveAspect = true;
                starImages[i] = img;
            }
            resultTitle.fontStyle = FontStyle.Bold;
            resultTitle.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            // Result button: a wooden pill with a gold rim, cream lettering with a brown outline (the Start / Quit look).
            RectTransform btnRect = ImageRect("ResultButton", card, S("BtnWoodA"), Image.Type.Sliced, new Vector2(0.5f, 0.5f), new Vector2(0f, -205f), new Vector2(560f, 150f));
            var btnImage = btnRect.GetComponent<Image>();
            btnImage.raycastTarget = true;
            btnImage.pixelsPerUnitMultiplier = 0.57f; // the pill is stretched ~1.8x, so its caps are drawn that much thicker
            var button = btnRect.gameObject.AddComponent<Button>();
            button.targetGraphic = btnImage;
            ColorBlock colours = button.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = new Color(1.12f, 1.08f, 1f, 1f);
            colours.pressedColor = new Color(0.82f, 0.78f, 0.74f, 1f);
            colours.selectedColor = Color.white;
            button.colors = colours;
            Text btnLabel = ObbyTestSceneBuilder.CreateText("Label", btnRect, font, config.restartLabel, 64, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.78f));
            ObbyTestSceneBuilder.Stretch(btnLabel.rectTransform);
            btnLabel.fontStyle = FontStyle.Bold;
            btnLabel.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            btnLabel.GetComponent<Outline>().effectColor = new Color(0.36f, 0.18f, 0.07f, 1f);
            result.gameObject.SetActive(false);

            ObbyTestSceneBuilder.SetRefs(refs.Hud,
                ("bossFill", bossFill), ("bossTrail", bossTrail), ("bossNameText", bossName),
                ("timerText", timer), ("bannerText", banner), ("hurtFlash", hurt), ("whiteFlash", white),
                ("resultPanel", result.gameObject), ("resultTitle", resultTitle), ("resultButton", button), ("resultButtonLabel", btnLabel),
                ("specialStar", specialStar), ("secretStarFull", secretStarSprite), ("secretStarEmpty", secretStarEmptySprite), ("progress", AssetDatabase.LoadAssetAtPath<WeatherForSale.Progress.ProgressService>("Assets/Data/ProgressService.asset")));
            ObbyTestSceneBuilder.SetRefArray(refs.Hud, "secretStars", starImages);
            specialStar.gameObject.SetActive(false);
            ShopSceneBuilder.AddPauseMenu(canvasGo.transform); // menu button top-left, as in the shop
            return refs;
        }

        private static void BuildTouchControls(Transform root, Font font, PlayerController player, ThirdPersonCamera cam)
        {
            RectTransform pad = ObbyTestSceneBuilder.CreateRect("TouchControls", root);
            ObbyTestSceneBuilder.Stretch(pad);
            var padImage = pad.gameObject.AddComponent<Image>();
            padImage.color = new Color(0f, 0f, 0f, 0f);
            padImage.raycastTarget = false; // TouchControls switches it on for touch devices only
            var touch = pad.gameObject.AddComponent<TouchControls>();

            RectTransform visuals = ObbyTestSceneBuilder.CreateRect("Visuals", pad);
            ObbyTestSceneBuilder.Stretch(visuals);

            RectTransform stickBase = ObbyTestSceneBuilder.CreateRect("StickBase", visuals);
            ObbyTestSceneBuilder.SetCentered(stickBase, new Vector2(260f, 260f));
            var baseImg = stickBase.gameObject.AddComponent<Image>();
            baseImg.sprite = S("JoystickBase");
            baseImg.color = new Color(1f, 1f, 1f, 0.85f);
            baseImg.preserveAspect = true;
            baseImg.raycastTarget = false;

            RectTransform knob = ObbyTestSceneBuilder.CreateRect("StickKnob", stickBase);
            ObbyTestSceneBuilder.SetCentered(knob, new Vector2(130f, 130f));
            var knobImg = knob.gameObject.AddComponent<Image>();
            knobImg.sprite = S("JoystickFull");
            knobImg.color = new Color(1f, 1f, 1f, 0.95f);
            knobImg.preserveAspect = true;
            knobImg.raycastTarget = false;

            RectTransform jump = ObbyTestSceneBuilder.CreateRect("JumpButton", visuals);
            jump.anchorMin = jump.anchorMax = new Vector2(1f, 0f);
            jump.pivot = new Vector2(0.5f, 0.5f);
            jump.anchoredPosition = new Vector2(-240f, 260f);
            jump.sizeDelta = new Vector2(240f, 224f);
            var jumpImg = jump.gameObject.AddComponent<Image>();
            jumpImg.sprite = S("JumpBtn");
            jumpImg.color = new Color(1f, 1f, 1f, 0.9f);
            jumpImg.preserveAspect = true;
            jumpImg.raycastTarget = false;

            ObbyTestSceneBuilder.SetRefs(touch,
                ("player", player), ("playerCamera", cam), ("visualsRoot", visuals.gameObject),
                ("stickBase", stickBase), ("stickKnob", knob), ("jumpButton", jump),
                ("jumpImage", jumpImg), ("jumpPressedSprite", S("JumpBtnPressed")));
        }

        private static Image FullScreenImage(string name, Transform parent)
        {
            RectTransform rt = ObbyTestSceneBuilder.CreateRect(name, parent);
            ObbyTestSceneBuilder.Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;
            return img;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, anchor.y == 1f ? 1f : anchor.y == 0f ? 0f : 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static RectTransform ImageRect(string name, Transform parent, Sprite sprite, Image.Type type, Vector2 anchor, Vector2 position, Vector2 size, Color? color = null, Vector2? pivot = null)
        {
            RectTransform rt = ObbyTestSceneBuilder.CreateRect(name, parent);
            Anchor(rt, anchor, position, size, pivot);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = type;
            img.color = color ?? Color.white;
            img.raycastTarget = false;
            return rt;
        }
    }
}
