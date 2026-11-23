using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WeatherForSale.Core;
using WeatherForSale.Shop;
using WeatherForSale.UI;

namespace WeatherForSale.Suction
{
    /// <summary>Phase of the suction minigame.</summary>
    public enum SuctionPhase
    {
        /// <summary>Not started (the obby level is still being played).</summary>
        Idle,
        /// <summary>Filling the gauge.</summary>
        Collecting,
        /// <summary>Gauge full; the Weather Collected screen is up.</summary>
        Collected,
        /// <summary>The Complete screen (stars, time, Key Item) is up.</summary>
        Complete,
        /// <summary>Next pressed; the shop is loading.</summary>
        Leaving,
        /// <summary>The level timer ran out before the gauge was full (Game Over is showing).</summary>
        Failed,
    }

    /// <summary>
    /// Suction Quick Time Event (the UI and input half; <see cref="SuctionStage"/> does the 3D half).
    /// PC: one random key from <see cref="SuctionConfig.keys"/> is shown and only that key adds gauge.
    /// Touch: a tap button sits at a random position inside the safe margins and tapping it adds gauge
    /// (mouse clicks work too). Each time the gauge reaches another step a new key is drawn (never the
    /// same twice in a row) or the button jumps. A full gauge shows Weather Collected, then the
    /// Complete screen; Next returns to the shop through <see cref="GameLoopController"/>.
    /// Transitions (only these): Idle to Collecting (<see cref="Begin"/>), Collecting to Collected
    /// (gauge full), Collected to Complete (Continue), Complete to Leaving (Next).
    /// Implements: story-003 criteria 2-8 (design/game-brief.md Features 4).
    /// </summary>
    public sealed class SuctionMinigame : MonoBehaviour
    {
        [SerializeField] private SuctionConfig config;
        [SerializeField] private SuctionStrings strings;
        [SerializeField] private GameLoopController loop;

        [SerializeField, Tooltip("Root of the suction HUD (prompt, gauge, tap area); hidden until Begin.")]
        private GameObject hudRoot;

        [Header("Gauge")]
        [SerializeField, Tooltip("Fill bar; its right anchor follows the gauge (0..1).")] private RectTransform gaugeFill;
        [SerializeField] private Text gaugePercentText;

        [Header("Prompt")]
        [SerializeField, Tooltip("PC only: the key cap and \"Press W!\" together; moved around the screen like the tap button.")] private GameObject keyPromptRoot;
        [SerializeField] private Text keyCapText;
        [SerializeField, Tooltip("Big prompt \"Press W!\" at the lower left; PC only.")] private Text promptText;
        [SerializeField, Tooltip("\"Collect the Weather!\" at the top centre while collecting.")] private Text titleText;

        [Header("Touch tap button")]
        [SerializeField] private RectTransform tapButton;
        [SerializeField] private Text tapButtonLabel;

        [Header("Follow-up screens")]
        [SerializeField] private WeatherCollectedScreen collectedScreen;
        [SerializeField] private StageCompletePanel completePanel;

        private SuctionGauge gauge;
        private Key currentKey = Key.None;
        private bool touchMode;
        private float collectSeconds;
        private float sinceChange;
        private int shownPercent = -1;
        private MapEntry map;

        /// <summary>Current phase.</summary>
        public SuctionPhase Phase { get; private set; } = SuctionPhase.Idle;

        /// <summary>The gauge (read it; only this class adds to it).</summary>
        public SuctionGauge Gauge => gauge;

        /// <summary>The key currently shown on PC (Key.None in touch mode or before Begin).</summary>
        public Key CurrentKey => currentKey;

        private void Awake()
        {
            gauge = new SuctionGauge(config.gaugeMax, config.stepSize, config.decayPerSecond);
            gauge.StepReached += OnStepReached;
            gauge.Filled += OnFilled;

            if (tapButton != null)
            {
                // Pointer-down (not click) so fast taps count on press, and a jump mid-tap loses nothing.
                EventTrigger trigger = tapButton.GetComponent<EventTrigger>();
                if (trigger == null) trigger = tapButton.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entry.callback.AddListener(OnTapPointerDown);
                trigger.triggers.Add(entry);
            }
            if (collectedScreen != null) collectedScreen.ContinueRequested += OnContinue;
            if (completePanel != null) completePanel.NextRequested += OnNext;
        }

