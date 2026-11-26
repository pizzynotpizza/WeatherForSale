#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WeatherForSale.Obby;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Glacier layouts (story 004): level 1 "Slippery ice sheets" (แผ่นน้ำแข็งลื่น) and level 2 "Avalanche
    /// slope" (เนินหิมะถล่ม). The course runs along +Z from the start pad at the origin over dark icy water.
    /// Pads are grey-blue rock hills (KayKit colour set 1, pale tint) with a thin white snow cap
    /// (<see cref="Context.PadCapMaterial"/>); ice pads get a pale-blue glossy cap and an <see cref="IceSurface"/>.
    /// Reach used to place everything (moveSpeed 6, jumpSpeed 9, gravity 25): about 4 m flat or 3.9 m with a
    /// 0.5 m rise. Main-path gaps are 2.0 - 3.1 m; the hidden-Shard branches use 3.4 - 3.5 m gaps on small
    /// slippery tops. Sliding makes every gap harder than it looks.
    /// </summary>
    internal static partial class MapLevelBuilder
    {
        /// <summary>Surface of the icy water (top of the kill slab).</summary>
        private const float GlacierWaterY = -3f;

        /// <summary>Bottom of pads' rock bodies, just under the water.</summary>
        private const float GlacierBottomY = -5f;

        private const float GlacierFogDensity = 0.007f;
        private static readonly Color GlacierFogColor = new Color(0.82f, 0.90f, 0.97f);

        /// <summary>Thickness of a cracked ice slab, metres.</summary>
        private const float CrackedIceThickness = 1.0f;

        /// <summary>Snowball visual radius and its (smaller) hit radius, metres.</summary>
        private const float SnowballRadius = 0.65f;
        private const float SnowballHitRadius = 0.5f;

        // Level 2 slope: the snowball path line passes this height above the step treads' centres.
        private const float SlopeTopBallZ = 47.5f;
        private const float SlopeTopBallY = 5.85f;
        private const float SlopeGrade = 1f / 6f; // 0.5 m rise per 3 m step

        private sealed class GlacierMats
        {
            // Snow: the KayKit palette with its green grass swatches turned white (no green skirt under the snow).
            public readonly Material PadRock = RecolouredPaletteMat("Obby_GL_PaletteRock", "snow", new Color(0.95f, 0.97f, 1.0f), new Color(1.05f, 1.08f, 1.18f));
            public readonly Material SnowRock = PaletteMat("Obby_GL_PaletteSnowRock", new Color(1.50f, 1.55f, 1.75f));
            public readonly Material Boulder = PaletteMat("Obby_GL_PaletteBoulder", new Color(1.10f, 1.10f, 1.25f));
            public readonly Material Pine = RecolouredPaletteMat("Obby_GL_PalettePine", "frost", new Color(0.78f, 0.90f, 0.86f), Color.white); // snow-frosted needles
            public readonly Material Pillar = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_GL_RockBody", new Color(0.62f, 0.68f, 0.76f));
            public readonly Material SnowCap = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_GL_SnowCap", new Color(0.96f, 0.98f, 1.00f));
            public readonly Material IceCap = IceMat("Obby_GL_IceCap", new Color(0.70f, 0.87f, 0.97f));
            public readonly Material Crack = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_GL_IceCrack", new Color(0.20f, 0.40f, 0.62f));
            public readonly Material Snowball = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_GL_Snowball", new Color(0.97f, 0.98f, 1.00f));
            public readonly Material Shard = GlowMat("Obby_GL_Shard", new Color(0.62f, 0.22f, 1.00f), 2.2f);
        }

        /// <summary>A glossy pale-blue ice material.</summary>
        private static Material IceMat(string name, Color color)
        {
            Material mat = ObbyTestSceneBuilder.GetOrCreateMaterial(name, color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void PrepareGlacier(Context c, GlacierMats m)
        {
            c.Result.HazardTopY = GlacierWaterY;
            c.PadMaterial = m.PadRock;
            c.PadColorSet = 1;
            c.PadCapMaterial = null; // the snow is in the recoloured palette; ice pads still add their own ice cap
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = GlacierFogColor;
            RenderSettings.fogDensity = GlacierFogDensity;
        }

        // ------------------------------------------------------------------ level 1: Slippery ice sheets

        private static void BuildGlacier1(Context c)
        {
            var m = new GlacierMats();
            PrepareGlacier(c, m);
            StartIsland(c, 6f, 6f, GlacierBottomY);

            // A: three ice sheets (slippery, 0.12 grip): you slide across each and must jump the gaps.
            IcePad(c, m, "Ice_A1", 0.0f, 0.0f, 8.0f, 5f, 6f, 0.12f, m.IceCap);
            IcePad(c, m, "Ice_A2", 0.5f, 0.3f, 15.5f, 4f, 5f, 0.12f, m.IceCap);
            IcePad(c, m, "Ice_A3", -0.5f, 0.6f, 22.5f, 4f, 5f, 0.12f, m.IceCap);
            AddCheckpoint(c, "Checkpoint_1", 0f, 0.9f, 30.5f, 5f, m.Pillar, GlacierBottomY);

            // B: a narrow ice strip, then four cracked slabs that break 0.45 s after the first step.
            IcePad(c, m, "Ice_B1", 0.0f, 0.9f, 38.5f, 3f, 6f, 0.12f, m.IceCap);
            CrackedIceSlab(c, m, "Cracked_1", 0.8f, 1.0f, 46.0f, 3f, 3f, 0.45f);
            CrackedIceSlab(c, m, "Cracked_2", -0.8f, 1.1f, 51.0f, 3f, 3f, 0.45f);
            CrackedIceSlab(c, m, "Cracked_3", 0.5f, 1.2f, 56.2f, 3f, 3f, 0.45f);
            CrackedIceSlab(c, m, "Cracked_4", -0.5f, 1.3f, 61.4f, 3f, 3f, 0.45f);
            AddCheckpoint(c, "Checkpoint_2", 0f, 1.4f, 68.5f, 5f, m.Pillar, GlacierBottomY);

            // C: two narrow slippery causeways (2.2 m), two more cracked slabs, then the finish pad.
            IcePad(c, m, "Ice_C1", 0.0f, 1.4f, 78.0f, 2.2f, 8f, 0.10f, m.IceCap);
            IcePad(c, m, "Ice_C2", 0.0f, 1.4f, 87.0f, 2.2f, 6f, 0.10f, m.IceCap);
            CrackedIceSlab(c, m, "Cracked_5", 0.0f, 1.6f, 94.0f, 3f, 3f, 0.45f);
            CrackedIceSlab(c, m, "Cracked_6", 0.8f, 1.7f, 99.0f, 3f, 3f, 0.45f);
            AddFinish(c, 0f, 1.8f, 106.0f, 6f, 6f, m.Pillar, GlacierBottomY);

            // Secret branch off checkpoint 2 toward -X: two small slippery ice tops and a cracked slab, 3.4 m gaps. Hidden Shard at the end.
            IcePad(c, m, "Secret_1", -6.6f, 1.8f, 66.5f, 1.4f, 1.4f, 0.12f, m.IceCap);
            IcePad(c, m, "Secret_2", -11.35f, 2.2f, 68.5f, 1.3f, 1.3f, 0.12f, m.IceCap);
            CrackedIceSlab(c, m, "Secret_3", -16.2f, 2.6f, 66.5f, 1.6f, 1.6f, 0.45f);
            AddShard(c, c.MapId, new Vector3(-16.2f, 3.7f, 66.5f), m.Shard, 0.4f, 0.8f);

            GlacierScenery(c, m, -15f, 120f, 6001);
        }

        // ------------------------------------------------------------------ level 2: Avalanche slope

        private static void BuildGlacier2(Context c)
        {
            var m = new GlacierMats();
            PrepareGlacier(c, m);
            StartIsland(c, 6f, 6f, GlacierBottomY);

            // A: an ice sheet and the first checkpoint at the foot of the slope.
            IcePad(c, m, "Ice_A1", 0.0f, 0.0f, 7.5f, 4f, 5f, 0.10f, m.IceCap);
            AddCheckpoint(c, "Checkpoint_1", 0f, 0.0f, 15.5f, 5f, m.Pillar, GlacierBottomY);

            // B: the slope, ten slippery rock-and-snow steps (0.5 m rise, 3 m deep, 8 m wide) rising to y 5.0.
            for (int k = 0; k < 10; k++)
            {
                IcePad(c, m, "Step_" + (k + 1), 0f, 0.5f * (k + 1), 20.5f + 3f * k, 8f, 3f, 0.08f, m.SnowCap);
            }

            // Boulders to hide behind: solid; the snowballs of their lane stop at them, so the ground just below is safe.
            Boulder(c, m, "Boulder_L0", 0.0f, 2.5f, 32.5f, "Rock_1_H", 1.3f, 20f);
            Boulder(c, m, "Boulder_Left", -2.3f, 1.5f, 26.5f, "Rock_1_I", 1.3f, 100f);
            Boulder(c, m, "Boulder_Corridor", 1.15f, 4.0f, 41.5f, "Rock_1_H", 1.2f, 200f);

            // Snowball waves in three lanes (x -2.3, 0, 2.3). Speed 6.5 - 7 m/s, staggered phases.
            SnowballLane(c, m, "Snowball_L0a", 0.0f, 47.5f, 34.4f, 6.5f, 1.6f, 0.0f);
            SnowballLane(c, m, "Snowball_L0b", 0.0f, 47.5f, 34.4f, 6.5f, 1.6f, 2.0f);
            SnowballLane(c, m, "Snowball_Left", -2.3f, 47.5f, 28.2f, 7.0f, 1.8f, 1.7f);
            SnowballLane(c, m, "Snowball_Right_a", 2.3f, 47.5f, 20.5f, 7.0f, 1.5f, 3.0f);
            SnowballLane(c, m, "Snowball_Right_b", 2.3f, 47.5f, 20.5f, 7.0f, 1.5f, 5.2f);

            AddCheckpoint(c, "Checkpoint_2", 0f, 5.0f, 54.5f, 5f, m.Pillar, GlacierBottomY);

            // C: an icy patch and three cracked slabs (0.4 s), another ice sheet, then the finish deck and weather dais.
            IcePad(c, m, "Ice_C1", 0.0f, 5.0f, 61.8f, 3.5f, 4f, 0.08f, m.IceCap);
            CrackedIceSlab(c, m, "Cracked_1", 0.0f, 5.2f, 67.3f, 3f, 3f, 0.4f);
            CrackedIceSlab(c, m, "Cracked_2", 1.0f, 5.4f, 72.4f, 3f, 3f, 0.4f);
            CrackedIceSlab(c, m, "Cracked_3", -1.0f, 5.6f, 77.5f, 3f, 3f, 0.4f);
            IcePad(c, m, "Ice_C2", 0.0f, 5.6f, 84.0f, 4f, 5f, 0.08f, m.IceCap);
            AddFinish(c, 0f, 5.6f, 93.5f, 6f, 9f, m.Pillar, GlacierBottomY, weatherDais: true);

            // Secret branch off checkpoint 2 toward +X: three small cracked slabs, 3.4 - 3.5 m gaps. Hidden Shard at the end.
            CrackedIceSlab(c, m, "Secret_1", 6.8f, 5.4f, 55.0f, 1.6f, 1.6f, 0.4f);
            CrackedIceSlab(c, m, "Secret_2", 11.8f, 5.8f, 53.0f, 1.6f, 1.6f, 0.4f);
            CrackedIceSlab(c, m, "Secret_3", 16.8f, 6.2f, 55.0f, 1.6f, 1.6f, 0.4f);
            AddShard(c, c.MapId, new Vector3(16.8f, 7.3f, 55.0f), m.Shard, 0.4f, 0.8f);

            GlacierScenery(c, m, -15f, 130f, 6002);
        }

        // ------------------------------------------------------------------ obstacle builders

        /// <summary>
        /// A slippery pad: a normal pad (rock hill, hidden gameplay box) with <paramref name="cap"/> on top and an
        /// <see cref="IceSurface"/> on its collider so the player slides on it.
        /// </summary>
        private static GameObject IcePad(Context c, GlacierMats m, string name, float x, float topY, float z, float sizeX, float sizeZ,
            float traction, Material cap)
        {
            Material previous = c.PadCapMaterial;
            c.PadCapMaterial = cap;
            GameObject pad = Slab(c, name, x, topY, z, sizeX, sizeZ, c.In.PlatformMaterial, m.Pillar, GlacierBottomY);
            c.PadCapMaterial = previous;
            var ice = pad.AddComponent<IceSurface>();
            SetFields(ice, ("traction", traction));
            return pad;
        }

        /// <summary>
        /// A cracked ice slab: slippery, with dark crack lines on top, that gives way after being stepped on
        /// (<see cref="CollapsingPlatform"/>: shakes, falls into the water, comes back after 3 s).
        /// </summary>
        private static GameObject CrackedIceSlab(Context c, GlacierMats m, string name, float x, float topY, float z, float sizeX, float sizeZ, float standSeconds)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(sizeX, CrackedIceThickness, sizeZ);
            box.center = new Vector3(0f, -CrackedIceThickness * 0.5f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            Material previous = c.PadCapMaterial;
            c.PadCapMaterial = m.IceCap;
            GameObject island = GrassIsland(c, name + "_Island", x, topY, z, sizeX, sizeZ, topY - CrackedIceThickness, null, visual.transform);
            c.PadCapMaterial = previous;
            if (island == null)
            {
                Prim(PrimitiveType.Cylinder, "Slab", visual.transform, new Vector3(0f, -CrackedIceThickness * 0.5f, 0f),
                    new Vector3(sizeX, CrackedIceThickness * 0.5f, sizeZ), m.IceCap);
            }

            // Crack lines on top (dark blue), a fixed pattern scaled to the slab.
            float[] angles = { 20f, 75f, 125f, 160f, 50f };
            float[] offsetX = { -0.25f, 0.2f, -0.05f, 0.3f, -0.3f };
            float[] offsetZ = { 0.15f, -0.2f, 0.3f, 0.0f, -0.3f };
            float[] lengths = { 0.8f, 0.6f, 0.9f, 0.5f, 0.7f };
            float minSize = Mathf.Min(sizeX, sizeZ);
            for (int i = 0; i < angles.Length; i++)
            {
                Prim(PrimitiveType.Cube, "Crack", visual.transform, new Vector3(offsetX[i] * sizeX, 0.06f, offsetZ[i] * sizeZ),
                    new Vector3(0.07f, 0.03f, lengths[i] * minSize), m.Crack, new Vector3(0f, angles[i], 0f));
            }

            var ice = root.AddComponent<IceSurface>();
            SetFields(ice, ("traction", 0.25f));
            var platform = root.AddComponent<CollapsingPlatform>();
            SetFields(platform,
                ("standSeconds", standSeconds),
                ("requireContinuousStand", false),
                ("visual", visual.transform),
                ("shakeAmplitude", 0.05f),
                ("fallSeconds", 0.4f),
                ("fallDistance", topY - GlacierWaterY + 1.5f),
                ("goneSeconds", 3f),
                ("returnSeconds", 1.2f));
            return root;
        }

        /// <summary>A solid boulder on a step (visual: a KayKit rock in light grey; box collider a little smaller than the rock).</summary>
        private static void Boulder(Context c, GlacierMats m, string name, float x, float topY, float z, string model, float scale, float yaw)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(1.7f, 2.0f, 1.7f);
            box.center = new Vector3(0f, 1.0f, 0f);
            GameObject rock = PlaceModel(model, new Vector3(x, topY, z), new Vector3(0f, yaw, 0f), Vector3.one * scale, m.Boulder, root.transform);
            if (rock == null) Prim(PrimitiveType.Sphere, "Rock", root.transform, new Vector3(0f, 1.0f, 0f), new Vector3(1.9f, 2.0f, 1.9f), m.Boulder);
        }

        /// <summary>Height of a snowball's centre on the slope line at a given z (it rides about 0.85 m above the step treads).</summary>
        private static float SlopeBallY(float z)
        {
            return SlopeTopBallY - (SlopeTopBallZ - z) * SlopeGrade;
        }

        /// <summary>
        /// A snowball rolling down the slope in one lane from <paramref name="startZ"/> to <paramref name="endZ"/>
        /// (a boulder or the foot of the slope), then pausing and starting again (<see cref="RollingHazard"/>).
        /// Stand-in model: a white sphere (no snowball model; see report).
        /// </summary>
        private static void SnowballLane(Context c, GlacierMats m, string name, float x, float startZ, float endZ,
            float speed, float restartDelay, float phase)
        {
            float startY = SlopeBallY(startZ), endY = SlopeBallY(endZ);
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, startY, startZ);

            var hit = root.AddComponent<SphereCollider>();
            hit.isTrigger = true;
            hit.radius = SnowballHitRadius;
            c.Result.Hazards.Add(root.AddComponent<KillVolume>());

            var spin = new GameObject("Spin");
            spin.transform.SetParent(root.transform, false);
            Prim(PrimitiveType.Sphere, "Ball", spin.transform, Vector3.zero, Vector3.one * (SnowballRadius * 2f), m.Snowball);
            // A few dark flecks so the roll is visible on the white ball.
            Prim(PrimitiveType.Cylinder, "Fleck", spin.transform, new Vector3(0f, SnowballRadius, 0.1f), new Vector3(0.22f, 0.02f, 0.22f), m.Crack);
            Prim(PrimitiveType.Cylinder, "Fleck", spin.transform, new Vector3(0.45f, -0.3f, 0.3f), new Vector3(0.18f, 0.02f, 0.18f), m.Crack, new Vector3(0f, 0f, -60f));
            Prim(PrimitiveType.Cylinder, "Fleck", spin.transform, new Vector3(-0.4f, 0.1f, -0.4f), new Vector3(0.18f, 0.02f, 0.18f), m.Crack, new Vector3(30f, 0f, 70f));

            var roll = root.AddComponent<RollingHazard>();
            SetFields(roll,
                ("travel", new Vector3(0f, endY - startY, endZ - startZ)),
                ("speed", speed),
                ("restartDelaySeconds", restartDelay),
                ("phaseSeconds", phase),
                ("spinVisual", spin.transform),
                ("spinAxis", new Vector3(-1f, 0f, 0f)),
                ("rollRadius", SnowballRadius));
        }

        // ------------------------------------------------------------------ scenery

        /// <summary>
        /// Snowy banks, grey-white cliffs and sharp peaks, snowy pines and floating ice chunks on the dark water
        /// (visual only, shared tinted materials, no shadows). Deterministic for a given <paramref name="seed"/>.
        /// </summary>
        private static void GlacierScenery(Context c, GlacierMats m, float zMin, float zMax, int seed)
        {
            var rng = new System.Random(seed);
            string[] pines = { "Tree_4_A", "Tree_4_B", "Tree_4_C", "Tree_5_B" };
            string[] cliffs = { "Rock_1_J", "Rock_1_K", "Rock_1_L", "Rock_1_M" };
            string[] peaks = { "Rock_4_D", "Rock_4_H", "Rock_4_C", "Rock_4_G" };

            for (float z = zMin; z <= zMax; z += 16f)
            {
                for (int s = 0; s < 2; s++)
                {
                    float side = s == 0 ? -1f : 1f;

                    // Snowy bank (rock hill with a snow cap): inner edge at |x| >= 21, clear of the secret branches (|x| up to 17.5).
                    float sizeX = 14f + (float)rng.NextDouble() * 8f;
                    float sizeZ = 14f + (float)rng.NextDouble() * 4f;
                    float topY = 0.5f + (float)rng.NextDouble() * 4.0f;
                    float cx = side * (21f + sizeX * 0.5f + (float)rng.NextDouble() * 4f);
                    float cz = z + (float)rng.NextDouble() * 4f;
                    GrassIsland(c, "Bank_" + (side < 0f ? "L_" : "R_") + z, cx, topY, cz, sizeX, sizeZ, GlacierWaterY - 1f, m.PadRock, c.Scenery);

                    // Snowy pines on the bank.
                    int pineCount = 2 + rng.Next(2);
                    for (int i = 0; i < pineCount; i++)
                    {
                        PlaceModel(pines[rng.Next(pines.Length)],
                            new Vector3(cx + ((float)rng.NextDouble() - 0.5f) * sizeX * 0.7f, topY, cz + ((float)rng.NextDouble() - 0.5f) * sizeZ * 0.7f),
                            new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * (1.5f + (float)rng.NextDouble() * 0.9f), m.Pine, c.Scenery);
                    }

                    // Grey-white cliff face and a sharp peak behind.
                    PlaceModel(cliffs[rng.Next(cliffs.Length)],
                        new Vector3(side * (36f + (float)rng.NextDouble() * 6f), GlacierWaterY - 2f, z + (float)rng.NextDouble() * 6f),
                        new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * (4.5f + (float)rng.NextDouble() * 1.5f), m.SnowRock, c.Scenery);
                    PlaceModel(peaks[rng.Next(peaks.Length)],
                        new Vector3(side * (48f + (float)rng.NextDouble() * 14f), GlacierWaterY - 2f, z + (float)rng.NextDouble() * 10f),
                        new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), new Vector3(7f, 6f + (float)rng.NextDouble() * 3f, 7f), m.SnowRock, c.Scenery);
                }
            }

            // Floating ice chunks on the dark water, away from the course line.
            for (int i = 0; i < 26; i++)
            {
                float side = rng.Next(2) == 0 ? -1f : 1f;
                float x = side * (5f + (float)rng.NextDouble() * 24f);
                float z = zMin + (float)rng.NextDouble() * (zMax - zMin);
                float radius = 1.0f + (float)rng.NextDouble() * 2.2f;
                Prim(PrimitiveType.Cylinder, "IceChunk", c.Scenery, new Vector3(x, GlacierWaterY + 0.02f, z),
                    new Vector3(radius * 2f, 0.18f, radius * 1.6f), m.IceCap,
                    new Vector3(((float)rng.NextDouble() - 0.5f) * 8f, (float)rng.NextDouble() * 180f, ((float)rng.NextDouble() - 0.5f) * 8f));
            }
        }
    }
}
#endif
