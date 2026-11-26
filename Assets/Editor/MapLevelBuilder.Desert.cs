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
    /// golden quicksand (the kill slab); pads are red-orange rock hills with a golden top (tinted KayKit hills).
    /// Reach used to place everything (moveSpeed 6, jumpSpeed 9, gravity 25): about 4 m flat or 3.9 m with a
    /// 0.5 m rise. Main-path gaps are 2.0 - 3.3 m (hard spots close to that); the hidden-Shard branches use
    /// 3.4 - 3.6 m gaps on small tops.
    /// </summary>
    internal static partial class MapLevelBuilder
    {
        /// <summary>Surface of the quicksand (top of the kill slab).</summary>
        private const float SandTopY = -2f;

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

            // A: a wide pad with three cacti to weave between.
            Slab(c, "Pad_A", 0f, 0.0f, 12.0f, 6f, 13f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Cactus(c, m, "Cactus_A1", c.Course, -1.7f, 0f, 9.0f, 0, 1.0f, 0f, true);
            Cactus(c, m, "Cactus_A2", c.Course, 1.7f, 0f, 12.0f, 1, 1.1f, 0f, true);
            Cactus(c, m, "Cactus_A3", c.Course, -1.7f, 0f, 15.0f, 0, 1.0f, 0f, true);

            // B: four narrow stone pillars (tops 1.2 - 1.5 m), each 0.3 m higher.
            Slab(c, "Pillar_1", 0.0f, 0.3f, 22.2f, 1.5f, 1.5f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Slab(c, "Pillar_2", 1.0f, 0.6f, 25.9f, 1.4f, 1.4f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Slab(c, "Pillar_3", -0.5f, 0.9f, 29.7f, 1.3f, 1.3f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Slab(c, "Pillar_4", 0.5f, 1.2f, 33.5f, 1.2f, 1.2f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            AddCheckpoint(c, "Checkpoint_1", 0f, 1.4f, 39.5f, 5f, m.Pillar, DesertBottomY);

            // C: a narrow ledge with two tumbleweeds rolling toward the player (jump them): 5 m/s, a ball every 2.5 s.
            Slab(c, "Pad_B", 0f, 1.4f, 52.0f, 2.4f, 16f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Tumbleweed(c, m, "Tumbleweed_1", 0f, 1.4f, 59.5f, -15f, 5.0f, 2.0f, 0.0f, null);
            Tumbleweed(c, m, "Tumbleweed_2", 0f, 1.4f, 59.5f, -15f, 5.0f, 2.0f, 2.0f, null);
            AddCheckpoint(c, "Checkpoint_2", 0f, 1.4f, 65.0f, 5f, m.Pillar, DesertBottomY);

            // D: three small pads, each with a cactus on one side, then two pillars to the finish.
            Slab(c, "Pad_N1", 0f, 1.6f, 72.4f, 3.4f, 3.4f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Cactus(c, m, "Cactus_N1", c.Course, 1.0f, 1.6f, 72.4f, 1, 0.9f, 0f, true);
            Slab(c, "Pad_N2", 0f, 1.8f, 78.2f, 3.4f, 3.4f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Cactus(c, m, "Cactus_N2", c.Course, -1.0f, 1.8f, 78.2f, 1, 0.9f, 180f, true);
            Slab(c, "Pad_N3", 0f, 2.0f, 84.0f, 3.4f, 3.4f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Cactus(c, m, "Cactus_N3", c.Course, 1.0f, 2.0f, 84.0f, 1, 0.9f, 0f, true);
            Slab(c, "Pillar_5", 0.3f, 2.2f, 89.0f, 1.3f, 1.3f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Slab(c, "Pillar_6", -0.3f, 2.4f, 92.5f, 1.2f, 1.2f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            AddFinish(c, 0f, 2.6f, 98.5f, 6f, 6f, m.Pillar, DesertBottomY);

            // Secret branch off checkpoint 2 toward +X: three small pillar tops, 3.4 - 3.5 m gaps. Hidden Shard at the end.
            Slab(c, "Secret_1", 6.6f, 1.8f, 64.0f, 1.2f, 1.2f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Slab(c, "Secret_2", 11.1f, 2.2f, 66.5f, 1.2f, 1.2f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Slab(c, "Secret_3", 15.8f, 2.6f, 64.0f, 1.4f, 1.4f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            AddShard(c, c.MapId, new Vector3(15.8f, 3.7f, 64.0f), m.Shard, 0.4f, 0.8f);

            RockArch(c, m, 0f, 30f, 7f, 11f);
            RockArch(c, m, 19f, 85f, 5.5f, 9f);
            DesertScenery(c, m, -15f, 110f, 5001);
        }

        // ------------------------------------------------------------------ level 2: Quicksand pits

        private static void BuildDesert2(Context c)
        {
            var m = new DesertMats();
            PrepareDesert(c, m);
            StartIsland(c, 6f, 6f, DesertBottomY);

            // A: four floating slabs that sink while stood on (0.9 m/s, about 2.5 s from rest to under the sand).
            SinkSlab(c, m, "Sink_A1", 0.0f, 0.0f, 6.6f, 3.2f, 3.2f, 0.9f);
            SinkSlab(c, m, "Sink_A2", 1.0f, 0.3f, 11.8f, 3.0f, 3.0f, 0.9f);
            SinkSlab(c, m, "Sink_A3", -1.0f, 0.6f, 17.2f, 3.0f, 3.0f, 0.9f);
            SinkSlab(c, m, "Sink_A4", 0.5f, 0.9f, 22.6f, 2.8f, 2.8f, 0.9f);
            AddCheckpoint(c, "Checkpoint_1", 0f, 1.0f, 29.5f, 5f, m.Pillar, DesertBottomY);

            // B: a long narrow sinking slab with two tumbleweeds rolling along it (they sink with it).
            GameObject longA = SinkSlab(c, m, "Sink_Long1", 0f, 1.0f, 40.5f, 2.6f, 12f, 0.9f);
            Tumbleweed(c, m, "Tumbleweed_1", 0f, 1.0f, 46.0f, -11f, 5.5f, 1.4f, 0.0f, longA.transform);
            Tumbleweed(c, m, "Tumbleweed_2", 0f, 1.0f, 46.0f, -11f, 5.5f, 1.4f, 1.7f, longA.transform);

            // C: two stone pillars, then a pad with two cacti to weave between.
            Slab(c, "Pillar_1", 0.2f, 1.3f, 50.2f, 1.4f, 1.4f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Slab(c, "Pillar_2", -0.6f, 1.6f, 54.0f, 1.3f, 1.3f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Slab(c, "Pad_C", 0f, 1.6f, 59.0f, 4.2f, 4.2f, c.In.PlatformMaterial, m.Pillar, DesertBottomY);
            Cactus(c, m, "Cactus_C1", c.Course, -1.2f, 1.6f, 58.0f, 1, 1.0f, 0f, true);
            Cactus(c, m, "Cactus_C2", c.Course, 1.2f, 1.6f, 60.2f, 1, 1.0f, 180f, true);
            AddCheckpoint(c, "Checkpoint_2", 0f, 1.6f, 66.5f, 5f, m.Pillar, DesertBottomY);

            // D: a wide first gap, then two sinking slabs, then another long sinking slab with two faster tumbleweeds.
            SinkSlab(c, m, "Sink_B1", 0.0f, 1.6f, 73.8f, 3.0f, 3.0f, 0.9f);
            SinkSlab(c, m, "Sink_B2", -0.8f, 1.9f, 79.0f, 3.0f, 3.0f, 0.9f);
            GameObject longB = SinkSlab(c, m, "Sink_Long2", 0f, 1.9f, 89.5f, 2.6f, 12f, 0.9f);
            Tumbleweed(c, m, "Tumbleweed_3", 0f, 1.9f, 95.0f, -11f, 6.0f, 1.2f, 0.0f, longB.transform);
            Tumbleweed(c, m, "Tumbleweed_4", 0f, 1.9f, 95.0f, -11f, 6.0f, 1.2f, 1.55f, longB.transform);

            // Finish: deck, step and weather dais, about 9 m of open sand behind the last obstacle and in front of Sam.
            AddFinish(c, 0f, 1.9f, 102.5f, 6f, 9f, m.Pillar, DesertBottomY, weatherDais: true);

            // Secret branch off checkpoint 2 toward -X: three small sinking slabs, 3.4 - 3.5 m gaps. Hidden Shard at the end.
            SinkSlab(c, m, "Secret_1", -6.75f, 2.0f, 65.5f, 1.5f, 1.5f, 0.9f);
            SinkSlab(c, m, "Secret_2", -11.65f, 2.4f, 67.5f, 1.5f, 1.5f, 0.9f);
            SinkSlab(c, m, "Secret_3", -16.65f, 2.8f, 65.5f, 1.5f, 1.5f, 0.9f);
            AddShard(c, c.MapId, new Vector3(-16.65f, 3.9f, 65.5f), m.Shard, 0.4f, 0.8f);

            RockArch(c, m, 0f, 48f, 7f, 11f);
            RockArch(c, m, -21f, 80f, 5.5f, 9f);
            DesertScenery(c, m, -15f, 125f, 5002);
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
            SetFields(sinking, ("sinkSpeed", sinkSpeed), ("riseSpeed", 0.9f), ("maxDepth", topY - SandTopY + 3f));
            return root;
        }

        /// <summary>
        /// A tumbleweed that rolls along -Z (<paramref name="travelZ"/> metres from its start) and starts again after
        /// a pause (<see cref="RollingHazard"/>); touching it respawns the player. It can be parented to a platform
        /// (a sinking slab) so it moves with it. Stand-in model: three crossed KayKit bushes in dry tones.
        /// </summary>
        private static GameObject Tumbleweed(Context c, DesertMats m, string name, float x, float surfaceY, float startZ,
            float travelZ, float speed, float restartDelay, float phase, Transform parent)
        {
            Vector3 center = new Vector3(x, surfaceY + TumbleweedRadius, startZ);
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

            var roll = root.AddComponent<RollingHazard>();
            SetFields(roll,
                ("travel", new Vector3(0f, 0f, travelZ)),
                ("speed", speed),
                ("restartDelaySeconds", restartDelay),
                ("phaseSeconds", phase),
                ("spinVisual", spin.transform),
                ("spinAxis", travelZ < 0f ? new Vector3(-1f, 0f, 0f) : new Vector3(1f, 0f, 0f)),
                ("rollRadius", TumbleweedRadius));

            if (parent != null) root.transform.SetParent(parent, true);
            return root;
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
