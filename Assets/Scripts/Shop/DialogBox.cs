using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// Bottom-of-screen Dialog box that shows "Name: line" with a typewriter effect. Clicking or
    /// tapping the box, or pressing Space, first finishes the line, then (if the line asked for it)
    /// raises <see cref="Advanced"/>. The speaker prefix appears at once; only the line types out.
    /// Implements: story-002 criterion 2 (problem told in a Dialog box at the bottom, in English).
    /// </summary>
    public sealed class DialogBox : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private ShopUiConfig config;
        [SerializeField] private ShopStrings strings;
        [SerializeField, Tooltip("The whole box; shown and hidden.")]
        private GameObject root;
        [SerializeField] private Text bodyText;
        [SerializeField, Tooltip("Small arrow shown when the player can continue.")]
        private GameObject continueIndicator;

        private string prefix = string.Empty;
        private string line = string.Empty;
        private int shownChars;
        private float charTimer;
        private bool waitForAdvance;

        /// <summary>Raised once the current line is fully visible.</summary>
        public event Action LineCompleted;

        /// <summary>Raised when the player continues past a fully shown line that asked to wait.</summary>
        public event Action Advanced;

        /// <summary>True while the typewriter is still revealing the line.</summary>
        public bool IsTyping { get; private set; }

        /// <summary>True while the box is visible.</summary>
        public bool IsVisible => root != null && root.activeSelf;

        /// <summary>When true, Space is ignored (e.g. a menu is open over the box).</summary>
        public bool InputBlocked { get; set; }

        private void Awake()
        {
            if (continueIndicator != null) continueIndicator.SetActive(false);
        }

        /// <summary>
        /// Shows <paramref name="text"/> spoken by <paramref name="speaker"/>. When
        /// <paramref name="waitForPlayer"/> is true, a continue arrow appears at the end and the
        /// next click/tap/Space raises <see cref="Advanced"/>.
        /// </summary>
        public void Show(string speaker, string text, bool waitForPlayer)
        {
            prefix = string.IsNullOrEmpty(speaker) ? string.Empty : string.Format(strings.dialogSpeakerFormat, speaker);
            line = text ?? string.Empty;
            waitForAdvance = waitForPlayer;
            shownChars = 0;
            charTimer = 0f;
            IsTyping = true;
            if (root != null) root.SetActive(true);
            if (continueIndicator != null) continueIndicator.SetActive(false);
            Refresh();
            if (config == null || config.typewriterSecondsPerChar <= 0f || line.Length == 0) CompleteLine();
        }

        /// <summary>Hides the box.</summary>
        public void Hide()
        {
            IsTyping = false;
            waitForAdvance = false;
            if (root != null) root.SetActive(false);
        }

        /// <summary>Reveals the rest of the line at once (no-op when already complete).</summary>
        public void CompleteLine()
        {
            if (!IsTyping) return;
            IsTyping = false;
            shownChars = line.Length;
            Refresh();
            if (continueIndicator != null) continueIndicator.SetActive(waitForAdvance);
            LineCompleted?.Invoke();
        }

        /// <summary>Same as a click on the box: finish typing, or continue if waiting.</summary>
        public void Advance()
        {
            if (!IsVisible) return;
            if (IsTyping)
            {
                CompleteLine();
                return;
            }
            if (!waitForAdvance) return;
            waitForAdvance = false;
            if (continueIndicator != null) continueIndicator.SetActive(false);
            Advanced?.Invoke();
        }

        /// <summary>Pointer click/tap on the box (Input System UI module).</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Advance();
        }

        private void Update()
        {
            if (!IsVisible) return;

            Keyboard keyboard = Keyboard.current;
            if (!InputBlocked && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                Advance();
                return;
            }

            if (!IsTyping) return;
            charTimer += Time.deltaTime;
            int target = Mathf.Min(line.Length, Mathf.FloorToInt(charTimer / config.typewriterSecondsPerChar));
            if (target == shownChars) return;
            shownChars = target;
            Refresh();
            if (shownChars >= line.Length) CompleteLine();
        }

        private void Refresh()
        {
            if (bodyText == null) return;
            bodyText.text = shownChars >= line.Length ? prefix + line : prefix + line.Substring(0, shownChars);
        }
    }
}