        private void OnDestroy()
        {
            if (gauge != null)
            {
                gauge.StepReached -= OnStepReached;
                gauge.Filled -= OnFilled;
            }
            if (collectedScreen != null) collectedScreen.ContinueRequested -= OnContinue;
            if (completePanel != null) completePanel.NextRequested -= OnNext;
        }

        private void Start()
        {
            if (titleText != null) titleText.text = strings.collectTitle;
            if (tapButtonLabel != null) tapButtonLabel.text = strings.tapButtonLabel;
            if (Phase == SuctionPhase.Idle && hudRoot != null) hudRoot.SetActive(false);
            if (collectedScreen != null && !collectedScreen.IsShowing) collectedScreen.Hide();
            if (completePanel != null && !completePanel.IsShowing) completePanel.Hide();
        }

        /// <summary>Starts collecting: shows the HUD and the first key or tap button. Ignored unless idle.</summary>
        public void Begin()
        {
            if (Phase != SuctionPhase.Idle) return;
            Phase = SuctionPhase.Collecting;
            map = loop != null ? loop.CurrentMap : null;
            collectSeconds = 0f;

            if (hudRoot != null) hudRoot.SetActive(true);
            switch (config.inputMode)
            {
                case SuctionInputMode.Touch: touchMode = true; break;
                case SuctionInputMode.Keyboard: touchMode = false; break;
                default: touchMode = Touchscreen.current != null; break;
            }
            ApplyMode();
            RefreshGauge(true);
        }

        private void Update()
        {
            if (Phase != SuctionPhase.Collecting) return;

            collectSeconds += Time.deltaTime;
            sinceChange += Time.deltaTime;
            if (config.changeEverySeconds > 0f && sinceChange >= config.changeEverySeconds) ChangePrompt();

            if (!touchMode)
            {
                // Auto mode: the first touch on a device that started in key mode switches to the tap button.
                if (config.inputMode == SuctionInputMode.Auto && Touchscreen.current != null
                    && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    touchMode = true;
                    ApplyMode();
                }
                else
                {
                    Keyboard keyboard = Keyboard.current;
                    if (keyboard != null && currentKey != Key.None && keyboard[currentKey].wasPressedThisFrame)
                    {
                        gauge.Add(config.amountPerPress);
                    }
                }
            }

            if (Phase == SuctionPhase.Collecting) gauge.Tick(Time.deltaTime);
            RefreshGauge(false);
        }

        private void OnTapPointerDown(BaseEventData _)
        {
            if (Phase != SuctionPhase.Collecting || !touchMode) return;
            gauge.Add(config.amountPerPress);
            RefreshGauge(false);
        }

        private void OnStepReached(int step)
        {
            ChangePrompt();
        }

        /// <summary>New key and new place on PC; new place for the tap button on touch.</summary>
        private void ChangePrompt()
        {
            sinceChange = 0f;
            if (touchMode)
            {
                MoveTapButton();
            }
            else
            {
                DrawKey();
                MoveKeyPrompt();
            }
        }

        private void OnFilled()
        {
            if (Phase != SuctionPhase.Collecting) return;
            Phase = SuctionPhase.Collected;
            RefreshGauge(true);
            if (keyPromptRoot != null) keyPromptRoot.SetActive(false);
            if (tapButton != null) tapButton.gameObject.SetActive(false);
            if (promptText != null) promptText.gameObject.SetActive(false);
            if (titleText != null) titleText.gameObject.SetActive(false);

            // The level timer kept running through the suction; the loop stops it and adds the time.
            if (loop != null) loop.SuctionCollected();

            if (collectedScreen != null) collectedScreen.Show(strings, map, config.panelInputDelaySeconds);
            else OnContinue();
        }

        /// <summary>Time ran out while collecting: stops input and hides the suction HUD. Ignored unless collecting.</summary>
        public void Fail()
        {
            if (Phase != SuctionPhase.Collecting) return;
            Phase = SuctionPhase.Failed;
            if (hudRoot != null) hudRoot.SetActive(false);
        }

        private void OnContinue()
        {
            if (Phase != SuctionPhase.Collected) return;
            Phase = SuctionPhase.Complete;
            if (collectedScreen != null) collectedScreen.Hide();

            ShopSession session = loop != null ? loop.Session : null;
            float total = session != null ? session.RunSeconds : collectSeconds;
            bool shard = session != null && session.RunShardCollected;
            int stars = config.StarsFor(map != null ? map.id : null, total);
            if (completePanel != null) completePanel.Show(strings, new StageResult(stars, total, shard), config.panelInputDelaySeconds);
            else OnNext();
        }

