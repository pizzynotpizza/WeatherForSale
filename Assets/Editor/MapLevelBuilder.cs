#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using WeatherForSale.Obby;
using Object = UnityEngine.Object;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Editor-only builder for the real obby layouts of story 004 (obstacles, scenery, hidden Shard).
    /// <see cref="ObbyTestSceneBuilder"/> calls <see cref="Build"/> for every map level that has a layout
    /// (<see cref="HasLayout"/>); the start pad, kill slab, player, camera, UI and game loop stay in that
    /// builder. Everything here is deterministic (fixed coordinates, fixed random seeds), so re-running
    /// the "Build Map Obby Scenes" menu regenerates identical scenes.
    /// This file holds the shared helpers; the per-map layouts live in the other partial files
    /// (MapLevelBuilder.Rainforest.cs, later Valley / Desert / Glacier).
    /// Implements: story-004 (obstacles per level, mid-level checkpoint, hidden Shard, KayKit dressing).
    /// </summary>
    internal static partial class MapLevelBuilder
    {
        /// <summary>What <see cref="ObbyTestSceneBuilder"/> hands to a layout.</summary>
        internal struct Inputs
        {
            /// <summary>Parent of everything the layout creates ("Level").</summary>
            public Transform Root;
            public Material PlatformMaterial, StartMaterial, CheckpointMaterial, FinishMaterial, WhiteMaterial;
        }

        /// <summary>What a layout hands back for the level manager and the suction stage.</summary>
        internal sealed class Result
        {
            /// <summary>Mid-level checkpoints, in course order.</summary>
            public readonly List<Checkpoint> Checkpoints = new List<Checkpoint>();
            /// <summary>Trap volumes (swinging flowers ...) that respawn the player, in addition to the water slab.</summary>
            public readonly List<KillVolume> Hazards = new List<KillVolume>();
            /// <summary>The hidden Shard pickups placed (counting is story 006).</summary>
            public readonly List<ShardPickup> Shards = new List<ShardPickup>();
            public FinishTrigger Finish;
            /// <summary>Centre of the top surface of the finish pad (the suction stage stands Sam here).</summary>
            public Vector3 FinishTop;
            /// <summary>Top of the water / kill slab, metres.</summary>
            public float HazardTopY = -6f;
            /// <summary>Way Sam faces during the suction on a level 2 (0 = +Z, toward the floating weather past the dais).</summary>
            public float SuctionSpotYaw = 180f;
            /// <summary>
            /// World-space box around the walkable route (start pad, checkpoints, finish). The kill slab is
            /// centred on it and sized to cover it, so a merged two-level course stays over the hazard.
            /// </summary>
            public Bounds CourseBounds = new Bounds(Vector3.zero, Vector3.zero);

            /// <summary>Recomputes <see cref="CourseBounds"/> from the start pad (origin), the checkpoints and the finish.</summary>
            public void RecomputeCourseBounds()
            {
                var b = new Bounds(Vector3.zero, Vector3.zero);
                foreach (Checkpoint cp in Checkpoints)
                {
                    if (cp != null) b.Encapsulate(cp.transform.position);
                }
                if (Finish != null) b.Encapsulate(FinishTop);
                CourseBounds = b;
            }
        }

        /// <summary>Per-build state shared by the helpers.</summary>
        private sealed class Context
        {
            public readonly string MapId;
            public readonly int Level;
            public readonly Inputs In;
            public readonly Result Result = new Result();
            /// <summary>Walkable pieces and obstacles.</summary>
            public readonly Transform Course;
            /// <summary>Visual-only dressing (no colliders).</summary>
            public readonly Transform Scenery;
            /// <summary>Palette material for the grass-hill pads (null = the original green KayKit look).</summary>
            public Material PadMaterial;
            /// <summary>KayKit colour set (1-8) the pad hills come from; 1 = green grass, 5 = golden sand.</summary>
            public int PadColorSet = 1;
            /// <summary>When set, every grass-island gets a thin cap of this material on top (snow, ice).</summary>
            public Material PadCapMaterial;

            public Context(string mapId, int level, Inputs inputs)
            {
                MapId = mapId;
                Level = level;
                In = inputs;
                Course = new GameObject("Course").transform;
                Course.SetParent(inputs.Root, false);
                Scenery = new GameObject("Scenery").transform;
                Scenery.SetParent(inputs.Root, false);
            }
        }

        private const string KayKitFolder = "Assets/KayKit_Forest_Nature_Pack_1.0_EXTRA/Assets/fbx(unity)/Color1/";
        private const string PaletteTexturePath = KayKitFolder + "forest_texture.png";

        /// <summary>True when this map level has a real layout here (otherwise the starter layout is used).</summary>
        internal static bool HasLayout(string mapId, int level)
        {
            return (mapId == "Rainforest" || mapId == "Valley" || mapId == "Desert" || mapId == "Glacier") && (level == 1 || level == 2);
        }

        /// <summary>
        /// Builds the layout of one map level under <see cref="Inputs.Root"/>. Throws (after logging a
        /// clear error naming the map and level) if anything goes wrong, so a batch build fails loudly.
        /// </summary>
        internal static Result Build(string mapId, int level, Inputs inputs)
        {
            try
            {
                if (inputs.Root == null) throw new ArgumentException("Inputs.Root is missing.");
                var ctx = new Context(mapId, level, inputs);
                BuildLevel(ctx);
                ctx.Result.RecomputeCourseBounds();
                Debug.Log("[MapLevelBuilder] Built " + mapId + " level " + level + ": "
                          + ctx.Result.Checkpoints.Count + " checkpoint(s), " + ctx.Result.Hazards.Count + " trap(s), "
                          + ctx.Result.Shards.Count + " shard(s).");
                return ctx.Result;
            }
            catch (Exception e)
            {
                Debug.LogError("[MapLevelBuilder] FAILED to build " + mapId + " level " + level + ": " + e);
                throw;
            }
        }

        /// <summary>Runs the per-map layout of <paramref name="ctx"/>'s map and level, then checks it made a finish and a checkpoint.</summary>
        private static void BuildLevel(Context ctx)
        {
            string mapId = ctx.MapId;
            int level = ctx.Level;
            if (mapId == "Rainforest" && level == 1) BuildRainforest1(ctx);
            else if (mapId == "Rainforest" && level == 2) BuildRainforest2(ctx);
            else if (mapId == "Valley" && level == 1) BuildValley1(ctx);
            else if (mapId == "Valley" && level == 2) BuildValley2(ctx);
            else if (mapId == "Desert" && level == 1) BuildDesert1(ctx);
            else if (mapId == "Desert" && level == 2) BuildDesert2(ctx);
            else if (mapId == "Glacier" && level == 1) BuildGlacier1(ctx);
            else if (mapId == "Glacier" && level == 2) BuildGlacier2(ctx);
            else throw new ArgumentException("No layout for " + mapId + " level " + level + ".");

            if (ctx.Result.Finish == null) throw new InvalidOperationException("Level " + level + " created no finish pad.");
            if (ctx.Result.Checkpoints.Count == 0) throw new InvalidOperationException("Level " + level + " created no checkpoint.");
        }

        // ------------------------------------------------------------------ merged map (level 1 + level 2 in one scene)

        // Join between level 1's finish and level 2's start (metres). Level 2 is moved along X/Z only, so its
        // water / sand / scenery stays at the climate's height; a staircase walks down from level 1's (higher)
        // finish to level 2's start pad.
        private const float ConnectorMaxStepRise = 0.5f;  // drop per step (looks only: a hidden ramp is what is walked on)
        private const float ConnectorRampAngleDeg = 33f;  // stair pitch; under the controller's 50° slope limit, so walkable both ways
        private const float ConnectorWidth = 4f;          // each step along X
        private const float ConnectorRampThickness = 0.6f; // hidden walk-on ramp collider
        private const float ConnectorEndGap = 0f;         // stairs end flush on level 2's start pad (no jump)
        private const float MergedStartPadSize = 6f;      // level 2's start pad (same as ObbyTestSceneBuilder's StartPad)
        private const float MergedStartPadTopY = 0f;      // level 2's start pad top, level-2 local
        /// <summary>Drop scenery that falls on the other level's side of the join (bands overlap after the offset).</summary>
        private const bool CullSceneryAcrossJoin = true;

        /// <summary>
        /// Both levels lay the same big flat ground sheet (Desert quicksand: one mesh, -10 to 130 m) under their
        /// course; after level 2 is moved forward the two sheets overlap at the same height and flicker
        /// (z-fighting). Level 2's sheet is slid along +Z so it starts exactly where level 1's ends.
        /// </summary>
        private static void AbutGroundSheets(Transform scenery1, Transform scenery2)
        {
            if (scenery1 == null || scenery2 == null) return;
            foreach (MeshFilter sheet2 in scenery2.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!IsGroundSheet(sheet2)) continue;
                foreach (MeshFilter sheet1 in scenery1.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (!IsGroundSheet(sheet1) || sheet1.sharedMesh != sheet2.sharedMesh) continue;
                    Bounds b1 = sheet1.GetComponent<Renderer>().bounds, b2 = sheet2.GetComponent<Renderer>().bounds;
                    if (b2.min.z >= b1.max.z || b2.max.z <= b1.min.z) continue; // no overlap
                    sheet2.transform.position += new Vector3(0f, 0f, b1.max.z - b2.min.z);
                }
            }
        }

        private static bool IsGroundSheet(MeshFilter f)
        {
            Renderer r = f.sharedMesh != null ? f.GetComponent<Renderer>() : null;
            if (r == null) return false;
            Vector3 size = r.bounds.size;
            return size.x > 15f && size.z > 15f && size.y < 1f;
        }

        /// <summary>True when both levels of the map have a layout, so <see cref="BuildMerged"/> can build it.</summary>
        internal static bool HasMergedLayout(string mapId)
        {
            return HasLayout(mapId, 1) && HasLayout(mapId, 2);
        }

        /// <summary>
        /// Builds a map's level 1 and level 2 as ONE continuous course under <see cref="Inputs.Root"/>:
        /// level 1 under "Level_L1" (the root's "StartPad" is moved in there), a walk-down "Connector", then
        /// level 2 under "Level_L2", moved forward so its start pad sits just past level 1's finish. Level 1's
        /// finish becomes a checkpoint, level 2's start pad is a checkpoint too, level 2's finish (the weather
        /// dais) ends the course. Only level 2's Shard is kept (one Shard per map). Throws (after logging)
        /// on failure. Implements: story-004 (one scene per map, one Shard per map).
        /// </summary>
        internal static Result BuildMerged(string mapId, Inputs inputs)
        {
            try
            {
                if (inputs.Root == null) throw new ArgumentException("Inputs.Root is missing.");

                // --- level 1 (built at the origin, as before)
                Transform root1 = NewLevelRoot("Level_L1", inputs.Root);
                Transform startPad = inputs.Root.Find("StartPad");
                if (startPad != null) startPad.SetParent(root1, true); // StartIsland looks for it under In.Root
                Inputs in1 = inputs; in1.Root = root1;
                var ctx1 = new Context(mapId, 1, in1);
                BuildLevel(ctx1);

                // --- level 2 (built at the origin too, moved afterwards), with its own start pad = a checkpoint
                Transform root2 = NewLevelRoot("Level_L2", inputs.Root);
                Inputs in2 = inputs; in2.Root = root2;
                var ctx2 = new Context(mapId, 2, in2);
                Checkpoint l2Start = ObbyTestSceneBuilder.CreateCheckpoint("StartPad",
                    new ObbyTestSceneBuilder.PlatformSpec("StartPad", 0f, MergedStartPadTopY, 0f, MergedStartPadSize, MergedStartPadSize),
                    inputs.CheckpointMaterial, root2);
                BuildLevel(ctx2);
                Renderer l2StartCloth = AddFlag(ctx2, "StartPad_L2_Flag",
                    new Vector3(MergedStartPadSize * 0.5f - 0.5f, MergedStartPadTopY, 0f), inputs.CheckpointMaterial);
                SetFields(l2Start, ("tintRenderer", l2StartCloth));

                // --- level 1's finish no longer ends the run: it becomes a checkpoint (flag tints yellow -> green)
                FinishTrigger l1Finish = ctx1.Result.Finish;
                Transform l1Pad = l1Finish.transform.parent;
                Vector3 l1Top = ctx1.Result.FinishTop;
                float l1EdgeZ = l1Pad.position.z + l1Pad.lossyScale.z * 0.5f;
                Checkpoint l1End = FinishToCheckpoint(ctx1, l1Finish, l1Pad);

                // --- connector: steps down (or up) from level 1's finish height to level 2's start height
                float bottomY = IslandBottomY(ctx1.Scenery, "FinishPad_Island", ctx1.Result.HazardTopY - 2f);
                Transform connectorRoot = NewLevelRoot("Connector", inputs.Root);
                Inputs inC = inputs; inC.Root = connectorRoot;
                var ctxC = new Context(mapId, 1, inC)
                {
                    PadMaterial = ctx1.PadMaterial, PadColorSet = ctx1.PadColorSet, PadCapMaterial = ctx1.PadCapMaterial,
                };
                float drop = l1Top.y - MergedStartPadTopY;
                // Steep stairs (short), walked on via one hidden ramp through every step's downhill top edge, so the
                // steps never poke above it and the player walks up or down without jumping.
                int steps = Mathf.Max(0, Mathf.CeilToInt(Mathf.Abs(drop) / ConnectorMaxStepRise - 0.0001f) - 1);
                float runLength = Mathf.Abs(drop) / Mathf.Tan(ConnectorRampAngleDeg * Mathf.Deg2Rad);
                float stepDepth = steps > 0 ? runLength / (steps + 1) : 0f;
                for (int i = 1; i <= steps; i++)
                {
                    float top = l1Top.y - drop * i / (steps + 1);
                    // drop > 0 (down): each step's far edge sits on the ramp; going up, its near edge does.
                    float z = l1EdgeZ + stepDepth * (drop > 0f ? i - 0.5f : i + 0.5f);
                    Slab(ctxC, "Connector_Step_" + i, l1Top.x, top, z, ConnectorWidth, stepDepth,
                        inputs.PlatformMaterial, inputs.PlatformMaterial, bottomY);
                }
                if (steps > 0)
                {
                    float rampAngle = Mathf.Atan2(drop, runLength) * Mathf.Rad2Deg; // + = descending along +Z
                    float slopeLength = Mathf.Sqrt(drop * drop + runLength * runLength);
                    var surfaceMid = new Vector3(l1Top.x, (l1Top.y + MergedStartPadTopY) * 0.5f, l1EdgeZ + runLength * 0.5f);
                    Vector3 normal = Quaternion.Euler(rampAngle, 0f, 0f) * Vector3.up;
                    GameObject ramp = Prim(PrimitiveType.Cube, "Connector_WalkRamp", ctxC.Course,
                        surfaceMid - normal * (ConnectorRampThickness * 0.5f),
                        new Vector3(ConnectorWidth, ConnectorRampThickness, slopeLength + 0.2f),
                        inputs.PlatformMaterial, new Vector3(rampAngle, 0f, 0f), keepCollider: true);
                    ramp.GetComponent<MeshRenderer>().enabled = false;
                }
                float l2NearEdgeZ = l1EdgeZ + (steps > 0 ? runLength : 0f) + ConnectorEndGap;

                // --- move level 2 into place (X/Z only; every piece is under root2, so it all moves together)
                var offset = new Vector3(l1Top.x, 0f, l2NearEdgeZ + MergedStartPadSize * 0.5f);
                root2.position = offset;

                AbutGroundSheets(ctx1.Scenery, ctx2.Scenery);

                int culled = 0;
                if (CullSceneryAcrossJoin)
                {
                    float joinZ = (l1EdgeZ + l2NearEdgeZ) * 0.5f;
                    culled += CullScenery(ctx1.Scenery, z => z > joinZ);
                    culled += CullScenery(ctx2.Scenery, z => z < joinZ);
                }

                // --- one Shard per map: keep level 2's (the harder level); level 1's is removed
                var keptShards = new List<ShardPickup>();
                if (ctx2.Result.Shards.Count > 0)
                {
                    foreach (ShardPickup s in ctx1.Result.Shards)
                    {
                        if (s != null) Object.DestroyImmediate(s.gameObject);
                    }
                    keptShards.AddRange(ctx2.Result.Shards);
                }
                else
                {
                    Debug.LogWarning("[MapLevelBuilder] " + mapId + " level 2 placed no Shard; keeping level 1's.");
                    keptShards.AddRange(ctx1.Result.Shards);
                }
                keptShards.RemoveAll(s => s == null);
                if (keptShards.Count > 1)
                {
                    for (int i = 1; i < keptShards.Count; i++) Object.DestroyImmediate(keptShards[i].gameObject);
                    keptShards.RemoveRange(1, keptShards.Count - 1);
                    Debug.LogWarning("[MapLevelBuilder] " + mapId + ": more than one Shard placed; kept the first.");
                }

                // --- merged result
                var merged = new Result();
                merged.Checkpoints.AddRange(ctx1.Result.Checkpoints);
                merged.Checkpoints.Add(l1End);
                merged.Checkpoints.Add(l2Start);
                merged.Checkpoints.AddRange(ctx2.Result.Checkpoints);
                merged.Hazards.AddRange(ctx1.Result.Hazards);
                merged.Hazards.AddRange(ctx2.Result.Hazards);
                merged.Shards.AddRange(keptShards);
                merged.Finish = ctx2.Result.Finish;
                merged.FinishTop = ctx2.Result.FinishTop + offset;
                merged.SuctionSpotYaw = ctx2.Result.SuctionSpotYaw;
                merged.HazardTopY = Mathf.Min(ctx1.Result.HazardTopY, ctx2.Result.HazardTopY);
                merged.RecomputeCourseBounds();

                Debug.Log("[MapLevelBuilder] Built merged " + mapId + ": level 2 offset " + offset + ", " + steps
                          + " connector step(s), " + merged.Checkpoints.Count + " checkpoint(s), " + merged.Hazards.Count
                          + " trap(s), " + merged.Shards.Count + " shard(s), " + culled + " overlapping scenery piece(s) removed.");
                return merged;
            }
            catch (Exception e)
            {
                Debug.LogError("[MapLevelBuilder] FAILED to build merged " + mapId + ": " + e);
                throw;
            }
        }

        private static Transform NewLevelRoot(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }

        /// <summary>Swaps a level-1 finish trigger for a checkpoint on the same pad; its flag cloth becomes the checkpoint tint.</summary>
        private static Checkpoint FinishToCheckpoint(Context c, FinishTrigger finish, Transform pad)
        {
            GameObject trigger = finish.gameObject;
            Object.DestroyImmediate(finish);
            Checkpoint cp = trigger.AddComponent<Checkpoint>();
            pad.name = "Checkpoint_L1End";
            trigger.name = "Checkpoint_L1End_Trigger";
            Transform cloth = c.Course.Find("Finish_Flag_Cloth");
            Transform pole = c.Course.Find("Finish_Flag_Pole");
            if (pole != null) pole.name = "Checkpoint_L1End_Flag_Pole";
            if (cloth != null)
            {
                cloth.name = "Checkpoint_L1End_Flag_Cloth";
                SetFields(cp, ("tintRenderer", cloth.GetComponent<Renderer>()));
            }
            return cp;
        }

        /// <summary>Bottom of a named grass island under <paramref name="scenery"/>, or <paramref name="fallback"/>.</summary>
        private static float IslandBottomY(Transform scenery, string islandName, float fallback)
        {
            Transform island = scenery.Find(islandName);
            if (island == null) return fallback;
            Bounds b = RendererBounds(island.gameObject);
            return b.size.y > 0.01f ? b.min.y : fallback;
        }

        /// <summary>Destroys the direct children of <paramref name="scenery"/> whose world centre Z matches <paramref name="drop"/>. Returns how many.</summary>
        private static int CullScenery(Transform scenery, Func<float, bool> drop)
        {
            var doomed = new List<GameObject>();
            foreach (Transform child in scenery)
            {
                Bounds b = RendererBounds(child.gameObject);
                float z = b.size.sqrMagnitude > 0.0001f ? b.center.z : child.position.z;
                if (drop(z)) doomed.Add(child.gameObject);
            }
            foreach (GameObject go in doomed) Object.DestroyImmediate(go);
            return doomed.Count;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Creates a primitive without a collider (unless asked), with shadows off by default (WebGL mobile budget).</summary>
        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, Vector3 localEuler = default, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>
        /// Instantiates a KayKit model (Color1 set) as visual-only scenery: colliders removed, shadows off,
        /// every material replaced by the shared <paramref name="material"/> (the recolour for the climate).
        /// Logs a warning and returns null if the model is missing.
        /// </summary>
        private static GameObject PlaceModel(string modelName, Vector3 worldPosition, Vector3 euler, Vector3 scale,
            Material material, Transform parent, int colorSet = 1)
        {
            string path = colorSet == 1 ? KayKitFolder + modelName + "_Color1.fbx"
                : KayKitFolder.Replace("/Color1/", "/Color" + colorSet + "/") + modelName + "_Color" + colorSet + ".fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[MapLevelBuilder] Model not found, skipped: " + path);
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = modelName;
            go.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(euler));
            go.transform.localScale = scale;
            foreach (Collider col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                if (material == null) continue;
                var shared = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < shared.Length; i++) shared[i] = material;
                r.sharedMaterials = shared;
            }
            return go;
        }

        /// <summary>
        /// Instantiates a prop prefab (Meshy models in Assets/Art/Props) as visual only: colliders removed,
        /// shadows off, scaled uniformly so its wider side is <paramref name="width"/>, and placed with its
        /// bottom centre (or top centre when <paramref name="hangFromTop"/>) at <paramref name="anchor"/>.
        /// <paramref name="material"/> replaces its materials when given. Returns null if the prefab is missing.
        /// </summary>
        internal static GameObject PlacePrefab(string prefabPath, Transform parent, Vector3 anchor, float width,
            Material material = null, float yawDegrees = 0f, bool hangFromTop = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MapLevelBuilder] Prop prefab not found, using the stand-in: " + prefabPath);
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            // Keep the prefab's own rotation (the FBX import's axis fix) and turn it about world up.
            go.transform.SetPositionAndRotation(anchor, Quaternion.Euler(0f, yawDegrees, 0f) * prefab.transform.rotation);
            foreach (Collider col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                if (material == null) continue;
                var shared = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < shared.Length; i++) shared[i] = material;
                r.sharedMaterials = shared;
            }
            Bounds b = RendererBounds(go);
            float wide = Mathf.Max(b.size.x, b.size.z);
            if (wide > 0.0001f) go.transform.localScale = go.transform.localScale * (width / wide);
            b = RendererBounds(go);
            float y = hangFromTop ? b.max.y : b.min.y;
            go.transform.position += new Vector3(anchor.x - b.center.x, anchor.y - y, anchor.z - b.center.z);
            return go;
        }

        private const string CrystalFolder = "Assets/Art/Props/Rainforest/Crystal/";
        private const string CrystalSourcePrefabPath = CrystalFolder + "Crystal.prefab";
        private const string CrystalPrefabPath = CrystalFolder + "CrystalOnly.prefab";

        /// <summary>
        /// The Shard is the crystals alone: a copy of the Meshy crystal model with its grey rock base removed
        /// (triangles whose texture is not purple). Made once; the original model is not changed.
        /// Returns the prefab path to use (the original when the copy cannot be made).
        /// </summary>
        private static string CrystalOnlyPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefabPath) != null) return CrystalPrefabPath;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalSourcePrefabPath);
            MeshFilter filter = source != null ? source.GetComponentInChildren<MeshFilter>(true) : null;
            string texturePath = CrystalFolder + "Crystal_BaseColor.png";
            if (filter == null || filter.sharedMesh == null || !System.IO.File.Exists(texturePath)) return CrystalSourcePrefabPath;

            var tex = new Texture2D(2, 2);
            tex.LoadImage(System.IO.File.ReadAllBytes(texturePath));
            Mesh mesh = filter.sharedMesh;
            Vector2[] uv = mesh.uv;
            int[] tris = mesh.triangles;
            var kept = new List<int>(tris.Length);
            for (int t = 0; t < tris.Length; t += 3)
            {
                Vector2 u = (uv[tris[t]] + uv[tris[t + 1]] + uv[tris[t + 2]]) / 3f;
                Color.RGBToHSV(tex.GetPixelBilinear(u.x, u.y), out float h, out float s, out float v);
                bool crystal = h > 0.66f && h < 0.97f && s > 0.25f; // violet / magenta; the rocks are grey or moss green
                if (!crystal) continue;
                kept.Add(tris[t]); kept.Add(tris[t + 1]); kept.Add(tris[t + 2]);
            }
            Object.DestroyImmediate(tex);

            var only = Object.Instantiate(mesh);
            only.name = "CrystalOnly";
            only.subMeshCount = 1;
            only.SetTriangles(kept, 0);
            only.RecalculateBounds();
            AssetDatabase.CreateAsset(only, CrystalFolder + "CrystalOnly_Mesh.asset");

            var copy = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            copy.GetComponentInChildren<MeshFilter>(true).sharedMesh = only;
            PrefabUtility.SaveAsPrefabAsset(copy, CrystalPrefabPath);
            Object.DestroyImmediate(copy);
            Debug.Log("[MapLevelBuilder] Made the crystal-only Shard (" + kept.Count / 3 + " of " + tris.Length / 3 + " triangles kept).");
            return CrystalPrefabPath;
        }

        /// <summary>A material that uses the KayKit palette texture, tinted for the climate.</summary>
        /// <summary>
        /// A palette material whose green swatches (grass, leaves) are recoloured to <paramref name="green"/>
        /// (e.g. snow white, frosted green); greys and browns stay. The recoloured palette is made once
        /// as Assets/Art/Props/Palette/forest_texture_&lt;variant&gt;.png.
        /// </summary>
        private static Material RecolouredPaletteMat(string name, string variant, Color green, Color tint)
        {
            string folder = "Assets/Art/Props/Palette";
            string texPath = folder + "/forest_texture_" + variant + ".png";
            if (!System.IO.File.Exists(texPath))
            {
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Art/Props", "Palette");
                var tex = new Texture2D(2, 2);
                tex.LoadImage(System.IO.File.ReadAllBytes(PaletteTexturePath));
                Color[] px = tex.GetPixels();
                for (int i = 0; i < px.Length; i++)
                {
                    Color.RGBToHSV(px[i], out float h, out float s, out float v);
                    if (h < 0.15f || h > 0.50f || s < 0.2f) continue; // not a green swatch
                    // keep a little of the swatch's own light/dark so shading steps still read
                    Color c = green * Mathf.Lerp(0.85f, 1.05f, v);
                    c.a = px[i].a;
                    px[i] = c;
                }
                tex.SetPixels(px);
                System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
            }
            Material mat = ObbyTestSceneBuilder.GetOrCreateMaterial(name, tint);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material PaletteMat(string name, Color tint)
        {
            Material mat = ObbyTestSceneBuilder.GetOrCreateMaterial(name, tint);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PaletteTexturePath);
            if (texture == null) Debug.LogWarning("[MapLevelBuilder] Palette texture not found: " + PaletteTexturePath);
            else mat.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>A flat-colour material that also glows (the purple Shard).</summary>
        private static Material GlowMat(string name, Color color, float intensity)
        {
            Material mat = ObbyTestSceneBuilder.GetOrCreateMaterial(name, color);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>A see-through unlit material (wind streaks, air columns). Falls back to opaque if the URP Unlit shader is missing.</summary>
        private static Material AirMat(string name, Color color)
        {
            string path = ObbyTestSceneBuilder.MaterialFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", 5f);  // SrcAlpha
            mat.SetFloat("_DstBlend", 10f); // OneMinusSrcAlpha
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// Sets serialized fields on a component by name. Supports float, int, bool, string, Vector3,
        /// Color, Object references and null. A missing field logs an error naming the field.
        /// </summary>
        private static void SetFields(Object target, params (string name, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach ((string name, object value) in values)
            {
                SerializedProperty prop = so.FindProperty(name);
                if (prop == null)
                {
                    Debug.LogError("[MapLevelBuilder] Missing serialized field '" + name + "' on " + target.GetType().Name);
                    continue;
                }

                switch (value)
                {
                    case null: prop.objectReferenceValue = null; break;
                    case float f: prop.floatValue = f; break;
                    case int i:
                        if (prop.propertyType == SerializedPropertyType.Float) prop.floatValue = i;
                        else prop.intValue = i;
                        break;
                    case bool b: prop.boolValue = b; break;
                    case string s: prop.stringValue = s; break;
                    case Vector3 v: prop.vector3Value = v; break;
                    case Vector3[] arr:
                        prop.arraySize = arr.Length;
                        for (int k = 0; k < arr.Length; k++) prop.GetArrayElementAtIndex(k).vector3Value = arr[k];
                        break;
                    case Color col: prop.colorValue = col; break;
                    case Object o: prop.objectReferenceValue = o; break;
                    default:
                        Debug.LogError("[MapLevelBuilder] Unsupported value type " + value.GetType().Name + " for field '" + name + "'.");
                        break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// A flat pad (start / landing decks): the box collider stays for gameplay, and what you see is a
        /// KayKit grass hill (green top, rock sides) filling the same footprint down to the water.
        /// </summary>
        private static GameObject Slab(Context c, string name, float x, float topY, float z, float sizeX, float sizeZ, Material material, Material pillarMaterial, float pillarBottomY)
        {
            GameObject slab = ObbyTestSceneBuilder.CreatePlatform(name, new ObbyTestSceneBuilder.PlatformSpec(name, x, topY, z, sizeX, sizeZ), material, c.Course);
            if (GrassIsland(c, name + "_Island", x, topY, z, sizeX, sizeZ, pillarBottomY) != null)
                slab.GetComponent<MeshRenderer>().enabled = false;
            else
                Pillar(c, name + "_Pillar", x, z, topY - 0.9f, pillarBottomY, Mathf.Min(sizeX, sizeZ) * 0.5f - 0.2f, pillarMaterial);
            return slab;
        }

        /// <summary>
        /// Shows the level's start pad (built by ObbyTestSceneBuilder as a grey box) as a grass hill
        /// instead; the box keeps its collider.
        /// </summary>
        private static void StartIsland(Context c, float sizeX, float sizeZ, float bottomY)
        {
            Transform pad = c.In.Root.Find("StartPad");
            GameObject padGo = pad != null ? pad.gameObject : GameObject.Find("StartPad");
            if (GrassIsland(c, "StartPad_Island", 0f, 0f, 0f, sizeX, sizeZ, bottomY) != null && padGo != null)
                padGo.GetComponent<MeshRenderer>().enabled = false;
        }

        // KayKit grass hill blocks by height (footprint 8 x 8); the tallest one that is not stretched too far is picked.
        private static readonly (string model, float height)[] GrassHills =
        {
            ("Hill_8x8x2", 2f), ("Hill_8x8x4", 4f), ("Hill_8x8x8", 8f),
        };

        /// <summary>
        /// A KayKit grass hill (original green palette, visual only) scaled to cover
        /// <paramref name="sizeX"/> x <paramref name="sizeZ"/> with its grass top at <paramref name="topY"/>
        /// and its base at <paramref name="bottomY"/>. Returns null when the model is missing.
        /// </summary>
        private static GameObject GrassIsland(Context c, string name, float x, float topY, float z, float sizeX, float sizeZ, float bottomY,
            Material material = null, Transform parent = null)
        {
            float height = Mathf.Max(0.5f, topY - bottomY);
            string model = GrassHills[GrassHills.Length - 1].model;
            foreach ((string m, float h) in GrassHills)
            {
                if (h >= height * 0.75f) { model = m; break; }
            }

            GameObject go = PlaceModel(model, Vector3.zero, Vector3.zero, Vector3.one, material != null ? material : c.PadMaterial,
                parent != null ? parent : c.Scenery, material != null ? 1 : c.PadColorSet);
            if (go == null) return null;
            // Walkable ground: Sam's shadow must land on it (scenery props stay unshadowed for the mobile budget).
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.receiveShadows = true;
            Bounds b = RendererBounds(go);
            if (b.size.x < 0.01f || b.size.y < 0.01f || b.size.z < 0.01f) return go;
            go.transform.localScale = new Vector3(sizeX / b.size.x, height / b.size.y, sizeZ / b.size.z);
            b = RendererBounds(go);
            go.transform.position += new Vector3(x - b.center.x, topY - b.max.y, z - b.center.z);
            go.name = name;
            if (c.PadCapMaterial != null)
            {
                // A thin snow / ice layer over the hill's top (the KayKit palettes have no white).
                GameObject cap = Prim(PrimitiveType.Cube, name + "_Cap", go.transform.parent, Vector3.zero,
                    new Vector3(Mathf.Max(0.1f, sizeX - 0.1f), 0.3f, Mathf.Max(0.1f, sizeZ - 0.1f)), c.PadCapMaterial);
                cap.transform.position = new Vector3(x, topY + 0.03f - 0.15f, z);
            }
            return go;
        }

        private static Bounds RendererBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        /// <summary>A stone column rising out of the water under a pad (visual only; the pad has the collider).</summary>
        private static void Pillar(Context c, string name, float x, float z, float topY, float bottomY, float radius, Material material)
        {
            float height = topY - bottomY;
            if (height <= 0.05f) return;
            Prim(PrimitiveType.Cylinder, name, c.Scenery, new Vector3(x, bottomY + height * 0.5f, z),
                new Vector3(radius * 2f, height * 0.5f, radius * 2f), material);
        }

        // Flags are about Sam's height (1.4 m) so they read as markers, not towers.
        private const float FlagPoleHeight = 1.4f;
        private static readonly Vector3 FlagClothSize = new Vector3(0.62f, 0.4f, 0.04f);

        /// <summary>A short flag (pole + cloth) standing on <paramref name="groundPoint"/>; returns the cloth's renderer.</summary>
        private static Renderer AddFlag(Context c, string name, Vector3 groundPoint, Material cloth)
        {
            Prim(PrimitiveType.Cylinder, name + "_Pole", c.Course, groundPoint + Vector3.up * (FlagPoleHeight * 0.5f),
                new Vector3(0.08f, FlagPoleHeight * 0.5f, 0.08f), c.In.WhiteMaterial);
            GameObject flag = Prim(PrimitiveType.Cube, name + "_Cloth", c.Course,
                groundPoint + new Vector3(FlagClothSize.x * 0.5f + 0.04f, FlagPoleHeight - FlagClothSize.y * 0.5f - 0.02f, 0f),
                FlagClothSize, cloth);
            return flag.GetComponent<Renderer>();
        }

        /// <summary>Hides a gameplay box (the grass hill under it is what shows) and removes the old tall finish flag.</summary>
        private static void HidePad(Context c, GameObject pad)
        {
            if (pad != null && pad.TryGetComponent(out MeshRenderer r)) r.enabled = false;
            foreach (string old in new[] { "FinishPole", "FinishFlag" })
            {
                Transform t = c.Course.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
        }

        /// <summary>
        /// A checkpoint: green grass ground with a short flag at its edge. The flag cloth is yellow until the
        /// player reaches it, then green (Checkpoint tint).
        /// </summary>
        private static Checkpoint AddCheckpoint(Context c, string name, float x, float topY, float z, float size, Material pillarMaterial, float pillarBottomY)
        {
            Checkpoint cp = ObbyTestSceneBuilder.CreateCheckpoint(name,
                new ObbyTestSceneBuilder.PlatformSpec(name, x, topY, z, size, size), c.In.CheckpointMaterial, c.Course);
            if (GrassIsland(c, name + "_Island", x, topY, z, size, size, pillarBottomY) != null)
                HidePad(c, cp.transform.parent.gameObject);
            else
                Pillar(c, name + "_Pillar", x, z, topY - 0.9f, pillarBottomY, size * 0.5f - 0.3f, pillarMaterial);
            Renderer cloth = AddFlag(c, name + "_Flag", new Vector3(x + size * 0.5f - 0.5f, topY, z), c.In.CheckpointMaterial);
            SetFields(cp, ("tintRenderer", cloth));
            c.Result.Checkpoints.Add(cp);
            return cp;
        }

        /// <summary>
        /// Level 1 finish: green ground with a short magenta flag. Level 2 finish
        /// (<paramref name="weatherDais"/>): green ground, a step and a raised dais (no flag); the weather
        /// floats past the dais and walking up onto the dais starts the suction, Sam facing the weather (+Z).
        /// </summary>
        private static void AddFinish(Context c, float x, float topY, float z, float sizeX, float sizeZ, Material pillarMaterial, float pillarBottomY,
            bool weatherDais = false)
        {
            if (!weatherDais)
            {
                c.Result.Finish = ObbyTestSceneBuilder.CreateFinish(
                    new ObbyTestSceneBuilder.PlatformSpec("FinishPad", x, topY, z, sizeX, sizeZ), c.In.FinishMaterial, c.In.WhiteMaterial, c.Course);
                c.Result.FinishTop = new Vector3(x, topY, z);
                if (GrassIsland(c, "FinishPad_Island", x, topY, z, sizeX, sizeZ, pillarBottomY) != null)
                    HidePad(c, c.Result.Finish.transform.parent.gameObject);
                else
                    Pillar(c, "FinishPad_Pillar", x, z, topY - 0.9f, pillarBottomY, Mathf.Min(sizeX, sizeZ) * 0.5f - 0.3f, pillarMaterial);
                AddFlag(c, "Finish_Flag", new Vector3(x + sizeX * 0.5f - 0.5f, topY, z), c.In.FinishMaterial);
                return;
            }

            // Ground deck, then a step (+DaisStep) and the dais (+2 x DaisStep) toward the far end.
            Slab(c, "FinishGround", x, topY, z, sizeX, sizeZ, c.In.PlatformMaterial, pillarMaterial, pillarBottomY);
            float stepTop = topY + DaisStep, daisTop = topY + DaisStep * 2f;
            float daisZ = z + sizeZ * 0.5f - DaisSize * 0.5f - 0.3f;
            float stepZ = daisZ - DaisSize * 0.5f - DaisStepDepth * 0.5f;
            Slab(c, "FinishStep", x, stepTop, stepZ, DaisSize, DaisStepDepth, c.In.PlatformMaterial, pillarMaterial, pillarBottomY);
            c.Result.Finish = ObbyTestSceneBuilder.CreateFinish(
                new ObbyTestSceneBuilder.PlatformSpec("FinishPad", x, daisTop, daisZ, DaisSize, DaisSize), c.In.FinishMaterial, c.In.WhiteMaterial, c.Course);
            if (GrassIsland(c, "FinishDais_Island", x, daisTop, daisZ, DaisSize, DaisSize, pillarBottomY) != null)
                HidePad(c, c.Result.Finish.transform.parent.gameObject);
            else
                HidePad(c, null);
            c.Result.FinishTop = new Vector3(x, daisTop, daisZ);
            c.Result.SuctionSpotYaw = 0f;
        }

        // The weather dais at the end of a level 2 (metres).
        private const float DaisStep = 0.5f;      // each rise (walkable without jumping: stepOffset 0.3 + a hop)
        private const float DaisSize = 3.6f;      // dais footprint (square)
        private const float DaisStepDepth = 1.4f; // the step in front of it

        /// <summary>
        /// The hidden Shard: a purple glowing crystal cluster on a trigger, spinning and bobbing.
        /// Stand-in model: tall KayKit rock spires recoloured purple (no crystal model in the project).
        /// </summary>
        private static ShardPickup AddShard(Context c, string mapId, Vector3 position, Material glowMaterial,
            float crystalScale, float triggerRadius)
        {
            var root = new GameObject("Shard_" + mapId);
            root.transform.SetParent(c.Course, false);
            root.transform.position = position;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = triggerRadius;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            // The purple crystal cluster (Meshy model, its own glowing material), centred on the pickup.
            float crystalWidth = 1.2f * (crystalScale / 0.4f);
            if (PlacePrefab(CrystalOnlyPrefab(), visual.transform, position + Vector3.down * crystalWidth * 0.6f, crystalWidth) != null)
            {
                ShardPickup crystalPickup = root.AddComponent<ShardPickup>();
                SetFields(crystalPickup, ("mapId", mapId.ToLowerInvariant()), ("visual", visual.transform));
                c.Result.Shards.Add(crystalPickup);
                return crystalPickup;
            }

            // Stand-in: Rock_4_C is a 3 m spire: centre it on the pickup position.
            float half = 1.5f * crystalScale;
            GameObject main = PlaceModel("Rock_4_C", position + Vector3.down * half, Vector3.zero,
                new Vector3(crystalScale, crystalScale, crystalScale), glowMaterial, visual.transform);
            if (main == null)
            {
                Prim(PrimitiveType.Sphere, "Crystal", visual.transform, Vector3.zero,
                    new Vector3(0.6f, 1.0f, 0.6f) * (crystalScale / 0.4f), glowMaterial);
            }
            else
            {
                PlaceModel("Rock_4_D", position + new Vector3(0.38f, -half * 0.8f, 0f), new Vector3(0f, 40f, 28f),
                    new Vector3(crystalScale * 0.6f, crystalScale * 0.5f, crystalScale * 0.6f), glowMaterial, visual.transform);
                PlaceModel("Rock_4_D", position + new Vector3(-0.34f, -half * 0.8f, 0.1f), new Vector3(0f, 200f, -30f),
                    new Vector3(crystalScale * 0.5f, crystalScale * 0.45f, crystalScale * 0.5f), glowMaterial, visual.transform);
            }

            ShardPickup pickup = root.AddComponent<ShardPickup>();
            SetFields(pickup, ("mapId", mapId.ToLowerInvariant()), ("visual", visual.transform));
            c.Result.Shards.Add(pickup);
            return pickup;
        }
    }
}
#endif
