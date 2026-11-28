#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using WeatherForSale.Obby;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Rainforest layouts (story 004): level 1 "Mushroom Path" (ทางเห็ดเด้ง: mushrooms only in its first third,
    /// then drooping giant leaves, then narrow mossy stones and a rising stone, so the jumps do not repeat) and
    /// level 2 "Logs and Vines" (ท่อนไม้กับเถาวัลย์). The course runs along +Z from the start pad at the origin.
    /// Swinging Rainbells and vines hang from vines out of the canopy (no trunks stand on the course).
    /// Difficulty pass: every section combines two or more mechanics (bounce through a swinging Rainbell,
    /// bounce onto a collapsing cap, collapsing chains, logs drifting across the lane under a Rainbell,
    /// three chained vines, a tree climb mixing rotten branches and a Rainbell), the safe pads are small
    /// and the checkpoints sit right after the hardest part of each third.
    /// Reach used to place everything (moveSpeed 6, jumpSpeed 9, gravity 25, 0.72 s in the air): about
    /// 4.3 m flat, 3.9 m with a 0.5 m rise, 3.6 m with a 0.85 m rise, 5 m with a 1.2 m drop. Main-path
    /// gaps (collider edge to edge) are 1.9 - 2.9 m, rises at most 0.85 m except off a bounce mushroom
    /// (launch 15 m/s: apex 4.5 m, 1.2 s in the air, about 5.7 m forward onto a +3 m cap). The
    /// hidden-Shard branch of each level uses smaller platforms and 2.6 - 3.3 m gaps with rises, so it is
    /// harder than the main path.
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

        // ---- swinging Rainbells (all swing left-right across the course)
        /// <summary>Stem length from the pivot to the bell, metres.</summary>
        private const float BellStemLength = 3.5f;
        /// <summary>How far the hit sphere's centre sits below the bell's origin (the Meshy head hangs low), metres.</summary>
        private const float BellHitDrop = 0.1f;
        /// <summary>Hit-sphere centre above the surface the bell guards: covers 0.18 - 1.42 m, Sam is 1.4 m tall.</summary>
        private const float BellHitHeightOverFloor = 0.8f;
        /// <summary>Level 1 swing, degrees: 45 sweeps the bell 2.5 m to each side.</summary>
        private const float BellSwingDegrees = 45f;
        /// <summary>Level 1, the bell across the flight off the first bounce mushroom (passes the middle every 1.1 s).</summary>
        private const float BounceBellPeriod = 2.2f;
        /// <summary>Level 1, the bell over the small cap Mushroom_C2 (passes the middle every 1.2 s).</summary>
        private const float CapBellPeriod = 2.4f;
        /// <summary>Level 1, the bell across the last bounce onto the finish (passes the middle every 1.0 s).</summary>
        private const float FinishBellPeriod = 2.0f;
        /// <summary>Level 2 swing, degrees: 40 sweeps the bell 2.25 m to each side.</summary>
        private const float LogsLevelBellSwingDegrees = 40f;
        /// <summary>Level 2, the bell over the third drifting log (passes the middle every 1.5 s).</summary>
        private const float LogBellPeriod = 3.0f;
        /// <summary>Level 2, the bell across the climb between branch 4 and 5 (passes the middle every 1.2 s).</summary>
        private const float ClimbBellPeriod = 2.4f;
        /// <summary>Level 2, the climb bell's hit centre above the branch before it (it blocks the jump arc).</summary>
        private const float ClimbBellHeightOverBranch = 1.2f;

        // ---- level 1 section B: drooping giant leaves (x, top, z); each sinks while stood on
        private static readonly (float x, float top, float z)[] LeafSteps =
        {
            (0f, 5.0f, 43.4f), (1.3f, 5.5f, 47.8f), (-1.3f, 6.0f, 52.2f), (0.6f, 6.4f, 56.6f),
        };
        private const float LeafSize = 2.6f;           // leaf width (walkable 2.2 m)
        private const float LeafSinkSpeed = 0.9f;      // droop while stood on, m/s
        private const float LeafRiseSpeed = 1.2f;      // springs back after, m/s
        private const float LeafMaxDroop = 1.6f;       // metres

        // ---- level 1 section C: narrow mossy stones (x, top, z), then a rising stone to the finish
        private static readonly (float x, float top, float z)[] StoneSteps =
        {
            (1.4f, 7.0f, 67.0f), (-0.6f, 7.3f, 70.8f), (1.2f, 7.6f, 74.6f), (-0.4f, 7.9f, 78.4f), (1.0f, 8.2f, 82.6f),
        };
        private const float StoneSize = 1.8f;
        private const float ElevatorSize = 2.2f;
        private const float ElevatorMidTop = 9.2f;     // top swings 7.8 - 10.6 (finish top 10.2)
        private const float ElevatorTravel = 1.4f;
        private const float ElevatorPeriod = 4.4f;

        /// <summary>Level 1 secret branch: leaf / stone size (1.4 m walkable), metres.</summary>
        private const float SecretLeafSize = 1.8f;
        /// <summary>Level 1 secret branch: the mossy stone's rise and fall either side of its middle, metres.</summary>
        private const float SecretStoneTravel = 0.6f;
        /// <summary>Level 1 secret branch: seconds for one rise and fall of the mossy stone.</summary>
        private const float SecretStonePeriod = 3.2f;

        // ---- level 2 chained vines: three vines, no landing between them. Neighbours swing in opposite
        // phase (half a period apart), so one foothold reaches the far end of its swing as the next one
        // reaches its near end, 2.4 m apart (centre to centre): jump in the last ~0.6 s before that moment.
        private const float VineLength = 6.0f;          // rope length: the swing reaches 3.18 m each way
        private const float VineSwingDegrees = 32f;
        private const float VinePeriod = 3.6f;          // seconds for one swing there and back (same for all three)
        private const float VineExtremeTopY = 0.7f;     // foothold top at both ends of the swing (0.9 m lower in the middle)

        // ---- level 2 tree climb: seven branches on separate trees, zigzagging left/right
        private const int ClimbBranches = 7;
        private const float ClimbRise = 0.85f;      // each branch this much higher than the last
        private const float ClimbStartZ = 60.4f;    // first branch (Checkpoint_2 ends at z 58.4)
        private const float ClimbSpacingZ = 2.3f;
        private const int ClimbBellBranch = 3;      // the Rainbell swings between this branch (index) and the next
        private const float ClimbBellSpacingZ = 3.0f; // wider gap there so a player waiting on the branch is clear of the bell
        private const float ClimbBranchWidth = 0.9f; // collider 0.72 m deep: a precision landing
        private const float ClimbBranchX = 1.3f;    // branch centre, left or right of the course line
        private const float ClimbTrunkX = 3.3f;     // trunk, outside the course
        private const float ClimbCrownWidth = 3.4f; // leafy crown on top of each climbing tree

        /// <summary>Which climb branches are rotten (break after RottenBranchStandSeconds of standing).</summary>
        private static readonly bool[] ClimbRotten = { false, true, true, false, true, true, false };

        /// <summary>
        /// Level 2 opening log lane: logs lying across the river that drift side to side (PlatformMover)
        /// at different speeds and directions while they roll (RollingSurface pushes the rider along Z).
        /// driftX / driftPeriod give the side-to-side swing (peak speeds 2.2 - 4.2 m/s); roll* the surface push.
        /// </summary>
        private static readonly (float z, float driftX, float driftPeriod, float driftPhase, float rollPeak, float rollPeriod, float rollPhase)[] RainforestLogLane =
        {
            (5.6f, 2.0f, 4.4f, 0.0f, 2.0f, 3.4f, 0.0f),
            (8.9f, 2.4f, 3.6f, 1.8f, 2.4f, 3.0f, 1.5f),
            (12.2f, 1.8f, 5.2f, 0.7f, 2.6f, 2.8f, 0.4f), // the Rainbell swings over this one
            (15.5f, 2.6f, 4.0f, 2.0f, 2.2f, 3.6f, 2.2f),
            (18.8f, 2.2f, 3.2f, 0.4f, 2.8f, 2.6f, 1.0f),
        };

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

        /// <summary>Seconds a collapsed mushroom stays gone before rising back (short, so a retry of a chain is not a long wait).</summary>
        private const float MushroomGoneSeconds = 2.0f;

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

            // A: one still cap, then a chain of four collapsing caps zigzagging upward (0.6 s each: keep
            // hopping), a bounce mushroom at the end of the chain that throws the player up onto a high
            // cap that also collapses, and the checkpoint just past it.
            Mushroom(c, m, "Mushroom_A1", 0f, 0.2f, 6.6f, 2.4f, 2.4f, MushroomKind.Static);
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                Mushroom(c, m, "Mushroom_A" + (i + 2) + "_Collapse", side * 1.2f, 0.45f + 0.25f * i, 10.6f + 4.0f * i,
                    2.4f, 2.4f, MushroomKind.Collapse);
            }
            Mushroom(c, m, "Mushroom_A6_Bounce", 0f, 1.2f, 26.8f, 2.8f, 2.8f, MushroomKind.Bounce);
            Mushroom(c, m, "Mushroom_A7_HighCollapse", 0f, 4.4f, 32.2f, 2.8f, 2.8f, MushroomKind.Collapse);
            AddCheckpoint(c, "Checkpoint_1", 0f, 4.6f, 37.8f, 4f, m.Pillar, PillarBottomY);

            // B (no more mushrooms): four giant leaves on tall stalks climbing toward checkpoint 2. Each leaf
            // droops while stood on (SinkingPlatform), so waiting makes the next rise too high: keep hopping.
            // A Rainbell swings across the second leaf.
            for (int i = 0; i < LeafSteps.Length; i++)
            {
                (float x, float top, float z) leaf = LeafSteps[i];
                DroopLeaf(c, m, "Leaf_B" + (i + 1), leaf.x, leaf.top, leaf.z, LeafSize);
            }
            RainbellAt(c, m, "Rainbell_B", new Vector3(LeafSteps[1].x, LeafSteps[1].top + BellHitHeightOverFloor, LeafSteps[1].z),
                BellSwingDegrees, BounceBellPeriod, 0f, 4.7f);
            AddCheckpoint(c, "Checkpoint_2", 0f, 6.8f, 62.3f, 4f, m.Pillar, PillarBottomY);

            // C (precision): narrow mossy stone pillars zigzagging up, a Rainbell across the second and the
            // last, then a stone that rises and falls (time the ride) up to the finish.
            for (int i = 0; i < StoneSteps.Length; i++)
            {
                (float x, float top, float z) stone = StoneSteps[i];
                Slab(c, "Stone_C" + (i + 1), stone.x, stone.top, stone.z, StoneSize, StoneSize, c.PadMaterial, m.Pillar, PillarBottomY);
            }
            RainbellAt(c, m, "Rainbell_C2", new Vector3(StoneSteps[1].x, StoneSteps[1].top + BellHitHeightOverFloor, StoneSteps[1].z),
                BellSwingDegrees, CapBellPeriod, 0.6f, -4.7f);
            (float x, float top, float z) lastStone = StoneSteps[StoneSteps.Length - 1];
            RainbellAt(c, m, "Rainbell_Finish", new Vector3(lastStone.x, lastStone.top + BellHitHeightOverFloor, lastStone.z),
                BellSwingDegrees, FinishBellPeriod, 0.3f, 4.7f);
            RisingStone(c, m, "Stone_Lift", 0.4f, ElevatorMidTop, 87.4f, ElevatorSize, ElevatorTravel, ElevatorPeriod);
            AddFinish(c, 0f, 10.2f, 93.0f, 6f, 7f, m.Pillar, PillarBottomY);

            // Secret branch off checkpoint 1 toward -X (no mushrooms, play-test): small drooping leaves 1.4 m
            // walkable, 2.4 - 3.3 m gaps, each a little higher, a mossy stone rising and falling (time the jump),
            // then the leaf with the Shard.
            DroopLeaf(c, m, "Secret_1_Leaf", -5.8f, 4.9f, 38.4f, SecretLeafSize);
            DroopLeaf(c, m, "Secret_2_Leaf", -9.6f, 5.3f, 36.4f, SecretLeafSize);
            RisingStone(c, m, "Secret_3_Stone", -13.2f, 5.6f, 38.4f, SecretLeafSize, SecretStoneTravel, SecretStonePeriod);
            DroopLeaf(c, m, "Secret_4_Leaf", -13.6f, 6.0f, 42.6f, SecretLeafSize);
            AddShard(c, c.MapId, new Vector3(-13.6f, 7.1f, 42.6f), m.Shard, 0.4f, 0.8f);

            RainforestScenery(c, m, -12f, 105f, 1001);
        }

        // ------------------------------------------------------------------ level 2: Logs and Vines

        private static void BuildRainforest2(Context c)
        {
            var m = new RainforestMats();
            PrepareRainforest(c);
            StartIsland(c, 6f, 6f, PillarBottomY);

            // A: five logs lying across the river drift side to side, each at its own speed and direction,
            // while they roll (the surface pushes along Z). A Rainbell swings over the third one.
            for (int i = 0; i < RainforestLogLane.Length; i++)
            {
                var log = RainforestLogLane[i];
                RollingLog(c, m, "Log_" + (i + 1), 0f, 0f, log.z, 3.4f, 1.8f,
                    peakSpeed: log.rollPeak, period: log.rollPeriod, phase: log.rollPhase,
                    travel: new Vector3(log.driftX, 0f, 0f), travelPeriod: log.driftPeriod, travelPhase: log.driftPhase);
            }
            RainbellAt(c, m, "Rainbell_LogLane", new Vector3(0f, BellHitHeightOverFloor, RainforestLogLane[2].z), LogsLevelBellSwingDegrees, LogBellPeriod, 0f, 4.7f);
            AddCheckpoint(c, "Checkpoint_1", 0f, 0.4f, 23.6f, 4f, m.Pillar, PillarBottomY);

            // B: three vines chained with no landing between (neighbours in opposite phase). Board vine 1 at
            // its near end, ride to the far end, jump to vine 2 as it reaches its near end, and so on.
            SwingingVine(c, m, "Vine_1", 0f, 31.2f, VineExtremeTopY, VineLength, VineSwingDegrees, VinePeriod, 0f, -4.7f);
            SwingingVine(c, m, "Vine_2", 0f, 39.95f, VineExtremeTopY, VineLength, VineSwingDegrees, VinePeriod, VinePeriod * 0.5f, 4.7f);
            SwingingVine(c, m, "Vine_3", 0f, 48.7f, VineExtremeTopY, VineLength, VineSwingDegrees, VinePeriod, 0f, -4.7f);
            const float checkpoint2Top = 0.6f;
            AddCheckpoint(c, "Checkpoint_2", 0f, checkpoint2Top, 56.4f, 4f, m.Pillar, PillarBottomY);

            // C: jump-climb through seven separate trees, zigzagging, 0.85 m higher each time; four of the
            // narrow branches are rotten, and a Rainbell swings across the gap after the 4th branch.
            float z = ClimbStartZ, topY = checkpoint2Top;
            float bellZ = 0f, bellBranchTop = 0f;
            for (int k = 0; k < ClimbBranches; k++)
            {
                if (k > 0) z += k == ClimbBellBranch + 1 ? ClimbBellSpacingZ : ClimbSpacingZ;
                topY = checkpoint2Top + ClimbRise * (k + 1);
                float side = k % 2 == 0 ? 1f : -1f;
                bool rotten = ClimbRotten[k];
                ClimbTree(c, m, "ClimbTree_" + (k + 1), side * ClimbTrunkX, z, topY + 2.4f);
                GameObject branch = Branch(c, "Climb_" + (k + 1) + (rotten ? "_Rotten" : ""), new Vector3(side * ClimbBranchX, topY, z),
                    side > 0f ? Vector3.left : Vector3.right, 3.0f, ClimbBranchWidth, 0.7f, rotten ? m.Rotten : m.Bark);
                if (rotten) MakeRotten(branch, m, topY);
                if (k == ClimbBellBranch)
                {
                    bellZ = z + ClimbBellSpacingZ * 0.5f;
                    bellBranchTop = topY;
                }
            }
            RainbellAt(c, m, "Rainbell_Climb", new Vector3(0f, bellBranchTop + ClimbBellHeightOverBranch, bellZ),
                LogsLevelBellSwingDegrees, ClimbBellPeriod, 0f, 5.2f);

            // D: down again on three rotten branches (1.2 m drops, keep going), onto a log drifting across
            // the river and a fast rolling log, then the deck.
            for (int i = 0; i < 3; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float descentTop = topY - 1.2f * (i + 1);
                float descentZ = 77.8f + 2.9f * i;
                ClimbTree(c, m, "DescentTree_" + (i + 1), side * ClimbTrunkX, descentZ, descentTop + 2.4f);
                GameObject branch = Branch(c, "Descent_" + (i + 1) + "_Rotten", new Vector3(side * ClimbBranchX, descentTop, descentZ),
                    side > 0f ? Vector3.left : Vector3.right, 3.2f, 1.2f, 0.7f, m.Rotten);
                MakeRotten(branch, m, descentTop);
            }
            RollingLog(c, m, "Log_6_Drifting", 0f, 0.4f, 87.3f, 3.4f, 1.8f, peakSpeed: 2.4f, period: 3.0f, phase: 0f,
                travel: new Vector3(2.2f, 0f, 0f), travelPeriod: 4.0f, travelPhase: 0f);
            RollingLog(c, m, "Log_7_Fast", 0f, 0.4f, 90.6f, 3.4f, 1.8f, peakSpeed: 3.6f, period: 2.6f, phase: 1.3f,
                travel: new Vector3(0f, 0.08f, 0f), travelPeriod: 2.6f, travelPhase: 1.1f);

            // Deck and the 9 x 9 finish ground with the dais: nothing else within 9 m (the suction happens there).
            Slab(c, "Deck", 0f, 0.6f, 95.5f, 6f, 5f, c.In.PlatformMaterial, m.Pillar, PillarBottomY);
            AddFinish(c, 0f, 0.6f, 102.5f, 9f, 9f, m.Pillar, PillarBottomY, weatherDais: true);

            // Secret branch off the 4th climb branch (left tree, the still one before the bell) toward -X:
            // three narrow rotten stubs, 2.8 - 3.2 m apart, 0.3 - 0.4 m higher each. Hidden Shard at the end.
            const float stubLength = 1.8f, stubWidth = 1.0f;
            (float x, float top, float z)[] stubs = { (-5.8f, 4.3f, 64.9f), (-9.9f, 4.7f, 62.5f), (-13.9f, 5.1f, 64.9f) };
            for (int i = 0; i < stubs.Length; i++)
            {
                var s = stubs[i];
                RainforestStubTrunk(c, m, "Secret_" + (i + 1) + "_Trunk", s.x - stubLength * 0.5f - 0.3f, s.z, s.top);
                MakeRotten(Branch(c, "Secret_" + (i + 1), new Vector3(s.x, s.top, s.z), Vector3.right, stubLength, stubWidth, 0.75f, m.Rotten), m, s.top);
            }
            AddShard(c, c.MapId, new Vector3(-13.9f, 6.2f, 64.9f), m.Shard, 0.4f, 0.8f);

            RainforestScenery(c, m, -12f, 112f, 2002);
        }

        /// <summary>
        /// A giant leaf on a tall stalk out of the river: a <paramref name="size"/> wide platform that droops
        /// while stood on (<see cref="SinkingPlatform"/>) and springs back after. Meshy lily-pad leaf when present,
        /// else a flat green disc. The stalk moves with the leaf.
        /// </summary>
        private static GameObject DroopLeaf(Context c, RainforestMats m, string name, float x, float topY, float z, float size)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);
            var box = root.AddComponent<BoxCollider>();
            float walk = size - 0.4f;
            box.size = new Vector3(walk, 0.3f, walk);
            box.center = new Vector3(0f, -0.15f, 0f);

            float stalkHeight = topY - (RiverTopY - 1f);
            Prim(PrimitiveType.Cylinder, "Stalk", root.transform, new Vector3(0f, -0.2f - stalkHeight * 0.5f, 0f),
                new Vector3(0.28f, stalkHeight * 0.5f, 0.28f), m.Vine);
            MeshyPropBaker.PropMesh leaf = MeshyPropBaker.BakeProp("Rainforest/LilyPad", "LilyPad", MeshyPropBaker.ZUp);
            if (leaf != null)
                MeshyPropBaker.PlaceProp(leaf, "Leaf", root.transform, new Vector3(0f, -0.25f, 0f), new Vector3(size, 0f, size), new Vector3(0f, (x * 47f) % 360f, 0f));
            else
                Prim(PrimitiveType.Cylinder, "Leaf", root.transform, new Vector3(0f, -0.1f, 0f), new Vector3(size, 0.1f, size), m.Vine);

            root.AddComponent<RideableSurface>();
            var sinking = root.AddComponent<SinkingPlatform>();
            SetFields(sinking, ("sinkSpeed", LeafSinkSpeed), ("riseSpeed", LeafRiseSpeed), ("maxDepth", LeafMaxDroop));
            return root;
        }

        /// <summary>
        /// A mossy stone that rises and falls (<see cref="PlatformMover"/>, vertical sine) around
        /// <paramref name="midTop"/> by <paramref name="travel"/>, carrying the player (grass-topped rock look).
        /// </summary>
        private static GameObject RisingStone(Context c, RainforestMats m, string name, float x, float midTop, float z, float size,
            float travel, float period)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, midTop, z);
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(size, 0.6f, size);
            box.center = new Vector3(0f, -0.3f, 0f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            if (GrassIsland(c, name + "_Island", x, midTop, z, size, size, midTop - 2.2f, null, visual.transform) == null)
                Prim(PrimitiveType.Cylinder, "Rock", visual.transform, new Vector3(0f, -1.1f, 0f), new Vector3(size, 1.1f, size), m.Pillar);
            root.AddComponent<RideableSurface>();
            var mover = root.AddComponent<PlatformMover>();
            SetFields(mover, ("travel", new Vector3(0f, travel, 0f)), ("periodSeconds", period), ("phaseSeconds", 0f));
            return root;
        }

        /// <summary>A bare bark trunk rising out of the river to just above a stub branch (visual only, no crown, so the camera stays clear).</summary>
        private static void RainforestStubTrunk(Context c, RainforestMats m, string name, float x, float z, float stubTop)
        {
            float bottom = RiverTopY - 1f;
            float height = stubTop + 0.6f - bottom;
            Prim(PrimitiveType.Cylinder, name, c.Scenery, new Vector3(x, bottom + height * 0.5f, z),
                new Vector3(0.6f, height * 0.5f, 0.6f), m.Bark);
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
                    ("goneSeconds", MushroomGoneSeconds),
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

        /// <summary>
        /// A log lying along X whose surface rolls (pushes the rider along Z, see RollingSurface) and which
        /// moves as a whole by <paramref name="travel"/> (PlatformMover sine): a small Y value is a bob, an X
        /// value makes it drift side to side across the course. Zero travel = no mover.
        /// </summary>
        private static GameObject RollingLog(Context c, RainforestMats m, string name, float x, float topY, float z,
            float length, float diameter, float peakSpeed, float period, float phase,
            Vector3 travel, float travelPeriod, float travelPhase)
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

            // Meshy log (mossy bark, ringed ends) stretched to the walkable length and diameter, length along X.
            MeshyPropBaker.PropMesh meshyLog = LogModel();
            if (meshyLog != null)
            {
                MeshyPropBaker.PlaceProp(meshyLog, "Log", roll.transform, new Vector3(0f, -radius, 0f), new Vector3(length, diameter, diameter))
                    .GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return FinishRollingLog(root, roll, radius, peakSpeed, period, phase, travel, travelPeriod, travelPhase);
            }

            // Fallback: faceted bark trunk (low-poly mesh with bark ridges), length along X.
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

            return FinishRollingLog(root, roll, radius, peakSpeed, period, phase, travel, travelPeriod, travelPhase);
        }

        /// <summary>The Meshy log (Assets/Art/Props/Rainforest/LogV2) baked with its length along X, or null.</summary>
        private static MeshyPropBaker.PropMesh LogModel()
        {
            MeshyPropBaker.PropMesh log = MeshyPropBaker.BakeProp("Rainforest/LogV2", "LogV2", MeshyPropBaker.ZUp);
            if (log == null || log.Size.x >= log.Size.z) return log;
            return MeshyPropBaker.BakeProp("Rainforest/LogV2", "LogV2", MeshyPropBaker.ZUp + new Vector3(0f, 90f, 0f));
        }

        private static GameObject FinishRollingLog(GameObject root, GameObject roll, float radius, float peakSpeed, float period, float phase,
            Vector3 travel, float travelPeriod, float travelPhase)
        {
            var surface = root.AddComponent<RollingSurface>();
            SetFields(surface,
                ("driveDirection", Vector3.forward),
                ("peakSurfaceSpeed", peakSpeed),
                ("periodSeconds", period),
                ("phaseSeconds", phase),
                ("rollVisual", roll.transform),
                ("rollAxis", Vector3.right),
                ("rollRadius", radius));

            if (travel.sqrMagnitude > 0f)
            {
                var mover = root.AddComponent<PlatformMover>();
                SetFields(mover, ("travel", travel), ("periodSeconds", travelPeriod), ("phaseSeconds", travelPhase));
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
            MeshyPropBaker.PropMesh vineModel = MeshyPropBaker.BakeProp("Rainforest/Vine", "Vine", MeshyPropBaker.ZUp);
            if (vineModel != null)
                MeshyPropBaker.PlaceProp(vineModel, "Rope", pivot.transform, new Vector3(0f, -length, 0f), new Vector3(0.7f, length, 0.7f));
            else
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
                hit.center = new Vector3(0f, -BellHitDrop, 0f);
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

        /// <summary>
        /// A swinging Rainbell (BellStemLength stem, swinging left-right) placed by where its hit sphere is
        /// at rest: <paramref name="hitCentre"/>. The pivot goes above it; the trunk at <paramref name="trunkOffsetX"/>.
        /// </summary>
        private static void RainbellAt(Context c, RainforestMats m, string name, Vector3 hitCentre, float amplitudeDegrees,
            float period, float phase, float trunkOffsetX)
        {
            Vector3 pivot = hitCentre + Vector3.up * (BellHitDrop + BellStemLength);
            RainbellFlower(c, m, name, pivot, BellStemLength, amplitudeDegrees, period, phase, trunkOffsetX);
        }

        /// <summary>Height of the vine a swinging pivot hangs from, metres (it disappears up into the canopy mist).</summary>
        private const float HangVineHeight = 10f;

        /// <summary>
        /// A vine dropping out of the canopy to a pivot, so the pivot hangs from something without a trunk standing
        /// on the course (play-test: the trunks looked like poles in the path). Meshy vine when present. Visual only.
        /// <paramref name="trunkOffsetX"/> is kept for the callers' layout; nothing stands beside the course now.
        /// </summary>
        private static void HangFrame(Context c, RainforestMats m, string name, Vector3 pivotPosition, float trunkOffsetX)
        {
            MeshyPropBaker.PropMesh vine = MeshyPropBaker.BakeProp("Rainforest/Vine", "Vine", MeshyPropBaker.ZUp);
            if (vine != null)
                MeshyPropBaker.PlaceProp(vine, name + "_HangVine", c.Scenery, pivotPosition, new Vector3(0.7f, HangVineHeight, 0.7f));
            else
                Prim(PrimitiveType.Cylinder, name + "_HangVine", c.Scenery, pivotPosition + Vector3.up * (HangVineHeight * 0.5f),
                    new Vector3(0.14f, HangVineHeight * 0.5f, 0.14f), m.Vine);
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
            // Meshy branch (its length turned along root-local Z, top at the walkable surface); else a flat cylinder.
            // Rotten branches use the Meshy rotten branch (grey cracked wood, fungus) so they read as the ones that break.
            bool rotten = material != null && material.name.Contains("Rotten");
            MeshyPropBaker.PropMesh limb = rotten ? RottenBranchModel() : MeshyPropBaker.BakeProp("Rainforest/Branch", "Branch", MeshyPropBaker.ZUp);
            if (limb != null)
            {
                const float limbDepth = 0.8f;
                // The model's long side goes along the branch (root-local Z): turned 90 degrees when it is along X.
                bool alongX = limb.Size.x >= limb.Size.z;
                MeshyPropBaker.PlaceProp(limb, "Limb", visual.transform, new Vector3(0f, -limbDepth, 0f),
                    alongX ? new Vector3(visualLength, limbDepth, width) : new Vector3(width, limbDepth, visualLength),
                    alongX ? new Vector3(0f, 90f, 0f) : Vector3.zero);
            }
            else
            {
                // Cylinder axis along the branch (root-local Z), 0.5 m thick, flat-ish.
                Prim(PrimitiveType.Cylinder, "Limb", visual.transform, new Vector3(0f, -0.25f, 0f),
                    new Vector3(width, visualLength * 0.5f, 0.5f), material,
                    Quaternion.LookRotation(Vector3.up, Vector3.forward).eulerAngles);
            }
            return root;
        }

        /// <summary>Turns a branch into a rotten one: pale wood with dark rot spots that breaks after being stood on too long.</summary>
        /// <summary>The Meshy rotten branch (Assets/Art/Props/Rainforest/RottenBranch), or null when not downloaded.</summary>
        private static MeshyPropBaker.PropMesh RottenBranchModel()
            => MeshyPropBaker.BakeProp("Rainforest/RottenBranch", "RottenBranch", MeshyPropBaker.ZUp);

        private static void MakeRotten(GameObject branch, RainforestMats m, float topY)
        {
            Transform visual = branch.transform.Find("Visual");
            if (RottenBranchModel() == null)
            {
                // Stand-in only: dark rot spots on the pale cylinder.
                Prim(PrimitiveType.Cylinder, "RotSpotA", visual, new Vector3(0.1f, 0.012f, -0.45f), new Vector3(0.32f, 0.012f, 0.32f), m.RotSpot);
                Prim(PrimitiveType.Cylinder, "RotSpotB", visual, new Vector3(-0.12f, 0.012f, 0.4f), new Vector3(0.28f, 0.012f, 0.28f), m.RotSpot);
            }

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
