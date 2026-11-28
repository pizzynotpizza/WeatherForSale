using UnityEngine;
using UnityEngine.Rendering;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Places the storybook-town Meshy models (Assets/Art/Props/Town/<Model>/: houses, towers, street lamps, fountain,
    /// market stalls, flower barrels) for the Shop backdrop and the Tempest arena, after the cover and the
    /// opening / "Tempest appears" CGI. Visual only, no colliders. A model that is not downloaded is skipped.
    /// </summary>
    internal static class TownDressing
    {
        /// <summary>One town piece: which model, where its bottom centre stands, which way it faces, how tall.</summary>
        internal readonly struct Piece
        {
            public readonly string Model;
            public readonly Vector3 Position;
            public readonly float Yaw;
            public readonly float Height;

            public Piece(string model, float x, float y, float z, float yaw, float height)
            {
                Model = model;
                Position = new Vector3(x, y, z);
                Yaw = yaw;
                Height = height;
            }
        }

        /// <summary>Extra turn so a model's door / front faces the way <see cref="Piece.Yaw"/> says (Meshy fronts face -Z).</summary>
        private const float ModelFrontYaw = 0f;

        /// <summary>
        /// Places every piece under a new <paramref name="rootName"/> object. Street lamps also get a warm point
        /// light in their lanterns when <paramref name="lampLights"/> is above 0 (the first that many lamps only).
        /// </summary>
        public static GameObject Place(Transform parent, string rootName, Piece[] pieces, int lampLights = 0,
            float lampLightIntensity = 2.2f, float lampLightRange = 9f)
        {
            var root = new GameObject(rootName);
            root.transform.SetParent(parent, false);
            int lightsLeft = lampLights;
            foreach (Piece piece in pieces)
            {
                MeshyPropBaker.PropMesh prop = MeshyPropBaker.BakeProp("Town/" + piece.Model, piece.Model, MeshyPropBaker.ZUp); // Assets/Art/Props/Town/<Model>/<Model>.fbx
                if (prop == null) continue;
                var holder = new GameObject(piece.Model);
                holder.transform.SetParent(root.transform, false);
                holder.transform.SetPositionAndRotation(piece.Position, Quaternion.Euler(0f, piece.Yaw + ModelFrontYaw, 0f));
                GameObject mesh = MeshyPropBaker.PlaceProp(prop, piece.Model + "_Mesh", holder.transform, Vector3.zero, new Vector3(0f, piece.Height, 0f));
                var renderer = mesh.GetComponent<MeshRenderer>();
                // Big buildings cast no shadows (cheap on WebGL); the small props do.
                renderer.shadowCastingMode = piece.Height > 6f ? ShadowCastingMode.Off : ShadowCastingMode.On;

                if (piece.Model == "StreetLamp" && lightsLeft > 0)
                {
                    lightsLeft--;
                    var lightGo = new GameObject("LanternLight");
                    lightGo.transform.SetParent(holder.transform, false);
                    lightGo.transform.localPosition = new Vector3(0f, piece.Height * 0.82f, 0f);
                    var light = lightGo.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1f, 0.78f, 0.45f);
                    light.intensity = lampLightIntensity;
                    light.range = lampLightRange;
                    light.shadows = LightShadows.None;
                }
            }
            return root;
        }
    }
}
