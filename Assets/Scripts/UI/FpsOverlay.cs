using UnityEngine;

namespace WeatherForSale.UI
{
    /// <summary>
    /// Small frames-per-second readout for device testing. It is created on its own at startup and
    /// stays hidden unless the web build's page URL contains <c>fps</c> (e.g. <c>index.html?fps</c>),
    /// so neither the Editor nor normal players ever see it. It shows the
    /// smoothed fps, the slowest recent frame and the lowest fps seen so far.
    /// Implements: story-001 criterion 7 (record fps on a real phone).
    /// </summary>
    public sealed class FpsOverlay : MonoBehaviour
    {
        private const float WindowSeconds = 1f;

        private GUIStyle style;
        private float windowTime;
        private int windowFrames;
        private float windowWorst;
        private float shownFps;
        private float shownWorstMs;
        private float lowestFps = float.MaxValue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            // Only on request: open the web build with ?fps in the address. Never in the Editor or by default.
            bool wanted = !Application.isEditor
                          && !string.IsNullOrEmpty(Application.absoluteURL) && Application.absoluteURL.Contains("fps");
            if (!wanted) return;
            var go = new GameObject("FpsOverlay");
            DontDestroyOnLoad(go);
            go.AddComponent<FpsOverlay>();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            windowTime += dt;
            windowFrames++;
            if (dt > windowWorst) windowWorst = dt;
            if (windowTime < WindowSeconds) return;

            shownFps = windowFrames / windowTime;
            shownWorstMs = windowWorst * 1000f;
            if (Time.unscaledTime > 3f && shownFps < lowestFps) lowestFps = shownFps; // ignore start-up
            windowTime = 0f;
            windowFrames = 0;
            windowWorst = 0f;
        }

        private void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            }
            style.fontSize = Mathf.Max(16, Screen.height / 36);
            string low = lowestFps == float.MaxValue ? "-" : lowestFps.ToString("0");
            string text = shownFps.ToString("0") + " fps  worst " + shownWorstMs.ToString("0") + " ms  low " + low;
            var rect = new Rect(Screen.width * 0.5f - Screen.width * 0.25f, Screen.height - style.fontSize * 2f, Screen.width * 0.5f, style.fontSize * 1.8f);
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
            style.normal.textColor = shownFps < 30f ? new Color(1f, 0.35f, 0.3f) : Color.white;
            GUI.Label(rect, text, style);
        }
    }
}
