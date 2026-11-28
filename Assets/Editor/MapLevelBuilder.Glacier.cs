#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WeatherForSale.Obby;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Glacier layouts (story 004): level 1 "Slippery ice sheets" (แผ่นน้ำแข็งลื่น) and level 2 "Avalanche
    /// slope" (เนินหิมะถล่ม). The course runs along +Z from the start pad at the origin over dark icy water.
    /// Pads are rock hills in the snow palette; ice pads get a pale-blue glossy cap and an <see cref="IceSurface"/>.
    /// Difficulty pass: every section mixes mechanics (ice under rolling snowballs, cracked slabs you must keep
    /// hopping across, narrow causeways, drifting icebergs, a smooth slippery snow ramp with avalanche lanes and
    /// cover boulders), safe pads are few and small, and each level has a precision sequence.
    /// Reach used to place everything (moveSpeed 6, jumpSpeed 9, gravity 25): about 4.3 m flat at full speed,
    /// 3.9 m with a 0.5 m rise. Main-path gaps are 2.0 - 3.3 m; the hidden-Shard branches use 3.3 - 3.5 m gaps
    /// on small slippery or cracked tops.
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

        // ---- snowballs
        /// <summary>Small snowball (flat-ice sweeps) visual radius and its smaller hit radius, metres.</summary>
        private const float SnowballRadius = 0.6f;
        private const float SnowballHitRadius = 0.5f;
        /// <summary>
        /// Avalanche ball on the ramp: 1.8 m across. Hit radius 0.8 + the player's 0.3 = 1.1 m, and the lanes are
        /// 2 m apart, so there is no safe seam between lanes: only cover or timing keeps you alive.
        /// </summary>
        private const float AvalancheRadius = 0.9f;
        private const float AvalancheHitRadius = 0.8f;
        /// <summary>A ball's centre rides this far above (radius) over its ground, so the blocker sweep never grazes the ground.</summary>
        private const float BallLift = 0.03f;
        /// <summary>The blocker sweep is this much thinner than the ball (and stops <see cref="BallBlockerGap"/> early), so a ball never overlaps a boulder.</summary>
        private const float BallProbeInset = 0.05f;
        private const float BallBlockerGap = 0.15f;
        /// <summary>Seconds the snow puff shows where a ball appears / ends.</summary>
        private const float SnowPuffSeconds = 0.4f;
        /// <summary>Every ice pad's and cracked slab's grip is scaled by this (play-test: the ice was not slippery enough).</summary>
        private const float IceGripScale = 0.45f;
        /// <summary>Snowball-lane pads: pad width (x) and the spacing between ball lanes along the pad (z), metres.</summary>
        private const float LanePadWidth = 3f;
        private const float LaneSpacing = 2f;
        /// <summary>Snowy chute beside a flat ice pad that the pad's snowballs roll down from: run out from the pad edge, rise, width, thickness (metres).</summary>
        private const float ChuteRun = 3.6f;
        private const float ChuteRise = 1.4f;
        private const float ChuteWidth = 1.6f;
        private const float ChuteThickness = 0.5f;

        // ---- level 2 snow ramp (a smooth tilted slab, not steps)
        /// <summary>
        /// Ramp tilt and grip. Player slide model (PlayerController): downhill pull = gravity 25 x sin x cos x (1 - traction)
        /// = 25 x 0.25 x 0.88 = 5.5 m/s^2; drag 2 /s gives a 2.75 m/s slide when standing still; at full uphill input the net
        /// climb is 6 - 2.75 = 3.25 m/s (54 % of run speed). Own acceleration on it is 45 x 0.12 = 5.4 m/s^2 (slow to steer).
        /// </summary>
        private const float RampAngleDeg = 15f;
        private const float RampTraction = 0.06f;
        /// <summary>Foot of the ramp: the far edge of checkpoint 1 (z) and its top (y).</summary>
        private const float RampFootZ = 47f;
        private const float RampFootY = 1f;
        /// <summary>Horizontal length (rise 34 x tan 15 = 9.1 m), width (5 lanes, 2 m apart, at x -4, -2, 0, 2, 4) and slab thickness, metres.</summary>
        private const float RampLength = 34f;
        private const float RampWidth = 12f;
        private const float RampThickness = 1.5f;
        /// <summary>Length of each rock block drawn under the ramp slab (visual only).</summary>
        private const float RampBodySegment = 2.5f;
        /// <summary>Avalanche balls appear this far up the ramp (horizontal metres from the foot): clear of the top deck by 0.3 m.</summary>
        private const float RampSpawnD = 32.6f;
        /// <summary>A ball with no boulder in its lane ends here, at the foot.</summary>
        private const float RampBallEndD = 0.2f;

        // ---- cover boulders on the ramp
        /// <summary>Boulder footprint across (one lane) and along the slope, metres.</summary>
        private const float BoulderWidth = 2.0f;
        private const float BoulderDepth = 1.8f;
        /// <summary>Collider top above the HIGHER (uphill) ground at the boulder: well over the 1.6 m jump apex, so it cannot be jumped.</summary>
        private const float BoulderClearHeight = 2.6f;
        /// <summary>How far the boulder sinks below the lower (downhill) ground.</summary>
        private const float BoulderSink = 0.3f;

        // ---- drifting icebergs
        /// <summary>Thickness of an iceberg's walkable collider, metres.</summary>
        private const float FloeColliderThickness = 0.8f;
        /// <summary>Grip on an iceberg's top (slippery while it carries you).</summary>
        private const float FloeTraction = 0.07f;

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

            // A: a wide ice sheet swept by two snowballs rolling across it (you slide, so time the crossing from the
            // start pad), then three small cracked slabs that break 0.4 s after the first step (keep hopping, zig-zag).
            SnowballLanePad(c, m, "Ice_A1", 0.3f, 5.0f, 9f, 5.0f, 1.4f, 0.12f);          // z 5.0 - 14.0, 3 m wide
            CrackedIceSlab(c, m, "Cracked_A1", 1.0f, 0.6f, 17.3f, 2.2f, 2.2f, 0.4f);        // gap 2.2
            CrackedIceSlab(c, m, "Cracked_A2", -1.0f, 0.9f, 21.9f, 2.2f, 2.2f, 0.4f);       // gap 2.4
            CrackedIceSlab(c, m, "Cracked_A3", 1.0f, 1.2f, 26.5f, 2.2f, 2.2f, 0.4f);        // gap 2.4
            AddCheckpoint(c, "Checkpoint_1", 0f, 1.4f, 31.6f, 4f, m.Pillar, GlacierBottomY); // z 29.6 - 33.6

            // B (precision): two 1.2 m ice causeways (the second offset sideways, diagonal 3.3 m jump), three 1.3 m posts
            // (the middle one cracked) you must stop on, then two icebergs drifting side to side in opposite phase.
            IcePad(c, m, "Causeway_B1", 0f, 1.4f, 38.6f, 1.2f, 6f, 0.10f, m.IceCap);        // z 35.6 - 41.6
            IcePad(c, m, "Causeway_B2", 1.5f, 1.6f, 47.6f, 1.2f, 6f, 0.10f, m.IceCap);      // z 44.6 - 50.6
            // (play-test: the three narrow posts were removed) a wide slippery pad, then a cracked slab.
            IcePad(c, m, "Ice_B3", 0.6f, 1.9f, 55.0f, 2.4f, 4.0f, 0.10f, m.IceCap);      // z 53 - 57, gap 2.4
            CrackedIceSlab(c, m, "Cracked_B4", -0.4f, 2.2f, 61.0f, 2.2f, 2.2f, 0.4f);       // z 59.9 - 62.1, gap 2.9
            IceFloe(c, m, "Floe_B1", 0f, 2.5f, 66.0f, 2.6f, 2.6f, 1.8f, 6f, 0f);           // z 64.7 - 67.3, x +-1.8
            IceFloe(c, m, "Floe_B2", 0f, 2.5f, 71.6f, 2.6f, 2.6f, 1.8f, 6f, 3f);           // z 70.3 - 72.9, opposite phase
            AddCheckpoint(c, "Checkpoint_2", 0f, 2.8f, 77.0f, 4f, m.Pillar, GlacierBottomY); // z 75 - 79

            // C: an ice sheet swept by two faster snowballs, two cracked slabs (0.35 s), a narrow ice strip after a
            // 3.3 m gap near full reach, then the finish pad.
            SnowballLanePad(c, m, "Ice_C1", 2.8f, 81.0f, 6f, 5.5f, 1.2f, 0.10f);         // z 81 - 87, 3 m wide
            CrackedIceSlab(c, m, "Cracked_C2", 0f, 3.1f, 90.4f, 2f, 2f, 0.35f);             // gap 2.4
            CrackedIceSlab(c, m, "Cracked_C3", 1.2f, 3.4f, 94.8f, 2f, 2f, 0.35f);           // gap 2.4
            IcePad(c, m, "Ice_C4", 1.2f, 3.4f, 100.6f, 1.4f, 3f, 0.10f, m.IceCap);          // gap 3.3 (flat)
            AddFinish(c, 0f, 3.4f, 107.6f, 5f, 5f, m.Pillar, GlacierBottomY);               // gap 3.0

            // Secret branch off checkpoint 2 toward -X: two tiny slippery ice tops and a cracked slab, 3.0 - 3.5 m
            // gaps rising 0.4 m each (you land sliding and must jump again at once). Hidden Shard at the end.
            IcePad(c, m, "Secret_1", -6.1f, 3.2f, 76.2f, 1.2f, 1.2f, 0.12f, m.IceCap);
            IcePad(c, m, "Secret_2", -10.3f, 3.6f, 77.9f, 1.1f, 1.1f, 0.12f, m.IceCap);
            CrackedIceSlab(c, m, "Secret_3", -14.5f, 4.0f, 76.3f, 1.4f, 1.4f, 0.35f);
            AddShard(c, c.MapId, new Vector3(-14.5f, 5.1f, 76.3f), m.Shard, 0.4f, 0.8f);

            GlacierScenery(c, m, -15f, 125f, 6001);
        }

        // ------------------------------------------------------------------ level 2: Avalanche slope

        private static void BuildGlacier2(Context c)
        {
            var m = new GlacierMats();
            PrepareGlacier(c, m);
            StartIsland(c, 6f, 6f, GlacierBottomY);

            // A: a drifting iceberg, two cracked slabs (0.4 s), then an ice sheet swept by two snowballs, and the
            // first checkpoint at the foot of the ramp.
            IceFloe(c, m, "Floe_A1", 0f, 0.3f, 6.8f, 2.6f, 2.6f, 1.8f, 5f, 0f);            // z 5.5 - 8.1, x +-1.8
            CrackedIceSlab(c, m, "Cracked_A2", -1.0f, 0.6f, 11.8f, 2.2f, 2.2f, 0.4f);       // gap 2.6
            CrackedIceSlab(c, m, "Cracked_A3", 1.0f, 0.9f, 16.3f, 2.2f, 2.2f, 0.4f);        // gap 2.3
            IcePad(c, m, "Causeway_A4", 1.0f, 0.9f, 22.3f, 1.4f, 5f, 0.10f, m.IceCap);       // z 19.8 - 24.8, gap 2.4
            IceFloe(c, m, "Floe_A5", 0f, 1.0f, 28.0f, 2.4f, 2.4f, 1.8f, 5f, 2.5f);           // z 26.8 - 29.2, x +-1.8
            CrackedIceSlab(c, m, "Cracked_A6", -1.0f, 1.0f, 32.6f, 2.2f, 2.2f, 0.4f);       // z 31.5 - 33.7
            SnowballLanePad(c, m, "Ice_A7", RampFootY, 36f, 5f, 5.5f, 1.6f, 0.10f);      // z 36 - 41, 3 m wide
            AddCheckpoint(c, "Checkpoint_1", 0f, RampFootY, RampFootZ - 2f, 4f, m.Pillar, GlacierBottomY); // z 43 - 47

            // B: the avalanche ramp, a smooth slippery 15 degree snow slab, 12 m wide and 34 m long (z 31 - 65,
            // y 1.0 - 10.1). You slide back unless you keep pushing uphill. Five ball lanes (x -4 A, -2 B, 0 C, 2 D,
            // 4 E), all live from the top; each ball stops at the uphill face of the first boulder in its lane, so the
            // lane is safe only below that boulder. One boulder per row, zigzagging across the ramp (never two in a
            // row): C wall at the foot, D right, B left, C again, E right edge, A left edge. Route: up C, round the
            // low C wall, up C to its high boulder, then timed crossings of live lanes to E or A, and a live dash to the top.
            SnowRamp(c, m);
            CoverBoulder(c, m, "Boulder_R1_C", 0f, 4.0f, "Rock_1_H", 0f);
            CoverBoulder(c, m, "Boulder_R2_D", 2f, 9.0f, "Rock_1_I", 90f);
            CoverBoulder(c, m, "Boulder_R3_B", -2f, 14.0f, "Rock_1_H", 180f);
            CoverBoulder(c, m, "Boulder_R4_C", 0f, 19.0f, "Rock_1_I", 270f);
            CoverBoulder(c, m, "Boulder_R5_E", 4f, 24.0f, "Rock_1_H", 90f);
            CoverBoulder(c, m, "Boulder_R6_A", -4f, 29.0f, "Rock_1_I", 0f);

            // Waves: every lane live, each on its own rhythm (speed, pause, phase) so the gaps drift against each other.
            // A ball meeting a boulder rolls sideways round it (outer lanes inward, the middle lane to either side) and
            // carries on down the neighbouring lane, so only the strip right behind a boulder is safe.
            // One ball per lane, each pausing 4 - 5.5 s after its run, so a wave comes roughly every 2 s across the ramp.
            RampSnowball(c, m, "Avalanche_A", -4f, 6f, 4.6f, 0f);
            RampSnowball(c, m, "Avalanche_B", -2f, 5.5f, 5.2f, -2.3f);
            RampSnowball(c, m, "Avalanche_C", 0f, 6f, 5.5f, -4.1f);
            RampSnowball(c, m, "Avalanche_D", 2f, 6.5f, 4.2f, -1.2f);
            RampSnowball(c, m, "Avalanche_E", 4f, 5.5f, 4.9f, -5.6f);

            float topY = RampSurfaceY(RampLength);
            float topZ = RampZ(RampLength); // 65
            Slab(c, "Ramp_Top", 0f, topY, topZ + 0.75f, RampWidth, 1.5f, c.In.PlatformMaterial, m.Pillar, GlacierBottomY); // z 53 - 54.5
            AddCheckpoint(c, "Checkpoint_2", 0f, topY, topZ + 3.5f, 4f, m.Pillar, GlacierBottomY);                       // z 54.5 - 58.5

            // C: a narrow slippery ice causeway,
            // a drifting iceberg, two cracked slabs (0.35 s), a tiny very slippery pad, then the finish deck and dais.
            IcePad(c, m, "Causeway_C1", 0f, topY, topZ + 10f, 1.4f, 5f, 0.10f, m.IceCap);   // z 60.5 - 65.5
            IceFloe(c, m, "Floe_C2", 0f, topY, topZ + 16f, 2.4f, 2.4f, 1.8f, 5f, 1.2f);    // z 67.8 - 70.2, x +-1.8
            CrackedIceSlab(c, m, "Cracked_C3", 0f, topY + 0.3f, topZ + 21f, 2f, 2f, 0.35f);   // z 73 - 75
            CrackedIceSlab(c, m, "Cracked_C4", 1.0f, topY + 0.6f, topZ + 25.4f, 2f, 2f, 0.35f); // z 77.4 - 79.4
            IcePad(c, m, "Ice_C5", 0f, topY + 0.6f, topZ + 30f, 2.5f, 2.5f, 0.08f, m.IceCap); // z 81.75 - 84.25
            AddFinish(c, 0f, topY + 0.6f, topZ + 39f, 9f, 10f, m.Pillar, GlacierBottomY, weatherDais: true); // z 87 - 97, 9 m wide

            // Secret branch off checkpoint 2 toward +X (run along its far side): two small cracked slabs and a tiny ice
            // top, 3.3 - 3.4 m gaps rising 0.4 m each. Hidden Shard at the end.
            CrackedIceSlab(c, m, "Secret_1", 6.1f, topY + 0.4f, topZ + 5.0f, 1.4f, 1.4f, 0.35f);
            CrackedIceSlab(c, m, "Secret_2", 10.85f, topY + 0.8f, topZ + 6.8f, 1.3f, 1.3f, 0.35f);
            IcePad(c, m, "Secret_3", 15.4f, topY + 1.2f, topZ + 5.0f, 1.2f, 1.2f, 0.12f, m.IceCap);
            AddShard(c, c.MapId, new Vector3(15.4f, topY + 2.3f, topZ + 5.0f), m.Shard, 0.4f, 0.8f);

            GlacierScenery(c, m, -15f, 135f, 6002);
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
            SetFields(ice, ("traction", traction * IceGripScale));
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
            SetFields(ice, ("traction", 0.25f * IceGripScale));
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

        /// <summary>
        /// A drifting iceberg: a pale-blue ice block rising out of the water that slides side to side
        /// (<see cref="PlatformMover"/>, <paramref name="driftX"/> metres each way) with a slippery top
        /// (<see cref="IceSurface"/>, which also carries the rider). Stand-in model: a KayKit hill block in the ice colour.
        /// </summary>
        private static GameObject IceFloe(Context c, GlacierMats m, string name, float x, float topY, float z, float sizeX, float sizeZ,
            float driftX, float periodSeconds, float phaseSeconds)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(sizeX, FloeColliderThickness, sizeZ);
            box.center = new Vector3(0f, -FloeColliderThickness * 0.5f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            float bottomY = GlacierWaterY - 1f;
            Material previous = c.PadCapMaterial;
            c.PadCapMaterial = null;
            GameObject berg = GrassIsland(c, name + "_Berg", x, topY, z, sizeX, sizeZ, bottomY, m.IceCap, visual.transform);
            c.PadCapMaterial = previous;
            if (berg == null)
            {
                float height = topY - bottomY;
                Prim(PrimitiveType.Cube, "Berg", visual.transform, new Vector3(0f, -height * 0.5f, 0f), new Vector3(sizeX, height, sizeZ), m.IceCap);
            }

            var ice = root.AddComponent<IceSurface>();
            SetFields(ice, ("traction", FloeTraction));
            var mover = root.AddComponent<PlatformMover>();
            SetFields(mover, ("travel", new Vector3(driftX, 0f, 0f)), ("periodSeconds", periodSeconds), ("phaseSeconds", phaseSeconds));
            return root;
        }

        // ---- the level 2 ramp

        private static float RampTan => Mathf.Tan(RampAngleDeg * Mathf.Deg2Rad);

        /// <summary>Unit normal of the ramp surface (tilted back toward -Z).</summary>
        private static Vector3 RampNormal => Quaternion.Euler(-RampAngleDeg, 0f, 0f) * Vector3.up;

        /// <summary>Unit direction straight up the ramp surface (+Z and up).</summary>
        private static Vector3 RampUphill => Quaternion.Euler(-RampAngleDeg, 0f, 0f) * Vector3.forward;

        /// <summary>Height of the ramp surface at <paramref name="d"/> horizontal metres from the foot.</summary>
        private static float RampSurfaceY(float d) => RampFootY + d * RampTan;

        /// <summary>World z at <paramref name="d"/> horizontal metres from the foot.</summary>
        private static float RampZ(float d) => RampFootZ + d;

        /// <summary>
        /// The smooth snow ramp: one tilted box (collider + white snow look) with a slippery <see cref="IceSurface"/>,
        /// over rock blocks that fill the space down to the water (visual only, their tops hidden inside the slab).
        /// </summary>
        private static void SnowRamp(Context c, GlacierMats m)
        {
            float slopeLength = RampLength / Mathf.Cos(RampAngleDeg * Mathf.Deg2Rad);
            Vector3 topMiddle = new Vector3(0f, RampSurfaceY(RampLength * 0.5f), RampZ(RampLength * 0.5f));
            Vector3 center = topMiddle - RampNormal * (RampThickness * 0.5f);
            GameObject ramp = Prim(PrimitiveType.Cube, "SnowRamp", c.Course, center, new Vector3(RampWidth, RampThickness, slopeLength),
                m.SnowCap, new Vector3(-RampAngleDeg, 0f, 0f), keepCollider: true);
            ramp.GetComponent<MeshRenderer>().receiveShadows = true;
            var ice = ramp.AddComponent<IceSurface>();
            SetFields(ice, ("traction", RampTraction));

            // Rock body: blocks whose tops sit 0.3 m under the surface at their downhill end. The slab is 1.55 m deep
            // (vertically), more than the 0.67 m the surface rises over one block, so no gap shows under it.
            int index = 0;
            for (float d0 = 0f; d0 < RampLength - 0.01f; d0 += RampBodySegment)
            {
                float segment = Mathf.Min(RampBodySegment, RampLength - d0);
                GrassIsland(c, "SnowRamp_Body_" + (++index), 0f, RampSurfaceY(d0) - 0.3f, RampZ(d0 + segment * 0.5f),
                    RampWidth - 0.6f, segment, GlacierBottomY);
            }
        }

        /// <summary>
        /// A cover boulder on the ramp, centred on lane <paramref name="x"/> at <paramref name="d"/> metres from the foot:
        /// a solid box <see cref="BoulderWidth"/> x <see cref="BoulderDepth"/> whose top is <see cref="BoulderClearHeight"/>
        /// above the uphill ground (cannot be jumped), with a KayKit rock stretched to exactly that box and a snow cap.
        /// Snowballs stop at its uphill face (<see cref="RollingHazard"/> blocker sweep); the player hides below it.
        /// </summary>
        private static void CoverBoulder(Context c, GlacierMats m, string name, float x, float d, string model, float yaw)
        {
            float bottom = RampSurfaceY(d - BoulderDepth * 0.5f) - BoulderSink;
            float top = RampSurfaceY(d + BoulderDepth * 0.5f) + BoulderClearHeight;
            float height = top - bottom; // 3.38 m on the 15 degree ramp
            float z = RampZ(d);

            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, bottom, z);
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(BoulderWidth, height, BoulderDepth);
            box.center = new Vector3(0f, height * 0.5f, 0f);

            Vector3 size = new Vector3(BoulderWidth, height, BoulderDepth);
            if (FitModel(model, new Vector3(x, bottom, z), size, yaw, m.Boulder, root.transform) == null)
                Prim(PrimitiveType.Cube, "Rock", root.transform, new Vector3(0f, height * 0.5f, 0f), size, m.Boulder);
            Prim(PrimitiveType.Sphere, "SnowCap", root.transform, new Vector3(0f, height - 0.1f, 0f),
                new Vector3(BoulderWidth * 0.8f, 0.5f, BoulderDepth * 0.8f), m.SnowCap);
        }

        /// <summary>
        /// Places a KayKit model (visual only) stretched to fill a box of <paramref name="size"/> standing on
        /// <paramref name="bottomCenter"/>. <paramref name="yaw"/> must be a multiple of 90 degrees so the fit is exact.
        /// Returns null when the model is missing.
        /// </summary>
        private static GameObject FitModel(string model, Vector3 bottomCenter, Vector3 size, float yaw, Material material, Transform parent)
        {
            GameObject go = PlaceModel(model, bottomCenter, new Vector3(0f, yaw, 0f), Vector3.one, material, parent);
            if (go == null) return null;
            Bounds b = RendererBounds(go);
            if (b.size.x < 0.01f || b.size.y < 0.01f || b.size.z < 0.01f) return go;

            int quarter = ((Mathf.RoundToInt(yaw / 90f) % 4) + 4) % 4;
            float sx = size.x / b.size.x, sy = size.y / b.size.y, sz = size.z / b.size.z;
            // Turned 90 / 270 degrees, the model's local X lies along world Z (and local Z along world X).
            go.transform.localScale = quarter % 2 == 1 ? new Vector3(sz, sy, sx) : new Vector3(sx, sy, sz);
            b = RendererBounds(go);
            go.transform.position += new Vector3(bottomCenter.x - b.center.x, bottomCenter.y - b.min.y, bottomCenter.z - b.center.z);
            return go;
        }

        /// <summary>
        /// An avalanche ball in ramp lane <paramref name="x"/>: appears (with a puff) near the top, rolls straight down
        /// the slope riding its surface, and ends at the uphill face of the first boulder in its lane (found at run
        /// time by a sweep, see <see cref="RollingHazard"/>) or at the foot, in a puff; then starts again.
        /// </summary>
        private static void RampSnowball(Context c, GlacierMats m, string name, float x, float speed, float restartDelay, float phase)
        {
            Vector3 surfacePoint = new Vector3(x, RampSurfaceY(RampSpawnD), RampZ(RampSpawnD));
            Vector3 start = surfacePoint + RampNormal * (AvalancheRadius + BallLift);
            float slopeDistance = (RampSpawnD - RampBallEndD) / Mathf.Cos(RampAngleDeg * Mathf.Deg2Rad);
            GameObject ball = Snowball(c, m, name, start, -RampUphill * slopeDistance, RampNormal, AvalancheRadius, AvalancheHitRadius,
                speed, restartDelay, phase, true);
            // Hitting a boulder it rolls sideways round it and carries on downhill (never through it, never bursting).
            SetFields(ball.GetComponent<RollingHazard>(),
                ("deflectAtBlockers", true),
                ("deflectAxis", Vector3.right),
                ("deflectPreferSign", x < 0f ? 1f : -1f),
                ("deflectLimit", RampWidth * 0.5f - AvalancheRadius),
                ("groundNormal", RampNormal));
        }

        /// <summary>
        /// A narrow ice pad (<see cref="LanePadWidth"/> wide, centred on x 0) from <paramref name="zStart"/> along
        /// <paramref name="length"/>, with a snowball lane every <see cref="LaneSpacing"/>: each lane has its own chute,
        /// alternating sides, and the balls run staggered so there is always a gap to time.
        /// </summary>
        private static void SnowballLanePad(Context c, GlacierMats m, string name, float topY, float zStart, float length,
            float speed, float restartDelay, float traction)
        {
            IcePad(c, m, name, 0f, topY, zStart + length * 0.5f, LanePadWidth, length, traction, m.IceCap);
            float inner = LanePadWidth * 0.5f - 0.6f;
            int lane = 0;
            for (float z = zStart + 1f; z <= zStart + length - 0.9f; z += LaneSpacing, lane++)
            {
                float side = lane % 2 == 0 ? -1f : 1f;
                SweepSnowball(c, m, name + "_Lane" + lane, topY, z, side * inner, -side * inner, speed, restartDelay, -lane * 0.75f);
            }
        }

        /// <summary>
        /// A snowball that rolls one way only: it starts at the top of a snowy chute beside the pad (on the
        /// <paramref name="fromX"/> side), rolls down onto the pad, straight across it at <paramref name="z"/>, and off
        /// the far edge into the water, where it ends in a puff; then the next one starts at the chute top.
        /// <paramref name="fromX"/> / <paramref name="toX"/> are the old sweep ends (0.6 m inside the pad's edges).
        /// </summary>
        private static void SweepSnowball(Context c, GlacierMats m, string name, float surfaceY, float z, float fromX, float toX,
            float speed, float restartDelay, float phase)
        {
            float dir = Mathf.Sign(toX - fromX);
            float edgeFrom = fromX - dir * 0.6f;
            float edgeTo = toX + dir * 0.6f;
            float rest = SnowballRadius + BallLift;

            // The chute: a tilted snow slab from the pad edge up and out, on a rock post.
            float angle = Mathf.Atan2(ChuteRise, ChuteRun) * Mathf.Rad2Deg;
            float slabLength = Mathf.Sqrt(ChuteRise * ChuteRise + ChuteRun * ChuteRun);
            Quaternion tilt = Quaternion.Euler(0f, 0f, -dir * angle);
            Vector3 normal = tilt * Vector3.up;
            Vector3 topMiddle = new Vector3(edgeFrom - dir * ChuteRun * 0.5f, surfaceY + ChuteRise * 0.5f, z);
            Prim(PrimitiveType.Cube, name + "_Chute", c.Course, topMiddle - normal * (ChuteThickness * 0.5f),
                new Vector3(slabLength, ChuteThickness, ChuteWidth), m.SnowCap, new Vector3(0f, 0f, -dir * angle), keepCollider: true);
            float postX = edgeFrom - dir * (ChuteRun - 0.5f);
            float postTop = surfaceY + ChuteRise - 0.4f;
            Prim(PrimitiveType.Cube, name + "_ChutePost", c.Course, new Vector3(postX, (postTop + GlacierWaterY - 1f) * 0.5f, z),
                new Vector3(0.9f, postTop - (GlacierWaterY - 1f), ChuteWidth * 0.8f), m.Pillar);

            // Path: chute top -> pad edge -> across -> just past the far edge -> down into the water.
            Vector3 start = new Vector3(edgeFrom - dir * (ChuteRun - 0.3f), surfaceY + ChuteRise * ((ChuteRun - 0.3f) / ChuteRun), z) + normal * rest;
            Vector3 atEdge = new Vector3(edgeFrom, surfaceY + rest, z);
            Vector3 farEdge = new Vector3(edgeTo + dir * 0.4f, surfaceY + rest, z);
            Vector3 inWater = new Vector3(edgeTo + dir * 1.6f, GlacierWaterY + 0.2f, z);
            GameObject ball = Snowball(c, m, name, start, inWater - start, Vector3.up, SnowballRadius, SnowballHitRadius,
                speed, restartDelay, phase, false);
            SetFields(ball.GetComponent<RollingHazard>(),
                ("waypoints", new[] { atEdge - start, farEdge - start, inWater - start }),
                ("groundNormal", Vector3.up));
        }

        /// <summary>
        /// A rolling snowball (<see cref="RollingHazard"/> + trigger <see cref="KillVolume"/>): starts at
        /// <paramref name="start"/> (its centre), rolls along <paramref name="travel"/> over ground whose normal is
        /// <paramref name="groundNormal"/>, then pauses and starts again. With <paramref name="stopAtBlockers"/> its run
        /// ends at the first solid collider in the way. A snow puff marks where it appears and ends.
        /// Stand-in model: a white sphere with a few dark flecks (no snowball model; see report).
        /// </summary>
        private static GameObject Snowball(Context c, GlacierMats m, string name, Vector3 start, Vector3 travel, Vector3 groundNormal,
            float radius, float hitRadius, float speed, float restartDelay, float phase, bool stopAtBlockers)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = start;

            var hit = root.AddComponent<SphereCollider>();
            hit.isTrigger = true;
            hit.radius = hitRadius;
            c.Result.Hazards.Add(root.AddComponent<KillVolume>());

            var spin = new GameObject("Spin");
            spin.transform.SetParent(root.transform, false);
            // Meshy packed snowball (its lumps show the roll); else a white sphere with a few dark flecks.
            MeshyPropBaker.PropMesh snow = MeshyPropBaker.BakeProp("Glacier", "Snowball", MeshyPropBaker.ZUp);
            if (snow != null)
            {
                MeshyPropBaker.PlaceProp(snow, "Ball", spin.transform, Vector3.down * radius, Vector3.one * (radius * 2f));
            }
            else
            {
                Prim(PrimitiveType.Sphere, "Ball", spin.transform, Vector3.zero, Vector3.one * (radius * 2f), m.Snowball);
                float k = radius / 0.65f;
                Prim(PrimitiveType.Cylinder, "Fleck", spin.transform, new Vector3(0f, 0.65f, 0.1f) * k, new Vector3(0.22f, 0.02f, 0.22f) * k, m.Crack);
                Prim(PrimitiveType.Cylinder, "Fleck", spin.transform, new Vector3(0.45f, -0.3f, 0.3f) * k, new Vector3(0.18f, 0.02f, 0.18f) * k, m.Crack, new Vector3(0f, 0f, -60f));
                Prim(PrimitiveType.Cylinder, "Fleck", spin.transform, new Vector3(-0.4f, 0.1f, -0.4f) * k, new Vector3(0.18f, 0.02f, 0.18f) * k, m.Crack, new Vector3(30f, 0f, 70f));
            }

            Transform puff = SnowPuff(c, m, name + "_Puff", start, radius);

            // Rolling without slipping: the top moves along the travel, so the spin axis is normal x direction.
            Vector3 axis = Vector3.Cross(groundNormal, travel.normalized);
            if (axis.sqrMagnitude < 0.0001f) axis = Vector3.right;

            var roll = root.AddComponent<RollingHazard>();
            SetFields(roll,
                ("travel", travel),
                ("speed", speed),
                ("restartDelaySeconds", restartDelay),
                ("phaseSeconds", phase),
                ("spinVisual", spin.transform),
                ("spinAxis", axis.normalized),
                ("rollRadius", radius),
                ("stopAtBlockers", stopAtBlockers),
                ("blockerProbeRadius", radius - BallProbeInset),
                ("blockerGap", BallBlockerGap),
                ("burstVisual", puff),
                ("burstSeconds", SnowPuffSeconds),
                ("burstAtStart", true));
            return root;
        }

        /// <summary>A small cluster of white spheres (visual only) used as the snowball's puff; RollingHazard moves and scales it.</summary>
        private static Transform SnowPuff(Context c, GlacierMats m, string name, Vector3 position, float radius)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = position;
            Vector3[] offsets =
            {
                Vector3.zero, new Vector3(0.6f, 0.15f, 0f), new Vector3(-0.6f, 0.1f, 0.1f),
                new Vector3(0.1f, 0.25f, 0.6f), new Vector3(-0.1f, 0.05f, -0.6f),
            };
            float[] sizes = { 1.2f, 0.75f, 0.7f, 0.75f, 0.65f };
            for (int i = 0; i < offsets.Length; i++)
            {
                Prim(PrimitiveType.Sphere, "Puff", root.transform, offsets[i] * radius, Vector3.one * (sizes[i] * radius), m.SnowCap);
            }
            return root.transform;
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
