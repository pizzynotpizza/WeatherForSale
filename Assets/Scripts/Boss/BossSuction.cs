using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WeatherForSale.Obby;
using WeatherForSale.Suction;

namespace WeatherForSale.Boss
{
    /// <summary>
    /// The suction of the boss fight: the purple bottle draws the tired boss while the player mashes the shown
    /// key (PC) or taps the button (touch). Uses the same <see cref="SuctionGauge"/> as the map suction.
    /// Sam is frozen in the pulling pose while collecting (the boss is tired and does not attack), and the
    /// gauge drains, so the player has <see cref="BossConfig.suctionSeconds"/> to fill it.
    /// Implements: story-008 criterion 6 (suction with the gauge system from story 003).
    /// </summary>
    public sealed class BossSuction : MonoBehaviour
    {
        [SerializeField] private BossConfig config;
        [SerializeField, Tooltip("The map suction settings: its keys, gauge size, speed, steps and decay are used here too, so the boss suction plays like the normal one.")] private SuctionConfig suctionConfig;
        [SerializeField, Tooltip("Root of the suction HUD; shown only while collecting.")] private GameObject root;
        [SerializeField, Tooltip("Fill bar (Image type Filled, horizontal); follows the gauge 0..1.")] private Image gaugeFill;
        [SerializeField] private Text titleText;
        [SerializeField, Tooltip("PC: key cap and the \"Press F!\" text.")] private GameObject keyPromptRoot;
        [SerializeField] private Text keyCapText;
        [SerializeField] private Text promptText;
        [SerializeField, Tooltip("Touch: the button to tap.")] private RectTransform tapButton;
        [SerializeField] private Text tapLabel;

        [Header("Sam's pose")]
        [SerializeField, Tooltip("The suction stage that poses Sam with the bottle (same pose as the map suction).")] private SuctionStage pose;
        [SerializeField] private PlayerController player;
        [SerializeField, Tooltip("What is drawn into the bottle (the boss).")] private Transform target;

        private SuctionGauge gauge;
        private float sinceChange;
        private bool boostPending;
        private bool boosted;
        private Key currentKey = Key.None;
        private bool touchMode;

        /// <summary>True when the running (or next) suction was boosted by a Storm Crystal.</summary>
        public bool Boosted => boosted;

        /// <summary>Seconds the player has for the current suction (longer when boosted).</summary>
        public float WindowSeconds => config.suctionSeconds + (boosted ? config.boostExtraSeconds : 0f);

        /// <summary>Marks the next suction as boosted (the Storm Crystal was picked up).</summary>
        public void Boost()
        {
            boostPending = true;
        }

        /// <summary>True while the player may fill the gauge.</summary>
        public bool IsActive { get; private set; }

        /// <summary>The key shown on PC (Key.None in touch mode or before the first Begin).</summary>
        public Key CurrentKey => currentKey;

        /// <summary>True once the gauge is full.</summary>
        public bool IsFull => gauge != null && gauge.IsFull;

        /// <summary>Gauge value 0..1 (0 before the first <see cref="Begin"/>).</summary>
        public float Fill => gauge != null ? gauge.Normalized : 0f;

        private void Awake()
        {
            if (tapButton != null)
            {
                // Pointer-down, not click, so fast taps count on press.
                EventTrigger trigger = tapButton.GetComponent<EventTrigger>();
                if (trigger == null) trigger = tapButton.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entry.callback.AddListener(OnTapPointerDown);
                trigger.triggers.Add(entry);
            }
            if (root != null) root.SetActive(false);
        }

