using UnityEngine;

namespace WeatherForSale.Boss
{
    /// <summary>
    /// A smooth round disc mesh for floor markings and warning circles. The built-in cylinder has only a
    /// handful of sides, so a big circle on it shows corners; this one has enough sides to look round.
    /// The mesh has the same size as the built-in cylinder (radius 0.5, height 2 centred), so a disc scaled
    /// to (diameter, thickness / 2, diameter) matches the same scale the cylinder would use.
    /// </summary>
    public static class BossShapes
    {
        private static Mesh disc;

        /// <summary>Sides of the disc.</summary>
        public const int Segments = 96;

        /// <summary>The shared disc mesh (built on first use).</summary>
        public static Mesh Disc
        {
            get
            {
                if (disc == null) disc = BuildDisc(Segments);
                return disc;
            }
        }

        /// <summary>
        /// Builds a flat ring (an annulus) with a height, in local space around the origin: top, bottom and both walls.
        /// A <paramref name="gapDegrees"/> above 0 leaves a gap of that width centred on <paramref name="gapCentreDegrees"/>
        /// (0 = a full, closed ring).
        /// </summary>
        public static Mesh BuildRing(float innerRadius, float outerRadius, float height, float gapCentreDegrees, float gapDegrees, int segments)
        {
            float sweep = 360f - Mathf.Clamp(gapDegrees, 0f, 340f);
            float start = gapCentreDegrees + Mathf.Clamp(gapDegrees, 0f, 340f) * 0.5f;
            int columns = segments + 1;
            var vertices = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            // Four strips: top, bottom, outer wall, inner wall; each has two vertices per column.
            void Strip(Vector3 a0, Vector3 a1, bool useRadiusA, float yA, float yB, float rA, float rB, Vector3 normal, bool flip)
            {
                int first = vertices.Count;
                for (int i = 0; i < columns; i++)
                {
                    float ang = (start + sweep * i / segments) * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                    vertices.Add(dir * rA + Vector3.up * yA);
                    vertices.Add(dir * rB + Vector3.up * yB);
                    Vector3 n = normal == Vector3.zero ? dir * (useRadiusA ? 1f : -1f) : normal;
                    normals.Add(n); normals.Add(n);
                }
                for (int i = 0; i < segments; i++)
                {
                    int a = first + i * 2, b = a + 1, c = a + 2, d = a + 3;
                    if (flip) { triangles.AddRange(new[] { a, c, b, b, c, d }); }
                    else { triangles.AddRange(new[] { a, b, c, b, d, c }); }
                }
            }

            float h = height;
            Strip(default, default, true, h, h, innerRadius, outerRadius, Vector3.up, false);        // top
            Strip(default, default, true, 0f, 0f, innerRadius, outerRadius, Vector3.down, true);     // bottom
            Strip(default, default, true, 0f, h, outerRadius, outerRadius, Vector3.zero, true);      // outer wall
            Strip(default, default, false, 0f, h, innerRadius, innerRadius, Vector3.zero, false);    // inner wall

            var mesh = new Mesh { name = "BossRing" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Builds a capped cylinder with <paramref name="segments"/> sides, radius 0.5 and height 2. UVs: the caps are mapped flat (0..1 across), the side wraps once around.</summary>
        public static Mesh BuildDisc(int segments)
        {
            var vertices = new Vector3[2 + segments * 4];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            uvs[0] = uvs[1] = new Vector2(0.5f, 0.5f);
            var triangles = new int[segments * 12];

            // Top and bottom centres.
            vertices[0] = new Vector3(0f, 1f, 0f); normals[0] = Vector3.up;
            vertices[1] = new Vector3(0f, -1f, 0f); normals[1] = Vector3.down;
            int topRing = 2, bottomRing = 2 + segments, sideTop = 2 + segments * 2, sideBottom = 2 + segments * 3;
            for (int i = 0; i < segments; i++)
            {
                float a = 2f * Mathf.PI * i / segments;
                float x = Mathf.Cos(a) * 0.5f, z = Mathf.Sin(a) * 0.5f;
                vertices[topRing + i] = new Vector3(x, 1f, z); normals[topRing + i] = Vector3.up;
                vertices[bottomRing + i] = new Vector3(x, -1f, z); normals[bottomRing + i] = Vector3.down;
                var n = new Vector3(x, 0f, z).normalized;
                vertices[sideTop + i] = new Vector3(x, 1f, z); normals[sideTop + i] = n;
                vertices[sideBottom + i] = new Vector3(x, -1f, z); normals[sideBottom + i] = n;
                uvs[topRing + i] = uvs[bottomRing + i] = new Vector2(x + 0.5f, z + 0.5f);
                uvs[sideTop + i] = new Vector2((float)i / segments, 1f);
                uvs[sideBottom + i] = new Vector2((float)i / segments, 0f);
            }
            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                int j = (i + 1) % segments;
                triangles[t++] = 0; triangles[t++] = topRing + j; triangles[t++] = topRing + i;                 // top fan
                triangles[t++] = 1; triangles[t++] = bottomRing + i; triangles[t++] = bottomRing + j;           // bottom fan
                triangles[t++] = sideTop + i; triangles[t++] = sideTop + j; triangles[t++] = sideBottom + i;    // side quad
                triangles[t++] = sideTop + j; triangles[t++] = sideBottom + j; triangles[t++] = sideBottom + i;
            }
            var mesh = new Mesh { name = "BossDisc" + segments, vertices = vertices, normals = normals, uv = uvs, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
