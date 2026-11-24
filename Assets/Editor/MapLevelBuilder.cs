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
            return mapId == "Rainforest" && (level == 1 || level == 2);
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
                if (mapId == "Rainforest" && level == 1) BuildRainforest1(ctx);
                else if (mapId == "Rainforest" && level == 2) BuildRainforest2(ctx);
                else throw new ArgumentException("No layout for " + mapId + " level " + level + ".");

                if (ctx.Result.Finish == null) throw new InvalidOperationException("The layout created no finish pad.");
                if (ctx.Result.Checkpoints.Count == 0) throw new InvalidOperationException("The layout created no checkpoint.");
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
            Material material, Transform parent)
        {
            string path = KayKitFolder + modelName + "_Color1.fbx";
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

        /// <summary>A material that uses the KayKit palette texture, tinted for the climate.</summary>
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
        private static GameObject GrassIsland(Context c, string name, float x, float topY, float z, float sizeX, float sizeZ, float bottomY)
        {
            float height = Mathf.Max(0.5f, topY - bottomY);
            string model = GrassHills[GrassHills.Length - 1].model;
            foreach ((string m, float h) in GrassHills)
            {
                if (h >= height * 0.75f) { model = m; break; }
            }

            GameObject go = PlaceModel(model, Vector3.zero, Vector3.zero, Vector3.one, null, c.Scenery);
            if (go == null) return null;
            Bounds b = RendererBounds(go);
            if (b.size.x < 0.01f || b.size.y < 0.01f || b.size.z < 0.01f) return go;
            go.transform.localScale = new Vector3(sizeX / b.size.x, height / b.size.y, sizeZ / b.size.z);
            b = RendererBounds(go);
            go.transform.position += new Vector3(x - b.center.x, topY - b.max.y, z - b.center.z);
            go.name = name;
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

            // Rock_4_C is a 3 m spire: centre it on the pickup position.
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