        private void OnNext()
        {
            if (Phase != SuctionPhase.Complete) return;
            Phase = SuctionPhase.Leaving;
            if (loop != null) loop.ReturnToShop();
            else Debug.LogWarning("[SuctionMinigame] No GameLoopController assigned; cannot return to the shop.");
        }

        // ------------------------------------------------------------------ prompt

        private void ApplyMode()
        {
            if (keyPromptRoot != null) keyPromptRoot.SetActive(!touchMode);
            if (tapButton != null) tapButton.gameObject.SetActive(touchMode);
            if (touchMode)
            {
                currentKey = Key.None;
                if (tapButton != null) tapButton.sizeDelta = new Vector2(config.tapButtonSize, config.tapButtonSize);
                MoveTapButton();
            }
            else
            {
                DrawKey();
                MoveKeyPrompt();
            }
            sinceChange = 0f;
            UpdatePromptText();
        }

        private void DrawKey()
        {
            Key[] keys = config.keys;
            if (keys == null || keys.Length == 0)
            {
                Debug.LogWarning("[SuctionMinigame] SuctionConfig has no keys; using Space.");
                currentKey = Key.Space;
            }
            else if (keys.Length == 1)
            {
                currentKey = keys[0];
            }
            else
            {
                // Never the same key twice in a row, so the change is noticeable.
                Key next;
                int guard = 0;
                do
                {
                    next = keys[Random.Range(0, keys.Length)];
                } while ((next == currentKey || next == Key.None) && ++guard < 16);
                currentKey = next;
            }
            if (keyCapText != null) keyCapText.text = currentKey.ToString();
            UpdatePromptText();
        }

        private void UpdatePromptText()
        {
            if (promptText == null) return;
            // Touch has no lower-left prompt: the tap button and the top title say enough.
            promptText.gameObject.SetActive(!touchMode);
            if (!touchMode) promptText.text = string.Format(strings.keyPromptFormat, currentKey.ToString());
        }

        private void MoveTapButton()
        {
            if (tapButton != null) MoveWithinMargins(tapButton);
        }

        private void MoveKeyPrompt()
        {
            if (keyPromptRoot != null) MoveWithinMargins(keyPromptRoot.transform as RectTransform);
        }

        /// <summary>
        /// Puts <paramref name="target"/> (centre-anchored on a stretched parent) at a random spot inside
        /// the config's safe margins, at least minJumpDistance from where it was when possible.
        /// </summary>
        private void MoveWithinMargins(RectTransform target)
        {
            if (target == null) return;
            var area = target.parent as RectTransform;
            if (area == null) return;

            Rect rect = area.rect;
            Vector2 half = target.rect.size * 0.5f;
            float xMin = rect.xMin + config.safeMarginSides + half.x;
            float xMax = rect.xMax - config.safeMarginSides - half.x;
            float yMin = rect.yMin + config.safeMarginBottom + half.y;
            float yMax = rect.yMax - config.safeMarginTop - half.y;
            // Screen too small for the margins: fall back to the centre line of the area.
            if (xMin > xMax) xMin = xMax = rect.center.x;
            if (yMin > yMax) yMin = yMax = rect.center.y;

            Vector2 centre = rect.center;
            Vector2 old = target.anchoredPosition + centre;
            Vector2 pick = old;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                pick = new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));
                if ((pick - old).sqrMagnitude >= config.minJumpDistance * config.minJumpDistance) break;
            }
            target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = pick - centre;
        }

        // ------------------------------------------------------------------ gauge display

        private void RefreshGauge(bool force)
        {
            float t = gauge.Normalized;
            if (gaugeFill != null)
            {
                Vector2 max = gaugeFill.anchorMax;
                if (force || !Mathf.Approximately(max.x, t))
                {
                    max.x = t;
                    gaugeFill.anchorMax = max;
                }
            }

            int percent = Mathf.Clamp(Mathf.FloorToInt(t * 100f), 0, 100);
            if (!force && percent == shownPercent) return;
            shownPercent = percent;
            if (gaugePercentText != null) gaugePercentText.text = string.Format(strings.gaugePercentFormat, percent);
        }
    }
}
