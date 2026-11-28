using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WeatherForSale.Progress
{
    /// <summary>
    /// On-screen "Achievement Unlocked!" notice at the top centre, below the level timer. Built in code by
    /// <see cref="ProgressService"/> on the first unlock and kept across scene loads, so no scene
    /// needs it placed. Notices queue and show one at a time, on unscaled time (works while paused).
    /// Implements: story-005 criterion 6 (notice on screen).
    /// </summary>
    public sealed class AchievementToast : MonoBehaviour
    {
        private readonly Queue<(string title, string name)> pending = new Queue<(string, string)>();
        private GameObject panel;
        private Text titleText;
        private Text nameText;
        private float seconds = 3f;
        private float hideAt;

        /// <summary>True while a notice is on screen.</summary>
        public bool IsShowing => panel != null && panel.activeSelf;

        /// <summary>Creates the notice canvas (kept across scene loads).</summary>
        public static AchievementToast Create(float showSeconds)
        {
            var root = new GameObject("AchievementToast");
            DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var toast = root.AddComponent<AchievementToast>();
            toast.seconds = Mathf.Max(0.5f, showSeconds);
            toast.Build(root.transform);
            return toast;
        }

        /// <summary>Queues a notice; shown at once when none is on screen.</summary>
        public void Enqueue(string title, string achievementName)
        {
            pending.Enqueue((title, achievementName));
            if (!IsShowing) ShowNext();
        }

        private void Update()
        {
            if (IsShowing && Time.unscaledTime >= hideAt) ShowNext();
        }

        private void ShowNext()
        {
            if (pending.Count == 0)
            {
                panel.SetActive(false);
                return;
            }
            (string title, string name) = pending.Dequeue();
            titleText.text = title;
            nameText.text = name;
            panel.SetActive(true);
            hideAt = Time.unscaledTime + seconds;
        }

        private void Build(Transform parent)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = (RectTransform)panel.transform;
            // Bottom-right corner, clear of the screen edges (the top holds the timer, menu button and Falls).
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-40f, 40f);
            rect.sizeDelta = new Vector2(560f, 120f);
            var image = panel.GetComponent<Image>();
            image.color = new Color(0.16f, 0.1f, 0.06f, 0.9f);
            image.raycastTarget = false;

            titleText = CreateText(rect, font, 30, new Color(1f, 0.84f, 0.35f), new Vector2(0f, 0.5f), new Vector2(1f, 1f));
            nameText = CreateText(rect, font, 40, Color.white, new Vector2(0f, 0f), new Vector2(1f, 0.55f));
            panel.SetActive(false);
        }

        private static Text CreateText(RectTransform parent, Font font, int size, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(20f, 6f);
            rect.offsetMax = new Vector2(-20f, -6f);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }
    }
}
