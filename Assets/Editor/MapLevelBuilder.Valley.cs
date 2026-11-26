#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WeatherForSale.Obby;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Valley layouts (story 004): level 1 "Floating rocks in the wind" (หินลอยกลางลม) and level 2
    /// "Fans and wind vents" (ใบพัดและช่องลม). The course runs along +Z from the start pad at the origin
    /// over a deep, cloud-filled chasm; pads are sandstone mesa pillars (tinted KayKit hills) going down
    /// into the clouds. Reach used to place everything (moveSpeed 6, jumpSpeed 9, gravity 25): about 4 m flat
    /// or 3.9 m with a 0.5 m rise. Main-path gaps are 2.5 - 3.2 m (hard spots close to that); the hidden-Shard
    /// branches use 3.4 - 3.6 m gaps on small rocks.
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

        private sealed class ValleyMats
        {
            // The KayKit hills / rocks keep their palette; this tint turns the green grass top golden-orange and the grey rock body orange.
            public readonly Material PadRock = PaletteMat("Obby_VY_PaletteSandstone", new Color(1.7f, 1.0f, 0.42f));
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

        // ------------------------------------------------------------------ level 1: Floating rocks in the wind

        private static void BuildValley1(Context c)
        {
            var m = new ValleyMats();
            PrepareValley(c, m);
            StartIsland(c, 6f, 6f, ValleyBottomY);

            // A: four floating rocks that bob and turn slowly; neighbours bob in step. Gaps 2.7 - 3.2 m.
            FloatingRock(c, m, "Rock_A1", 0.0f, 0.0f, 6.8f, 3.0f, 0.30f, 3.0f, 0f, 12f);
            FloatingRock(c, m, "Rock_A2", 1.2f, 0.3f, 11.6f, 3.0f, 0.30f, 3.0f, 0f, -12f);
            FloatingRock(c, m, "Rock_A3", -1.0f, 0.6f, 16.6f, 3.0f, 0.30f, 3.0f, 0f, 12f);
            FloatingRock(c, m, "Rock_A4", 0.5f, 0.9f, 22.0f, 3.0f, 0.30f, 3.0f, 0f, -12f);
            AddCheckpoint(c, "Checkpoint_1", 0f, 1.0f, 28.5f, 5f, m.Pillar, ValleyBottomY);

            // B: gusts push sideways (+X) in a 1.4 s on / 1.8 s off rhythm while crossing three rocks.
            WindZone(c, m, "Wind_A", new Vector3(0f, 3.5f, 39.5f), new Vector3(12f, 9f, 15f), new Vector3(3.8f, 0f, 0f), 1.4f, 1.8f, 0f, 3001);
            FloatingRock(c, m, "Rock_B1", 1.0f, 1.2f, 35.0f, 3.0f, 0.30f, 3.0f, 0f, 12f);
            FloatingRock(c, m, "Rock_B2", -0.8f, 1.5f, 40.3f, 3.0f, 0.30f, 3.0f, 0f, -12f);
            FloatingRock(c, m, "Rock_B3", 0.8f, 1.8f, 45.6f, 3.0f, 0.30f, 3.0f, 0f, 12f);
            AddCheckpoint(c, "Checkpoint_2", 0f, 2.0f, 52.0f, 5f, m.Pillar, ValleyBottomY);

            // C: bigger bobbing (neighbours out of step) and gusts from the other side (-X), shorter calm.
            WindZone(c, m, "Wind_B", new Vector3(0f, 5.5f, 67f), new Vector3(12f, 9f, 22f), new Vector3(-4.2f, 0f, 0f), 1.2f, 1.6f, 0.8f, 3002);
            FloatingRock(c, m, "Rock_C1", 0.0f, 2.4f, 58.8f, 3.0f, 0.40f, 2.6f, 0.0f, 12f);
            FloatingRock(c, m, "Rock_C2", -1.2f, 2.8f, 64.2f, 3.0f, 0.40f, 2.6f, 1.3f, -12f);
            FloatingRock(c, m, "Rock_C3", 0.6f, 3.2f, 69.6f, 3.0f, 0.40f, 2.6f, 0.0f, 12f);
            FloatingRock(c, m, "Rock_C4", -0.6f, 3.5f, 75.0f, 3.0f, 0.40f, 2.6f, 1.3f, -12f);
            AddFinish(c, 0f, 4.0f, 82.0f, 6f, 6f, m.Pillar, ValleyBottomY);

            // Secret branch off checkpoint 2 toward +X: three small rocks, 3.6 m gaps. Hidden Shard at the end.
            FloatingRock(c, m, "Secret_1", 6.85f, 2.4f, 52.5f, 2.0f, 0.25f, 3.0f, 0f, 0f);
            FloatingRock(c, m, "Secret_2", 11.95f, 2.8f, 50.0f, 2.0f, 0.25f, 3.0f, 0f, 0f);
            FloatingRock(c, m, "Secret_3", 16.55f, 3.2f, 52.0f, 2.0f, 0.25f, 3.0f, 0f, 0f);
            AddShard(c, c.MapId, new Vector3(16.55f, 4.4f, 52.0f), m.Shard, 0.4f, 0.8f);

            ValleyScenery(c, m, -20f, 110f, 4001);
        }

        // ------------------------------------------------------------------ level 2: Fans and wind vents

        private static void BuildValley2(Context c)
        {
            var m = new ValleyMats();
            PrepareValley(c, m);
            StartIsland(c, 6f, 6f, ValleyBottomY);

            // A: two narrow ledges, each with a giant fan whose blades sweep the ledge. Wait, then run through the gap.
            Slab(c, "Ledge_A", 0f, 0.0f, 9.0f, 2.2f, 9.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);
            GiantFan(c, m, "Fan_1", 0f, 0.0f, 9.0f, 4.2f, 3, 62f, 0f, 1f);
            Slab(c, "Ledge_B", 0f, 0.4f, 21.0f, 2.2f, 9.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);
            GiantFan(c, m, "Fan_2", 0f, 0.4f, 21.0f, 4.2f, 3, 78f, 40f, -1f);
            AddCheckpoint(c, "Checkpoint_1", 0f, 0.4f, 30.0f, 5f, m.Pillar, ValleyBottomY);

            // B: a wind vent lifts the player 3 m up to a small ledge, then a third (faster) fan on a narrow ledge.
            Slab(c, "Ledge_V1", 0f, 0.4f, 38.0f, 3.0f, 6.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);
            Vent(c, m, "Vent_1", 0f, 0.4f, 39.4f, 3.8f);
            Slab(c, "Landing_T1", 0f, 3.4f, 44.0f, 3.0f, 3.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);
            Slab(c, "Ledge_C", 0f, 3.4f, 51.0f, 2.2f, 8.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);
            GiantFan(c, m, "Fan_3", 0f, 3.4f, 51.0f, 4.2f, 3, 88f, 80f, 1f);
            AddCheckpoint(c, "Checkpoint_2", 0f, 3.4f, 60.5f, 5f, m.Pillar, ValleyBottomY);

            // C: ride a swinging suspension bridge across a 12.8 m gap, then a second vent up to the finish deck.
            SuspensionBridge(c, m, "Bridge", 0f, 69.4f, 3.7f, 10f, 18f, 4.6f, 0f, 5.0f, 2.4f);
            Slab(c, "Landing_P2", 0f, 3.4f, 78.9f, 5f, 6f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);
            Vent(c, m, "Vent_2", 0f, 3.4f, 80.6f, 4.2f);
            Slab(c, "Landing_T2", 0f, 6.6f, 85.0f, 3.0f, 3.0f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);

            // Finish: deck, step and weather dais, about 9 m of open air behind the last obstacle and in front of Sam.
            AddFinish(c, 0f, 6.6f, 93.1f, 6f, 9f, m.Pillar, ValleyBottomY, weatherDais: true);

            // Secret branch off checkpoint 2 toward -X: small ledges and one bobbing rock, 3.4 - 3.6 m gaps. Hidden Shard at the end.
            Slab(c, "Secret_1", -7.0f, 3.8f, 61.0f, 1.8f, 1.8f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);
            FloatingRock(c, m, "Secret_2", -12.0f, 4.2f, 59.0f, 1.9f, 0.30f, 2.8f, 0f, 0f);
            Slab(c, "Secret_3", -17.2f, 4.6f, 61.0f, 1.8f, 1.8f, c.In.PlatformMaterial, m.Pillar, ValleyBottomY);
            AddShard(c, c.MapId, new Vector3(-17.2f, 5.7f, 61.0f), m.Shard, 0.4f, 0.8f);

            ValleyScenery(c, m, -20f, 125f, 4002);
        }

        // ------------------------------------------------------------------ obstacle builders

        /// <summary>
        /// A floating rock platform: a sandstone hill block with a rock spire hanging under it, bobbing up and
        /// down and turning slowly around Y. The player is carried (RideableSurface), rotation included.
        /// Stand-in model: KayKit hill + inverted rock spire (no floating-island model; see report).
        /// </summary>
        private static GameObject FloatingRock(Context c, ValleyMats m, string name, float x, float topY, float z, float size,
            float bobAmplitude, float bobPeriod, float bobPhase, float spinDegreesPerSecond)
        {
            var root = new GameObject(name);
            root.transform.SetParent(c.Course, false);
            root.transform.position = new Vector3(x, topY, z);

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(size * FloatingRockWalkFraction, 0.5f, size * FloatingRockWalkFraction);
            box.center = new Vector3(0f, -0.25f, 0f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            GameObject island = GrassIsland(c, name + "_Island", x, topY, z, size, size, topY - FloatingRockThickness, m.PadRock, visual.transform);
            if (island == null)
            {
                Prim(PrimitiveType.Sphere, "Rock", visual.transform, new Vector3(0f, -FloatingRockThickness * 0.5f, 0f),
                    new Vector3(size, FloatingRockThickness, size), m.Pillar);
            }
            PlaceModel("Rock_4_F", new Vector3(x, topY - FloatingRockThickness + 0.1f, z), new Vector3(180f, 0f, 0f),
                Vector3.one * (size * 0.28f), m.PadRock, visual.transform);

            root.AddComponent<RideableSurface>();
            var mover = root.AddComponent<PlatformMover>();
            SetFields(mover, ("travel", new Vector3(0f, bobAmplitude, 0f)), ("periodSeconds", bobPeriod), ("phaseSeconds", bobPhase));
            if (!Mathf.Approximately(spinDegreesPerSecond, 0f))
            {
                var spinner = root.AddComponent<Spinner>();
                SetFields(spinner, ("axis", Vector3.up), ("degreesPerSecond", spinDegreesPerSecond), ("phaseDegrees", bobPhase * 40f));
            }
            return root;
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
                Prim(PrimitiveType.Sphere, "Paddle", blade.transform, mid, new Vector3(0.9f, bladeSpan, 0.18f), m.FanBlade);

                var hit = blade.AddComponent<BoxCollider>();
                hit.isTrigger = true;
                hit.size = new Vector3(0.8f, bladeSpan, 0.5f);
                hit.center = mid;
                c.Result.Hazards.Add(blade.AddComponent<KillVolume>());
            }

            var spinner = rotor.AddComponent<Spinner>();
            SetFields(spinner, ("axis", Vector3.forward), ("degreesPerSecond", degreesPerSecond), ("phaseDegrees", phaseDegrees));

            // Support: a sandstone tower beside the ledge, a strut behind the blade plane and a short axle to the hub.
            float towerX = x + towerSide * 3.4f;
            float towerTop = hubY + 1.0f;
            float towerHeight = towerTop - ValleyBottomY;
            Prim(PrimitiveType.Cylinder, name + "_Tower", c.Scenery, new Vector3(towerX, ValleyBottomY + towerHeight * 0.5f, z + 0.8f),
                new Vector3(1.8f, towerHeight * 0.5f, 1.8f), m.Tower);
            Prim(PrimitiveType.Cylinder, name + "_Strut", c.Scenery, new Vector3(x + towerSide * 1.7f, hubY, z + 0.8f),
                new Vector3(0.45f, 1.7f, 0.45f), m.Tower, new Vector3(0f, 0f, 90f));
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
        /// A swinging suspension bridge: a plank deck hung by two ropes from a beam between two sandstone towers,
        /// swinging along Z like a pendulum while staying level; the player rides it across a gap.
        /// <paramref name="extremeTopY"/> is the deck height at both ends of the swing. Stand-in model: planks and
        /// cylinders (no bridge model; see report).
        /// </summary>
        private static void SuspensionBridge(Context c, ValleyMats m, string name, float x, float zCenter, float extremeTopY,
            float length, float amplitudeDegrees, float period, float phase, float deckLength, float deckWidth)
        {
            float restTop = extremeTopY - length * (1f - Mathf.Cos(amplitudeDegrees * Mathf.Deg2Rad));
            var pivotPosition = new Vector3(x, restTop + length, zCenter);
            float half = deckWidth * 0.5f;

            var pivot = new GameObject(name + "_Pivot");
            pivot.transform.SetParent(c.Course, false);
            pivot.transform.position = pivotPosition;
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(PrimitiveType.Cylinder, "Rope", pivot.transform, new Vector3(s * half, -length * 0.5f, 0f), new Vector3(0.1f, length * 0.5f, 0.1f), m.Rope);
            }

            var deck = new GameObject("Deck");
            deck.transform.SetParent(pivot.transform, false);
            deck.transform.localPosition = new Vector3(0f, -length, 0f);
            var box = deck.AddComponent<BoxCollider>();
            box.size = new Vector3(deckWidth, 0.3f, deckLength);
            box.center = new Vector3(0f, -0.15f, 0f);
            const int planks = 8;
            float plankLength = deckLength / planks;
            for (int i = 0; i < planks; i++)
            {
                float pz = -deckLength * 0.5f + plankLength * (i + 0.5f);
                Prim(PrimitiveType.Cube, "Plank", deck.transform, new Vector3(0f, -0.06f, pz), new Vector3(deckWidth - 0.05f, 0.12f, plankLength - 0.06f), m.Plank);
            }
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(PrimitiveType.Cylinder, "Rail", deck.transform, new Vector3(s * half, 0.7f, 0f), new Vector3(0.07f, deckLength * 0.5f, 0.07f), m.Rope, new Vector3(90f, 0f, 0f));
                for (int p = -1; p <= 1; p++)
                {
                    Prim(PrimitiveType.Cylinder, "Post", deck.transform, new Vector3(s * half, 0.35f, p * (deckLength * 0.5f - 0.1f)), new Vector3(0.1f, 0.35f, 0.1f), m.Plank);
                }
            }
            deck.AddComponent<RideableSurface>();

            var swing = pivot.AddComponent<PendulumSwing>();
            SetFields(swing,
                ("swingAxis", Vector3.right),
                ("amplitudeDegrees", amplitudeDegrees),
                ("periodSeconds", period),
                ("phaseSeconds", phase),
                ("keepUpright", deck.transform));

            // Towers and the beam the ropes hang from (visual only).
            float towerX = half + 1.8f;
            float beamY = pivotPosition.y + 0.3f;
            float towerHeight = beamY + 0.4f - ValleyBottomY;
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(PrimitiveType.Cylinder, name + "_Tower", c.Scenery, new Vector3(x + s * towerX, ValleyBottomY + towerHeight * 0.5f, zCenter),
                    new Vector3(1.6f, towerHeight * 0.5f, 1.6f), m.Tower);
            }
            Prim(PrimitiveType.Cylinder, name + "_Beam", c.Scenery, new Vector3(x, beamY, zCenter), new Vector3(0.5f, towerX, 0.5f), m.Tower, new Vector3(0f, 0f, 90f));
        }

        // ------------------------------------------------------------------ scenery

        /// <summary>
        /// Tall flat-topped sandstone mesas on both sides of the canyon with small dry trees and bushes on top,
        /// a second row of taller ones behind, and white wind streaks drifting through. Visual only,
        /// shared tinted materials, no shadows. Deterministic for a given <paramref name="seed"/>.
        /// </summary>
        private static void ValleyScenery(Context c, ValleyMats m, float zMin, float zMax, int seed)
        {
            var rng = new System.Random(seed);
            string[] plants = { "Tree_1_A", "Tree_2_A", "Tree_3_A", "Tree_4_A", "Bush_3_A", "Bush_2_A" };

            for (float z = zMin; z <= zMax; z += 20f)
            {
                for (int s = 0; s < 2; s++)
                {
                    float side = s == 0 ? -1f : 1f;

                    // Near mesa: inner edge at |x| >= 24, clear of the secret branches (|x| up to 18.4).
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