        /// <summary>Starts a fresh, empty gauge and shows the prompt.</summary>
        public void Begin()
        {
            boosted = boostPending;
            boostPending = false;
            gauge = new SuctionGauge(suctionConfig.gaugeMax, suctionConfig.stepSize, boosted ? 0f : suctionConfig.decayPerSecond * config.suctionDecayMultiplier);
            sinceChange = 0f;
            gauge.StepReached += OnStepReached;
            IsActive = true;
            touchMode = Touchscreen.current != null;
            if (root != null) root.SetActive(true);
            if (titleText != null) titleText.text = boosted ? config.boostedTitle : config.collectingTitle;
            if (tapLabel != null) tapLabel.text = config.tapLabel;
            if (keyPromptRoot != null) keyPromptRoot.SetActive(!touchMode);
            if (tapButton != null) tapButton.gameObject.SetActive(touchMode);
            if (!touchMode) DrawKey();
            Refresh();

            // Sam stands still, faces the boss and pulls with the bottle (the map suction pose).
            if (player != null)
            {
                player.SetVirtualMove(Vector2.zero);
                player.ControlEnabled = false;
            }
            if (pose != null && target != null) pose.BeginFacing(target, config.suctionStreakColor, config.suctionStreakRadius, config.bottleColour);
        }

        /// <summary>Where the held bottle's mouth is: the boss is drawn in there.</summary>
        public Vector3 BottleMouth => pose != null && pose.IsRunning ? pose.BottleMouth : transform.position;

        /// <summary>
        /// Hides the prompt and stops reading input. With <paramref name="keepPose"/> Sam stays in the pulling pose, bottle in
        /// hand (the final blow: the boss is drawn into the bottle); call <see cref="ReleasePose"/> afterwards.
        /// </summary>
        public void End(bool keepPose = false)
        {
            IsActive = false;
            if (gauge != null) gauge.StepReached -= OnStepReached;
            if (root != null) root.SetActive(false);
            if (keepPose) return;
            ReleasePose();
        }

        /// <summary>Puts the bottle away and gives Sam back to the player.</summary>
        public void ReleasePose()
        {
            if (pose != null) pose.End();
            if (player != null) player.ControlEnabled = true;
        }

        private void Update()
        {
            if (!IsActive || gauge == null) return;

            if (!touchMode && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                touchMode = true;
                if (keyPromptRoot != null) keyPromptRoot.SetActive(false);
                if (tapButton != null) tapButton.gameObject.SetActive(true);
            }

            Keyboard keyboard = Keyboard.current;
            if (!touchMode && keyboard != null && currentKey != Key.None && keyboard[currentKey].wasPressedThisFrame)
            {
                gauge.Add(suctionConfig.amountPerPress * config.suctionGainMultiplier * (boosted ? config.boostGainMultiplier : 1f));
                if (pose != null) pose.NotifyPull();
            }
            gauge.Tick(Time.deltaTime);
            sinceChange += Time.deltaTime;
            if (!touchMode && suctionConfig.changeEverySeconds > 0f && sinceChange >= suctionConfig.changeEverySeconds) DrawKey();
            Refresh();
        }

        private void OnTapPointerDown(BaseEventData _)
        {
            if (!IsActive || !touchMode || gauge == null) return;
            gauge.Add(suctionConfig.amountPerPress * config.suctionGainMultiplier * (boosted ? config.boostGainMultiplier : 1f));
            if (pose != null) pose.NotifyPull();
            Refresh();
        }

        private void OnStepReached(int step)
        {
            if (!touchMode) DrawKey();
        }

        /// <summary>A new key from the map suction's list, never the same one twice in a row.</summary>
        private void DrawKey()
        {
            sinceChange = 0f;
            Key[] keys = suctionConfig.keys;
            if (keys == null || keys.Length == 0) currentKey = Key.Space;
            else if (keys.Length == 1) currentKey = keys[0];
            else
            {
                Key next;
                int guard = 0;
                do { next = keys[Random.Range(0, keys.Length)]; } while ((next == currentKey || next == Key.None) && ++guard < 16);
                currentKey = next;
            }
            if (keyCapText != null) keyCapText.text = KeyLabel(currentKey);
            if (promptText != null) promptText.text = string.Format(config.promptFormat, KeyLabel(currentKey));
        }

        private static string KeyLabel(Key key)
        {
            return key == Key.Space ? "SPACE" : key.ToString();
        }

        private void Refresh()
        {
            float f = Fill;
            if (gaugeFill != null) gaugeFill.fillAmount = Mathf.Clamp01(f);
        }
    }
}
