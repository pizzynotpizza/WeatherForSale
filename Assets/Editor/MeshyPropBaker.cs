using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Turns the Meshy-generated prop models (Assets/Art/Props/Glider, Assets/Art/Props/Hawk) into game-ready
    /// mesh assets: the model is baked through its import transform, re-oriented and scaled to game metres, and
    /// cut into parts by triangle (the glider's control frame is folded down under the wing; the hawk's wings
    /// are split off so they can flap). Meshes and materials are saved under each prop's "Generated" folder
    /// and rebuilt every time a map is built. Returns null when a model is missing, so builders fall back to
    /// their primitive stand-ins. Implements: story-004 (Valley glide: Meshy glider and hawks).
    /// </summary>
    public static class MeshyPropBaker
    {
        private const string GliderModelPath = "Assets/Art/Props/Glider/Glider.fbx";
        private const string GliderTexturePath = "Assets/Art/Props/Glider/Glider_BaseColor.png";
        private const string HawkModelPath = "Assets/Art/Props/Hawk/Hawk.fbx";
        private const string HawkTexturePath = "Assets/Art/Props/Hawk/Hawk_BaseColor.png";

        // Glider (model space after import: wing flat in XZ, nose +Z, span 1.9 m; its own control frame lies flat
        // behind the keel from about z -0.5 and is dropped: Sam's short arms cannot reach a bar over the big head,
        // so MapLevelBuilder.Valley builds a frame with a handle pole each side of the head instead).
        private const float GliderSpan = 2.2f;              // game wingspan, metres
        private const float GliderFrameHingeZ = -0.5f;      // model z where the frame starts (frame = z below this, near the keel)
        private const float GliderFrameHalfWidth = 0.34f;   // model |x| of the frame

        // Hawk (model space after import: wings spread along X, upright, facing -Z, span 1.9 m; checked in play:
        // Hawk turns its root to face its travel, so the baked mesh must face +Z).
        private const float HawkSpan = 2.6f;
        private const float HawkFlightPitch = 50f;          // tips the upright pose forward into level flight, degrees
        private const float HawkWingRootX = 0.2f;           // model |x| where a wing starts

        /// <summary>The baked glider wing (no frame): origin at the keel under the wing's middle (lowest point of the wing, centre of its chord), nose +Z.</summary>
        public sealed class GliderParts
        {
            public Mesh Mesh;
            public Material Material;
        }

        /// <summary>The baked hawk: body plus two wings, each wing's origin at its shoulder hinge.</summary>
        public sealed class HawkParts
        {
            public Mesh Body, LeftWing, RightWing;
            public Vector3 LeftHinge, RightHinge;
            public Material Material;
        }

        /// <summary>Bakes the Meshy glider's wing, without its frame (null when the model is missing).</summary>
        public static GliderParts BakeGlider()
        {
            if (!TryReadModel(GliderModelPath, out Vector3[] pos, out Vector2[] uv, out int[] tris)) return null;
            float s = GliderSpan / SpanX(pos);

            var kept = new List<int>();
            for (int t = 0; t < tris.Length; t += 3)
            {
                Vector3 c = (pos[tris[t]] + pos[tris[t + 1]] + pos[tris[t + 2]]) / 3f;
                if (c.z < GliderFrameHingeZ && Mathf.Abs(c.x) < GliderFrameHalfWidth) continue; // the model's frame
                kept.Add(t);
            }
            var wing = new Bounds(pos[tris[kept[0]]], Vector3.zero);
            foreach (int t in kept)
            {
                for (int k = 0; k < 3; k++) wing.Encapsulate(pos[tris[t + k]]);
            }
            var origin = new Vector3(0f, wing.min.y, wing.center.z);

            var builder = new MeshBuilder();
            foreach (int t in kept)
            {
                for (int k = 0; k < 3; k++) builder.Add((pos[tris[t + k]] - origin) * s, uv[tris[t + k]]);
            }
            return new GliderParts
            {
                Mesh = builder.Save("Assets/Art/Props/Glider/Generated/Glider_Baked.asset", "Glider_Baked"),
                Material = GetMaterial("Assets/Art/Props/Glider/Generated/Glider_Mat.mat", GliderTexturePath),
            };
        }

        /// <summary>Bakes the Meshy hawk in level flight, facing +Z (null when the model is missing).</summary>
        public static HawkParts BakeHawk()
        {
            if (!TryReadModel(HawkModelPath, out Vector3[] pos, out Vector2[] uv, out int[] tris)) return null;
            float s = HawkSpan / SpanX(pos);
            Quaternion orient = Quaternion.Euler(HawkFlightPitch, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f);
            Vector3 centre = Centre(pos);

            var body = new MeshBuilder();
            var left = new MeshBuilder();
            var right = new MeshBuilder();
            Vector3 leftHinge = Vector3.zero, rightHinge = Vector3.zero;
            int leftCount = 0, rightCount = 0;
            for (int t = 0; t < tris.Length; t += 3)
            {
                Vector3 c = (pos[tris[t]] + pos[tris[t + 1]] + pos[tris[t + 2]]) / 3f;
                // After the 180° turn the model's +X is the hawk's left (-X in game).
                MeshBuilder part = c.x > HawkWingRootX ? left : c.x < -HawkWingRootX ? right : body;
                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = orient * (pos[tris[t + k]] - centre) * s;
                    part.Add(p, uv[tris[t + k]]);
                    if (part != body && Mathf.Abs(Mathf.Abs(pos[tris[t + k]].x) - HawkWingRootX) < 0.05f)
                    {
                        if (part == left) { leftHinge += p; leftCount++; }
                        else { rightHinge += p; rightCount++; }
                    }
                }
            }
            leftHinge = leftCount > 0 ? leftHinge / leftCount : Vector3.left * HawkWingRootX * s;
            rightHinge = rightCount > 0 ? rightHinge / rightCount : Vector3.right * HawkWingRootX * s;
            left.Offset(-leftHinge);
            right.Offset(-rightHinge);

            const string dir = "Assets/Art/Props/Hawk/Generated/";
            return new HawkParts
            {
                Body = body.Save(dir + "Hawk_Body.asset", "Hawk_Body"),
                LeftWing = left.Save(dir + "Hawk_WingL.asset", "Hawk_WingL"),
                RightWing = right.Save(dir + "Hawk_WingR.asset", "Hawk_WingR"),
                LeftHinge = leftHinge,
                RightHinge = rightHinge,
                Material = GetMaterial(dir + "Hawk_Mat.mat", HawkTexturePath),
            };
        }

        /// <summary>Adds a mesh renderer child (no collider) showing <paramref name="mesh"/>.</summary>
        public static GameObject AddMeshChild(string name, Transform parent, Mesh mesh, Material material, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.On;
            return go;
        }

        // ------------------------------------------------------------------ helpers

        private static bool TryReadModel(string path, out Vector3[] positions, out Vector2[] uvs, out int[] triangles)
        {
            positions = null;
            uvs = null;
            triangles = null;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            MeshFilter mf = model != null ? model.GetComponentInChildren<MeshFilter>() : null;
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning("[MeshyPropBaker] " + path + " is missing; using the primitive stand-in.");
                return false;
            }
            // Import transform (Meshy FBX: root at 0.01 with a -90° X child), normalised to metres by the span later.
            Matrix4x4 m = model.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            Vector3[] v = mf.sharedMesh.vertices;
            positions = new Vector3[v.Length];
            for (int i = 0; i < v.Length; i++) positions[i] = m.MultiplyPoint3x4(v[i]);
            float span = SpanX(positions);
            if (span < 1e-6f) return false;
            float toModelMetres = 1.9f / span; // Meshy exports ~1.9 m across; work in that space so constants above hold
            for (int i = 0; i < positions.Length; i++) positions[i] *= toModelMetres;
            Vector3 c = Centre(positions);
            for (int i = 0; i < positions.Length; i++) positions[i] -= new Vector3(c.x, c.y, c.z);
            uvs = mf.sharedMesh.uv;
            if (uvs == null || uvs.Length != v.Length) uvs = new Vector2[v.Length];
            triangles = mf.sharedMesh.triangles;
            return true;
        }

        private static float SpanX(Vector3[] p)
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (Vector3 v in p)
            {
                min = Mathf.Min(min, v.x);
                max = Mathf.Max(max, v.x);
            }
            return max - min;
        }

        private static Vector3 Centre(Vector3[] p)
        {
            var b = new Bounds(p[0], Vector3.zero);
            foreach (Vector3 v in p) b.Encapsulate(v);
            return b.center;
        }

        private static Material GetMaterial(string path, string texturePath)
        {
            EnsureFolder(path);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.15f);
            mat.SetFloat("_Cull", (float)CullMode.Off); // thin cloth and feathers: both faces
            mat.doubleSidedGI = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureFolder(string assetPath)
        {
            string folder = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        /// <summary>Collects unshared triangles (position + uv) and saves them as a mesh asset.</summary>
        private sealed class MeshBuilder
        {
            private readonly List<Vector3> positions = new List<Vector3>();
            private readonly List<Vector2> uvs = new List<Vector2>();

            public void Add(Vector3 p, Vector2 uv)
            {
                positions.Add(p);
                uvs.Add(uv);
            }

            public void Offset(Vector3 delta)
            {
                for (int i = 0; i < positions.Count; i++) positions[i] += delta;
            }

            public Mesh Save(string path, string name)
            {
                var indices = new int[positions.Count];
                for (int i = 0; i < indices.Length; i++) indices[i] = i;
                var mesh = new Mesh { name = name, indexFormat = positions.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(positions);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                EnsureFolder(path);
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(mesh, path);
                    return mesh;
                }
                EditorUtility.CopySerialized(mesh, existing);
                existing.name = name;
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }
        }
    }
}
