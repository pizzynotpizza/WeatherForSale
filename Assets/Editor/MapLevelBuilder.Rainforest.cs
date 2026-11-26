#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using WeatherForSale.Obby;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Rainforest layouts (story 004): level 1 "Mushroom Path" (ทางเห็ดเด้ง) and level 2 "Logs and Vines"
    /// (ท่อนไม้กับเถาวัลย์). The course runs along +Z from the start pad at the origin.
    /// Reach used to place everything (moveSpeed 6, jumpSpeed 9, gravity 25): about 4 m flat gap or
    /// 3.9 m with a 0.5 m rise; main-path gaps are at most 3 m with rises of 0.5 m or less except where a
    /// bounce mushroom is involved (jump apex 15 m/s: 4.5 m, 1.2 s in the air). The hidden-Shard branch
    /// of each level uses smaller platforms and 3.0 - 3.4 m gaps, so it is harder than the main path.
    /// </summary>
    internal static partial class MapLevelBuilder
    {
        // ---- layout tuning (metres / seconds); the runtime tuning lives in the obstacle components' fields

        /// <summary>Water surface (top of the kill slab). Mushrooms float 1.5 - 3 m above it.</summary>
        private const float RiverTopY = -2.5f;

        /// <summary>Width of the swinging Rainbell head model, metres (its hit sphere is BellHitRadius).</summary>
        private const float RainbellHeadWidth = 1.15f;
        /// <summary>Rainbell plants growing along the banks (visual only), metres wide.</summary>
        private const float RainbellPlantWidth = 1.8f;

        // Level 2 tree climb: six branches on separate trees, zigzagging left/right.
        private const int ClimbBranches = 5;
        private const float ClimbRise = 0.9f;       // each branch this much higher than the last
        private const float ClimbStartZ = 53.0f;    // first branch (Checkpoint_2 ends at z 51.5)
        private const float ClimbSpacingZ = 2.2f;
        private const float ClimbBranchX = 1.3f;    // branch centre, left or right of the course line
        private const float ClimbTrunkX = 3.3f;     // trunk, outside the course
        private const float ClimbCrownWidth = 3.4f; // leafy crown on top of each climbing tree

        // Grass banks along both sides of the river, under the scenery trees and cliffs (visual only).
        private const float BankInnerX = 16.5f;   // inner edge, clear of the secret branches (down to x = -16)
        private const float BankWidth = 28f;      // out past the cliffs
        private const float BankTopY = -2.1f;     // just above the river surface
        private const float BankBottomY = -6f;

        /// <summary>Bottom of the stone pillars under pads (just under the water).</summary>
        private const float PillarBottomY = -4f;

        private const float MistDensity = 0.016f;
        private static readonly Color MistColor = new Color(0.68f, 0.77f, 0.84f);

        /// <summary>Height of the dome of a giant mushroom cap.</summary>
        private const float MushroomCapHeight = 0.8f;

        /// <summary>Walkable box side as a fraction of the cap diameter (the dome is only flat near its middle).</summary>
        private const float MushroomWalkFraction = 0.74f;

        private const float MushroomColliderThickness = 0.4f;
        private const float MushroomStemLength = 1.4f;

        /// <summary>Upward launch speed of a bounce mushroom, m/s (apex = v^2 / 2g = 4.5 m with gravity 25).</summary>
        private const float BounceLaunchSpeed = 15f;

        /// <summary>Seconds from the first step to a collapsing mushroom giving way (story: 1 second).</summary>
        private const float MushroomCollapseSeconds = 0.6f;

        /// <summary>Seconds a rotten branch can be stood on (continuously) before it breaks.</summary>
        private const float RottenBranchStandSeconds = 0.6f;

        /// <summary>Rainbell hit sphere radius (matches the bell mesh), metres.</summary>
        private const float BellHitRadius = 0.62f;

        // ------------------------------------------------------------------ materials

        private enum MushroomKind { Static, Bounce, Collapse }

        private sealed class RainforestMats
        {
            public readonly Material CapStatic = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_CapStatic", new Color(0.84f, 0.28f, 0.24f));
            public readonly Material CapBounce = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_CapBounce", new Color(1.00f, 0.62f, 0.12f));
            public readonly Material CapCollapse = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_CapCollapse", new Color(0.80f, 0.74f, 0.50f));
            public readonly Material Stem = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_Stem", new Color(0.93f, 0.88f, 0.76f));
            public readonly Material Spot = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_Spot", new Color(0.98f, 0.95f, 0.88f));
            public readonly Material Bark = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_Bark", new Color(0.40f, 0.28f, 0.18f));
            public readonly Material Rotten = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_RottenWood", new Color(0.70f, 0.64f, 0.52f));
            public readonly Material RotSpot = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_RotSpot", new Color(0.22f, 0.17f, 0.12f));
            // Floating logs: all brown (bark, end wood, growth ring, stubs).
            public readonly Material Log = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_Log", new Color(0.45f, 0.28f, 0.15f));
            public readonly Material LogEnd = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_LogEnd", new Color(0.80f, 0.60f, 0.38f));
            public readonly Material LogRing = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_LogRing", new Color(0.60f, 0.39f, 0.21f));
            public readonly Material LogStub = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_LogStub", new Color(0.34f, 0.21f, 0.11f));
            public readonly Material Vine = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_Vine", new Color(0.24f, 0.60f, 0.26f));
            public readonly Material Knot = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_VineKnot", new Color(0.45f, 0.32f, 0.20f));
            public readonly Material Bell = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_RainbellBlue", new Color(0.36f, 0.52f, 0.96f));
            public readonly Material BellRim = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_RainbellRim", new Color(0.80f, 0.86f, 1.00f));
            public readonly Material Pillar = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_RF_PillarRock", new Color(0.46f, 0.52f, 0.50f));
            public readonly Material Shard = GlowMat("Obby_RF_Shard", new Color(0.62f, 0.22f, 1.00f), 2.2f);
            public readonly Material ModelCapBounce = MushroomVariant("Orange", 0.075f, 1.0f, 1.05f);
            public readonly Material ModelCapCollapse = MushroomVariant("Pale", 0.10f, 0.35f, 1.15f);
            public readonly Material PaletteTree = PaletteMat("Obby_RF_PaletteTree", new Color(0.86f, 1.00f, 0.90f));
            public readonly Material PaletteRock = PaletteMat("Obby_RF_PaletteMossRock", new Color(0.68f, 0.78f, 0.72f));
        }

        private static void PrepareRainforest(Context c)
        {
            c.Result.HazardTopY = RiverTopY;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = MistColor;
            RenderSettings.fogDensity = MistDensity;
        }

        // ------------------------------------------------------------------ level 1: Mushroom Path

        private static void BuildRainforest1(Context c)
        {
            var m = new RainforestMats();
            PrepareRainforest(c);
            StartIsland(c, 6f, 6f, PillarBottomY);

            // A: stepping caps, then two that collapse 1 s after the first step.
            Mushroom(c, m, "Mushroom_A1", 0f, 0.0f, 6.5f, 3.4f, 3.4f, MushroomKind.Static);
            Mushroom(c, m, "Mushroom_A2", 1.4f, 0.3f, 10.6f, 3.2f, 3.2f, MushroomKind.Static);
            Mushroom(c, m, "Mushroom_A3_Collapse", -0.8f, 0.6f, 14.6f, 3.0f, 3.0f, MushroomKind.Collapse);
            Mushroom(c, m, "Mushroom_A4_Collapse", 0.8f, 0.9f, 18.4f, 3.0f, 3.0f, MushroomKind.Collapse);
            AddCheckpoint(c, "Checkpoint_1", 0f, 1.0f, 23.5f, 5f, m.Pillar, PillarBottomY);

            // B: a bounce mushroom launches the player up onto a high cap and the second checkpoint.
            Mushroom(c, m, "Mushroom_B1_Bounce", 0f, 1.0f, 30.0f, 3.4f, 3.4f, MushroomKind.Bounce);
            Mushroom(c, m, "Mushroom_B2_HighCap", 0f, 4.5f, 35.5f, 4.4f, 4.4f, MushroomKind.Static);
            AddCheckpoint(c, "Checkpoint_2", 0f, 4.5f, 41.0f, 5f, m.Pillar, PillarBottomY);

            // C: two more collapsing caps, then a long shelf mushroom under two swinging Rainbell flowers.
            Mushroom(c, m, "Mushroom_C1_Collapse", 0.8f, 4.8f, 47.0f, 3.0f, 3.0f, MushroomKind.Collapse);
            Mushroom(c, m, "Mushroom_C2_Collapse", -0.8f, 5.1f, 50.8f, 3.0f, 3.0f, MushroomKind.Collapse);
            Mushroom(c, m, "Mushroom_Shelf", 0f, 5.2f, 57.8f, 3.4f, 9.0f, MushroomKind.Static);
            RainbellFlower(c, m, "Rainbell_1", new Vector3(0f, 5.2f + 0.9f + 4f, 56.5f), 4f, 40f, 2.3f, 0f, -4.7f);
            RainbellFlower(c, m, "Rainbell_2", new Vector3(0f, 5.2f + 0.9f + 4f, 59.5f), 4f, 40f, 2.3f, 1.15f, 4.7f);

            // D: last bounce mushroom throws the player onto the finish pad.
            Mushroom(c, m, "Mushroom_B3_Bounce", 0f, 5.2f, 65.0f, 3.4f, 3.4f, MushroomKind.Bounce);
            AddFinish(c, 0f, 7.5f, 72.5f, 6f, 7f, m.Pillar, PillarBottomY);

            // Secret branch off checkpoint 1: small caps, 3.0 - 3.4 m gaps, one collapses. Hidden Shard at the end.
            Mushroom(c, m, "Secret_1", -6.3f, 1.5f, 24.0f, 1.9f, 1.9f, MushroomKind.Static);
            Mushroom(c, m, "Secret_2_Collapse", -10.6f, 2.0f, 26.0f, 1.8f, 1.8f, MushroomKind.Collapse);
            Mushroom(c, m, "Secret_3", -14.8f, 2.6f, 24.0f, 1.8f, 1.8f, MushroomKind.Static);
            AddShard(c, c.MapId, new Vector3(-14.8f, 3.7f, 24.0f), m.Shard, 0.4f, 0.8f);

            RainforestScenery(c, m, -12f, 90f, 1001);
        }

        // ------------------------------------------------------------------ level 2: Logs and Vines

        private static void BuildRainforest2(Context c)
        {
            var m = new RainforestMats();
            PrepareRainforest(c);
            StartIsland(c, 6f, 6f, PillarBottomY);

            // A: four floating logs that roll back and forth and bob; neighbours roll opposite ways.
            for (int i = 0; i < 4; i++)
            {
                RollingLog(c, m, "Log_" + (i + 1), 0f, 0f, 5.6f + 3.3f * i, 3.4f, 1.8f,
                    peakSpeed: 3.2f, period: 3.4f, phase: (i % 2) * 2f, bobAmplitude: 0.08f, bobPeriod: 2.6f, bobPhase: i * 0.7f);
            }
            AddCheckpoint(c, "Checkpoint_1", 0f, 0.4f, 20.0f, 5f, m.Pillar, PillarBottomY);

            // B: two swinging vines. The footholds are lowest in the middle of the swing and rise to
            // platform height (+0.3 m) at both ends, where the swing is slowest: that is where to hop on or off.
            SwingingVine(c, m, "Vine_1", 0f, 27.0f, 0.7f, 6.0f, 32f, 3.6f, 0f, -4.7f);
            Slab(c, "Landing_1", 0f, 0.4f, 34.5f, 4f, 4f, c.In.PlatformMaterial, m.Pillar, PillarBottomY);
            SwingingVine(c, m, "Vine_2", 0f, 41.5f, 0.7f, 6.5f, 34f, 3.0f, 1.0f, 4.7f);
            AddCheckpoint(c, "Checkpoint_2", 0f, 0.4f, 49.0f, 5f, m.Pillar, PillarBottomY);

            // C: jump-climb up through the trees: six separate trees left and right of the river, each with
            // one branch reaching in over the course. Hop from branch to branch, zigzagging, 0.75 m higher each time.
            for (int k = 0; k < ClimbBranches; k++)
            {
                float side = k % 2 == 0 ? 1f : -1f;
                float topY = 0.4f + ClimbRise * (k + 1);
                float z = ClimbStartZ + ClimbSpacingZ * k;
                ClimbTree(c, m, "ClimbTree_" + (k + 1), side * ClimbTrunkX, z, topY + 2.4f);
                Branch(c, "Climb_" + (k + 1), new Vector3(side * ClimbBranchX, topY, z), side > 0f ? Vector3.left : Vector3.right,
                    3.0f, 1.0f, 0.7f, m.Bark);
            }

            // D: rotten branches (break after ~1 s of standing), then two fast rolling logs.
            for (int i = 0; i < 3; i++)
            {
                GameObject branch = Branch(c, "RottenBranch_" + (i + 1), new Vector3(0f, 4.9f, 63.6f + 3.7f * i), Vector3.forward, 2.8f, 1.0f, 0.75f, m.Rotten);
                MakeRotten(branch, m, 4.9f);
            }
            RollingLog(c, m, "Log_5", 0f, 4.9f, 74.8f, 3.4f, 1.8f, peakSpeed: 3.8f, period: 2.8f, phase: 0f, bobAmplitude: 0.08f, bobPeriod: 2.6f, bobPhase: 0.3f);
            RollingLog(c, m, "Log_6", 0f, 4.9f, 78.0f, 3.4f, 1.8f, peakSpeed: 3.8f, period: 2.8f, phase: 1.6f, bobAmplitude: 0.08f, bobPeriod: 2.6f, bobPhase: 1.1f);

            // Landing deck and finish pad: nothing else within 6 m (the suction stage stands Sam on the pad facing -Z).
            Slab(c, "Deck", 0f, 4.9f, 84.0f, 6f, 6f, c.In.PlatformMaterial, m.Pillar, PillarBottomY);
            AddFinish(c, 0f, 4.9f, 90.0f, 6f, 6f, m.Pillar, PillarBottomY, weatherDais: true);

            // Secret branch off the climb (the 4th branch, on the left tree): three small rotten stubs toward -X: three small rotten stubs, narrow and
            // 3.0 - 3.4 m apart, rising 0.5 m each. Hidden Shard at the end.
            const float stubLength = 1.8f, stubWidth = 1.1f;
            MakeRotten(Branch(c, "Secret_1", new Vector3(-5.6f, 3.4f, 58.4f), Vector3.right, stubLength, stubWidth, 0.75f, m.Rotten), m, 3.4f);
            MakeRotten(Branch(c, "Secret_2", new Vector3(-9.3f, 3.9f, 56.0f), Vector3.right, stubLength, stubWidth, 0.75f, m.Rotten), m, 3.9f);
            MakeRotten(Branch(c, "Secret_3", new Vector3(-13.0f, 4.5f, 58.4f), Vector3.right, stubLength, stubWidth, 0.75f, m.Rotten), m, 4.5f);
            AddShard(c, c.MapId, new Vector3(-13.0f, 5.6f, 58.4f), m.Shard, 0.4f, 0.8f);

            RainforestScenery(c, m, -12f, 100f, 2002);
        }

        // ------------------------------------------------------------------ obstacle builders

        /// <summary>
        /// A giant floating mushroom. Stand-in model: squashed sphere cap + cylinder stem + flat cream spots
        /// (no mushroom model in the project; see the report). Collider is a flat box matching the top of the dome.
        /// </summary>
        // Meshy models made from the user's Rainforest asset sheet (design/ref/Rainforest Fantasy Game Asset Sheet.png).
        private const string MushroomPrefabPath = "Assets/Art/Props/Rainforest/Mushroom/Mushroom.prefab";
        private const string MushroomFolder = "Assets/Art/Props/Rainforest/Mushroom/";
        private const string RainbellHeadPrefabPath = "Assets/Art/Props/Rainforest/Rainbell/RainbellHead.prefab";
        private const string RainbellPlantPrefabPath = "Assets/Art/Props/Rainforest/Rainbell/Rainbell.prefab";
        /// <summary>How far the model's dome sits below the walkable top (the dome's peak is above its rim).</summary>
        private const float MushroomModelSink = 0.12f;

        /// <summary>
        /// The mushroom model's material with its red cap recoloured (hue / saturation / brightness), so bounce
        /// and collapsing caps read differently. The recoloured texture is made once from the original.
        /// Null when the original material is missing (the stand-in colours are used then).
        /// </summary>
        private static Material MushroomVariant(string variant, float hue, float satScale, float valScale)
        {
            string matPath = MushroomFolder + "Mushroom_" + variant + "_Mat.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null) return existing;
            var baseMat = AssetDatabase.LoadAssetAtPath<Material>(MushroomFolder + "Mushroom_Mat.mat");
            if (baseMat == null) return null;

            string texPath = MushroomFolder + "Mushroom_BaseColor_" + variant + ".png";
            if (!System.IO.File.Exists(texPath))
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(System.IO.File.ReadAllBytes(MushroomFolder + "Mushroom_BaseColor.png"));
                Color[] px = tex.GetPixels();
                for (int i = 0; i < px.Length; i++)
                {
                    Color.RGBToHSV(px[i], out float h, out float s, out float v);
                    bool red = (h < 0.05f || h > 0.93f) && s > 0.35f;
                    if (!red) continue;
                    Color shifted = Color.HSVToRGB(hue, Mathf.Clamp01(s * satScale), Mathf.Clamp01(v * valScale));
                    shifted.a = px[i].a;
                    px[i] = shifted;
                }
                tex.SetPixels(px);
                System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                if (AssetImporter.GetAtPath(texPath) is TextureImporter ti) { ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
            }
            var mat = new Material(baseMat) { name = "Mushroom_" + variant + "_Mat" };
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        private static GameObject Mushroom(Context c, RainforestMats m, string name, float x, float topY, float z,
            float sizeX, float sizeZ, MushroomKind kind)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(sizeX * MushroomWalkFraction, MushroomColliderThickness, sizeZ * MushroomWalkFraction);
            box.center = new Vector3(0f, -MushroomColliderThickness * 0.5f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            // The Meshy mushroom: red (still), orange (bounce) or pale tan (collapses); stand-in primitives if missing.
            Material modelMaterial = kind == MushroomKind.Bounce ? m.ModelCapBounce : kind == MushroomKind.Collapse ? m.ModelCapCollapse : null;
            GameObject model = PlacePrefab(MushroomPrefabPath, visual.transform, Vector3.zero, Mathf.Max(sizeX, sizeZ),
                modelMaterial, (x * 53f + z * 31f) % 360f);
            if (model != null)
            {
                Bounds mb = RendererBounds(model);
                model.transform.position += new Vector3(x, topY + MushroomModelSink - mb.max.y, z);
            }

            if (model == null)
            {
                Material capMaterial = kind == MushroomKind.Bounce ? m.CapBounce : kind == MushroomKind.Collapse ? m.CapCollapse : m.CapStatic;
                float h = MushroomCapHeight;
                Prim(PrimitiveType.Sphere, "Cap", visual.transform, new Vector3(0f, -h * 0.5f, 0f), new Vector3(sizeX, h, sizeZ), capMaterial);

                float minSize = Mathf.Min(sizeX, sizeZ);
                Prim(PrimitiveType.Cylinder, "Stem", visual.transform, new Vector3(0f, -h * 0.6f - MushroomStemLength * 0.5f, 0f),
                    new Vector3(minSize * 0.38f, MushroomStemLength * 0.5f, minSize * 0.38f), m.Stem);

                int spots = kind == MushroomKind.Bounce ? 5 : 4;
                float spotDiameter = 0.2f * minSize;
                for (int i = 0; i < spots; i++)
                {
                    float angle = (i * (360f / spots) + 25f * (i % 2)) * Mathf.Deg2Rad;
                    float rho = 0.5f + 0.06f * (i % 2); // fraction of the cap radius
                    float px = Mathf.Cos(angle) * rho * sizeX * 0.5f;
                    float pz = Mathf.Sin(angle) * rho * sizeZ * 0.5f;
                    float py = -h * 0.5f + h * 0.5f * Mathf.Sqrt(1f - rho * rho) + 0.015f;
                    Prim(PrimitiveType.Cylinder, "Spot", visual.transform, new Vector3(px, py, pz),
                        new Vector3(spotDiameter, 0.01f, spotDiameter), m.Spot);
                }

            }

            if (kind == MushroomKind.Bounce)
            {
                root.AddComponent<RideableSurface>();
                var pad = root.AddComponent<BouncePad>();
                SetFields(pad, ("launchVelocity", new Vector3(0f, BounceLaunchSpeed, 0f)), ("squashTarget", visual.transform));
            }
            else if (kind == MushroomKind.Collapse)
            {
                root.AddComponent<RideableSurface>();
                var platform = root.AddComponent<CollapsingPlatform>();
                SetFields(platform,
                    ("standSeconds", MushroomCollapseSeconds),
                    ("requireContinuousStand", false),
                    ("visual", visual.transform),
                    ("fallDistance", topY - RiverTopY + 1.5f),
                    ("returnSeconds", 1.2f));
            }
            return root;
        }

        /// <summary>
        /// A floating log lying along X that rolls: its surface pushes the rider along Z and back, and it bobs.
        /// Stand-in model: cylinder with a moss stripe and branch stubs so the roll is visible (no log model; see report).
        /// </summary>
        private const string LogMeshPath = "Assets/Art/Props/Log/LogBark.asset";

        /// <summary>
        /// A unit log trunk (length 1 along X, diameter about 1): a 10-sided prism in 4 segments with
        /// alternating bark ridges and a little seeded radius jitter, flat shaded (low-poly look).
        /// Saved once as a mesh asset and reused by every log.
        /// </summary>
        private static Mesh LogBarkMesh()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(LogMeshPath);
            if (existing != null) return existing;

            const int sides = 10, segments = 4;
            var rng = new System.Random(77);
            var radius = new float[segments + 1, sides];
            for (int i = 0; i <= segments; i++)
            {
                for (int j = 0; j < sides; j++)
                {
                    float ridge = j % 2 == 0 ? 1f : 0.92f;
                    radius[i, j] = 0.5f * ridge * (0.95f + 0.07f * (float)rng.NextDouble());
                }
            }

            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();
            for (int i = 0; i < segments; i++)
            {
                float x0 = -0.5f + (float)i / segments, x1 = -0.5f + (float)(i + 1) / segments;
                for (int j = 0; j < sides; j++)
                {
                    int k = (j + 1) % sides;
                    float a0 = j * Mathf.PI * 2f / sides, a1 = k * Mathf.PI * 2f / sides;
                    Vector3 p00 = new Vector3(x0, Mathf.Cos(a0) * radius[i, j], Mathf.Sin(a0) * radius[i, j]);
                    Vector3 p01 = new Vector3(x0, Mathf.Cos(a1) * radius[i, k], Mathf.Sin(a1) * radius[i, k]);
                    Vector3 p10 = new Vector3(x1, Mathf.Cos(a0) * radius[i + 1, j], Mathf.Sin(a0) * radius[i + 1, j]);
                    Vector3 p11 = new Vector3(x1, Mathf.Cos(a1) * radius[i + 1, k], Mathf.Sin(a1) * radius[i + 1, k]);
                    int b = vertices.Count;
                    vertices.Add(p00); vertices.Add(p10); vertices.Add(p11); vertices.Add(p01);
                    triangles.Add(b); triangles.Add(b + 2); triangles.Add(b + 1);
                    triangles.Add(b); triangles.Add(b + 3); triangles.Add(b + 2);
                }
            }

            var mesh = new Mesh { name = "LogBark" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); // vertices are not shared between quads, so this is flat shading
            mesh.RecalculateBounds();

            if (!AssetDatabase.IsValidFolder("Assets/Art/Props/Log")) AssetDatabase.CreateFolder("Assets/Art/Props", "Log");
            AssetDatabase.CreateAsset(mesh, LogMeshPath);
            return mesh;
        }

        private static GameObject RollingLog(Context c, RainforestMats m, string name, float x, float topY, float z,
            float length, float diameter, float peakSpeed, float period, float phase,
            float bobAmplitude, float bobPeriod, float bobPhase)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            // Flat walkable strip on top of the (round) log.
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(length * 0.9f, 0.4f, diameter * 0.62f);
            box.center = new Vector3(0f, -0.2f, 0f);

            float radius = diameter * 0.5f;
            var roll = new GameObject("RollVisual");
            roll.transform.SetParent(root.transform, false);
            roll.transform.localPosition = new Vector3(0f, -radius, 0f);

            // Faceted bark trunk (low-poly mesh with bark ridges), length along X.
            var trunk = new GameObject("Trunk");
            trunk.transform.SetParent(roll.transform, false);
            trunk.transform.localScale = new Vector3(length, diameter, diameter);
            trunk.AddComponent<MeshFilter>().sharedMesh = LogBarkMesh();
            var trunkRenderer = trunk.AddComponent<MeshRenderer>();
            trunkRenderer.sharedMaterial = m.Log;
            trunkRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // End caps: light cut wood with a darker growth ring and a light core, on both ends.
            for (int e = -1; e <= 1; e += 2)
            {
                float ex = e * length * 0.5f;
                Prim(PrimitiveType.Cylinder, "EndWood", roll.transform, new Vector3(ex, 0f, 0f), new Vector3(diameter * 0.86f, 0.02f, diameter * 0.86f), m.LogEnd, new Vector3(0f, 0f, 90f));
                Prim(PrimitiveType.Cylinder, "EndRing", roll.transform, new Vector3(ex + e * 0.006f, 0f, 0f), new Vector3(diameter * 0.58f, 0.02f, diameter * 0.58f), m.LogRing, new Vector3(0f, 0f, 90f));
                Prim(PrimitiveType.Cylinder, "EndCore", roll.transform, new Vector3(ex + e * 0.012f, 0f, 0f), new Vector3(diameter * 0.40f, 0.02f, diameter * 0.40f), m.LogEnd, new Vector3(0f, 0f, 90f));
                Prim(PrimitiveType.Cylinder, "EndPith", roll.transform, new Vector3(ex + e * 0.018f, 0f, 0f), new Vector3(diameter * 0.14f, 0.02f, diameter * 0.14f), m.LogRing, new Vector3(0f, 0f, 90f));
            }
            float[] stubAngles = { 70f, 200f, 300f };
            float[] stubX = { -0.3f, 0.1f, 0.35f };
            for (int i = 0; i < stubAngles.Length; i++)
            {
                float a = stubAngles[i] * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a));
                Prim(PrimitiveType.Cylinder, "Stub", roll.transform, new Vector3(stubX[i] * length, 0f, 0f) + radial * (radius + 0.05f),
                    new Vector3(0.24f, 0.16f, 0.24f), m.LogStub, new Vector3(stubAngles[i], 0f, 0f));
            }

            var surface = root.AddComponent<RollingSurface>();
            SetFields(surface,
                ("driveDirection", Vector3.forward),
                ("peakSurfaceSpeed", peakSpeed),
                ("periodSeconds", period),
                ("phaseSeconds", phase),
                ("rollVisual", roll.transform),
                ("rollAxis", Vector3.right),
                ("rollRadius", radius));

            if (bobAmplitude > 0f)
            {
                var mover = root.AddComponent<PlatformMover>();
                SetFields(mover, ("travel", new Vector3(0f, bobAmplitude, 0f)), ("periodSeconds", bobPeriod), ("phaseSeconds", bobPhase));
            }
            return root;
        }

        /// <summary>
        /// A swinging vine: a rope from an overhead limb to a round foothold the player rides.
        /// The foothold swings along Z and stays level. <paramref name="extremeTopY"/> is the height of the
        /// foothold top at both ends of the swing. Stand-in model: cylinders (no vine model; see report).
        /// </summary>
        private static void SwingingVine(Context c, RainforestMats m, string name, float x, float z, float extremeTopY,
            float length, float amplitudeDegrees, float period, float phase, float trunkOffsetX)
        {
            float restTop = extremeTopY - length * (1f - Mathf.Cos(amplitudeDegrees * Mathf.Deg2Rad));
            var pivotPosition = new Vector3(x, restTop + length, z);

            var pivot = new GameObject(name + "_Pivot");
            pivot.transform.SetParent(c.Course, false);
            pivot.transform.position = pivotPosition;
            Prim(PrimitiveType.Cylinder, "Rope", pivot.transform, new Vector3(0f, -length * 0.5f, 0f), new Vector3(0.14f, length * 0.5f, 0.14f), m.Vine);

            var knot = new GameObject("Foothold");
            knot.transform.SetParent(pivot.transform, false);
            knot.transform.localPosition = new Vector3(0f, -length, 0f);
            var box = knot.AddComponent<BoxCollider>();
            box.size = new Vector3(1.3f, 0.3f, 1.3f);
            box.center = new Vector3(0f, -0.15f, 0f);
            Prim(PrimitiveType.Cylinder, "Disc", knot.transform, new Vector3(0f, -0.12f, 0f), new Vector3(1.5f, 0.12f, 1.5f), m.Knot);
            Prim(PrimitiveType.Sphere, "Bulb", knot.transform, new Vector3(0f, -0.4f, 0f), new Vector3(0.7f, 0.5f, 0.7f), m.Vine);
            knot.AddComponent<RideableSurface>();

            var swing = pivot.AddComponent<PendulumSwing>();
            SetFields(swing,
                ("swingAxis", Vector3.right),
                ("amplitudeDegrees", amplitudeDegrees),
                ("periodSeconds", period),
                ("phaseSeconds", phase),
                ("keepUpright", knot.transform));

            HangFrame(c, m, name, pivotPosition, trunkOffsetX);
        }

        /// <summary>
        /// A swinging Rainbell flower hanging over the path: a stem and a blue bell whose trigger respawns the
        /// player. Swings left-right across the path. Stand-in model: sphere + cylinders (no flower model; see report).
        /// </summary>
        private static void RainbellFlower(Context c, RainforestMats m, string name, Vector3 pivotPosition, float stemLength,
            float amplitudeDegrees, float period, float phase, float trunkOffsetX)
        {
            var pivot = new GameObject(name + "_Pivot");
            pivot.transform.SetParent(c.Course, false);
            pivot.transform.position = pivotPosition;
            Prim(PrimitiveType.Cylinder, "Stem", pivot.transform, new Vector3(0f, -stemLength * 0.5f, 0f), new Vector3(0.12f, stemLength * 0.5f, 0.12f), m.Vine);

            var bell = new GameObject("Bell");
            bell.transform.SetParent(pivot.transform, false);
            bell.transform.localPosition = new Vector3(0f, -stemLength, 0f);
            var hit = bell.AddComponent<SphereCollider>();
            hit.isTrigger = true;
            hit.radius = BellHitRadius;
            KillVolume trap = bell.AddComponent<KillVolume>();
            c.Result.Hazards.Add(trap);
            // The Meshy Rainbell head hanging from the stem; stand-in primitives if missing.
            if (PlacePrefab(RainbellHeadPrefabPath, bell.transform, bell.transform.position + Vector3.up * 0.55f, RainbellHeadWidth,
                    hangFromTop: true) != null)
            {
                hit.center = new Vector3(0f, -0.1f, 0f);
            }
            else
            {
                Prim(PrimitiveType.Sphere, "Dome", bell.transform, new Vector3(0f, 0.1f, 0f), new Vector3(1.1f, 1.0f, 1.1f), m.Bell);
                Prim(PrimitiveType.Cylinder, "Rim", bell.transform, new Vector3(0f, -0.38f, 0f), new Vector3(1.4f, 0.05f, 1.4f), m.BellRim);
                Prim(PrimitiveType.Sphere, "Clapper", bell.transform, new Vector3(0f, -0.5f, 0f), new Vector3(0.28f, 0.28f, 0.28f), c.In.WhiteMaterial);
            }

            var swing = pivot.AddComponent<PendulumSwing>();
            SetFields(swing,
                ("swingAxis", Vector3.forward),
                ("amplitudeDegrees", amplitudeDegrees),
                ("periodSeconds", period),
                ("phaseSeconds", phase));

            HangFrame(c, m, name, pivotPosition, trunkOffsetX);
        }

        /// <summary>A trunk beside the course with a limb reaching over it, so a pivot hangs from something. Visual only.</summary>
        private static void HangFrame(Context c, RainforestMats m, string name, Vector3 pivotPosition, float trunkOffsetX)
        {
            float trunkX = pivotPosition.x + trunkOffsetX;
            float trunkTop = pivotPosition.y + 0.7f;
            float height = trunkTop - PillarBottomY;
            Prim(PrimitiveType.Cylinder, name + "_Trunk", c.Scenery, new Vector3(trunkX, PillarBottomY + height * 0.5f, pivotPosition.z),
                new Vector3(1.0f, height * 0.5f, 1.0f), m.Bark);

            float limbLength = Mathf.Abs(trunkOffsetX) + 0.3f;
            Prim(PrimitiveType.Cylinder, name + "_Limb", c.Scenery,
                new Vector3(pivotPosition.x + trunkOffsetX * 0.5f, pivotPosition.y + 0.3f, pivotPosition.z),
                new Vector3(0.45f, limbLength * 0.5f, 0.45f), m.Bark, new Vector3(0f, 0f, 90f));
        }

        /// <summary>
        /// A horizontal branch whose top surface centre is at <paramref name="topCenter"/>, running along
        /// <paramref name="direction"/>. Stand-in model: flattened cylinder (no branch model; see report).
        /// </summary>
        /// <summary>A climbing tree: a bark trunk rising out of the river to <paramref name="topY"/>, crowned with a KayKit bush (visual only).</summary>
        private static void ClimbTree(Context c, RainforestMats m, string name, float x, float z, float topY)
        {
            float bottom = RiverTopY - 1f;
            float height = topY - bottom;
            Prim(PrimitiveType.Cylinder, name + "_Trunk", c.Scenery, new Vector3(x, bottom + height * 0.5f, z),
                new Vector3(0.9f, height * 0.5f, 0.9f), m.Bark);
            // Leafy crown sized to ClimbCrownWidth and centred on the trunk top.
            GameObject crown = PlaceModel("Bush_1_A", new Vector3(x, topY, z), new Vector3(0f, (z * 47f) % 360f, 0f), Vector3.one, m.PaletteTree, c.Scenery);
            if (crown == null) return;
            Bounds b = RendererBounds(crown);
            if (b.size.x > 0.01f) crown.transform.localScale = Vector3.one * (ClimbCrownWidth / b.size.x);
            b = RendererBounds(crown);
            crown.transform.position += new Vector3(x - b.center.x, topY - b.min.y - 0.6f, z - b.center.z);
        }

        private static GameObject Branch(Context c, string name, Vector3 topCenter, Vector3 direction, float visualLength,
            float width, float colliderLengthFraction, Material material)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.SetPositionAndRotation(topCenter, Quaternion.LookRotation(direction, Vector3.up));

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(width * 0.8f, 0.3f, visualLength * colliderLengthFraction);
            box.center = new Vector3(0f, -0.15f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            // Cylinder axis along the branch (root-local Z), 0.5 m thick, flat-ish.
            Prim(PrimitiveType.Cylinder, "Limb", visual.transform, new Vector3(0f, -0.25f, 0f),
                new Vector3(width, visualLength * 0.5f, 0.5f), material,
                Quaternion.LookRotation(Vector3.up, Vector3.forward).eulerAngles);
            return root;
        }

        /// <summary>Turns a branch into a rotten one: pale wood with dark rot spots that breaks after being stood on too long.</summary>
        private static void MakeRotten(GameObject branch, RainforestMats m, float topY)
        {
            Transform visual = branch.transform.Find("Visual");
            Prim(PrimitiveType.Cylinder, "RotSpotA", visual, new Vector3(0.1f, 0.012f, -0.45f), new Vector3(0.32f, 0.012f, 0.32f), m.RotSpot);
            Prim(PrimitiveType.Cylinder, "RotSpotB", visual, new Vector3(-0.12f, 0.012f, 0.4f), new Vector3(0.28f, 0.012f, 0.28f), m.RotSpot);

            branch.AddComponent<RideableSurface>();
            var platform = branch.AddComponent<CollapsingPlatform>();
            SetFields(platform,
                ("standSeconds", RottenBranchStandSeconds),
                ("requireContinuousStand", true),
                ("visual", visual),
                ("shakeAmplitude", 0.05f),
                ("fallSeconds", 0.5f),
                ("fallDistance", topY - RiverTopY + 1.5f),
                ("tiltDegrees", 25f),
                ("returnSeconds", 1.2f));
        }

        // ------------------------------------------------------------------ scenery

        /// <summary>
        /// Mossy cliff walls, giant trees and river boulders along both banks (visual only, shared tinted
        /// materials, no shadows). Deterministic for a given <paramref name="seed"/>.
        /// </summary>
        private static void RainforestScenery(Context c, RainforestMats m, float zMin, float zMax, int seed)
        {
            var rng = new System.Random(seed);
            string[] trees = { "Tree_4_C", "Tree_5_F", "Tree_7_C", "Tree_2_E", "Tree_6_C" };
            string[] cliffs = { "Rock_1_J", "Rock_1_K", "Rock_1_L", "Rock_1_M", "Rock_5_H" };
            string[] boulders = { "Rock_5_G", "Rock_5_F", "Rock_5_E", "Rock_1_H", "Rock_1_I" };

            for (float z = zMin; z <= zMax; z += 12f)
            {
                for (int s = 0; s < 2; s++)
                {
                    float side = s == 0 ? -1f : 1f;

                    // Green grass bank (KayKit hill, original palette) under the trees and cliffs, just above the river.
                    GrassIsland(c, "Bank_" + (side < 0f ? "L_" : "R_") + z, side * (BankInnerX + BankWidth * 0.5f), BankTopY,
                        z + 6f, BankWidth, 12.2f, BankBottomY);

                    // A Rainbell plant at the water's edge of the bank.
                    PlacePrefab(RainbellPlantPrefabPath, c.Scenery,
                        new Vector3(side * (BankInnerX + 1.5f + (float)rng.NextDouble() * 2f), BankTopY, z + 3f + (float)rng.NextDouble() * 4f),
                        RainbellPlantWidth, null, side < 0f ? 0f : 180f);

                    // Giant trees well clear of the course and of the secret branches (x down to -16).
                    PlaceModel(trees[rng.Next(trees.Length)],
                        new Vector3(side * (19f + (float)rng.NextDouble() * 8f), -3f, z + (float)rng.NextDouble() * 6f),
                        new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * (2.0f + (float)rng.NextDouble() * 0.8f),
                        m.PaletteTree, c.Scenery);

                    // Mossy cliff face behind them.
                    PlaceModel(cliffs[rng.Next(cliffs.Length)],
                        new Vector3(side * (34f + (float)rng.NextDouble() * 8f), -5f, z + (float)rng.NextDouble() * 6f),
                        new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * (4.5f + (float)rng.NextDouble() * 1.5f),
                        m.PaletteRock, c.Scenery);

                    // Boulders poking out of the river beside the course.
                    PlaceModel(boulders[rng.Next(boulders.Length)],
                        new Vector3(side * (8f + (float)rng.NextDouble() * 5f), RiverTopY - 0.6f, z + (float)rng.NextDouble() * 8f),
                        new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * (2.5f + (float)rng.NextDouble() * 1.0f),
                        m.PaletteRock, c.Scenery);
                }
            }
        }
    }
}
#endif
