using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WeatherForSale.Shop;
using WeatherForSale.Suction;

namespace WeatherForSale.UI
{
    /// <summary>
    /// "Weather Collected!" screen shown when the suction gauge is full: the map's bottle (from
    /// <see cref="MapCatalog"/>, shaking side to side in place), the map name and a Continue button. Display only; it raises
    /// <see cref="ContinueRequested"/> and the minigame decides what happens next.
    /// Implements: story-003 criterion 5.
    /// </summary>
    public sealed class WeatherCollectedScreen : MonoBehaviour
    {
        [SerializeField, Tooltip("Screen root; shown and hidden.")] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Image bottleImage;
        [SerializeField] private Text mapNameText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text continueLabel;

        [Header("Bottle shake")]
        [SerializeField, Tooltip("How far the bottle tips each way, degrees. 0 = still.")] private float shakeDegrees = 12f;
        [SerializeField, Tooltip("Shakes per second.")] private float shakeHz = 2.2f;
        [SerializeField, Tooltip("Side-to-side slide that goes with the tip, canvas units.")] private float shakeSlide = 10f;

        private Vector2 bottleHome;
        private bool bottleHomeKnown;
        private float shakeTime;

        private Coroutine unlockRoutine;

        /// <summary>Raised when the player presses Continue.</summary>
        public event Action ContinueRequested;

        /// <summary>True while the screen is showing.</summary>
        public bool IsShowing => root != null && root.activeSelf;

        private void Awake()
        {
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        }

        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinue);
        }

        /// <summary>
        /// Shows the screen for <paramref name="map"/> (null = no bottle art). The Continue button
        /// accepts input after <paramref name="inputDelaySeconds"/> (unscaled).
        /// </summary>
        public void Show(SuctionStrings strings, MapEntry map, float inputDelaySeconds)
        {
            if (strings != null)
            {
                if (titleText != null) titleText.text = strings.weatherCollectedTitle;
                if (continueLabel != null) continueLabel.text = strings.continueLabel;
            }
            if (bottleImage != null)
            {
                bottleImage.sprite = map != null ? map.bottleSprite : null;
                bottleImage.enabled = bottleImage.sprite != null;
                bottleImage.preserveAspect = true;
            }
            if (mapNameText != null) mapNameText.text = map != null ? map.displayName : "";
            if (root != null) root.SetActive(true);
            shakeTime = 0f;

            if (unlockRoutine != null) StopCoroutine(unlockRoutine);
            unlockRoutine = StartCoroutine(UnlockAfter(inputDelaySeconds));
        }

        private void Update()
        {
            if (!IsShowing || bottleImage == null) return;
            RectTransform rt = bottleImage.rectTransform;
            if (!bottleHomeKnown)
            {
                bottleHome = rt.anchoredPosition;
                bottleHomeKnown = true;
            }
            shakeTime += Time.unscaledDeltaTime;
            float wave = Mathf.Sin(shakeTime * shakeHz * 2f * Mathf.PI);
            rt.localRotation = Quaternion.Euler(0f, 0f, wave * shakeDegrees);
            rt.anchoredPosition = bottleHome + new Vector2(-wave * shakeSlide, 0f);
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
            if (continueButton == null) yield break;
            continueButton.interactable = false;
            float waited = 0f;
            while (waited < seconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            continueButton.interactable = true;
            // Keyboard / gamepad: Submit presses the focused button.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
            unlockRoutine = null;
        }

        private void OnContinue()
        {
            if (!IsShowing) return;
            ContinueRequested?.Invoke();
        }
    }
}
