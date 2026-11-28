using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WeatherForSale.Suction;

namespace WeatherForSale.UI
{
    /// <summary>Numbers shown on the Complete screen.</summary>
    public readonly struct StageResult
    {
        /// <summary>Stars earned, 0-3.</summary>
        public readonly int Stars;
        /// <summary>Total run time in seconds.</summary>
        public readonly float TotalSeconds;
        /// <summary>True when this run's Shard was collected.</summary>
        public readonly bool ShardCollected;

        /// <summary>Creates a result; stars are clamped to 0-3.</summary>
        public StageResult(int stars, float totalSeconds, bool shardCollected)
        {
            Stars = Mathf.Clamp(stars, 0, 3);
            TotalSeconds = Mathf.Max(0f, totalSeconds);
            ShardCollected = shardCollected;
        }
    }

    /// <summary>
    /// Complete screen after Weather Collected: 0-3 stars, total time (mm:ss), Shard 0/1
    /// and a Next button. Display only; it raises <see cref="NextRequested"/> and the loop returns
    /// to the shop.
    /// Implements: story-003 criterion 7 (design/game-brief.md "Goal &amp; fail state": Complete screen).
    /// </summary>
    public sealed class StageCompletePanel : MonoBehaviour
    {
        [SerializeField, Tooltip("Screen root; shown and hidden.")] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField, Tooltip("Three star images, left to right.")] private Image[] starImages = Array.Empty<Image>();
        [SerializeField] private Color starEarnedColor = Color.white;
        [SerializeField] private Color starMissingColor = new Color(0.25f, 0.2f, 0.18f, 0.35f);
        [SerializeField] private Text timeLabelText;
        [SerializeField] private Text timeValueText;
        [SerializeField] private Text keyItemLabelText;
        [SerializeField] private Text keyItemValueText;
        [SerializeField, Tooltip("Shard icon; dimmed when the Shard was not collected.")] private Image keyItemIcon;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextLabel;

        private Coroutine unlockRoutine;

        /// <summary>Raised when the player presses Next.</summary>
        public event Action NextRequested;

        /// <summary>True while the screen is showing.</summary>
        public bool IsShowing => root != null && root.activeSelf;

        private void Awake()
        {
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
        }

        /// <summary>
        /// Shows <paramref name="result"/>. The Next button accepts input after
        /// <paramref name="inputDelaySeconds"/> (unscaled).
        /// </summary>
        public void Show(SuctionStrings strings, StageResult result, float inputDelaySeconds)
        {
            if (strings != null)
            {
                if (titleText != null) titleText.text = strings.stageCompleteTitle;
                if (timeLabelText != null) timeLabelText.text = strings.timeLabel;
                if (timeValueText != null) timeValueText.text = strings.FormatTime(result.TotalSeconds);
                if (keyItemLabelText != null) keyItemLabelText.text = strings.keyItemLabel;
                if (keyItemValueText != null) keyItemValueText.text = string.Format(strings.keyItemValueFormat, result.ShardCollected ? 1 : 0);
                if (nextLabel != null) nextLabel.text = strings.nextLabel;
            }
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null) starImages[i].color = i < result.Stars ? starEarnedColor : starMissingColor;
            }
            if (keyItemIcon != null) keyItemIcon.color = result.ShardCollected ? starEarnedColor : starMissingColor;
            if (root != null) root.SetActive(true);

            if (unlockRoutine != null) StopCoroutine(unlockRoutine);
            unlockRoutine = StartCoroutine(UnlockAfter(inputDelaySeconds));
        }

        /// <summary>Hides the screen.</summary>
        public void Hide()
        {
            if (unlockRoutine != null) StopCoroutine(unlockRoutine);
            unlockRoutine = null;
            if (root != null) root.SetActive(false);
        }

        private IEnumerator UnlockAfter(float seconds)
        {
            if (nextButton == null) yield break;
            nextButton.interactable = false;
            float waited = 0f;
            while (waited < seconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            nextButton.interactable = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
            unlockRoutine = null;
        }

        private void OnNext()
        {
            if (!IsShowing) return;
            NextRequested?.Invoke();
        }
    }
}
