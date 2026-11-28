#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using WeatherForSale.Obby;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Desert layouts (story 004): level 1 "Cacti and stone pillars" (กระบองเพชรกับเสาไฟ) and level 2
    /// "Quicksand pits" (บ่อทรายดูด). The course runs along +Z from the start pad at the origin over rippled
    /// golden quicksand (the kill slab); pads are KayKit hills in colour set 5 (golden sand top).
    /// Level 1 (play-test pass): a tumbleweed canyon (one continuous path; four lanes of tumbleweeds tumble in over
    /// the dune banks on both sides, drop into the canyon, cross the path and bounce away over the far bank),
    /// collapsing sandstone pillars, a narrow winding path that weaves between cacti in a light sandstorm, a timed
    /// sand geyser up to checkpoint 2, a rotating sandstone turnstile, and a sandstone bridge that crumbles span by
    /// span as you run. Level 2: sinking slabs, sand ferries, a tumbleweed slab, a sandstorm ledge, a narrow
    /// winding cactus path, crumbling pillars.
    /// Reach used to place everything (moveSpeed 6, jumpSpeed 9, gravity 25): about 4 m flat or 3.9 m with a
    /// 0.5 m rise, 1.6 m jump apex. Main-path gaps (edge to edge) are 0.9 - 2.75 m with 0 - 0.4 m rises (the geyser
    /// ledges rise 3.0 - 3.2 m and are reachable only by the geyser); the hidden-Shard branches use 3.35 - 3.46 m gaps.
    /// </summary>
    internal static partial class MapLevelBuilder
    {
        /// <summary>Surface of the quicksand (top of the kill slab).</summary>
        private const float SandTopY = -2f;

        // ---- Difficulty tuning (story 004 difficulty pass). Layout coordinates stay inline in the builders;
        // per-instance timings (tumbleweed speed / phase, ferry period) are written next to each call.

        /// <summary>Rise speed of every sinking slab after the player steps off, metres per second.</summary>
        private const float DesertSinkRiseSpeed = 1.0f;
        /// <summary>Level 2 opening chain and last slab: a slab 2 m above the sand is under in about 1.7 s.</summary>
        private const float DesertSinkChainSpeed = 1.2f;
        /// <summary>Level 2 long slab with the tumbleweeds: slower, about 6.5 s from rest to under the sand.</summary>
        private const float DesertSinkLongSpeed = 0.55f;
        /// <summary>Hidden-Shard branch slabs (level 2), faster than the main path.</summary>
        private const float DesertSinkSecretSpeed = 1.4f;

        /// <summary>Collapsing sandstone pillar: seconds from the first step until it gives way (keeps counting if you leave).</summary>
        private const float DesertCrumbleStandSeconds = 0.55f;
        /// <summary>Seconds the pillar takes to drop under the sand.</summary>
        private const float DesertCrumbleFallSeconds = 0.5f;
        /// <summary>Seconds the pillar stays gone before it rises back.</summary>
        private const float DesertCrumbleGoneSeconds = 2.5f;
        /// <summary>Seconds the pillar takes to rise back.</summary>
        private const float DesertCrumbleReturnSeconds = 1.0f;
        /// <summary>Peak shake of a crumbling pillar during its warning, metres.</summary>
        private const float DesertCrumbleShakeAmplitude = 0.07f;

        /// <summary>Level 1 tumbleweed canyon: half-width of the path (2.4 m wide), metres.</summary>
        private const float DesertCanyonPathHalfWidth = 1.2f;
        /// <summary>Level 1 tumbleweed canyon: inner edge of the dune banks (a 2 m quicksand moat each side of the path), |x| metres.</summary>
        private const float DesertCanyonBankInnerX = 3.2f;
        /// <summary>Level 1 tumbleweed canyon: outer edge of the dune banks, |x| metres.</summary>
        private const float DesertCanyonBankOuterX = 13.0f;
        /// <summary>Level 1 tumbleweed canyon: top of the dune banks. 2.4 m over the path, out of jump reach (apex 1.6 m), so no bypass.</summary>
        private const float DesertCanyonBankTopY = 2.4f;
        /// <summary>A canyon tumbleweed appears and vanishes this far out (|x|), below the bank top (behind the dune).</summary>
        private const float DesertCanyonHideX = 14.6f;
        /// <summary>How far below the bank top a hidden tumbleweed's centre sits, metres (its top stays under the crest).</summary>
        private const float DesertCanyonHideDepth = 1.4f;
        /// <summary>Extra height of a tumbleweed's bounce from the path up onto the far bank, metres.</summary>
        private const float DesertCanyonHopHeight = 0.8f;
        /// <summary>Pause between runs of a canyon tumbleweed, seconds (lane rhythm = run time + this, split between its balls).</summary>
        private const float DesertCanyonRollDelay = 0.3f;

        /// <summary>Narrow cactus path: overlap of consecutive path segments, metres (they are offset sideways to wind).</summary>
        private const float DesertWeaveOverlap = 0.4f;
        /// <summary>Narrow cactus path: cactus trunk offset from the segment's centre line, metres. On a 1.6 - 1.8 m path this
        /// leaves a 0.83 - 0.93 m lane beside each cactus and forces at least a 0.54 m sidestep between consecutive cacti.</summary>
        private const float DesertWeaveCactusOffset = 0.3f;
        /// <summary>Narrow cactus path: cactus scale (short design: 1.5 m tall, trunk hit radius 0.27 m).</summary>
        private const float DesertWeaveCactusScale = 0.85f;
        /// <summary>Narrow cactus path: along-path spacing of the cacti, metres.</summary>
        private const float DesertWeaveCactusSpacing = 2.2f;
        /// <summary>Consecutive weave segments alternate their top by this much, metres, so overlapping hill tops do not z-fight.</summary>
        private const float DesertWeaveTopStagger = 0.02f;

        /// <summary>Level 1 sandstorm over the winding cactus path: a gentle push toward +X, metres per second (run speed is 6).</summary>
        private const float DesertSandstorm1Wind = 1.4f;
        private const float DesertSandstorm1OnSeconds = 1.2f;
        private const float DesertSandstorm1OffSeconds = 2.0f;
        /// <summary>Level 2 sandstorm over the first thin ledge: push toward -X, metres per second.</summary>
        private const float DesertSandstorm2Wind = 2.6f;
        private const float DesertSandstorm2OnSeconds = 1.2f;
        private const float DesertSandstorm2OffSeconds = 1.6f;
        /// <summary>Number of drifting sand streaks drawn in a sandstorm zone.</summary>
        private const int DesertSandstormStreakCount = 16;

        /// <summary>Sand geyser cycle, seconds (dormant + warning + eruption).</summary>
        private const float DesertGeyserPeriod = 3.0f;
        /// <summary>Sand geyser rumble before each eruption, seconds.</summary>
        private const float DesertGeyserWarning = 0.9f;
        /// <summary>Sand geyser eruption, seconds (a rider on the vent at any moment of it is launched).</summary>
        private const float DesertGeyserErupt = 0.6f;
        /// <summary>Sand geyser launch speed, metres per second: apex 14^2 / 50 = 3.9 m, clears a 3.2 m rise.</summary>
        private const float DesertGeyserLaunchSpeed = 14f;
        /// <summary>Height of the drawn sand column at full eruption, metres.</summary>
        private const float DesertGeyserColumnHeight = 4.5f;

        /// <summary>Sandstone turnstile: arm length tip to tip (radius 3.2 m), metres.</summary>
        private const float DesertTurnstileArmLength = 6.4f;
        /// <summary>Sandstone turnstile: arm width, metres.</summary>
        private const float DesertTurnstileArmWidth = 1.4f;
        /// <summary>Sandstone turnstile: turning speed, degrees per second (an arm lines up with the course every 2.5 s; tips move 2 m/s).</summary>
        private const float DesertTurnstileDegreesPerSecond = 36f;
        /// <summary>Thickness of the turnstile arms, metres.</summary>
        private const float DesertTurnstileThickness = 0.5f;

        /// <summary>Crumbling bridge: seconds from the first step on a span until it gives way (run, do not stop).</summary>
        private const float DesertBridgeStandSeconds = 0.35f;
        /// <summary>Crumbling bridge: span length along the course, metres (crossed in 0.25 s at run speed).</summary>
        private const float DesertBridgeSpanLength = 1.5f;
        /// <summary>Crumbling bridge: width, metres.</summary>
        private const float DesertBridgeWidth = 1.6f;
        /// <summary>Crumbling bridge: span thickness, metres.</summary>
        private const float DesertBridgeThickness = 0.6f;
        /// <summary>Drawn gap between spans, metres (the colliders touch).</summary>
        private const float DesertBridgeJoint = 0.06f;

        /// <summary>Thickness of a sand ferry's walkable collider, metres (the drawn body reaches into the sand).</summary>
        private const float DesertFerryColliderDepth = 0.8f;

        /// <summary>Bottom of the pads' rock bodies, just under the sand.</summary>
        private const float DesertBottomY = -4f;

        private const float DesertFogDensity = 0.004f;
        private static readonly Color DesertFogColor = new Color(0.45f, 0.72f, 0.98f);

        /// <summary>Thickness of a floating (sinking) slab, metres.</summary>
        private const float SinkSlabThickness = 1.2f;

        /// <summary>Radius of a tumbleweed's visual and of its (slightly smaller) hit sphere, metres.</summary>
        private const float TumbleweedRadius = 0.6f;
        private const float TumbleweedHitRadius = 0.5f;

        private const float CactusTrunkRadius = 0.38f;
        private const float CactusArmRadius = 0.2f;
        /// <summary>Cactus hit boxes are this much smaller than the drawn cactus on every side, metres.</summary>
        private const float CactusHitInset = 0.06f;

        private const string DesertArtFolder = "Assets/Art/Props/Desert";
        private const string QuicksandMeshPath = DesertArtFolder + "/QuicksandRipples.asset";

        private sealed class DesertMats
        {
            // KayKit palette tints. The grass top (green) turns golden, the grey rock body red-orange; dunes are paler sand.
            public readonly Material PadSand = PaletteMat("Obby_DS_PaletteSand", new Color(1.55f, 0.85f, 0.50f));
            public readonly Material Dune = PaletteMat("Obby_DS_PaletteDune", new Color(1.90f, 1.15f, 0.60f));
            public readonly Material RedRock = PaletteMat("Obby_DS_PaletteRedRock", new Color(1.45f, 0.55f, 0.38f));
            public readonly Material Dry = PaletteMat("Obby_DS_PaletteDry", new Color(1.60f, 0.90f, 0.30f));
            public readonly Material Pillar = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_DS_RockBody", new Color(0.85f, 0.45f, 0.25f));
            public readonly Material Cactus = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_DS_Cactus", new Color(0.28f, 0.62f, 0.30f));
            public readonly Material Flower = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_DS_CactusFlower", new Color(1.00f, 0.45f, 0.65f));
            public readonly Material FlowerCenter = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_DS_CactusFlowerCenter", new Color(1.00f, 0.85f, 0.20f));
            public readonly Material Sand = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_DS_QuicksandRipples", new Color(0.93f, 0.73f, 0.36f));
            public readonly Material Shard = GlowMat("Obby_DS_Shard", new Color(0.62f, 0.22f, 1.00f), 2.2f);
            /// <summary>Dark cracks on the top of a collapsing sandstone pillar (also the geyser vent ring and turnstile tips).</summary>
            public readonly Material Crack = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_DS_Crack", new Color(0.42f, 0.20f, 0.10f));
            /// <summary>See-through sand streaks of a sandstorm gust (also the geyser's sand column and puff).</summary>
            public readonly Material SandStreak = AirMat("Obby_DS_SandStreak", new Color(0.98f, 0.84f, 0.56f, 0.7f));
            /// <summary>Pale sandstone on the walking tops of the turnstile and the crumbling bridge.</summary>
            public readonly Material Sandstone = ObbyTestSceneBuilder.GetOrCreateMaterial("Obby_DS_Sandstone", new Color(0.95f, 0.74f, 0.46f));
        }

        private static void PrepareDesert(Context c, DesertMats m)
        {
            c.Result.HazardTopY = SandTopY;
            // Pads come from KayKit's colour set 5 (golden sand top) in its own colours, not a tint.
            c.PadMaterial = null;
            c.PadColorSet = 5;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = DesertFogColor;
            RenderSettings.fogDensity = DesertFogDensity;
            QuicksandSurface(c, m);
        }

        // ------------------------------------------------------------------ level 1: Cacti and stone pillars

        private static void BuildDesert1(Context c)
        {
            var m = new DesertMats();
            PrepareDesert(c, m);
            StartIsland(c, 6f, 6f, DesertBottomY);
            Material pad = c.In.PlatformMaterial;

            // A (z 4.5 - 19): tumbleweed canyon. One continuous 2.4 m path (no cacti) between two dune banks 2.4 m higher
            // (x 3.2 - 13 each side, out of jump reach) across a 2 m quicksand moat. Four lanes of tumbleweeds, three balls
            // each, alternate direction and run at different speeds: each ball rises over the far side of its bank, rolls
            // across the top, drops into the canyon, crosses the path, bounces up onto the other bank and rolls away over
            // it, so nothing pops in near the player. Between the lanes there is about 1.8 m of safe path to wait on.
            Slab(c, "Path_A", 0f, 0f, 11.75f, DesertCanyonPathHalfWidth * 2f, 14.5f, pad, m.Pillar, DesertBottomY); // z 4.5 - 19, x -1.2 - 1.2
            float bankWidth = DesertCanyonBankOuterX - DesertCanyonBankInnerX;
            float bankX = (DesertCanyonBankInnerX + DesertCanyonBankOuterX) * 0.5f;
            Slab(c, "Bank_AL", -bankX, DesertCanyonBankTopY, 11.75f, bankWidth, 15.5f, pad, m.Pillar, DesertBottomY); // x -13 - -3.2, z 4 - 19.5
            Slab(c, "Bank_AR", bankX, DesertCanyonBankTopY, 11.75f, bankWidth, 15.5f, pad, m.Pillar, DesertBottomY);  // x 3.2 - 13
            DesertTumbleweedLane(c, m, "Tumbleweed_A1", 6.8f, 1f, 5.0f, 3, 0.0f);    // from the left
            DesertTumbleweedLane(c, m, "Tumbleweed_A2", 10.2f, -1f, 6.0f, 3, 0.9f);  // from the right, fastest
            DesertTumbleweedLane(c, m, "Tumbleweed_A3", 13.6f, 1f, 4.5f, 3, 1.7f);   // from the left, slowest
            DesertTumbleweedLane(c, m, "Tumbleweed_A4", 17.0f, -1f, 5.5f, 3, 0.4f);  // from the right
            DesertCanyonDressing(c, m);

            // B (z 20 - 33): four sandstone pillars that crumble 0.55 s after the first step, each 0.3 m higher.
            DesertCrumblePillar(c, m, "Crumble_1", 0.0f, 0.3f, 21.0f, 1.4f);
            DesertCrumblePillar(c, m, "Crumble_2", 1.1f, 0.6f, 24.6f, 1.3f);
            DesertCrumblePillar(c, m, "Crumble_3", -0.3f, 0.9f, 28.3f, 1.2f);
            DesertCrumblePillar(c, m, "Crumble_4", 0.7f, 1.2f, 32.0f, 1.2f);
            AddCheckpoint(c, "Checkpoint_1", 0f, 1.4f, 36.6f, 5f, m.Pillar, DesertBottomY);

            // C (z 40.1 - 61.5): a narrow 1.8 m path winding in three segments (centre x 0 / 0.6 / 0, overlapping 0.4 m),
            // nine cacti alternating sides 2.2 m apart (0.54 m sidestep each time, 1.14 m across the two bends); a gentle
            // sandstorm (+X, 1.4 m/s, on 1.2 s / off 2 s) over the middle segment.
            DesertCactusWeave(c, m, "Ledge_C", 1.4f, 40.1f, 1.8f, new[] { 0f, 0.6f, 0f }, 7.4f, 3);
            DesertSandstorm(c, m, "Sandstorm_C", new Vector3(0.6f, 1.4f + 1.75f, 50.8f), new Vector3(6f, 3.5f, 7.4f),
                new Vector3(DesertSandstorm1Wind, 0f, 0f), DesertSandstorm1OnSeconds, DesertSandstorm1OffSeconds, 0f, 5101);

            // D (z 62.4 - 71.1): a sand geyser vent (z 62.4 - 64.8, 0.9 m past the path) erupts every 3 s after a 0.9 s
            // rumble and launches whoever stands on it 3.9 m up: the only way onto checkpoint 2, 3.0 m higher, 1.3 m ahead.
            DesertGeyser(c, m, "Geyser_D", 0f, 1.4f, 63.6f, 2.4f, 0.0f);
            AddCheckpoint(c, "Checkpoint_2", 0f, 4.4f, 68.6f, 5f, m.Pillar, DesertBottomY);          // z 66.1 - 71.1

            // E (z 71.5 - 95.3): a sandstone turnstile (two crossed 6.4 m arms turning 36 deg/s round a post; tips pass
            // 0.4 m from the pads, an arm lines up every 2.5 s) carries you to a landing, then a 14 m sandstone bridge
            // whose 1.5 m spans crumble 0.35 s after you step on each (keep running), with one solid pier halfway.
            DesertTurnstile(c, m, "Turnstile_E", 0f, 4.4f, 74.7f, 0f);                               // sweeps z 71.5 - 77.9
            Slab(c, "Pad_E1", 0f, 4.4f, 79.8f, 4f, 3f, pad, m.Pillar, DesertBottomY);                // z 78.3 - 81.3
            float spanZ = 81.3f + DesertBridgeSpanLength * 0.5f;
            for (int i = 0; i < 4; i++, spanZ += DesertBridgeSpanLength)
                DesertBridgeSpan(c, m, "Bridge_E" + (i + 1), 0f, 4.4f, spanZ, i % 2 == 0 ? 6f : -6f);  // z 81.3 - 87.3
            Slab(c, "Bridge_Pier", 0f, 4.4f, 88.3f, DesertBridgeWidth, 2.0f, pad, m.Pillar, DesertBottomY); // z 87.3 - 89.3, solid
            spanZ = 89.3f + DesertBridgeSpanLength * 0.5f;
            for (int i = 4; i < 8; i++, spanZ += DesertBridgeSpanLength)
                DesertBridgeSpan(c, m, "Bridge_E" + (i + 1), 0f, 4.4f, spanZ, i % 2 == 0 ? 6f : -6f);  // z 89.3 - 95.3
            AddFinish(c, 0f, 4.4f, 98.3f, 6f, 6f, m.Pillar, DesertBottomY);                          // z 95.3 - 101.3

            // Secret branch off checkpoint 2 toward +X: a 3.4 m flat jump to a small geyser vent (out of phase with the
            // main one), which launches you onto the Shard pillar 3.2 m up and 2.2 m on. Back the way you came.
            DesertGeyser(c, m, "Secret_Geyser", 6.5f, 4.4f, 68.6f, 1.2f, 1.5f);                      // x 5.9 - 7.1
            Slab(c, "Secret_Top", 9.9f, 7.6f, 68.6f, 1.2f, 1.2f, pad, m.Pillar, DesertBottomY);      // x 9.3 - 10.5
            AddShard(c, c.MapId, new Vector3(9.9f, 8.7f, 68.6f), m.Shard, 0.4f, 0.8f);

            RockArch(c, m, 0f, 27f, 7f, 10f);      // over the crumbling pillars
            RockArch(c, m, 0f, 63.6f, 5f, 13f);    // over the geyser (capstone above the launch apex)
            RockArch(c, m, 0f, 88.3f, 5f, 12f);    // over the bridge pier
            RockArch(c, m, 19f, 92f, 5.5f, 9f);    // side arch beside the bridge
            DesertScenery(c, m, -15f, 118f, 5001);
        }

        // ------------------------------------------------------------------ level 2: Quicksand pits

        private static void BuildDesert2(Context c)
        {
            var m = new DesertMats();
            PrepareDesert(c, m);
            StartIsland(c, 6f, 6f, DesertBottomY);
            Material pad = c.In.PlatformMaterial;

            // A (z 4.8 - 25): five sinking slabs (1.2 m/s, 1.7 - 2.3 s from rest to under), 2.4 -> 2.0 m wide,
            // cacti standing on two of them (they sink with the slab) so the landing line has to bend.
            SinkSlab(c, m, "Sink_A1", 0.0f, 0.0f, 6.0f, 2.4f, 2.4f, DesertSinkChainSpeed);
            GameObject a2 = SinkSlab(c, m, "Sink_A2", 1.2f, 0.2f, 10.5f, 2.2f, 2.2f, DesertSinkChainSpeed);
            Cactus(c, m, "Cactus_A2", a2.transform, 1.25f, 0.2f, 10.9f, 1, 0.8f, 0f, true);    // pass on the left
            SinkSlab(c, m, "Sink_A3", -0.6f, 0.4f, 15.0f, 2.2f, 2.2f, DesertSinkChainSpeed);
            GameObject a4 = SinkSlab(c, m, "Sink_A4", 0.8f, 0.6f, 19.5f, 2.0f, 2.0f, DesertSinkChainSpeed);
            Cactus(c, m, "Cactus_A4", a4.transform, 0.2f, 0.6f, 19.9f, 1, 0.8f, 180f, true);   // pass on the right
            SinkSlab(c, m, "Sink_A5", -0.4f, 0.8f, 24.0f, 2.0f, 2.0f, DesertSinkChainSpeed);
            AddCheckpoint(c, "Checkpoint_1", 0f, 1.0f, 29.8f, 5f, m.Pillar, DesertBottomY);

            // B (z 34.5 - 59.2): three sand ferries sliding over the quicksand (1 and 2 side to side in opposite
            // phase, 3 toward and away), then a long sinking slab with tumbleweeds rolling down two lanes at the
            // player (the lanes cover its whole width: jump each ball). Checkpoint 2 sits off the slab's +X side,
            // before the end where the balls appear.
            DesertSandFerry(c, m, "Ferry_1", 0f, 1.0f, 35.6f, 2.2f, new Vector3(2.0f, 0f, 0f), 4.0f, 0.0f);
            DesertSandFerry(c, m, "Ferry_2", 0f, 1.2f, 40.1f, 2.2f, new Vector3(2.0f, 0f, 0f), 4.0f, 2.0f);
            DesertSandFerry(c, m, "Ferry_3", 0f, 1.4f, 45.6f, 2.2f, new Vector3(0f, 0f, 1.2f), 3.0f, 0.0f);
            GameObject longSlab = SinkSlab(c, m, "Sink_Long", 0f, 1.6f, 54.15f, 2.4f, 10.1f, DesertSinkLongSpeed); // z 49.1 - 59.2
            // The balls come over a dune behind the slab's far end (hidden until they crest it), roll the slab's
            // length and drop off its near end into the quicksand: nothing pops in or out on the slab itself.
            DesertSlabDune(c, m, "Sink_Long_Dune", 0f, 1.6f, 59.2f);
            DesertSlabTumbleweed(c, m, "Tumbleweed_L", -0.6f, 1.6f, 59.2f, 49.1f, 5.0f, 1.0f, 0.0f, longSlab.transform);
            DesertSlabTumbleweed(c, m, "Tumbleweed_R", 0.6f, 1.6f, 59.2f, 49.1f, 6.0f, 1.3f, 1.4f, longSlab.transform);
            AddCheckpoint(c, "Checkpoint_2", 5.0f, 1.6f, 55.0f, 5f, m.Pillar, DesertBottomY);    // x 2.5 - 7.5, z 52.5 - 57.5

            // C (precision, z 59.5 - 84): a 1 m ledge in a sandstorm gust (push toward -X, on 1.2 s, off 1.6 s), a narrow
            // 1.6 m path winding in two segments (centre x 2.6 then 2.2) past four cacti on alternating sides (0.54 m
            // sidesteps, 0.94 m across the bend), two crumbling pillars, and one last sinking slab with a cactus on it.
            Slab(c, "Ledge_C1", 4.4f, 1.8f, 62.5f, 1.0f, 6.0f, pad, m.Pillar, DesertBottomY);   // z 59.5 - 65.5
            DesertSandstorm(c, m, "Sandstorm_C1", new Vector3(4.4f, 1.8f + 1.75f, 62.5f), new Vector3(5f, 3.5f, 7f),
                new Vector3(-DesertSandstorm2Wind, 0f, 0f), DesertSandstorm2OnSeconds, DesertSandstorm2OffSeconds, 0f, 5201);
            DesertCactusWeave(c, m, "Ledge_C2", 2.0f, 66.9f, 1.6f, new[] { 2.6f, 2.2f }, 4.6f, 2);     // z 66.9 - 75.7
            DesertCrumblePillar(c, m, "Crumble_1", 1.0f, 2.2f, 77.4f, 1.2f);                          // z 76.8 - 78.0
            DesertCrumblePillar(c, m, "Crumble_2", 0.4f, 2.4f, 80.0f, 1.1f);                          // z 79.45 - 80.55
            GameObject last = SinkSlab(c, m, "Sink_D1", 0.2f, 2.6f, 82.7f, 2.4f, 2.4f, DesertSinkChainSpeed);  // z 81.5 - 83.9
            Cactus(c, m, "Cactus_D1", last.transform, -0.4f, 2.6f, 83.0f, 1, 0.8f, 180f, true);         // pass on the right

            // Finish: a 9 x 11 m deck, step and weather dais (dais centre z 95.2), about 11 m clear of the last
            // obstacle and 4.5 m of open ground on each side for the suction minigame.
            AddFinish(c, 0.2f, 2.6f, 91.8f, 9f, 11f, m.Pillar, DesertBottomY, weatherDais: true);

            // Secret branch off checkpoint 2 toward +X and back: two small fast-sinking slabs and a static Shard
            // pillar, 3.37 - 3.46 m gaps with 0.2 m rises (plus however far the slab under you has sunk).
            SinkSlab(c, m, "Secret_1", 11.55f, 1.8f, 55.5f, 1.3f, 1.3f, DesertSinkSecretSpeed);
            SinkSlab(c, m, "Secret_2", 15.6f, 2.0f, 52.1f, 1.3f, 1.3f, DesertSinkSecretSpeed);
            Slab(c, "Secret_3", 14.0f, 2.2f, 47.5f, 1.2f, 1.2f, pad, m.Pillar, DesertBottomY);
            AddShard(c, c.MapId, new Vector3(14.0f, 3.3f, 47.5f), m.Shard, 0.4f, 0.8f);

            RockArch(c, m, 0f, 47.5f, 7f, 11f);  // over the third ferry and the long slab
            RockArch(c, m, 2.0f, 67f, 6f, 12f);  // over the thin ledges
            RockArch(c, m, -20f, 88f, 5.5f, 9f); // side arch beside the last slab
            DesertScenery(c, m, -15f, 112f, 5002);
        }

        // ------------------------------------------------------------------ section builders (play-test pass)

        /// <summary>
        /// One lane of the level 1 tumbleweed canyon at <paramref name="z"/>: <paramref name="count"/> tumbleweeds evenly
        /// spaced in time on the same bent path (<see cref="RollingHazard"/> waypoints). Each one appears below the crest
        /// on the far side of the start bank (hidden by the dune), rolls over the bank top, drops into the canyon, crosses
        /// the path, bounces up onto the other bank, rolls across it and vanishes over its far side.
        /// <paramref name="direction"/> +1 rolls toward +X (from the left bank), -1 toward -X.
        /// </summary>
        private static void DesertTumbleweedLane(Context c, DesertMats m, string name, float z, float direction, float speed, int count, float phaseOffset)
        {
            float s = direction < 0f ? -1f : 1f;
            float r = TumbleweedRadius;
            float crest = DesertCanyonBankTopY + r;                 // ball centre rolling on a bank top
            float hidden = DesertCanyonBankTopY - DesertCanyonHideDepth + r;
            float bankEdge = DesertCanyonBankInnerX + 0.3f;         // centre at the inner edge of a bank
            float lip = DesertCanyonPathHalfWidth + 0.3f;           // centre just off the path edge (path top is 0)

            var points = new List<Vector3>
            {
                new Vector3(-s * DesertCanyonHideX, hidden, z),
                new Vector3(-s * DesertCanyonBankOuterX, crest, z),
                new Vector3(-s * bankEdge, crest, z),
            };
            for (int i = 1; i <= 3; i++)
            {
                // Falls off the bank into the canyon (accelerating drop), landing just off the near path edge.
                float f = i / 3f;
                points.Add(new Vector3(-s * Mathf.Lerp(bankEdge, lip, f), crest - (crest - r) * f * f, z));
            }
            points.Add(new Vector3(s * lip, r, z));
            for (int i = 1; i <= 4; i++)
            {
                // Bounces off the far path edge up onto the other bank (a hop over the moat).
                float f = i / 4f;
                points.Add(new Vector3(s * Mathf.Lerp(lip, bankEdge + 0.3f, f), r + (crest - r) * f + DesertCanyonHopHeight * 4f * f * (1f - f), z));
            }
            points.Add(new Vector3(s * DesertCanyonBankOuterX, crest, z));
            points.Add(new Vector3(s * DesertCanyonHideX, hidden, z));

            Vector3 home = points[0];
            var waypoints = new Vector3[points.Count - 1];
            float runLength = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                waypoints[i - 1] = points[i] - home;
                runLength += Vector3.Distance(points[i - 1], points[i]);
            }
            float cycle = runLength / speed + DesertCanyonRollDelay;
            for (int i = 0; i < count; i++)
            {
                Tumbleweed(c, m, name + "_" + (i + 1), home.x, home.y - r, z, waypoints[waypoints.Length - 1], speed, DesertCanyonRollDelay,
                    phaseOffset + cycle * i / count, null, waypoints);
            }
        }

        /// <summary>Visual dressing on the canyon's dune banks: red rocks between the lanes and dry bushes (no colliders).</summary>
        private static void DesertCanyonDressing(Context c, DesertMats m)
        {
            // Rocks sit between lanes (lanes at z 6.8 / 10.2 / 13.6 / 17.0), clear of every ball's path (+-0.6 m).
            float[] rockX = { -7.5f, -10.5f, -6.0f, 6.5f, 9.5f, 7.8f };
            float[] rockZ = { 8.5f, 11.9f, 15.3f, 8.5f, 11.9f, 15.3f };
            for (int i = 0; i < rockX.Length; i++)
            {
                PlaceModel("Rock_1_K", new Vector3(rockX[i], DesertCanyonBankTopY - 0.3f, rockZ[i]), new Vector3(0f, 40f + i * 67f, 0f),
                    new Vector3(0.42f, 0.45f, 0.4f), m.RedRock, c.Scenery);
            }
            // Bushes at the bank ends, outside the lanes.
            float[] bushX = { -5.0f, -9.0f, -11.5f, 5.0f, 8.5f, 11.5f };
            float[] bushZ = { 5.0f, 18.6f, 5.2f, 18.6f, 5.0f, 18.8f };
            for (int i = 0; i < bushX.Length; i++)
            {
                PlaceModel("Bush_3_A", new Vector3(bushX[i], DesertCanyonBankTopY, bushZ[i]), new Vector3(0f, i * 53f, 0f), Vector3.one * 1.3f,
                    null, c.Scenery, 5); // dry golden bush
            }
        }

        /// <summary>
        /// A narrow path winding in straight segments (each <paramref name="segmentLength"/> long, centred on
        /// <paramref name="centersX"/>, overlapping the next by <see cref="DesertWeaveOverlap"/>) with
        /// <paramref name="cactiPerSegment"/> hazard cacti per segment on alternating sides of its centre line
        /// (<see cref="DesertWeaveCactusOffset"/>, arms pointing out over the edge), so the player zigzags between them.
        /// The first cactus stands on the +X side. Segment tops alternate by <see cref="DesertWeaveTopStagger"/>.
        /// </summary>
        private static void DesertCactusWeave(Context c, DesertMats m, string name, float topY, float zStart, float width,
            float[] centersX, float segmentLength, int cactiPerSegment)
        {
            float step = segmentLength - DesertWeaveOverlap;
            int cactus = 0;
            for (int k = 0; k < centersX.Length; k++)
            {
                float cz = zStart + segmentLength * 0.5f + step * k;
                float top = topY + (k % 2 == 1 ? DesertWeaveTopStagger : 0f);
                Slab(c, name + "_" + (k + 1), centersX[k], top, cz, width, segmentLength, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
                for (int j = 0; j < cactiPerSegment; j++, cactus++)
                {
                    float side = cactus % 2 == 0 ? 1f : -1f;
                    float z = cz + (j - (cactiPerSegment - 1) * 0.5f) * DesertWeaveCactusSpacing;
                    Cactus(c, m, name + "_Cactus_" + (cactus + 1), c.Course, centersX[k] + side * DesertWeaveCactusOffset, top, z,
                        1, DesertWeaveCactusScale, side > 0f ? 0f : 180f, true);
                }
            }
        }

        /// <summary>
        /// A sand geyser vent (<see cref="DesertSandGeyser"/>): a sand pad with a dark vent ring that rumbles and puffs,
        /// then erupts a sand column and launches a rider <see cref="DesertGeyserLaunchSpeed"/> m/s upward, every
        /// <see cref="DesertGeyserPeriod"/> s (<paramref name="phaseSeconds"/> staggers it). The collider reaches down to
        /// the sand so its sides are solid.
        /// </summary>
        private static DesertSandGeyser DesertGeyser(Context c, DesertMats m, string name, float x, float topY, float z, float size, float phaseSeconds)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            float depth = Mathf.Max(0.6f, topY - SandTopY);
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(size, depth, size);
            box.center = new Vector3(0f, -depth * 0.5f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            if (GrassIsland(c, name + "_Island", x, topY, z, size, size, DesertBottomY, null, visual.transform) == null)
            {
                float height = topY - DesertBottomY;
                Prim(PrimitiveType.Cylinder, "Body", visual.transform, new Vector3(0f, -height * 0.5f, 0f),
                    new Vector3(size, height * 0.5f, size), m.Pillar);
            }
            // Vent: a dark ring with loose sand in the middle, so the pad reads as a geyser before it ever erupts.
            Prim(PrimitiveType.Cylinder, "VentRing", visual.transform, new Vector3(0f, 0.02f, 0f), new Vector3(size * 0.62f, 0.02f, size * 0.62f), m.Crack);
            Prim(PrimitiveType.Cylinder, "VentSand", visual.transform, new Vector3(0f, 0.035f, 0f), new Vector3(size * 0.42f, 0.02f, size * 0.42f), m.Sand);

            // Column: pivot at the vent, a unit-tall cylinder above it, scaled to full height (the script scales Y from 0).
            var column = new GameObject("Column");
            column.transform.SetParent(root.transform, false);
            Prim(PrimitiveType.Cylinder, "SandColumn", column.transform, new Vector3(0f, 0.5f, 0f), new Vector3(size * 0.45f, 0.5f, size * 0.45f), m.SandStreak);
            column.transform.localScale = new Vector3(1f, DesertGeyserColumnHeight, 1f);
            GameObject puff = Prim(PrimitiveType.Sphere, "SandPuff", root.transform, new Vector3(0f, 0.12f, 0f),
                new Vector3(size * 0.65f, 0.45f, size * 0.65f), m.SandStreak);

            root.AddComponent<RideableSurface>();
            var geyser = root.AddComponent<DesertSandGeyser>();
            SetFields(geyser,
                ("periodSeconds", DesertGeyserPeriod),
                ("warningSeconds", DesertGeyserWarning),
                ("eruptSeconds", DesertGeyserErupt),
                ("phaseSeconds", phaseSeconds),
                ("launchVelocity", new Vector3(0f, DesertGeyserLaunchSpeed, 0f)),
                ("column", column.transform),
                ("warningPuff", puff.transform),
                ("rumbleVisual", visual.transform));
            return geyser;
        }

        /// <summary>
        /// A sandstone turnstile over the quicksand: two crossed arms (<see cref="DesertTurnstileArmLength"/> tip to tip)
        /// turning round a vertical post (<see cref="Spinner"/>, <see cref="DesertTurnstileDegreesPerSecond"/>); the
        /// player rides an arm (<see cref="RideableSurface"/> carries them round) and steps off when it lines up with the
        /// next pad. Dark tips mark the arm ends. The post under it is scenery.
        /// </summary>
        private static GameObject DesertTurnstile(Context c, DesertMats m, string name, float x, float topY, float z, float phaseDegrees)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            const float t = DesertTurnstileThickness;
            const float length = DesertTurnstileArmLength;
            const float width = DesertTurnstileArmWidth;
            for (int i = 0; i < 2; i++)
            {
                bool alongX = i == 0;
                Prim(PrimitiveType.Cube, alongX ? "Arm_X" : "Arm_Z", root.transform, new Vector3(0f, -t * 0.5f, 0f),
                    alongX ? new Vector3(length, t, width) : new Vector3(width, t, length), m.Pillar, default, true);
                Prim(PrimitiveType.Cube, "Top", root.transform, new Vector3(0f, 0.01f, 0f),
                    alongX ? new Vector3(length - 0.1f, 0.03f, width - 0.25f) : new Vector3(width - 0.25f, 0.03f, length - 0.1f), m.Sandstone);
                for (int s = -1; s <= 1; s += 2)
                {
                    float tip = s * (length * 0.5f - 0.25f);
                    Prim(PrimitiveType.Cube, "Tip", root.transform, alongX ? new Vector3(tip, 0.02f, 0f) : new Vector3(0f, 0.02f, tip),
                        alongX ? new Vector3(0.4f, 0.04f, width - 0.1f) : new Vector3(width - 0.1f, 0.04f, 0.4f), m.Crack);
                }
            }
            Prim(PrimitiveType.Cylinder, "Hub", root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(1.7f, 0.03f, 1.7f), m.Crack);

            float postTop = topY - t;
            float postBottom = SandTopY - 0.3f;
            Prim(PrimitiveType.Cylinder, name + "_Post", c.Scenery, new Vector3(x, (postTop + postBottom) * 0.5f, z),
                new Vector3(1.0f, (postTop - postBottom) * 0.5f, 1.0f), m.Pillar);

            root.AddComponent<RideableSurface>();
            var spinner = root.AddComponent<Spinner>();
            SetFields(spinner, ("axis", Vector3.up), ("degreesPerSecond", DesertTurnstileDegreesPerSecond), ("phaseDegrees", phaseDegrees));
            return root;
        }

        /// <summary>
        /// One span of a crumbling sandstone bridge (<see cref="CollapsingPlatform"/>): it gives way
        /// <see cref="DesertBridgeStandSeconds"/> after the first step (whether or not you stay), drops under the
        /// quicksand tilting by <paramref name="tiltDegrees"/>, and rises back. Spans placed end to end crumble in sequence
        /// behind a running player. Cracks on top mark it as crumbling.
        /// </summary>
        private static GameObject DesertBridgeSpan(Context c, DesertMats m, string name, float x, float topY, float z, float tiltDegrees)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            const float t = DesertBridgeThickness;
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(DesertBridgeWidth, t, DesertBridgeSpanLength);
            box.center = new Vector3(0f, -t * 0.5f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            float drawnLength = DesertBridgeSpanLength - DesertBridgeJoint;
            Prim(PrimitiveType.Cube, "Block", visual.transform, new Vector3(0f, -t * 0.5f, 0f), new Vector3(DesertBridgeWidth, t, drawnLength), m.Pillar);
            Prim(PrimitiveType.Cube, "Top", visual.transform, new Vector3(0f, 0.01f, 0f),
                new Vector3(DesertBridgeWidth - 0.12f, 0.03f, drawnLength - 0.12f), m.Sandstone);
            Prim(PrimitiveType.Cube, "Crack", visual.transform, new Vector3(-0.15f, 0.03f, 0.1f), new Vector3(0.06f, 0.03f, 1.0f), m.Crack, new Vector3(0f, 28f, 0f));
            Prim(PrimitiveType.Cube, "Crack", visual.transform, new Vector3(0.25f, 0.03f, -0.2f), new Vector3(0.06f, 0.03f, 0.7f), m.Crack, new Vector3(0f, -55f, 0f));

            root.AddComponent<RideableSurface>();
            var platform = root.AddComponent<CollapsingPlatform>();
            SetFields(platform,
                ("standSeconds", DesertBridgeStandSeconds),
                ("requireContinuousStand", false),
                ("visual", visual.transform),
                ("shakeAmplitude", DesertCrumbleShakeAmplitude),
                ("fallSeconds", DesertCrumbleFallSeconds),
                ("fallDistance", topY - SandTopY + 1.5f),
                ("tiltDegrees", tiltDegrees),
                ("goneSeconds", DesertCrumbleGoneSeconds),
                ("returnSeconds", DesertCrumbleReturnSeconds));
            return root;
        }

        // ------------------------------------------------------------------ obstacle builders

        /// <summary>
        /// A floating stone slab that sinks while the player stands on it and rises when they leave
        /// (<see cref="SinkingPlatform"/>). Sinking under the quicksand respawns the player.
        /// </summary>
        private static GameObject SinkSlab(Context c, DesertMats m, string name, float x, float topY, float z, float sizeX, float sizeZ, float sinkSpeed)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(sizeX, SinkSlabThickness, sizeZ);
            box.center = new Vector3(0f, -SinkSlabThickness * 0.5f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            GameObject island = GrassIsland(c, name + "_Island", x, topY, z, sizeX, sizeZ, topY - SinkSlabThickness, null, visual.transform);
            if (island == null)
            {
                Prim(PrimitiveType.Cylinder, "Slab", visual.transform, new Vector3(0f, -SinkSlabThickness * 0.5f, 0f),
                    new Vector3(sizeX, SinkSlabThickness * 0.5f, sizeZ), m.Pillar);
            }

            root.AddComponent<RideableSurface>();
            var sinking = root.AddComponent<SinkingPlatform>();
            SetFields(sinking, ("sinkSpeed", sinkSpeed), ("riseSpeed", DesertSinkRiseSpeed), ("maxDepth", topY - SandTopY + 3f));
            return root;
        }

        // Dune behind a slab's far end that tumbleweeds roll in over (metres).
        private const float SlabDuneRise = 1.0f;       // crest height above the slab top
        private const float SlabDuneDepth = 5.0f;      // along Z
        private const float SlabDuneWidth = 4.4f;      // along X
        private const float SlabDuneGap = 0.5f;        // slab far end to the dune's near foot

        /// <summary>
        /// A rounded sand dune rising out of the quicksand just past a slab's far end (<paramref name="farZ"/>, slab
        /// running toward -Z), its crest <see cref="SlabDuneRise"/> above the slab top. Visual only, no collider:
        /// tumbleweeds hide behind it and roll over it onto the slab.
        /// </summary>
        private static void DesertSlabDune(Context c, DesertMats m, string name, float x, float slabTopY, float farZ)
        {
            float crestY = slabTopY + SlabDuneRise;
            float halfHeight = (crestY - SandTopY) + 1f;   // reaches 1 m under the quicksand
            Prim(PrimitiveType.Sphere, name, c.Course, new Vector3(x, crestY - halfHeight, farZ + SlabDuneGap + SlabDuneDepth * 0.5f),
                new Vector3(SlabDuneWidth, halfHeight * 2f, SlabDuneDepth), m.Sandstone); // plain colour: the palette material stripes on a sphere
        }

        /// <summary>
        /// A tumbleweed for a sinking slab (toward -Z): it starts hidden inside the dune behind the slab's far end
        /// (<see cref="DesertSlabDune"/>), rolls over the crest and down onto the slab at <paramref name="farZ"/>,
        /// along the slab to <paramref name="nearZ"/>, then drops off the end and sinks into the quicksand.
        /// </summary>
        private static void DesertSlabTumbleweed(Context c, DesertMats m, string name, float x, float slabTopY, float farZ, float nearZ,
            float speed, float restartDelay, float phase, Transform slab)
        {
            float r = TumbleweedRadius;
            float duneZ = farZ + SlabDuneGap + SlabDuneDepth * 0.5f;
            var points = new List<Vector3>
            {
                new Vector3(x, slabTopY - 1.8f + r, duneZ + 1.2f),            // hidden in the dune's back slope, below the crest line
                new Vector3(x, slabTopY + SlabDuneRise + r, duneZ),           // over the crest
                new Vector3(x, slabTopY + 0.35f + r, farZ + 0.3f),            // down the near slope
                new Vector3(x, slabTopY + r, farZ - 0.4f),                    // onto the slab
                new Vector3(x, slabTopY + r, nearZ + 0.3f),                   // the slab's length
                new Vector3(x, slabTopY + r - 0.5f, nearZ - 0.4f),            // tips off the end
                new Vector3(x, SandTopY - r - 0.3f, nearZ - 0.9f),            // sinks into the quicksand
            };
            Vector3 home = points[0];
            var waypoints = new Vector3[points.Count - 1];
            for (int i = 1; i < points.Count; i++) waypoints[i - 1] = points[i] - home;
            Tumbleweed(c, m, name, home.x, home.y - r, home.z, waypoints[waypoints.Length - 1], speed, restartDelay, phase, slab, waypoints);
        }

        /// <summary>
        /// A tumbleweed that rolls along <paramref name="travel"/> (metres from its start, any horizontal direction)
        /// and starts again after a pause (<see cref="RollingHazard"/>); touching it respawns the player. It can be
        /// parented to a platform (a sinking slab) so it moves with it. With <paramref name="waypoints"/> (relative to the
        /// start, parent space) it follows that bent path instead of <paramref name="travel"/>. Stand-in model: three crossed KayKit bushes
        /// in colour set 5 (dry golden).
        /// </summary>
        private static GameObject Tumbleweed(Context c, DesertMats m, string name, float x, float surfaceY, float z,
            Vector3 travel, float speed, float restartDelay, float phase, Transform parent, Vector3[] waypoints = null)
        {
            Vector3 center = new Vector3(x, surfaceY + TumbleweedRadius, z);
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = center;

            var hit = root.AddComponent<SphereCollider>();
            hit.isTrigger = true;
            hit.radius = TumbleweedHitRadius;
            c.Result.Hazards.Add(root.AddComponent<KillVolume>());

            var spin = new GameObject("Spin");
            spin.transform.SetParent(root.transform, false);

            // Bush_3_A is about 1 m wide and 0.5 m tall; three crossed copies make a ball about 1.2 m across.
            const float bushScale = 1.2f;
            const float halfHeight = 0.25f * bushScale;
            GameObject a = PlaceModel("Bush_3_A", center + Vector3.down * halfHeight, Vector3.zero, Vector3.one * bushScale, null, spin.transform, 5); // golden dry bush colours
            if (a == null)
            {
                Prim(PrimitiveType.Sphere, "Ball", spin.transform, Vector3.zero, Vector3.one * (TumbleweedRadius * 2f), m.Dry);
            }
            else
            {
                PlaceModel("Bush_3_A", center + new Vector3(0f, 0f, -halfHeight), new Vector3(90f, 0f, 0f), Vector3.one * bushScale, null, spin.transform, 5);
                PlaceModel("Bush_3_A", center + new Vector3(halfHeight, 0f, 0f), new Vector3(0f, 0f, 90f), Vector3.one * bushScale, null, spin.transform, 5);
            }

            // Rolling along direction d turns the ball around up x d (toward -Z: (-1,0,0); toward +X: (0,0,-1)).
            Vector3 flat = new Vector3(travel.x, 0f, travel.z);
            Vector3 rollDirection = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.back;
            var roll = root.AddComponent<RollingHazard>();
            SetFields(roll,
                ("travel", travel),
                ("speed", speed),
                ("restartDelaySeconds", restartDelay),
                ("phaseSeconds", phase),
                ("spinVisual", spin.transform),
                ("spinAxis", Vector3.Cross(Vector3.up, rollDirection)),
                ("rollRadius", TumbleweedRadius));
            // A bent path (canyon lanes) replaces travel; the roll axis then follows each leg.
            if (waypoints != null && waypoints.Length > 0) SetFields(roll, ("waypoints", waypoints));

            if (parent != null) root.transform.SetParent(parent, true);
            return root;
        }

        /// <summary>
        /// A sandstone pillar that crumbles (<see cref="CollapsingPlatform"/>): stepping on it starts a
        /// <see cref="DesertCrumbleStandSeconds"/> countdown that keeps running if the player leaves; it shakes, drops
        /// under the quicksand, stays gone <see cref="DesertCrumbleGoneSeconds"/> and rises back. Dark cracks on its top
        /// tell it apart from the solid pillars. Its collider reaches down to the sand so its sides are solid.
        /// </summary>
        private static GameObject DesertCrumblePillar(Context c, DesertMats m, string name, float x, float topY, float z, float size)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            float depth = Mathf.Max(0.6f, topY - SandTopY);
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(size, depth, size);
            box.center = new Vector3(0f, -depth * 0.5f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            GameObject island = GrassIsland(c, name + "_Island", x, topY, z, size, size, DesertBottomY, null, visual.transform);
            if (island == null)
            {
                float height = topY - DesertBottomY;
                Prim(PrimitiveType.Cylinder, "Pillar", visual.transform, new Vector3(0f, -height * 0.5f, 0f),
                    new Vector3(size, height * 0.5f, size), m.Pillar);
            }

            // Crack lines on top (fixed pattern scaled to the top), under the visual so they shake and fall with it.
            float[] angles = { 25f, 80f, 135f, 165f };
            float[] offsetX = { -0.2f, 0.18f, -0.05f, 0.25f };
            float[] offsetZ = { 0.15f, -0.18f, 0.25f, -0.05f };
            float[] lengths = { 0.75f, 0.55f, 0.6f, 0.45f };
            for (int i = 0; i < angles.Length; i++)
            {
                Prim(PrimitiveType.Cube, "Crack", visual.transform, new Vector3(offsetX[i] * size, 0.02f, offsetZ[i] * size),
                    new Vector3(0.06f, 0.03f, lengths[i] * size), m.Crack, new Vector3(0f, angles[i], 0f));
            }

            root.AddComponent<RideableSurface>();
            var platform = root.AddComponent<CollapsingPlatform>();
            SetFields(platform,
                ("standSeconds", DesertCrumbleStandSeconds),
                ("requireContinuousStand", false),
                ("visual", visual.transform),
                ("shakeAmplitude", DesertCrumbleShakeAmplitude),
                ("fallSeconds", DesertCrumbleFallSeconds),
                ("fallDistance", topY - SandTopY + 1.5f),
                ("goneSeconds", DesertCrumbleGoneSeconds),
                ("returnSeconds", DesertCrumbleReturnSeconds));
            return root;
        }

        /// <summary>
        /// A sand ferry: a square sandstone block ploughing through the quicksand back and forth
        /// (<see cref="PlatformMover"/>, sine motion of <paramref name="travel"/> around its start) that carries the
        /// player (<see cref="RideableSurface"/>). It does not sink. The walkable collider is the top
        /// <see cref="DesertFerryColliderDepth"/> metres; the drawn body reaches just under the sand.
        /// </summary>
        private static GameObject DesertSandFerry(Context c, DesertMats m, string name, float x, float topY, float z, float size,
            Vector3 travel, float periodSeconds, float phaseSeconds)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(size, DesertFerryColliderDepth, size);
            box.center = new Vector3(0f, -DesertFerryColliderDepth * 0.5f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            float bottomY = SandTopY - 0.4f;
            if (GrassIsland(c, name + "_Island", x, topY, z, size, size, bottomY, null, visual.transform) == null)
            {
                float height = topY - bottomY;
                Prim(PrimitiveType.Cylinder, "Body", visual.transform, new Vector3(0f, -height * 0.5f, 0f),
                    new Vector3(size, height * 0.5f, size), m.Pillar);
            }

            root.AddComponent<RideableSurface>();
            var mover = root.AddComponent<PlatformMover>();
            SetFields(mover, ("travel", travel), ("periodSeconds", periodSeconds), ("phaseSeconds", phaseSeconds));
            return root;
        }

        /// <summary>
        /// A sandstorm gust: a trigger box (<see cref="WindGust"/>) that pushes the player along X while it blows
        /// (on / off rhythm, absolute time) and shows drifting sand streaks only while it blows (<see cref="StreakDrift"/>),
        /// so the next gust can be read before stepping in. The player can walk against it (run speed 6).
        /// </summary>
        private static WindGust DesertSandstorm(Context c, DesertMats m, string name, Vector3 center, Vector3 size, Vector3 wind,
            float onSeconds, float offSeconds, float phaseSeconds, int seed)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = center;
            var zone = root.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = size;

            // Streaks lie along X with no X offset (StreakDrift moves them along X and wraps them).
            var streakRoot = new GameObject("SandStreaks");
            streakRoot.transform.SetParent(root.transform, false);
            var rng = new System.Random(seed);
            for (int i = 0; i < DesertSandstormStreakCount; i++)
            {
                var position = new Vector3(0f, ((float)rng.NextDouble() - 0.5f) * size.y * 0.8f, ((float)rng.NextDouble() - 0.5f) * size.z * 0.9f);
                float halfLength = 0.5f + (float)rng.NextDouble() * 0.5f;
                Prim(PrimitiveType.Cylinder, "Streak", streakRoot.transform, position, new Vector3(0.07f, halfLength, 0.07f),
                    m.SandStreak, new Vector3(0f, 0f, 90f));
            }
            var drift = streakRoot.AddComponent<StreakDrift>();
            SetFields(drift, ("localDirection", new Vector3(wind.x < 0f ? -1f : 1f, 0f, 0f)), ("speed", 13f), ("loopLength", size.x));

            var gust = root.AddComponent<WindGust>();
            SetFields(gust, ("windVelocity", wind), ("onSeconds", onSeconds), ("offSeconds", offSeconds),
                ("phaseSeconds", phaseSeconds), ("streaks", drift));
            return gust;
        }

        // ------------------------------------------------------------------ cactus

        private struct CactusArm
        {
            public float Side, BaseY, Reach, Rise;

            public CactusArm(float side, float baseY, float reach, float rise)
            {
                Side = side; BaseY = baseY; Reach = reach; Rise = rise;
            }
        }

        private struct CactusDesign
        {
            public string MeshName;
            public float Height;
            public CactusArm[] Arms;
        }

        // Design 0: tall, two arms. Design 1: short, one arm (arm toward +X; turn the cactus with its yaw).
        private static readonly CactusDesign[] CactusDesigns =
        {
            new CactusDesign { MeshName = "CactusTall", Height = 2.4f, Arms = new[] { new CactusArm(1f, 1.0f, 0.95f, 0.9f), new CactusArm(-1f, 1.4f, 0.8f, 0.7f) } },
            new CactusDesign { MeshName = "CactusShort", Height = 1.8f, Arms = new[] { new CactusArm(1f, 0.8f, 0.8f, 0.6f) } },
        };

        /// <summary>
        /// A low-poly cactus: ribbed trunk and arms (shared mesh asset, flat shaded) with pink flowers on the tips.
        /// With <paramref name="hazard"/> its slightly smaller hit boxes (trunk capsule + arm boxes) are triggers
        /// that respawn the player; without it the cactus is scenery. Stand-in model built from a generated mesh and
        /// primitives (no cactus model in the project; see report).
        /// </summary>
        private static GameObject Cactus(Context c, DesertMats m, string name, Transform parent, float x, float groundY, float z,
            int design, float scale, float yaw, bool hazard)
        {
            CactusDesign d = CactusDesigns[design];
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(new Vector3(x, groundY, z), Quaternion.Euler(0f, yaw, 0f));

            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * scale;
            body.AddComponent<MeshFilter>().sharedMesh = CactusMesh(d);
            var meshRenderer = body.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = m.Cactus;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

            AddCactusFlower(root.transform, m, new Vector3(0f, (d.Height + CactusTrunkRadius * 0.55f) * scale, 0f), 0.5f * scale);
            foreach (CactusArm arm in d.Arms)
            {
                AddCactusFlower(root.transform, m,
                    new Vector3(arm.Side * arm.Reach * scale, (arm.BaseY + arm.Rise + CactusArmRadius * 0.55f) * scale, 0f), 0.32f * scale);
            }
            if (!hazard) return root;

            // Hit boxes, a little smaller than what is drawn.
            var trunk = root.AddComponent<CapsuleCollider>();
            trunk.isTrigger = true;
            trunk.direction = 1;
            trunk.radius = (CactusTrunkRadius - CactusHitInset) * scale;
            trunk.height = (d.Height + 0.2f) * scale;
            trunk.center = new Vector3(0f, trunk.height * 0.5f, 0f);
            float armHit = (CactusArmRadius - CactusHitInset) * 2f * scale;
            foreach (CactusArm arm in d.Arms)
            {
                var across = root.AddComponent<BoxCollider>();
                across.isTrigger = true;
                across.size = new Vector3(arm.Reach * scale, armHit, armHit);
                across.center = new Vector3(arm.Side * arm.Reach * 0.5f * scale, arm.BaseY * scale, 0f);

                var up = root.AddComponent<BoxCollider>();
                up.isTrigger = true;
                up.size = new Vector3(armHit, (arm.Rise + CactusArmRadius) * scale, armHit);
                up.center = new Vector3(arm.Side * arm.Reach * scale, (arm.BaseY + (arm.Rise + CactusArmRadius) * 0.5f) * scale, 0f);
            }
            c.Result.Hazards.Add(root.AddComponent<KillVolume>());
            return root;
        }

        private static void AddCactusFlower(Transform parent, DesertMats m, Vector3 position, float size)
        {
            Prim(PrimitiveType.Cylinder, "FlowerPetals", parent, position, new Vector3(size, 0.05f * size, size), m.Flower);
            Prim(PrimitiveType.Cylinder, "FlowerCenter", parent, position + new Vector3(0f, 0.04f * size, 0f),
                new Vector3(size * 0.4f, 0.04f * size, size * 0.4f), m.FlowerCenter);
        }

        /// <summary>
        /// The cactus mesh of a design: an 8-sided ribbed trunk (alternating radius) with a pointed cap and arms
        /// (a sideways tube and an upward tube each). Flat shaded, no shared vertices. Saved once as an asset.
        /// </summary>
        private static Mesh CactusMesh(CactusDesign design)
        {
            string path = DesertArtFolder + "/" + design.MeshName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            AddTube(vertices, triangles, Vector3.zero, new Vector3(0f, design.Height, 0f), CactusTrunkRadius, CactusTrunkRadius * 0.92f, false, true);
            foreach (CactusArm arm in design.Arms)
            {
                float tipX = arm.Side * arm.Reach;
                AddTube(vertices, triangles, new Vector3(0f, arm.BaseY, 0f), new Vector3(tipX, arm.BaseY, 0f), CactusArmRadius, CactusArmRadius, false, false);
                AddTube(vertices, triangles, new Vector3(tipX, arm.BaseY - CactusArmRadius, 0f), new Vector3(tipX, arm.BaseY + arm.Rise, 0f),
                    CactusArmRadius, CactusArmRadius * 0.92f, true, true);
            }

            var mesh = new Mesh { name = design.MeshName };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EnsureDesertFolder();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void EnsureDesertFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
            if (!AssetDatabase.IsValidFolder("Assets/Art/Props")) AssetDatabase.CreateFolder("Assets/Art", "Props");
            if (!AssetDatabase.IsValidFolder(DesertArtFolder)) AssetDatabase.CreateFolder("Assets/Art/Props", "Desert");
        }

        /// <summary>An 8-sided ribbed tube from <paramref name="a"/> to <paramref name="b"/>, optionally closed with a pointed cap at either end.</summary>
        private static void AddTube(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, float radiusA, float radiusB, bool capStart, bool capEnd)
        {
            const int sides = 8;
            Vector3 axis = (b - a).normalized;
            Vector3 reference = Mathf.Abs(axis.y) < 0.99f ? Vector3.up : Vector3.right;
            Vector3 u = Vector3.Cross(axis, reference).normalized;
            Vector3 w = Vector3.Cross(axis, u);

            var ringA = new Vector3[sides];
            var ringB = new Vector3[sides];
            for (int j = 0; j < sides; j++)
            {
                float angle = j * Mathf.PI * 2f / sides;
                float rib = j % 2 == 0 ? 1f : 0.8f; // alternating radius = ribs
                Vector3 dir = u * Mathf.Cos(angle) + w * Mathf.Sin(angle);
                ringA[j] = a + dir * radiusA * rib;
                ringB[j] = b + dir * radiusB * rib;
            }

            Vector3 apexEnd = b + axis * radiusB * 0.7f;
            Vector3 apexStart = a - axis * radiusA * 0.7f;
            for (int j = 0; j < sides; j++)
            {
                int k = (j + 1) % sides;
                AddOutwardTriangle(vertices, triangles, ringA[j], ringB[j], ringB[k], a, axis);
                AddOutwardTriangle(vertices, triangles, ringA[j], ringB[k], ringA[k], a, axis);
                if (capEnd) AddOutwardTriangle(vertices, triangles, ringB[j], apexEnd, ringB[k], a, axis);
                if (capStart) AddOutwardTriangle(vertices, triangles, ringA[j], apexStart, ringA[k], a, axis);
            }
        }

        /// <summary>Adds one triangle (own vertices, flat shading), flipped if needed so it faces away from the axis line.</summary>
        private static void AddOutwardTriangle(List<Vector3> vertices, List<int> triangles, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 axisOrigin, Vector3 axis)
        {
            Vector3 centroid = (p0 + p1 + p2) / 3f;
            Vector3 onAxis = axisOrigin + axis * Vector3.Dot(centroid - axisOrigin, axis);
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
            if (Vector3.Dot(normal, centroid - onAxis) < 0f)
            {
                Vector3 swap = p1;
                p1 = p2;
                p2 = swap;
            }
            int i = vertices.Count;
            vertices.Add(p0); vertices.Add(p1); vertices.Add(p2);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
        }

        // ------------------------------------------------------------------ quicksand, arches, scenery

        /// <summary>
        /// Rippled golden quicksand drawn just above the kill slab: a flat-shaded wave mesh (made once, saved as an
        /// asset) so the sand reads as dune ripples. Visual only; the kill slab does the work.
        /// </summary>
        private static void QuicksandSurface(Context c, DesertMats m)
        {
            Mesh mesh = QuicksandMesh();
            var go = new GameObject("QuicksandRipples");
            go.transform.SetParent(c.Scenery, false);
            go.transform.position = new Vector3(0f, SandTopY + 0.05f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = m.Sand;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static Mesh QuicksandMesh()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(QuicksandMeshPath);
            if (existing != null) return existing;

            const float cell = 2f, xMin = -36f, zMin = -10f;
            const int cellsX = 36, cellsZ = 70;
            var vertices = new List<Vector3>(cellsX * cellsZ * 6);
            var triangles = new List<int>(cellsX * cellsZ * 6);
            for (int i = 0; i < cellsX; i++)
            {
                for (int k = 0; k < cellsZ; k++)
                {
                    float x0 = xMin + i * cell, x1 = x0 + cell, z0 = zMin + k * cell, z1 = z0 + cell;
                    Vector3 p00 = new Vector3(x0, SandRipple(x0, z0), z0);
                    Vector3 p10 = new Vector3(x1, SandRipple(x1, z0), z0);
                    Vector3 p01 = new Vector3(x0, SandRipple(x0, z1), z1);
                    Vector3 p11 = new Vector3(x1, SandRipple(x1, z1), z1);
                    int b = vertices.Count;
                    vertices.Add(p00); vertices.Add(p01); vertices.Add(p11);
                    vertices.Add(p00); vertices.Add(p11); vertices.Add(p10);
                    for (int n = 0; n < 6; n++) triangles.Add(b + n);
                }
            }

            var mesh = new Mesh { name = "QuicksandRipples" };
            mesh.indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); // not shared between triangles = flat shaded
            mesh.RecalculateBounds();
            EnsureDesertFolder();
            AssetDatabase.CreateAsset(mesh, QuicksandMeshPath);
            return mesh;
        }

        /// <summary>Height of the sand ripples at a point, metres (about +-0.09).</summary>
        private static float SandRipple(float x, float z)
        {
            return 0.06f * (Mathf.Sin(z * 1.15f + Mathf.Sin(x * 0.3f) * 1.8f) + 0.5f * Mathf.Sin(x * 0.45f + z * 0.3f));
        }

        /// <summary>
        /// A natural stone arch built from KayKit rocks (two tall rocks and a wide capstone, red-rock tint):
        /// visual only. <paramref name="halfSpan"/> is half the opening width, <paramref name="height"/> the pillar height above the sand.
        /// </summary>
        private static void RockArch(Context c, DesertMats m, float centerX, float z, float halfSpan, float height)
        {
            float baseY = SandTopY - 0.4f;
            for (int s = -1; s <= 1; s += 2)
            {
                PlaceModel("Rock_4_H", new Vector3(centerX + s * halfSpan, baseY, z), new Vector3(0f, s > 0 ? 20f : 200f, 0f),
                    new Vector3(2.6f, (height + 0.4f) / 3.7f, 2.6f), m.RedRock, c.Scenery);
            }
            // Rock_1_K is about 3.7 m wide and 3.6 m tall: stretch it across the opening and sink it onto the pillar tops.
            float capWidth = halfSpan * 2f + 4f;
            PlaceModel("Rock_1_K", new Vector3(centerX, baseY + height - 1.4f, z), Vector3.zero,
                new Vector3(capWidth / 3.7f, 0.9f, 2.0f), m.RedRock, c.Scenery);
        }

        /// <summary>
        /// Dune banks and red cliffs along both sides, with bare trees, dry bushes and decorative cacti on the dunes
        /// (visual only, shared tinted materials, no shadows). Deterministic for a given <paramref name="seed"/>.
        /// </summary>
        private static void DesertScenery(Context c, DesertMats m, float zMin, float zMax, int seed)
        {
            var rng = new System.Random(seed);
            string[] trees = { "Tree_Bare_1_A", "Tree_Bare_1_B", "Tree_Bare_2_A", "Tree_Bare_2_B" };
            string[] cliffs = { "Rock_1_J", "Rock_1_K", "Rock_1_L", "Rock_1_M", "Rock_5_H" };
            string[] boulders = { "Rock_5_G", "Rock_5_F", "Rock_5_E", "Rock_1_H", "Rock_1_I" };

            for (float z = zMin; z <= zMax; z += 16f)
            {
                for (int s = 0; s < 2; s++)
                {
                    float side = s == 0 ? -1f : 1f;

                    // Dune bank: inner edge at |x| >= 19, clear of the secret branches (|x| up to 17.4).
                    float sizeX = 14f + (float)rng.NextDouble() * 8f;
                    float sizeZ = 14f + (float)rng.NextDouble() * 4f;
                    float topY = 1.0f + (float)rng.NextDouble() * 3.5f;
                    float cx = side * (19f + sizeX * 0.5f + (float)rng.NextDouble() * 4f);
                    float cz = z + (float)rng.NextDouble() * 4f;
                    // Same golden-sand KayKit colour set as the pads (PadColorSet 5), not a tint.
                    GrassIsland(c, "Dune_" + (side < 0f ? "L_" : "R_") + z, cx, topY, cz, sizeX, sizeZ, SandTopY - 1f, null, c.Scenery);

                    // Dry life on the dune.
                    PlaceModel(trees[rng.Next(trees.Length)],
                        new Vector3(cx + ((float)rng.NextDouble() - 0.5f) * sizeX * 0.6f, topY, cz + ((float)rng.NextDouble() - 0.5f) * sizeZ * 0.6f),
                        new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * (1.2f + (float)rng.NextDouble() * 0.6f), null, c.Scenery); // own bare-wood colours
                    for (int i = 0; i < 2; i++)
                    {
                        PlaceModel("Bush_3_A",
                            new Vector3(cx + ((float)rng.NextDouble() - 0.5f) * sizeX * 0.7f, topY, cz + ((float)rng.NextDouble() - 0.5f) * sizeZ * 0.7f),
                            new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * (1.2f + (float)rng.NextDouble() * 0.8f), null, c.Scenery, 5); // dry golden bush
                    }
                    Cactus(c, m, "DecorCactus", c.Scenery, cx + ((float)rng.NextDouble() - 0.5f) * sizeX * 0.5f, topY, cz + ((float)rng.NextDouble() - 0.5f) * sizeZ * 0.5f,
                        rng.Next(2), 1.2f + (float)rng.NextDouble() * 0.6f, (float)rng.NextDouble() * 360f, false);

                    // Red cliff face behind the dunes.
                    PlaceModel(cliffs[rng.Next(cliffs.Length)],
                        new Vector3(side * (38f + (float)rng.NextDouble() * 8f), SandTopY - 2f, z + (float)rng.NextDouble() * 6f),
                        new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * (4.5f + (float)rng.NextDouble() * 1.5f), m.RedRock, c.Scenery);

                    // Boulders half buried in the sand beside the course.
                    PlaceModel(boulders[rng.Next(boulders.Length)],
                        new Vector3(side * (8f + (float)rng.NextDouble() * 4f), SandTopY - 0.5f, z + (float)rng.NextDouble() * 8f),
                        new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * (2.0f + (float)rng.NextDouble() * 1.0f), m.RedRock, c.Scenery);
                }
            }
        }
    }
}
#endif
