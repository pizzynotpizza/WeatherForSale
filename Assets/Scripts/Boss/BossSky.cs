using System.Collections;
using UnityEngine;

namespace WeatherForSale.Boss
{
    /// <summary>
    /// The sky of the boss arena: a swirling storm sphere that calms down when the boss is sealed, a
    /// lightning flash, and the rainbow of the True Ending. The swirl itself is the shader WeatherForSale/BossSkySwirl.
    /// </summary>
    public sealed class BossSky : MonoBehaviour
    {
        private static readonly int CalmId = Shader.PropertyToID("_Calm");

        [SerializeField, Tooltip("Renderer of the inside-out sky sphere.")] private Renderer skyRenderer;
        [SerializeField] private Light sun;
        [SerializeField, Min(0.1f)] private float calmSeconds = 3f;
        [SerializeField] private Color stormSun = new Color(0.82f, 0.84f, 0.92f);
        [SerializeField] private Color calmSun = new Color(1f, 0.95f, 0.8f);
        [SerializeField, Min(1f), Tooltip("How far in front of Sam the rainbow stands, metres.")] private float rainbowDistance = 46f;
        [SerializeField, Min(1f)] private float rainbowRadius = 34f;

        private Material skyMaterial;
        private float sunIntensity = 1f;
        private GameObject rainbow;

        private void Awake()
        {
            if (skyRenderer != null)
            {
                skyMaterial = skyRenderer.material; // an instance: the asset is never changed
                skyMaterial.SetFloat(CalmId, 0f);
            }
            if (sun != null)
            {
                sunIntensity = sun.intensity;
                sun.color = stormSun;
            }
        }

        /// <summary>Brief lightning flash of the sun light.</summary>
        public void Flash()
        {
            if (sun != null) StartCoroutine(FlashRoutine());
        }

        /// <summary>The weather stops raging: the swirl fades to a clear sky and the light turns warm.</summary>
        public void Calm()
        {
            StartCoroutine(CalmRoutine());
        }

        /// <summary>Draws the rainbow arc in the sky (True Ending look).</summary>
        public void ShowRainbow(Vector3 origin, Vector3 forward)
        {
            if (rainbow != null) return;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 rainbowCentre = origin + forward * rainbowDistance + Vector3.down * 6f;
            rainbow = new GameObject("Rainbow");
            rainbow.transform.SetParent(transform, false);
            Color[] bands =
            {
                new Color(1f, 0.25f, 0.25f), new Color(1f, 0.6f, 0.15f), new Color(1f, 0.95f, 0.3f),
                new Color(0.4f, 0.9f, 0.4f), new Color(0.3f, 0.7f, 1f), new Color(0.35f, 0.4f, 1f), new Color(0.7f, 0.4f, 1f),
            };
            Material mat = new Material(Shader.Find("Sprites/Default"));
            for (int b = 0; b < bands.Length; b++)
            {
                var go = new GameObject("Band" + b);
                go.transform.SetParent(rainbow.transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = mat;
                line.useWorldSpace = true;
                line.startColor = line.endColor = bands[b];
                line.startWidth = line.endWidth = 1.5f;
                line.numCapVertices = 6;
                const int points = 40;
                line.positionCount = points;
                float r = rainbowRadius - b * 1.45f;
                for (int i = 0; i < points; i++)
                {
                    float a = Mathf.Lerp(Mathf.PI * 0.08f, Mathf.PI * 0.92f, i / (points - 1f));
                    line.SetPosition(i, rainbowCentre + right * (Mathf.Cos(a) * r) + Vector3.up * (Mathf.Sin(a) * r));
                }
            }
        }

        private IEnumerator FlashRoutine()
        {
            sun.intensity = sunIntensity * 1.8f;
            yield return new WaitForSeconds(0.09f);
            sun.intensity = sunIntensity;
        }

        private IEnumerator CalmRoutine()
        {
            for (float t = 0f; t < calmSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / calmSeconds);
                if (skyMaterial != null) skyMaterial.SetFloat(CalmId, k);
                if (sun != null) sun.color = Color.Lerp(stormSun, calmSun, k);
                yield return null;
            }
            if (skyMaterial != null) skyMaterial.SetFloat(CalmId, 1f);
            if (sun != null) sun.color = calmSun;
        }
    }
}
