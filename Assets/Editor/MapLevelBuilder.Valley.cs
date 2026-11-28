using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WeatherForSale.Obby;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Valley layouts (story 004): level 1 "Canyon glide" and level 2 "Fans and wind vents" (ใบพัดและช่องลม).
    /// Both courses run along +Z from the start pad at the origin.
    /// Level 1: walk off the cliff-top start pad into a glide (GlideZone / GlideMode) and fly a winding,
    /// walled sandstone canyon over a river: three glide legs (about 44 + 60 + 39 m) split by two landing
    /// ledges (the checkpoints), with hawks (Hawk: loops, figure-8s, dives, stacked pairs), pinches, stone
    /// arches and an overhang to fly under, rock spires, and the Shard in a side alcove
    /// behind a circling hawk. Walls, arches, spires, ledge rock and the river are kill triggers.
    /// Level 2: pads are sandstone mesa pillars going down into a cloud-filled chasm; fans, vents and two
    /// long suspension bridges with an irregular swing (IrregularSwing) and a static rock between them.
    /// Reach used to place level 2 (moveSpeed 6, jumpSpeed 9, gravity 25, edge to edge of the walkable
    /// boxes): about 4.3 m flat, 4.0 m with a 0.5 m rise, 3.5 m with a 1.0 m rise.
    /// Rule kept everywhere: no kill trigger (fan blade) inside a wind zone or a vent column.
    /// </summary>
    internal static partial class MapLevelBuilder
    {
        /// <summary>Bottom of the mesa pillars, just inside the cloud slab (its top is at -60).</summary>
        private const float ValleyBottomY = -61f;

        private const float ValleyFogDensity = 0.009f;
        private static readonly Color ValleyFogColor = new Color(0.86f, 0.90f, 0.98f);

        /// <summary>Walkable box side of a floating rock as a fraction of its footprint.</summary>
        private const float FloatingRockWalkFraction = 0.75f;

        /// <summary>Thickness of a floating rock's hill block, metres.</summary>
        private const float FloatingRockThickness = 1.8f;

        /// <summary>Columns of wind vents lift for this long above their grate (metres).</summary>
        private const float VentColumnWidth = 2.2f;

        /// <summary>Side of the Valley checkpoint pads, metres (was 5; smaller safe pads for the difficulty pass).</summary>
        private const float ValleyCheckpointSize = 4f;

        // ---- crumbling rocks (CollapsingPlatform) ----

        /// <summary>Seconds a secret-branch crumbling rock holds (shorter than the main path).</summary>
        private const float ValleySecretCrumbleStandSeconds = 0.7f;

        /// <summary>How far a crumbling rock drops before it vanishes, metres.</summary>
        private const float ValleyCrumbleFallDistance = 9f;

        /// <summary>Seconds a crumbled rock stays gone before it rises back.</summary>
        private const float ValleyCrumbleGoneSeconds = 2.5f;

        /// <summary>Seconds a crumbled rock takes to rise back into place.</summary>
        private const float ValleyCrumbleReturnSeconds = 1.0f;

        /// <summary>Peak shake of a crumbling rock's visual during its warning, metres (bigger than the mushroom default so it reads at a distance).</summary>
        private const float ValleyCrumbleShake = 0.09f;

        /// <summary>Degrees a crumbling rock's visual tilts while it falls away.</summary>
        private const float ValleyCrumbleTiltDegrees = 18f;

        // ---- fans ----

        /// <summary>Gap between a giant fan's blade tip and its support tower, metres (keeps the blades out of the tower).</summary>
        private const float ValleyFanTowerClearance = 1.1f;

        /// <summary>Sweeper (flat fan on a raft): height of the blades' underside above the deck, metres.</summary>
        private const float ValleySweeperBladeBottom = 0.2f;

        /// <summary>Sweeper blade thickness, metres (top at 0.5 m: a normal jump, apex 1.6 m, clears it).</summary>
        private const float ValleySweeperBladeThickness = 0.3f;

        /// <summary>Sweeper blade width (across the direction it sweeps), metres.</summary>
        private const float ValleySweeperBladeWidth = 0.35f;

        /// <summary>Radius of the sweeper's central post; the blades start here, metres.</summary>
        private const float ValleySweeperHubRadius = 0.2f;

        // ---- bridges ----

        /// <summary>Length of one bridge plank along the deck, metres. A missing plank pair is a 1.2 m hole you must hop.</summary>
        private const float ValleyPlankLength = 0.6f;
        /// <summary>Swing of each bridge section, degrees (steady): reach about 2.7 m on the 12 m ropes.</summary>
        private const float Valley2PairSwingDegrees = 13f;
        /// <summary>Gap left between the two sections of a zone when they meet, metres (they never overlap).</summary>
        private const float Valley2PairMeetGap = 0.15f;
        /// <summary>Meshy bridge plank drawn this tall (board plus its rope posts), metres.</summary>
        private const float ValleyPlankModelHeight = 0.5f;
        /// <summary>Height of the board's top above the plank model's bottom, metres (it sits on the deck surface).</summary>
        private const float ValleyPlankModelBoardTop = 0.27f;

        // ---- level 2 tuning ----

        /// <summary>Level 2 bridge gust speed, m/s (decks are 2.2 m wide).</summary>
        private const float Valley2BridgeGustSpeed = 1.5f; // play-test: the gust was too strong (was 3.4)
        /// <summary>Level 2 bridges: rope length (metres). Longer ropes = a longer, slower arc.</summary>
        private const float Valley2BridgeRope = 12f;
        /// <summary>Level 2 bridges: deck end to pad edge at the widest swing, metres (the deck never touches a pad).</summary>
        private const float Valley2BridgeClearance = 0.7f;
        /// <summary>Level 2 bridges: deck width, metres.</summary>
        private const float Valley2BridgeDeckWidth = 2.2f;
        /// <summary>Level 2: the static rock between the two bridges (Z depth; 2.6 m wide), metres.</summary>
        private const float Valley2MidRockSize = 2.6f;
        /// <summary>Level 2: finish deck centre past the vent-3 landing centre, metres (deck starts 2.2 m past the vent column).</summary>
        private const float Valley2FinishAfterLanding = 7.8f;
        /// <summary>
        /// Bridge 1 swing: amplitude 10 - 18 deg drifting over 9.7 s, average period 4.0 s sped up / slowed by a
        /// 0.5 rad wobble over 6.1 s (single swings about 3 - 6 s). Boarding gap at the smallest swing 2.3 m.
        /// </summary>
        private static readonly BridgeSwing Valley2Bridge1Swing = new BridgeSwing(14f, 4f, 4.0f, 9.7f, 0.5f, 6.1f, 0f);
        /// <summary>
        /// Bridge 2 swing: amplitude 12 - 18 deg over 8.3 s, average period 4.6 s with a 0.4 rad wobble over 7.7 s
        /// (single swings about 3.7 - 6 s), 1.3 s offset. Boarding gap at the smallest swing 1.9 m.
        /// </summary>
        private static readonly BridgeSwing Valley2Bridge2Swing = new BridgeSwing(15f, 3f, 4.6f, 8.3f, 0.4f, 7.7f, 1.3f);

        /// <summary>Tuning of an <see cref="IrregularSwing"/> bridge (degrees, seconds, radians).</summary>
        private readonly struct BridgeSwing
        {
            public readonly float BaseAmplitude, AmplitudeVariation, Period, AmplitudePeriod, Wobble, WobblePeriod, Phase;

            public BridgeSwing(float baseAmplitude, float amplitudeVariation, float period, float amplitudePeriod,
                float wobble, float wobblePeriod, float phase)
            {
                BaseAmplitude = baseAmplitude;
                AmplitudeVariation = amplitudeVariation;
                Period = period;
                AmplitudePeriod = amplitudePeriod;
                Wobble = wobble;
                WobblePeriod = wobblePeriod;
                Phase = phase;
            }

            /// <summary>Widest swing, degrees.</summary>
            public float MaxAmplitude => BaseAmplitude + Mathf.Abs(AmplitudeVariation);
        }

        // ---- level 1: canyon glide ----

        /// <summary>Canyon floor (river bed) height, metres. The river kill slab's top is <see cref="GlideFloorKillTop"/>.</summary>
        private const float GlideFloorY = -12f;
        private const float GlideFloorKillTop = -11.8f;
        /// <summary>Top of the canyon walls, metres (far above every glide ceiling: no flying over).</summary>
        private const float GlideWallTopY = 24f;
        private const float GlideWallThickness = 6f;
        /// <summary>Canyon walls are built in blocks this long along Z, metres.</summary>
        private const float GlideWallSegment = 3f;
        /// <summary>Kill triggers sit this far inside the visible rock faces, metres (a graze is forgiven).</summary>
        private const float GlideKillInset = 0.2f;
        // A darker strata band on every wall block, above any head height.
        private const float GlideWallBandY = 7f, GlideWallBandHeight = 1.2f, GlideWallBandProtrude = 0.15f, GlideWallBandDepth = 0.5f;
        /// <summary>The canyon runs from behind the start pad to the far edge of the finish pad (level 2 joins on there when merged).</summary>
        private const float GlideCourseStartZ = -9f, GlideCourseEndZ = 177f;
        /// <summary>Glide ceiling (feet) above each launch edge, metres.</summary>
        private const float GlideCeilingAboveLaunch = 2.5f;
        /// <summary>Landing zones reach this far above the leg's ceiling, metres.</summary>
        private const float GlideLandingHeadroom = 2f;
        /// <summary>Landing ledge pads (checkpoint / finish) are this square, metres; an apron of <see cref="GlideApronLength"/> lies in front.</summary>
        private const float GlideLedgeSize = 6f;
        private const float GlideApronLength = 4f;
        // Launch trigger: starts this far past the edge, this deep, from this far below to this far above the pad top.
        private const float GlideLaunchGap = 0.4f, GlideLaunchDepth = 3.6f, GlideLaunchBelow = 6f, GlideLaunchAbove = 3f;
        private const float GlideRiverWidth = 3.2f;
        /// <summary>Hawk kill box (wingspan, height, length), metres; the model's wings reach 2.6 m.</summary>
        private static readonly Vector3 HawkHitbox = new Vector3(1.5f, 0.45f, 0.8f);

        /// <summary>A canyon control point: centre X and the half-widths to the left / right wall faces at Z.</summary>
        private readonly struct CanyonPoint
        {
            public readonly float Z, X, HalfLeft, HalfRight;

            public CanyonPoint(float z, float x, float halfLeft, float halfRight)
            {
                Z = z; X = x; HalfLeft = halfLeft; HalfRight = halfRight;
            }
        }

        /// <summary>
        /// Level 1 canyon (interpolated linearly between points, sampled per wall block). Pinches: z 27 - 36
        /// (half-width 3.8), 105.5 - 111 and 149 - 156 (3.4). Side alcove on the left z 78 - 93 (5 m deep).
        /// Ledges: z 46 - 58 and 117 - 129 (half-width 4.5).
        /// </summary>
        private static readonly CanyonPoint[] Valley1Canyon =
        {
            new CanyonPoint(-9f, 0f, 5f, 5f),
            new CanyonPoint(4f, 0f, 5f, 5f),
            new CanyonPoint(10f, 1f, 7f, 7f),
            new CanyonPoint(22f, 2.5f, 7f, 7f),
            new CanyonPoint(27f, 2f, 3.8f, 3.8f),
            new CanyonPoint(36f, 1f, 3.8f, 3.8f),
            new CanyonPoint(41f, -0.5f, 6f, 6f),
            new CanyonPoint(46f, -2f, 4.5f, 4.5f),
            new CanyonPoint(58f, -2f, 4.5f, 4.5f),
            new CanyonPoint(64f, -0.5f, 6f, 6f),
            new CanyonPoint(74f, 2f, 6.5f, 6.5f),
            new CanyonPoint(77.9f, 2f, 6f, 6f),
            new CanyonPoint(78f, 2f, 11f, 6f),
            new CanyonPoint(92.9f, 1.4f, 11f, 6f),
            new CanyonPoint(93f, 1.4f, 6f, 6f),
            new CanyonPoint(103f, 0.5f, 6f, 6f),
            new CanyonPoint(105.5f, 0f, 3.4f, 3.4f),
            new CanyonPoint(111f, 0f, 3.4f, 3.4f),
            new CanyonPoint(117f, -1f, 4.5f, 4.5f),
            new CanyonPoint(129f, -1f, 4.5f, 4.5f),
            new CanyonPoint(134f, 0f, 6.5f, 6.5f),
            new CanyonPoint(144f, 2f, 6.5f, 6.5f),
            new CanyonPoint(149f, 2f, 3.4f, 3.4f),
            new CanyonPoint(156f, 1f, 3.4f, 3.4f),
            new CanyonPoint(160f, 0f, 6f, 6f),
            new CanyonPoint(166f, 0f, 4.5f, 4.5f),
            new CanyonPoint(177f, 0f, 4.5f, 4.5f),
        };

        private sealed class ValleyMats
        {
            // The KayKit hills / rocks keep their palette; this tint turns the green grass top golden-orange and the grey rock body orange.
            public readonly Material PadRock = PaletteMat("Obby_VY_PaletteSandstone", new Color(1.7f, 1.0f, 0.42f));
            // Crumbling rocks: darker, redder sandstone so they read as different before you step on them.
            public readonly Material CrumbleRock = PaletteMat("Obby_VY_PaletteCrumble", new Color(1.15f, 0.52f, 0.34f));
            public readonly Material DryTree = PaletteMat("Obby_VY_PaletteDryTree", new Color(0.95f, 1.0f, 0.55f));
            public readonly Material Pillar = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_Sandstone", new Color(0.85f, 0.50f, 0.30f));
            public readonly Material Tower = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_TowerRock", new Color(0.72f, 0.42f, 0.26f));
            public readonly Material FanBlade = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_FanBlade", new Color(0.93f, 0.95f, 1.00f));
            public readonly Material FanHub = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_FanHub", new Color(0.45f, 0.30f, 0.22f));
            public readonly Material Plank = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_Plank", new Color(0.78f, 0.56f, 0.34f));
            public readonly Material Rope = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_Rope", new Color(0.92f, 0.86f, 0.68f));
            public readonly Material VentDark = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_VentGrate", new Color(0.16f, 0.14f, 0.18f));
            public readonly Material VentRim = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_VentRim", new Color(0.95f, 0.90f, 0.80f));
            public readonly Material Air = AirMat("Obby_VY_AirColumn", new Color(0.85f, 0.95f, 1.00f, 0.22f));
            public readonly Material Streak = AirMat("Obby_VY_WindStreak", new Color(1f, 1f, 1f, 0.75f));
            public readonly Material Shard = GlowMat("Obby_VY_Shard", new Color(0.62f, 0.22f, 1.00f), 2.2f);
            // Level 1 glide canyon.
            public readonly Material CanyonWall = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_CanyonWall", new Color(0.82f, 0.48f, 0.29f));
            public readonly Material CanyonBand = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_CanyonBand", new Color(0.62f, 0.35f, 0.23f));
            public readonly Material ArchRock = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_ArchRock", new Color(0.70f, 0.40f, 0.25f));
            public readonly Material RiverBed = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_RiverBed", new Color(0.42f, 0.30f, 0.22f));
            public readonly Material River = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_River", new Color(0.30f, 0.62f, 0.72f));
            public readonly Material HawkBrown = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_HawkBrown", new Color(0.30f, 0.19f, 0.12f));
            public readonly Material HawkWhite = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_HawkWhite", new Color(0.97f, 0.95f, 0.90f));
            public readonly Material HawkBeak = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_HawkBeak", new Color(0.98f, 0.76f, 0.22f));
            public readonly Material GliderLeaf = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_GliderLeaf", new Color(0.90f, 0.97f, 0.74f));
            public readonly Material GliderFrame = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_VY_GliderFrame", new Color(0.66f, 0.50f, 0.32f));
        }

        private static void PrepareValley(Context c, ValleyMats m)
        {
            c.Result.HazardTopY = -60f;
            c.PadMaterial = m.PadRock;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = ValleyFogColor;
            RenderSettings.fogDensity = ValleyFogDensity;
        }

        // ------------------------------------------------------------------ level 1: Canyon glide
        // Glide (GlideZone defaults): forward 8 m/s (7 climbing, 9.5 diving), A/D 6 m/s, sink 1.1, W +2.6, S -5,
        // ceiling 2.5 m above each launch edge. Heights below are feet heights.

        private static void BuildValley1(Context c)
        {
            var m = new ValleyMats();
            PrepareValley(c, m);
            StartIsland(c, 6f, 6f, GlideFloorY);
            GameObject glider = GliderVisual(c, m);
            CanyonWalls(c, m, Valley1Canyon);
            CanyonFloor(c, m, Valley1Canyon);

            // Leg 1 (launch top 0, ceiling 2.5): a tilted loop patrol, a pinch under a stone arch (underside 0.8:
            // dive to feet <= -0.4), then a figure-8 pair at two heights before the first ledge (top -2: climb back).
            GlideLaunch(c, m, "Launch_1", 0f, 0f, 3f, glider);
            AddHawk(c, m, "Hawk_1_Loop", new Vector3(2.5f, 0f, 21f), HawkPattern.Loop, new Vector3(4.5f, 1.5f, 2.5f), 4.2f, 0f);
            // Extra hawks (user: more to dodge): a sweep right after launch.
            AddHawkInCanyon(c, m, "Hawk_1_SweepEarly", 12f, 0f, -1.4f, HawkPattern.Sweep, new Vector3(4.0f, 0.4f, 0.8f), 2.8f, 0.4f);
            StoneArch(c, m, Valley1Canyon, "Arch_1", 31.5f, 3f, 0.8f, 9f);
            AddHawk(c, m, "Hawk_1_PairLow", new Vector3(-0.35f, -3.2f, 40.5f), HawkPattern.FigureEight, new Vector3(3.0f, 0.5f, 2.0f), 4.4f, 0f);
            AddHawk(c, m, "Hawk_1_PairHigh", new Vector3(-0.35f, 0.6f, 40.5f), HawkPattern.FigureEight, new Vector3(3.0f, 0.5f, 2.0f), 4.4f, 2.2f);
            GlideLedge(c, m, Valley1Canyon, "Checkpoint_1", -2f, -2f, 54f, -2f + GlideCeilingAboveLaunch, false); // apron z 47 - 51, pad 51 - 57

            // Leg 2 (launch top -2, ceiling 0.5): a hawk diving across the path, the Shard alcove on the left
            // (z 78 - 93, Shard low at y -7 behind a circling hawk; climb back by gliding up), then a pinch with a
            // rock overhang from the right: thread the 2 m slot on the left, or dive under it (feet <= -2.6).
            float ceiling2 = -2f + GlideCeilingAboveLaunch;
            GlideLaunch(c, m, "Launch_2", -2f, -2f, 57f, glider);
            AddHawkInCanyon(c, m, "Hawk_2_SweepEarly", 63f, 0f, -3.4f, HawkPattern.Sweep, new Vector3(4.0f, 0.4f, 0.8f), 2.6f, 1.1f);
            AddHawk(c, m, "Hawk_2_Dive", new Vector3(1.0f, -2.0f, 70f), HawkPattern.Dive, new Vector3(4.5f, 2.3f, -5f), 3.4f, 0f, 0.45f, 6f);
            SampleCanyon(Valley1Canyon, 84f, out float alcoveX, out _, out _);
            AddHawk(c, m, "Hawk_2_AlcoveGuard", new Vector3(alcoveX - 6.2f, -6.6f, 84f), HawkPattern.Loop, new Vector3(1.5f, 0f, 3.2f), 3.0f, 0.6f);
            AddShard(c, c.MapId, new Vector3(alcoveX - 8.5f, -7.0f, 84f), m.Shard, 0.4f, 0.8f);
            // No updraft here (play-test): climb back from the alcove with W / hold Jump while gliding.
            SampleCanyon(Valley1Canyon, 108.25f, out float shelfX, out _, out float shelfRight);
            AddHawkInCanyon(c, m, "Hawk_2_EightMid", 99f, 0.3f, -2.8f, HawkPattern.FigureEight, new Vector3(3.2f, 0.6f, 2.0f), 4.0f, 0.9f);
            RockSlab(c, m, "Overhang_2", shelfX - 1.4f, shelfX + shelfRight + 1f, 108.25f, 2.5f, -1.0f, 8f);
            AddHawkInCanyon(c, m, "Hawk_2_SweepLate", 115f, 0f, -3.2f, HawkPattern.Sweep, new Vector3(3.8f, 0.3f, 0.8f), 2.6f, 0.2f);
            GlideLedge(c, m, Valley1Canyon, "Checkpoint_2", -1f, -1f, 125f, ceiling2, false); // apron z 118 - 122, pad 122 - 128

            // Leg 3 (launch top -1, ceiling 1.5): a stacked pair sweeping across in opposite phase (pick a height),
            // a tall rock spire mid-canyon and a low one by the right wall, a pinch under an arch (underside 0:
            // feet <= -1.2), a last diving hawk, then the finish ledge (top 0).
            float ceiling3 = -1f + GlideCeilingAboveLaunch;
            GlideLaunch(c, m, "Launch_3", -1f, -1f, 128f, glider);
            AddHawk(c, m, "Hawk_3_SweepLow", new Vector3(0.6f, -3.4f, 137f), HawkPattern.Sweep, new Vector3(4.2f, 0.3f, 0.8f), 3.0f, 0f);
            AddHawk(c, m, "Hawk_3_SweepHigh", new Vector3(0.6f, 0.4f, 137f), HawkPattern.Sweep, new Vector3(4.2f, 0.3f, 0.8f), 3.0f, 1.5f);
            AddHawkInCanyon(c, m, "Hawk_3_LoopMid", 132f, -0.5f, -2.2f, HawkPattern.Loop, new Vector3(3.6f, 1.2f, 2.0f), 3.8f, 1.3f);
            RockSpire(c, m, "Spire_3a", 2.0f, 144f, 1.0f, 6f);
            RockSpire(c, m, "Spire_3b", 4.6f, 147.5f, 1.0f, -2.5f);
            StoneArch(c, m, Valley1Canyon, "Arch_3", 152.5f, 3f, 0.0f, 9f);
            AddHawk(c, m, "Hawk_3_Dive", new Vector3(0f, -1.2f, 162f), HawkPattern.Dive, new Vector3(4.0f, 2.0f, -4.5f), 3.2f, 0.7f, 0.45f, 6f);
            GlideLedge(c, m, Valley1Canyon, "FinishPad", 0f, 0f, 174f, ceiling3, true); // apron z 167 - 171, finish 171 - 177

            ValleyScenery(c, m, -20f, 180f, 4001, false);
            int hidden = CullSceneryBehindCanyonWalls(c);
            if (hidden > 0) Debug.Log("[MapLevelBuilder] valley 1: removed " + hidden + " scenery piece(s) beside the canyon (out of sight while gliding).");
        }

        /// <summary>
        /// The glide runs inside the canyon and its walls block the view out, so no dressing beside the canyon is ever
        /// seen. Removes every scenery piece along the canyon (its centre within the walls' Z range, or up to
        /// <see cref="BehindStartReach"/> before it) that lies outside
        /// the walls: its face nearest the centre line is past the inner face of a wall on its own side at that Z.
        /// Saves draw calls and scene size. Returns the number removed.
        /// </summary>
        private static int CullSceneryBehindCanyonWalls(Context c)
        {
            Transform group = c.Course.Find("CanyonWalls");
            if (group == null || c.Scenery == null) return 0;
            var walls = new List<Bounds>();
            foreach (Transform w in group)
            {
                if (!(w.name.StartsWith("Wall_L_") || w.name.StartsWith("Wall_R_")) || w.name.EndsWith("_Band")) continue;
                if (w.TryGetComponent(out Renderer wr)) walls.Add(wr.bounds);
            }
            if (walls.Count == 0) return 0;
            float zMin = float.MaxValue, zMax = float.MinValue;
            foreach (Bounds w in walls) { zMin = Mathf.Min(zMin, w.min.z); zMax = Mathf.Max(zMax, w.max.z); }

            var doomed = new List<GameObject>();
            foreach (Transform piece in c.Scenery)
            {
                Renderer[] rs = piece.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) continue;
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                // Also the big mesas behind the start pad, which the canyon's mouth hides the same way.
                if (b.center.z < zMin - BehindStartReach || b.center.z > zMax) continue;
                if (OutsideWalls(b, walls, Mathf.Clamp(b.center.z, zMin, zMax))) doomed.Add(piece.gameObject);
            }
            foreach (GameObject go in doomed) Object.DestroyImmediate(go);
            return doomed.Count;
        }

        private const float BehindStartReach = 30f; // metres before the canyon's first wall still culled

        private static bool OutsideWalls(Bounds o, List<Bounds> walls, float atZ)
        {
            bool left = o.center.x < 0f;
            float near = left ? -o.max.x : o.min.x; // the piece's face nearest the canyon's centre line
            float innerAtZ = float.MaxValue;
            foreach (Bounds w in walls)
            {
                if ((w.center.x < 0f) != left || atZ < w.min.z - 1f || atZ > w.max.z + 1f) continue;
                innerAtZ = Mathf.Min(innerAtZ, left ? -w.max.x : w.min.x);
            }
            return innerAtZ < float.MaxValue && near >= innerAtZ + 0.3f;
        }

        // ------------------------------------------------------------------ level 2: Fans and wind vents

        private static void BuildValley2(Context c)
        {
            var m = new ValleyMats();
            PrepareValley(c, m);
            StartIsland(c, 6f, 6f, ValleyBottomY);

            // A "Fan alley": a 1.8 m ledge with two counter-rotating fans 4.8 m apart, a sliding raft with a flat
            // sweeper fan on its deck (jump its bar while the raft slides), then a 2 m ledge with two more fans
            // only 3.2 m apart.
            Slab(c, "Ledge_A", 0f, 0.0f, 9.6f, 1.8f, 11.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);          // z 4.1 - 15.1, gap 1.1
            GiantFan(c, m, "Fan_1", 0f, 0.0f, 7.2f, 4.2f, 2, 42f, 0f, 1f);
            GiantFan(c, m, "Fan_2", 0f, 0.0f, 12.0f, 4.2f, 2, -50f, 60f, -1f);
            ValleySweeperRaft(c, m, "Raft_A", 0f, 0.3f, 18.8f, 3.2f, new Vector3(1.4f, 0f, 0f), 4.4f, 0f, 1.5f, 2, 100f); // z 17.6 - 20.0, gap 2.5
            Slab(c, "Ledge_C", 0f, 0.8f, 26.0f, 2.0f, 7.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);          // z 22.5 - 29.5, gap 2.5, +0.5
            GiantFan(c, m, "Fan_3", 0f, 0.8f, 24.4f, 4.0f, 2, 58f, 30f, 1f);
            GiantFan(c, m, "Fan_4", 0f, 0.8f, 27.6f, 3.6f, 2, -48f, 0f, -1f);
            AddCheckpoint(c, "Checkpoint_1", 0f, 1.0f, 33.6f, ValleyCheckpointSize, m.Pillar, ValleyBottomY);         // gap 2.1

            // B "Vent chain": vent 1 lifts you (hover at its top), drift across a fan whose lowest blades sweep the
            // air between the vents into vent 2's column, then vent 2 drops you onto a 1.8 m ledge right into the
            // sweep of fan 6 (time the exit) and past fan 7.
            Slab(c, "Ledge_V1", 0f, 1.0f, 39.4f, 2.4f, 4.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);         // z 37.4 - 41.4, gap 1.8
            Vent(c, m, "Vent_1", 0f, 1.0f, 40.2f, 3.8f);                                                             // column z 39.1 - 41.3, top 4.8
            GiantFan(c, m, "Fan_5", 0f, 4.2f, 43.0f, 3.0f, 2, 38f, 0f, 1f);                                           // air fan: hub y 7.4, sweeps y 4.4 - 10.4
            Slab(c, "VentPad_2", 0f, 4.2f, 45.6f, 2.0f, 2.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);        // z 44.6 - 46.6
            Vent(c, m, "Vent_2", 0f, 4.2f, 45.6f, 3.4f);                                                             // column z 44.5 - 46.7, top 7.6
            Slab(c, "Ledge_D", 0f, 7.2f, 52.4f, 1.8f, 7.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);          // z 48.9 - 55.9, 2.2 from the column
            GiantFan(c, m, "Fan_6", 0f, 7.2f, 49.8f, 4.0f, 2, 47f, 0f, -1f);
            GiantFan(c, m, "Fan_7", 0f, 7.2f, 53.8f, 4.0f, 2, -54f, 50f, 1f);
            AddCheckpoint(c, "Checkpoint_2", 0f, 7.4f, 59.8f, ValleyCheckpointSize, m.Pillar, ValleyBottomY);         // gap 1.9

            // C "Broken bridges": two long (9.6 m) suspension bridges with an irregular swing (amplitude and tempo
            // drift, so you read each swing instead of counting beats) and a static rock between them, decks 2.2 m
            // wide with 1.2 m holes, under a +X gust. Every swing reaches its current amplitude at both ends, so a
            // boarding hop (2.3 m or less at the smallest swing, 0.7 m at the widest) comes once per swing (3 - 6 s).
            // Positions follow from the geometry; values in comments are for the tuning above.
            const float bridgeTop = 7.6f;                       // deck top at the widest swing (= the rock / landing tops)
            const string bridge1Planks = "XXXX..XXX..XXXXX";    // 16 planks, two 1.2 m holes
            const string bridge2Planks = "XXX..XX..XX..XXX";    // 16 planks, three 1.2 m holes
            float cp2FarEdge = 59.8f + ValleyCheckpointSize * 0.5f;                                            // 61.8
            float deck1 = bridge1Planks.Length * ValleyPlankLength;                                            // 9.6
            float reach1 = Valley2BridgeRope * Mathf.Sin(Valley2Bridge1Swing.MaxAmplitude * Mathf.Deg2Rad);   // 3.71
            float bridge1Z = cp2FarEdge + Valley2BridgeClearance + deck1 * 0.5f + reach1;                      // 71.0
            float midNear = bridge1Z + reach1 + deck1 * 0.5f + Valley2BridgeClearance;                         // 80.2
            float deck2 = bridge2Planks.Length * ValleyPlankLength;
            float reach2 = Valley2BridgeRope * Mathf.Sin(Valley2Bridge2Swing.MaxAmplitude * Mathf.Deg2Rad);
            float bridge2Z = midNear + Valley2MidRockSize + Valley2BridgeClearance + deck2 * 0.5f + reach2;    // 92.0
            float landNear = bridge2Z + reach2 + deck2 * 0.5f + Valley2BridgeClearance;                        // 101.2
            float landZ = landNear + 1.5f;                                                                     // 102.7 (3 x 3 landing)

            SwingPairBridge(c, m, "Bridge_1", 0f, cp2FarEdge, midNear, bridgeTop, Valley2BridgeRope, Valley2Bridge1Swing.Period, 0f, Valley2BridgeDeckWidth);
            Slab(c, "Bridge_MidRock", 0f, bridgeTop, midNear + Valley2MidRockSize * 0.5f, 2.6f, Valley2MidRockSize,
                c.In.PlatformMaterial, m.Pillar, ValleyBottomY);                                                // z 80.2 - 82.8
            SwingPairBridge(c, m, "Bridge_2", 0f, midNear + Valley2MidRockSize, landNear, bridgeTop, Valley2BridgeRope, Valley2Bridge2Swing.Period, 1.3f, Valley2BridgeDeckWidth);
            float windStart = cp2FarEdge + 1f, windEnd = landNear - 1.2f;                                      // z 62.8 - 100.0
            WindZone(c, m, "Wind_Bridges", new Vector3(0f, 9.0f, (windStart + windEnd) * 0.5f), new Vector3(12f, 8f, windEnd - windStart),
                new Vector3(Valley2BridgeGustSpeed, 0f, 0f), 1.0f, 2.2f, 0.4f, 3011);
            Slab(c, "Landing_L", 0f, 7.6f, landZ, 3.0f, 3.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);
            Vent(c, m, "Vent_3", 0f, 7.6f, landZ, 3.6f);                                                     // column top 11.2

            // Finish: deck (2.2 m past the vent column), step and weather dais; nothing stands beyond it, so the
            // suction has open air around it.
            AddFinish(c, 0f, 10.6f, landZ + Valley2FinishAfterLanding, 6f, 9f, m.Pillar, ValleyBottomY, weatherDais: true); // z 106 - 115

            // Secret branch off checkpoint 2 toward -X: a crumbling rock, a rock sliding along Z (across the jump)
            // and a bobbing rock, all small, 3.4 m gaps. Hidden Shard at the end.
            // (play-test: the Shard was too hard to find) two still rocks with short hops, in plain view of checkpoint 2.
            ValleyMovingRock(c, m, "Secret_1", -5.0f, 7.6f, 59.8f, 2.4f, Vector3.zero, 1f, 0f, 0f);   // gap 1.8
            ValleyMovingRock(c, m, "Secret_2", -8.8f, 7.8f, 59.8f, 2.4f, Vector3.zero, 1f, 0f, 0f);   // gap 1.4
            AddShard(c, c.MapId, new Vector3(-8.8f, 9.0f, 59.8f), m.Shard, 0.4f, 0.8f);

            ValleyScenery(c, m, -20f, 130f, 4002);
        }

        // ------------------------------------------------------------------ obstacle builders

        /// <summary>
        /// A floating rock platform that bobs up and down and turns slowly around Y (see
        /// <see cref="ValleyMovingRock"/>). A zero <paramref name="bobAmplitude"/> gives a rock that only spins.
        /// </summary>
        private static GameObject FloatingRock(Context c, ValleyMats m, string name, float x, float topY, float z, float size,
            float bobAmplitude, float bobPeriod, float bobPhase, float spinDegreesPerSecond)
        {
            return ValleyMovingRock(c, m, name, x, topY, z, size, new Vector3(0f, bobAmplitude, 0f), bobPeriod, bobPhase, spinDegreesPerSecond);
        }

        /// <summary>
        /// A floating rock platform that moves back and forth (sine wave, <paramref name="travel"/> = peak offset
        /// from its home position, any direction: a bob, a sideways slider, a ferry along Z, a lift) and may turn
        /// around Y. The player is carried (RideableSurface), rotation included.
        /// Stand-in model: KayKit hill + inverted rock spire (no floating-island model; see report).
        /// </summary>
        private static GameObject ValleyMovingRock(Context c, ValleyMats m, string name, float x, float topY, float z, float size,
            Vector3 travel, float period, float phase, float spinDegreesPerSecond)
        {
            GameObject root = ValleyRockBody(c, m, name, x, topY, z, size, m.PadRock);
            root.AddComponent<RideableSurface>();
            if (travel.sqrMagnitude > 0.000001f)
            {
                var mover = root.AddComponent<PlatformMover>();
                SetFields(mover, ("travel", travel), ("periodSeconds", period), ("phaseSeconds", phase));
            }
            if (!Mathf.Approximately(spinDegreesPerSecond, 0f))
            {
                var spinner = root.AddComponent<Spinner>();
                SetFields(spinner, ("axis", Vector3.up), ("degreesPerSecond", spinDegreesPerSecond), ("phaseDegrees", phase * 40f));
            }
            return root;
        }

        /// <summary>
        /// A crumbling rock: a static floating rock in darker, redder sandstone that shakes once stepped on and
        /// falls away <paramref name="standSeconds"/> later (whether or not you are still on it), then rises back
        /// (<see cref="CollapsingPlatform"/>).
        /// </summary>
        private static GameObject ValleyCrumbleRock(Context c, ValleyMats m, string name, float x, float topY, float z, float size, float standSeconds)
        {
            GameObject root = ValleyRockBody(c, m, name, x, topY, z, size, m.CrumbleRock);
            root.AddComponent<RideableSurface>();
            var platform = root.AddComponent<CollapsingPlatform>();
            SetFields(platform,
                ("standSeconds", standSeconds),
                ("requireContinuousStand", false),
                ("visual", root.transform.Find("Visual")),
                ("shakeAmplitude", ValleyCrumbleShake),
                ("fallDistance", ValleyCrumbleFallDistance),
                ("tiltDegrees", ValleyCrumbleTiltDegrees),
                ("goneSeconds", ValleyCrumbleGoneSeconds),
                ("returnSeconds", ValleyCrumbleReturnSeconds));
            return root;
        }

        /// <summary>
        /// The body shared by all floating rocks: a root at the walkable top centre with a box collider
        /// (<see cref="FloatingRockWalkFraction"/> of the footprint) and a "Visual" child holding a hill block
        /// and an inverted rock spire, both in <paramref name="material"/>.
        /// </summary>
        private static GameObject ValleyRockBody(Context c, ValleyMats m, string name, float x, float topY, float z, float size, Material material)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(size * FloatingRockWalkFraction, 0.5f, size * FloatingRockWalkFraction);
            box.center = new Vector3(0f, -0.25f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            // Meshy floating island (grassy top, rock point below) for the plain rocks; crumbling rocks keep their
            // redder tint so they still read as the ones that fall.
            MeshyPropBaker.PropMesh islandModel = material == m.PadRock ? MeshyPropBaker.BakeProp("Valley/Island", "FloatingIsland", MeshyPropBaker.ZUp) : null;
            if (islandModel != null)
            {
                float height = Mathf.Max(FloatingRockThickness * 1.6f, size * 0.9f);
                MeshyPropBaker.PlaceProp(islandModel, "Island", visual.transform, Vector3.down * height, new Vector3(size, height, size));
                return root;
            }
            GameObject island = GrassIsland(c, name + "_Island", x, topY, z, size, size, topY - FloatingRockThickness, material, visual.transform);
            if (island == null)
            {
                Prim(PrimitiveType.Sphere, "Rock", visual.transform, new Vector3(0f, -FloatingRockThickness * 0.5f, 0f),
                    new Vector3(size, FloatingRockThickness, size), m.Pillar);
            }
            PlaceModel("Rock_4_F", new Vector3(x, topY - FloatingRockThickness + 0.1f, z), new Vector3(180f, 0f, 0f),
                Vector3.one * (size * 0.28f), material, visual.transform);
            return root;
        }

        /// <summary>
        /// A sliding raft with a flat sweeper fan on its deck: a floating rock that moves by
        /// <paramref name="travel"/> (sine), carrying a rotor that spins around Y with <paramref name="blades"/>
        /// low white blades (kill triggers, 0.2 - 0.5 m above the deck) out to <paramref name="sweepRadius"/>.
        /// Jump the bar as it comes round. Stand-in model: cubes and a cylinder post (no fan model; see report).
        /// </summary>
        /// <summary>The Meshy single fan blade (length along model X, flat along Y), or null to use the stand-in paddles.</summary>
        private static MeshyPropBaker.PropMesh FanBladeModel()
        {
            return MeshyPropBaker.BakeProp("Valley/Fan", "FanPaddle", MeshyPropBaker.ZUp);
        }

        private static GameObject ValleySweeperRaft(Context c, ValleyMats m, string name, float x, float topY, float z, float size,
            Vector3 travel, float period, float phase, float sweepRadius, int blades, float degreesPerSecond)
        {
            GameObject raft = ValleyMovingRock(c, m, name, x, topY, z, size, travel, period, phase, 0f);

            var rotor = new GameObject(name + "_Sweeper");
            rotor.transform.SetParent(raft.transform, false);
            rotor.transform.localPosition = Vector3.zero;
            Prim(PrimitiveType.Cylinder, "Post", rotor.transform, new Vector3(0f, 0.45f, 0f),
                new Vector3(ValleySweeperHubRadius * 2f, 0.45f, ValleySweeperHubRadius * 2f), m.FanHub);

            float span = sweepRadius - ValleySweeperHubRadius;
            float bladeY = ValleySweeperBladeBottom + ValleySweeperBladeThickness * 0.5f;
            var mid = new Vector3(ValleySweeperHubRadius + span * 0.5f, bladeY, 0f);
            MeshyPropBaker.PropMesh bladeModel = FanBladeModel();
            for (int i = 0; i < blades; i++)
            {
                var blade = new GameObject("Blade_" + (i + 1));
                blade.transform.SetParent(rotor.transform, false);
                blade.transform.localRotation = Quaternion.AngleAxis(360f / blades * i, Vector3.up);
                // Meshy blade: its length (model X) along the arm, flat (model Y) up, width (model Z) across.
                if (bladeModel != null)
                    MeshyPropBaker.PlaceProp(bladeModel, "Paddle", blade.transform, mid + Vector3.down * (ValleySweeperBladeThickness * 0.5f),
                        new Vector3(span, ValleySweeperBladeThickness, ValleySweeperBladeWidth));
                else
                    Prim(PrimitiveType.Cube, "Paddle", blade.transform, mid,
                        new Vector3(span, ValleySweeperBladeThickness, ValleySweeperBladeWidth), m.FanBlade);

                var hit = blade.AddComponent<BoxCollider>();
                hit.isTrigger = true;
                hit.size = new Vector3(span, ValleySweeperBladeThickness, ValleySweeperBladeWidth);
                hit.center = mid;
                c.Result.Hazards.Add(blade.AddComponent<KillVolume>());
            }

            var spinner = rotor.AddComponent<Spinner>();
            SetFields(spinner, ("axis", Vector3.up), ("degreesPerSecond", degreesPerSecond), ("phaseDegrees", 0f));
            return raft;
        }

        /// <summary>
        /// A wind zone: a trigger box that pushes the player sideways while it blows (on/off rhythm) and shows
        /// white streaks only while it blows.
        /// </summary>
        private static void WindZone(Context c, ValleyMats m, string name, Vector3 center, Vector3 size, Vector3 wind,
            float onSeconds, float offSeconds, float phaseSeconds, int seed)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = center;
            var zone = root.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = size;

            var streakRoot = new GameObject("Streaks");
            streakRoot.transform.SetParent(root.transform, false);
            var rng = new System.Random(seed);
            for (int i = 0; i < 14; i++)
            {
                var position = new Vector3(0f, ((float)rng.NextDouble() - 0.5f) * size.y * 0.8f, ((float)rng.NextDouble() - 0.5f) * size.z * 0.9f);
                Prim(PrimitiveType.Cylinder, "Streak", streakRoot.transform, position, new Vector3(0.05f, 0.9f, 0.05f), m.Streak, new Vector3(0f, 0f, 90f));
            }
            var drift = streakRoot.AddComponent<StreakDrift>();
            SetFields(drift, ("localDirection", new Vector3(Mathf.Sign(wind.x), 0f, 0f)), ("speed", 14f), ("loopLength", size.x));

            var gust = root.AddComponent<WindGust>();
            SetFields(gust, ("windVelocity", wind), ("onSeconds", onSeconds), ("offSeconds", offSeconds),
                ("phaseSeconds", phaseSeconds), ("streaks", drift));
        }

        /// <summary>
        /// A giant fan standing across a narrow ledge: blades turn in the XY plane at the ledge's z, hub high
        /// enough that the lowest blade tip nearly touches the ledge, so a blade passing the bottom blocks the way.
        /// <paramref name="ledgeTopY"/> may also be a height in mid-air (a fan sweeping the air between vents).
        /// Each blade is a kill trigger. Stand-in model: ellipsoid paddles + cylinders (no fan model; see report).
        /// </summary>
        private static void GiantFan(Context c, ValleyMats m, string name, float x, float ledgeTopY, float z, float bladeLength,
            int blades, float degreesPerSecond, float phaseDegrees, float towerSide)
        {
            float hubY = ledgeTopY + 0.2f + bladeLength;
            var rotor = new GameObject(name + "_Rotor");
            rotor.transform.SetParent(c.Course, false);
            rotor.transform.position = new Vector3(x, hubY, z);

            Prim(PrimitiveType.Cylinder, "Hub", rotor.transform, Vector3.zero, new Vector3(1.1f, 0.25f, 1.1f), m.FanHub, new Vector3(90f, 0f, 0f));
            const float hubRadius = 0.6f;
            float bladeSpan = bladeLength - hubRadius;
            for (int i = 0; i < blades; i++)
            {
                var blade = new GameObject("Blade_" + (i + 1));
                blade.transform.SetParent(rotor.transform, false);
                blade.transform.localRotation = Quaternion.AngleAxis(360f / blades * i, Vector3.forward);
                Vector3 mid = new Vector3(0f, -(hubRadius + bladeSpan * 0.5f), 0f);
                // Meshy blade turned so its length (model X) runs down the arm, its width (model Z) across and
                // its flat side (model Y) faces the camera axis Z; else an ellipsoid paddle.
                MeshyPropBaker.PropMesh bladeModel = FanBladeModel();
                if (bladeModel != null)
                    MeshyPropBaker.PlaceProp(bladeModel, "Paddle", blade.transform, mid + Vector3.back * 0.09f,
                        new Vector3(bladeSpan, 0.18f, 0.9f), Quaternion.LookRotation(Vector3.right, Vector3.forward).eulerAngles);
                else
                    Prim(PrimitiveType.Sphere, "Paddle", blade.transform, mid, new Vector3(0.9f, bladeSpan, 0.18f), m.FanBlade);

                var hit = blade.AddComponent<BoxCollider>();
                hit.isTrigger = true;
                hit.size = new Vector3(0.8f, bladeSpan, 0.5f);
                hit.center = mid;
                c.Result.Hazards.Add(blade.AddComponent<KillVolume>());
            }

            var spinner = rotor.AddComponent<Spinner>();
            SetFields(spinner, ("axis", Vector3.forward), ("degreesPerSecond", degreesPerSecond), ("phaseDegrees", phaseDegrees));

            // Support: a sandstone tower beside the ledge (clear of the blade tips), a strut behind the blade
            // plane and a short axle to the hub. Visual only.
            float towerOffset = bladeLength + ValleyFanTowerClearance;
            float towerX = x + towerSide * towerOffset;
            float towerTop = hubY + 1.0f;
            float towerHeight = towerTop - ValleyBottomY;
            Prim(PrimitiveType.Cylinder, name + "_Tower", c.Scenery, new Vector3(towerX, ValleyBottomY + towerHeight * 0.5f, z + 0.8f),
                new Vector3(1.8f, towerHeight * 0.5f, 1.8f), m.Tower);
            Prim(PrimitiveType.Cylinder, name + "_Strut", c.Scenery, new Vector3(x + towerSide * towerOffset * 0.5f, hubY, z + 0.8f),
                new Vector3(0.45f, towerOffset * 0.5f, 0.45f), m.Tower, new Vector3(0f, 0f, 90f));
            Prim(PrimitiveType.Cylinder, name + "_Axle", c.Scenery, new Vector3(x, hubY, z + 0.4f),
                new Vector3(0.3f, 0.45f, 0.3f), m.FanHub, new Vector3(90f, 0f, 0f));
        }

        /// <summary>
        /// A wind vent: a dark grate flush with the ground, a translucent air column and upward streaks,
        /// with an <see cref="UpdraftVent"/> trigger over it.
        /// </summary>
        private static void Vent(Context c, ValleyMats m, string name, float x, float floorY, float z, float columnHeight)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, floorY, z);

            var zone = root.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(VentColumnWidth, columnHeight, VentColumnWidth);
            zone.center = new Vector3(0f, columnHeight * 0.5f, 0f);

            Prim(PrimitiveType.Cylinder, "Rim", root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(2.1f, 0.03f, 2.1f), m.VentRim);
            Prim(PrimitiveType.Cylinder, "Grate", root.transform, new Vector3(0f, 0.06f, 0f), new Vector3(1.7f, 0.03f, 1.7f), m.VentDark);
            Prim(PrimitiveType.Cylinder, "AirColumn", root.transform, new Vector3(0f, columnHeight * 0.5f, 0f),
                new Vector3(VentColumnWidth * 0.9f, columnHeight * 0.5f, VentColumnWidth * 0.9f), m.Air);

            var streakRoot = new GameObject("Streaks");
            streakRoot.transform.SetParent(root.transform, false);
            var rng = new System.Random(5000 + (int)(z * 10f));
            for (int i = 0; i < 8; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = 0.2f + (float)rng.NextDouble() * 0.7f;
                Prim(PrimitiveType.Cylinder, "Streak", streakRoot.transform,
                    new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius), new Vector3(0.05f, 0.5f, 0.05f), m.Streak);
            }
            var drift = streakRoot.AddComponent<StreakDrift>();
            SetFields(drift, ("localDirection", Vector3.up), ("speed", 6f), ("loopLength", columnHeight));
            streakRoot.transform.localPosition = new Vector3(0f, columnHeight * 0.5f, 0f);

            root.AddComponent<UpdraftVent>(); // default lift (55 m/s^2, top speed 8.5 m/s)
        }

        /// <summary>
        /// A bridge zone from <paramref name="zStart"/> to <paramref name="zEnd"/> (the fixed edges either side) made of
        /// two deck sections on their own ropes, swinging along Z in opposite phase with the same steady amplitude
        /// (play-test: two per zone, swinging and meeting, never overlapping). At one end of the swing the first section
        /// is a short hop from the start edge and the second from the end edge; at the other end the two sections meet,
        /// <see cref="Valley2PairMeetGap"/> apart, so you walk from one onto the other. They never overlap: same period
        /// and amplitude, exactly half a swing apart. <paramref name="extremeTopY"/> is the deck height at the widest swing.
        /// </summary>
        private static void SwingPairBridge(Context c, ValleyMats m, string name, float x, float zStart, float zEnd, float extremeTopY,
            float rope, float period, float phase, float deckWidth)
        {
            float reach = rope * Mathf.Sin(Valley2PairSwingDegrees * Mathf.Deg2Rad);
            float clearance = Valley2BridgeClearance;
            float section = (zEnd - zStart - 2f * clearance - 4f * reach - Valley2PairMeetGap) * 0.5f;
            float restTop = extremeTopY - rope * (1f - Mathf.Cos(Valley2PairSwingDegrees * Mathf.Deg2Rad));
            float half = deckWidth * 0.5f;
            MeshyPropBaker.PropMesh plankModel = MeshyPropBaker.BakeProp("Valley/Plank", "BridgePlank", MeshyPropBaker.ZUp);
            float[] pivots = { zStart + clearance + reach + section * 0.5f, zEnd - clearance - reach - section * 0.5f };
            for (int k = 0; k < 2; k++)
            {
                var pivot = new GameObject(name + "_Pivot_" + (k + 1));
                pivot.transform.SetParent(c.Course, false);
                pivot.transform.position = new Vector3(x, restTop + rope, pivots[k]);
                for (int s = -1; s <= 1; s += 2)
                    Prim(PrimitiveType.Cylinder, "Rope", pivot.transform, new Vector3(s * half, -rope * 0.5f, 0f), new Vector3(0.1f, rope * 0.5f, 0.1f), m.Rope);

                var deck = new GameObject("Deck");
                deck.transform.SetParent(pivot.transform, false);
                deck.transform.localPosition = new Vector3(0f, -rope, 0f);
                var box = deck.AddComponent<BoxCollider>();
                box.size = new Vector3(deckWidth, 0.3f, section);
                box.center = new Vector3(0f, -0.15f, 0f);
                if (plankModel != null)
                    MeshyPropBaker.PlaceProp(plankModel, "Planks", deck.transform, new Vector3(0f, -ValleyPlankModelBoardTop, 0f),
                        new Vector3(deckWidth, ValleyPlankModelHeight, section));
                else
                    Prim(PrimitiveType.Cube, "Planks", deck.transform, new Vector3(0f, -0.06f, 0f), new Vector3(deckWidth - 0.05f, 0.12f, section - 0.06f), m.Plank);
                for (int s = -1; s <= 1; s += 2)
                    Prim(PrimitiveType.Cylinder, "Rail", deck.transform, new Vector3(s * half, 0.7f, 0f), new Vector3(0.07f, section * 0.5f, 0.07f), m.Rope, new Vector3(90f, 0f, 0f));
                deck.AddComponent<RideableSurface>();

                // Steady swing (no amplitude drift, no wobble) so the pair stays exactly opposite.
                var swing = pivot.AddComponent<PendulumSwing>();
                SetFields(swing,
                    ("swingAxis", Vector3.right),
                    ("amplitudeDegrees", Valley2PairSwingDegrees),
                    ("periodSeconds", period),
                    ("phaseSeconds", phase + k * period * 0.5f),
                    ("keepUpright", deck.transform));

                // Towers either side and the beam the ropes hang from (visual only).
                float towerX = half + 1.8f;
                float beamY = restTop + rope + 0.3f;
                float towerHeight = beamY + 0.4f - ValleyBottomY;
                for (int s = -1; s <= 1; s += 2)
                    Prim(PrimitiveType.Cylinder, name + "_Tower_" + (k + 1), c.Scenery, new Vector3(x + s * towerX, ValleyBottomY + towerHeight * 0.5f, pivots[k]),
                        new Vector3(1.6f, towerHeight * 0.5f, 1.6f), m.Tower);
                Prim(PrimitiveType.Cylinder, name + "_Beam_" + (k + 1), c.Scenery, new Vector3(x, beamY, pivots[k]), new Vector3(0.5f, towerX, 0.5f), m.Tower, new Vector3(0f, 0f, 90f));
            }
        }

        // ------------------------------------------------------------------ level 1 glide builders

        /// <summary>Canyon centre X and wall half-widths at <paramref name="z"/>, interpolated linearly between control points.</summary>
        private static void SampleCanyon(CanyonPoint[] path, float z, out float centreX, out float halfLeft, out float halfRight)
        {
            CanyonPoint a = path[0], b = path[path.Length - 1];
            if (z <= a.Z) { centreX = a.X; halfLeft = a.HalfLeft; halfRight = a.HalfRight; return; }
            if (z >= b.Z) { centreX = b.X; halfLeft = b.HalfLeft; halfRight = b.HalfRight; return; }
            for (int i = 1; i < path.Length; i++)
            {
                if (z > path[i].Z) continue;
                a = path[i - 1];
                b = path[i];
                break;
            }
            float t = (z - a.Z) / Mathf.Max(0.0001f, b.Z - a.Z);
            centreX = Mathf.Lerp(a.X, b.X, t);
            halfLeft = Mathf.Lerp(a.HalfLeft, b.HalfLeft, t);
            halfRight = Mathf.Lerp(a.HalfRight, b.HalfRight, t);
        }

        /// <summary>A kill trigger box (world centre and size) registered as a level hazard.</summary>
        private static KillVolume HazardBox(Context c, string name, Vector3 centre, Vector3 size, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : c.Course, false);
            go.transform.position = centre;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(Mathf.Max(0.05f, size.x), Mathf.Max(0.05f, size.y), Mathf.Max(0.05f, size.z));
            KillVolume kill = go.AddComponent<KillVolume>();
            c.Result.Hazards.Add(kill);
            return kill;
        }

        /// <summary>
        /// Both canyon walls, block by block along Z: a sandstone cube with a darker strata band, and a kill
        /// trigger <see cref="GlideKillInset"/> inside the inner face. A back wall closes the canyon behind the start.
        /// The canyon ends at the finish pad's far edge (level 2 continues there in the merged scene).
        /// </summary>
        private static void CanyonWalls(Context c, ValleyMats m, CanyonPoint[] path)
        {
            var group = new GameObject("CanyonWalls").transform;
            group.SetParent(c.Course, false);
            int index = 0;
            for (float z0 = GlideCourseStartZ; z0 < GlideCourseEndZ - 0.01f; z0 += GlideWallSegment, index++)
            {
                float z1 = Mathf.Min(z0 + GlideWallSegment, GlideCourseEndZ);
                SampleCanyon(path, (z0 + z1) * 0.5f, out float cx, out float halfLeft, out float halfRight);
                WallBlock(c, m, group, "Wall_L_" + index, cx - halfLeft - GlideWallThickness, cx - halfLeft, z0, z1, -1f);
                WallBlock(c, m, group, "Wall_R_" + index, cx + halfRight, cx + halfRight + GlideWallThickness, z0, z1, 1f);
            }
            float height = GlideWallTopY - GlideFloorY + 0.5f;
            Prim(PrimitiveType.Cube, "Wall_Back", group, new Vector3(0f, GlideFloorY - 0.5f + height * 0.5f, GlideCourseStartZ - 1.5f),
                new Vector3(24f, height, 3f), m.CanyonWall);
        }

        /// <summary>One wall block from <paramref name="xMin"/> to <paramref name="xMax"/>; <paramref name="side"/> is -1 (left wall) or +1 (right wall).</summary>
        private static void WallBlock(Context c, ValleyMats m, Transform parent, string name, float xMin, float xMax, float z0, float z1, float side)
        {
            float yMin = GlideFloorY - 0.5f;
            var centre = new Vector3((xMin + xMax) * 0.5f, (yMin + GlideWallTopY) * 0.5f, (z0 + z1) * 0.5f);
            var size = new Vector3(xMax - xMin, GlideWallTopY - yMin, z1 - z0);
            Prim(PrimitiveType.Cube, name, parent, centre, size, m.CanyonWall);

            float inner = side < 0f ? xMax : xMin;
            float bandX = inner - side * (GlideWallBandProtrude - GlideWallBandDepth) * 0.5f;
            Prim(PrimitiveType.Cube, name + "_Band", parent, new Vector3(bandX, GlideWallBandY, centre.z),
                new Vector3(GlideWallBandDepth + GlideWallBandProtrude, GlideWallBandHeight, size.z + 0.02f), m.CanyonBand);

            HazardBox(c, name + "_Kill", centre, new Vector3(size.x - 2f * GlideKillInset, size.y, size.z), parent);
        }

        /// <summary>The river bed and river ribbon along the canyon, and the kill slab just above the floor.</summary>
        private static void CanyonFloor(Context c, ValleyMats m, CanyonPoint[] path)
        {
            var group = new GameObject("CanyonFloor").transform;
            group.SetParent(c.Course, false);
            float length = GlideCourseEndZ - GlideCourseStartZ;
            float midZ = (GlideCourseStartZ + GlideCourseEndZ) * 0.5f;
            Prim(PrimitiveType.Cube, "RiverBed", group, new Vector3(0f, GlideFloorY - 0.5f, midZ), new Vector3(50f, 1f, length), m.RiverBed);
            int index = 0;
            for (float z0 = GlideCourseStartZ; z0 < GlideCourseEndZ - 0.01f; z0 += GlideWallSegment, index++)
            {
                float z1 = Mathf.Min(z0 + GlideWallSegment, GlideCourseEndZ);
                SampleCanyon(path, (z0 + z1) * 0.5f, out float cx, out float halfLeft, out float halfRight);
                Prim(PrimitiveType.Cube, "River_" + index, group, new Vector3(cx + (halfRight - halfLeft) * 0.5f, GlideFloorY + 0.03f, (z0 + z1) * 0.5f),
                    new Vector3(GlideRiverWidth, 0.06f, z1 - z0 + 0.05f), m.River);
            }
            float killBottom = GlideFloorY - 6f;
            HazardBox(c, "RiverKill", new Vector3(0f, (GlideFloorKillTop + killBottom) * 0.5f, midZ),
                new Vector3(60f, GlideFloorKillTop - killBottom, length), group);
        }

        /// <summary>
        /// A rock slab hanging in the canyon (arch lintel, overhang): visual cube with a mesa cap on top, and a
        /// kill trigger <see cref="GlideKillInset"/> inside its underside and front / back faces.
        /// </summary>
        private static void RockSlab(Context c, ValleyMats m, string name, float xMin, float xMax, float z, float thickness, float bottomY, float topY)
        {
            var centre = new Vector3((xMin + xMax) * 0.5f, (bottomY + topY) * 0.5f, z);
            var size = new Vector3(xMax - xMin, topY - bottomY, thickness);
            Prim(PrimitiveType.Cube, name, c.Course, centre, size, m.ArchRock);
            HazardBox(c, name + "_Kill", centre, new Vector3(size.x, size.y - 2f * GlideKillInset, size.z - 2f * GlideKillInset));
            GrassIsland(c, name + "_Cap", centre.x, topY + 0.6f, z, size.x, thickness + 0.4f, topY - 0.8f, m.PadRock);
        }

        /// <summary>A stone arch spanning the canyon at <paramref name="z"/> (its ends sink 1 m into the walls); fly under it.</summary>
        private static void StoneArch(Context c, ValleyMats m, CanyonPoint[] path, string name, float z, float thickness, float undersideY, float topY)
        {
            SampleCanyon(path, z, out float cx, out float halfLeft, out float halfRight);
            RockSlab(c, m, name, cx - halfLeft - 1f, cx + halfRight + 1f, z, thickness, undersideY, topY);
        }

        /// <summary>A rock spire rising from the river bed to <paramref name="topY"/> (capsule kill trigger, a small rock tip on top).</summary>
        private static void RockSpire(Context c, ValleyMats m, string name, float x, float z, float radius, float topY)
        {
            float height = topY - GlideFloorY;
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, GlideFloorY + height * 0.5f, z);
            Prim(PrimitiveType.Cylinder, "Column", root.transform, Vector3.zero, new Vector3(radius * 2f, height * 0.5f, radius * 2f), m.ArchRock);
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.direction = 1;
            capsule.radius = Mathf.Max(0.1f, radius - GlideKillInset);
            capsule.height = Mathf.Max(capsule.radius * 2f, height - GlideKillInset);
            c.Result.Hazards.Add(root.AddComponent<KillVolume>());
            PlaceModel("Rock_4_C", new Vector3(x, topY - 0.4f, z), new Vector3(0f, x * 37f, 0f), Vector3.one * (radius * 0.45f), m.PadRock, c.Scenery);
        }

        /// <summary>
        /// A landing ledge: a solid apron then the checkpoint (or the level 1 finish) pad, both
        /// <see cref="GlideLedgeSize"/> wide; a kill trigger in the rock under their 1 m solid tops; and a
        /// <see cref="GlideLandingZone"/> over them, the full canyon width, from the top up past the leg's
        /// ceiling, so every glide that reaches the ledge ends there.
        /// </summary>
        private static void GlideLedge(Context c, ValleyMats m, CanyonPoint[] path, string name, float x, float topY, float padCentreZ,
            float legCeilingY, bool finish)
        {
            float half = GlideLedgeSize * 0.5f;
            float padNear = padCentreZ - half, padFar = padCentreZ + half, apronStart = padNear - GlideApronLength;
            Slab(c, name + "_Apron", x, topY, (apronStart + padNear) * 0.5f, GlideLedgeSize, GlideApronLength, c.In.PlatformMaterial, m.Pillar, GlideFloorY);
            if (finish) AddFinish(c, x, topY, padCentreZ, GlideLedgeSize, GlideLedgeSize, m.Pillar, GlideFloorY);
            else AddCheckpoint(c, name, x, topY, padCentreZ, GlideLedgeSize, m.Pillar, GlideFloorY);

            float bodyTop = topY - 1.05f;
            HazardBox(c, name + "_RockKill", new Vector3(x, (GlideFloorY + bodyTop) * 0.5f, (apronStart + padFar) * 0.5f),
                new Vector3(GlideLedgeSize - 2f * GlideKillInset, bodyTop - GlideFloorY, padFar - apronStart - 2f * GlideKillInset));

            SampleCanyon(path, (apronStart + padFar) * 0.5f, out float cx, out float halfLeft, out float halfRight);
            float bottom = topY + 0.05f, top = legCeilingY + GlideLandingHeadroom;
            var landing = new GameObject(name + "_GlideLanding");
            landing.transform.SetParent(c.Course, false);
            landing.transform.position = new Vector3(cx + (halfRight - halfLeft) * 0.5f, (bottom + top) * 0.5f, (apronStart + padFar) * 0.5f);
            var box = landing.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(halfLeft + halfRight - 2f * GlideKillInset, top - bottom, padFar - apronStart);
            landing.AddComponent<GlideLandingZone>();
        }

        /// <summary>
        /// A launch past a pad's +Z edge at <paramref name="edgeZ"/>: a <see cref="GlideZone"/> trigger in the air
        /// just beyond the edge (ceiling <see cref="GlideCeilingAboveLaunch"/> above the pad top), a pale lip on
        /// the edge and white streaks drifting forward as the cue.
        /// </summary>
        private static void GlideLaunch(Context c, ValleyMats m, string name, float x, float topY, float edgeZ, GameObject glider)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, edgeZ);
            float width = GlideLedgeSize + 1f;
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(width, GlideLaunchBelow + GlideLaunchAbove, GlideLaunchDepth);
            box.center = new Vector3(0f, (GlideLaunchAbove - GlideLaunchBelow) * 0.5f, GlideLaunchGap + GlideLaunchDepth * 0.5f);
            var zone = root.AddComponent<GlideZone>();
            SetFields(zone, ("ceilingHeight", GlideCeilingAboveLaunch), ("gliderVisual", glider.transform));

            Prim(PrimitiveType.Cube, "LaunchLip", root.transform, new Vector3(0f, 0.02f, -0.25f), new Vector3(GlideLedgeSize - 0.4f, 0.04f, 0.4f), m.VentRim);
            var streaks = new GameObject("Streaks");
            streaks.transform.SetParent(root.transform, false);
            streaks.transform.localPosition = new Vector3(0f, 1f, GlideLaunchGap + 5f);
            var rng = new System.Random(7000 + (int)(edgeZ * 10f));
            for (int i = 0; i < 8; i++)
            {
                var local = new Vector3(((float)rng.NextDouble() - 0.5f) * width, ((float)rng.NextDouble() - 0.3f) * 3f, 0f);
                Prim(PrimitiveType.Cylinder, "Streak", streaks.transform, local, new Vector3(0.05f, 0.7f, 0.05f), m.Streak, new Vector3(90f, 0f, 0f));
            }
            var drift = streaks.AddComponent<StreakDrift>();
            SetFields(drift, ("localDirection", Vector3.forward), ("speed", 9f), ("loopLength", 10f));
        }

        /// <summary>
        /// A hawk (original stand-in from primitives: dark brown body and wings, white head, chest and wing tips,
        /// yellow beak), wingspan 2.6 m, flying <paramref name="pattern"/> around <paramref name="centre"/>.
        /// Its kill box (<see cref="HawkHitbox"/>) is smaller than the model, so a near miss is a miss.
        /// </summary>
        /// <summary>A hawk centred on the canyon's middle at <paramref name="z"/> (plus <paramref name="offsetX"/>), at height <paramref name="y"/>.</summary>
        private static Hawk AddHawkInCanyon(Context c, ValleyMats m, string name, float z, float offsetX, float y, HawkPattern pattern, Vector3 radii,
            float period, float phase)
        {
            SampleCanyon(Valley1Canyon, z, out float centreX, out _, out _);
            return AddHawk(c, m, name, new Vector3(centreX + offsetX, y, z), pattern, radii, period, phase);
        }

        private static Hawk AddHawk(Context c, ValleyMats m, string name, Vector3 centre, HawkPattern pattern, Vector3 radii, float period, float phase,
            float diveFraction = 0.45f, float returnLift = 6f)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = centre;
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = HawkHitbox;
            c.Result.Hazards.Add(root.AddComponent<KillVolume>());

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            Transform[] wings = new Transform[2];
            MeshyPropBaker.HawkParts meshy = MeshyHawk();
            if (meshy != null)
            {
                // Meshy hawk: body plus two wing meshes hinged at the shoulders (Hawk flaps them).
                MeshyPropBaker.AddMeshChild("Body", visual, meshy.Body, meshy.Material, Vector3.zero);
                wings[0] = MeshyPropBaker.AddMeshChild("Wing_L", visual, meshy.LeftWing, meshy.Material, meshy.LeftHinge).transform;
                wings[1] = MeshyPropBaker.AddMeshChild("Wing_R", visual, meshy.RightWing, meshy.Material, meshy.RightHinge).transform;
            }
            else
            {
                Prim(PrimitiveType.Sphere, "Body", visual, Vector3.zero, new Vector3(0.42f, 0.36f, 1.0f), m.HawkBrown);
                Prim(PrimitiveType.Sphere, "Chest", visual, new Vector3(0f, -0.06f, 0.18f), new Vector3(0.34f, 0.28f, 0.5f), m.HawkWhite);
                Prim(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 0.08f, 0.55f), new Vector3(0.3f, 0.28f, 0.32f), m.HawkWhite);
                Prim(PrimitiveType.Cube, "Beak", visual, new Vector3(0f, 0.04f, 0.74f), new Vector3(0.08f, 0.08f, 0.16f), m.HawkBeak);
                Prim(PrimitiveType.Cube, "Tail", visual, new Vector3(0f, 0.02f, -0.62f), new Vector3(0.42f, 0.05f, 0.36f), m.HawkBrown);
                Prim(PrimitiveType.Cube, "TailTip", visual, new Vector3(0f, 0.02f, -0.83f), new Vector3(0.44f, 0.05f, 0.08f), m.HawkWhite);

                for (int s = 0; s < 2; s++)
                {
                    float side = s == 0 ? -1f : 1f;
                    var wing = new GameObject(s == 0 ? "Wing_L" : "Wing_R").transform;
                    wing.SetParent(visual, false);
                    wing.localPosition = new Vector3(side * 0.12f, 0.08f, 0.05f);
                    Prim(PrimitiveType.Sphere, "Feathers", wing, new Vector3(side * 0.62f, 0f, 0f), new Vector3(1.25f, 0.07f, 0.6f), m.HawkBrown);
                    Prim(PrimitiveType.Sphere, "Tip", wing, new Vector3(side * 1.15f, 0f, -0.05f), new Vector3(0.4f, 0.06f, 0.42f), m.HawkWhite);
                    wings[s] = wing;
                }
            }

            var hawk = root.AddComponent<Hawk>();
            SetFields(hawk,
                ("pattern", (int)pattern),
                ("radii", radii),
                ("periodSeconds", period),
                ("phaseSeconds", phase),
                ("diveFraction", diveFraction),
                ("returnLift", returnLift),
                ("leftWing", wings[0]),
                ("rightWing", wings[1]));
            return hawk;
        }

        // Glider frame for Sam (metres, glider space: origin between the handle poles at grip height, +Z forward).
        private const float GliderGripHalfWidth = 0.32f;  // handle poles each side of the head, at Sam's reach up and out
        private const float GliderWingAboveGrip = 0.45f;  // wing underside above the grips (clears the top of Sam's head)
        private const float GliderWingAhead = 0.1f;       // wing chord centre ahead of the grips
        private const float GliderPoleRadius = 0.022f;
        private const float GliderBraceRadius = 0.016f;

        /// <summary>A wooden rod (cylinder, no collider) from <paramref name="a"/> to <paramref name="b"/>, local to <paramref name="parent"/>.</summary>
        private static void Strut(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material material)
        {
            Vector3 d = b - a;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, d.normalized);
            Prim(PrimitiveType.Cylinder, name, parent, (a + b) * 0.5f, new Vector3(radius * 2f, d.magnitude * 0.5f, radius * 2f), material, rot.eulerAngles);
        }

        private static MeshyPropBaker.HawkParts cachedHawk;
        private static int cachedHawkFrame = -1;

        /// <summary>The baked Meshy hawk, baked once per build (null when the model is missing).</summary>
        private static MeshyPropBaker.HawkParts MeshyHawk()
        {
            int frame = Time.frameCount;
            if (cachedHawk == null || cachedHawkFrame != frame || cachedHawk.Body == null)
            {
                cachedHawk = MeshyPropBaker.BakeHawk();
                cachedHawkFrame = frame;
            }
            return cachedHawk;
        }

        /// <summary>
        /// The glider shown on Sam's back while gliding (original stand-in: a broad pale leaf canopy with a
        /// pointed tip, a spine and veins, on a short strut). Built inactive; GlideMode attaches and shows it.
        /// Visual only, no colliders.
        /// </summary>
        private static GameObject GliderVisual(Context c, ValleyMats m)
        {
            var root = new GameObject("GliderVisual");
            root.transform.SetParent(c.Course, false);
            Transform t = root.transform;
            MeshyPropBaker.GliderParts meshy = MeshyPropBaker.BakeGlider();
            if (meshy != null)
            {
                // Meshy wing on a frame made for Sam (short arms, big head): the origin is the middle between the two
                // handle poles, at grip height; GlideMode puts it at Sam's raised hands. Each pole runs from just under
                // the wing down past the grip, with braces up to the keel ahead of and behind the head. Nothing
                // crosses over the head. Visual only, no colliders.
                MeshyPropBaker.AddMeshChild("Wing", t, meshy.Mesh, meshy.Material, new Vector3(0f, GliderWingAboveGrip, GliderWingAhead));
                float keelY = GliderWingAboveGrip + 0.02f;
                for (int s = -1; s <= 1; s += 2)
                {
                    var grip = new Vector3(s * GliderGripHalfWidth, 0f, 0f);
                    string side = s < 0 ? "_L" : "_R";
                    Strut(t, "Pole" + side, grip + Vector3.down * 0.12f, new Vector3(s * 0.2f, keelY, 0f), GliderPoleRadius, m.GliderFrame);
                    Strut(t, "BraceFront" + side, grip + Vector3.down * 0.12f, new Vector3(0f, keelY, 0.55f), GliderBraceRadius, m.GliderFrame);
                    Strut(t, "BraceBack" + side, grip + Vector3.down * 0.12f, new Vector3(0f, keelY, -0.4f), GliderBraceRadius, m.GliderFrame);
                    Prim(PrimitiveType.Sphere, "Handle" + side, t, grip, new Vector3(0.07f, 0.13f, 0.07f), m.GliderFrame);
                }
                Strut(t, "Keel", new Vector3(0f, keelY, -0.45f), new Vector3(0f, keelY, 0.6f), GliderBraceRadius, m.GliderFrame);
                root.SetActive(false);
                return root;
            }
            Prim(PrimitiveType.Cylinder, "Strut", t, new Vector3(0f, 0.12f, 0f), new Vector3(0.05f, 0.12f, 0.05f), m.GliderFrame);
            Prim(PrimitiveType.Cube, "Grip", t, Vector3.zero, new Vector3(0.5f, 0.04f, 0.04f), m.GliderFrame);
            Prim(PrimitiveType.Sphere, "Canopy", t, new Vector3(0f, 0.27f, 0f), new Vector3(2.2f, 0.06f, 0.95f), m.GliderLeaf);
            Prim(PrimitiveType.Sphere, "Tip", t, new Vector3(0f, 0.28f, 0.5f), new Vector3(0.55f, 0.05f, 0.6f), m.GliderLeaf);
            Prim(PrimitiveType.Cube, "Spine", t, new Vector3(0f, 0.31f, 0.05f), new Vector3(0.05f, 0.04f, 1.25f), m.GliderFrame);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(PrimitiveType.Cube, "Vein", t, new Vector3(s * 0.45f, 0.31f, 0.02f), new Vector3(0.03f, 0.03f, 1.0f), m.GliderFrame, new Vector3(0f, s * 55f, 0f));
            }
            root.SetActive(false);
            return root;
        }

        // ------------------------------------------------------------------ scenery

        /// <summary>
        /// Tall flat-topped sandstone mesas on both sides of the canyon with small dry trees and bushes on top,
        /// a second row of taller ones behind, and white wind streaks drifting through. Visual only,
        /// shared tinted materials, no shadows. Deterministic for a given <paramref name="seed"/>.
        /// <paramref name="ambientStreaks"/> false leaves out the drifting streaks (the walled glide canyon).
        /// </summary>
        private static void ValleyScenery(Context c, ValleyMats m, float zMin, float zMax, int seed, bool ambientStreaks = true)
        {
            var rng = new System.Random(seed);
            string[] plants = { "Tree_1_A", "Tree_2_A", "Tree_3_A", "Tree_4_A", "Bush_3_A", "Bush_2_A" };

            for (float z = zMin; z <= zMax; z += 20f)
            {
                for (int s = 0; s < 2; s++)
                {
                    float side = s == 0 ? -1f : 1f;

                    // Near mesa: inner edge at |x| >= 24, clear of the secret branches (|x| up to 16.7).
                    float sizeX = 14f + (float)rng.NextDouble() * 8f;
                    float sizeZ = 16f + (float)rng.NextDouble() * 6f;
                    float topY = 8f + (float)rng.NextDouble() * 26f;
                    float cx = side * (24f + sizeX * 0.5f + (float)rng.NextDouble() * 8f);
                    float cz = z + (float)rng.NextDouble() * 6f;
                    GrassIsland(c, "Mesa_" + (side < 0f ? "L_" : "R_") + z, cx, topY, cz, sizeX, sizeZ, ValleyBottomY, m.PadRock, c.Scenery);

                    int plantCount = 3 + rng.Next(3);
                    for (int i = 0; i < plantCount; i++)
                    {
                        Vector3 position = new Vector3(cx + ((float)rng.NextDouble() - 0.5f) * sizeX * 0.6f, topY,
                            cz + ((float)rng.NextDouble() - 0.5f) * sizeZ * 0.6f);
                        PlaceModel(plants[rng.Next(plants.Length)], position, new Vector3(0f, (float)rng.NextDouble() * 360f, 0f),
                            Vector3.one * (1.2f + (float)rng.NextDouble() * 0.6f), m.DryTree, c.Scenery);
                    }

                    // Far mesa, taller, no plants (it fades into the mist).
                    float farX = side * (62f + (float)rng.NextDouble() * 20f);
                    float farTop = 24f + (float)rng.NextDouble() * 24f;
                    float farSize = 20f + (float)rng.NextDouble() * 10f;
                    if (Mathf.RoundToInt(z / 20f) % 2 == 0) // every other step, to keep the triangle count down
                    {
                        GrassIsland(c, "FarMesa_" + (side < 0f ? "L_" : "R_") + z, farX, farTop, cz + 8f, farSize, 22f, ValleyBottomY, m.PadRock, c.Scenery);
                    }

                    // Ambient white wind streaks drifting across the canyon.
                    if (!ambientStreaks) continue;
                    var group = new GameObject("AmbientStreaks");
                    group.transform.SetParent(c.Scenery, false);
                    group.transform.position = new Vector3(side * (10f + (float)rng.NextDouble() * 8f), -2f + (float)rng.NextDouble() * 14f, cz);
                    for (int i = 0; i < 5; i++)
                    {
                        var local = new Vector3(0f, ((float)rng.NextDouble() - 0.5f) * 6f, ((float)rng.NextDouble() - 0.5f) * 14f);
                        Prim(PrimitiveType.Cylinder, "Streak", group.transform, local, new Vector3(0.06f, 1.2f, 0.06f), m.Streak, new Vector3(0f, 0f, 90f));
                    }
                    var drift = group.AddComponent<StreakDrift>();
                    SetFields(drift, ("localDirection", Vector3.right), ("speed", 9f), ("loopLength", 40f));
                }
            }
        }
    }
}
#endif
