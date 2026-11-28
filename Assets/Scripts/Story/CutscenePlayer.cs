using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeatherForSale.Progress;

namespace WeatherForSale.Story
{
    /// <summary>
    /// Plays the cutscene <see cref="GameModeState.PendingCutscene"/> names: one still at a time with its
    /// text typed out; a click, tap, Space or Enter finishes the line or moves on, Skip jumps to the end.
    /// At the end it saves the ending (and the secret boss flag after the True Ending), then loads the
    /// next scene or shows the Story Complete screen (day stars and time, Menu back to Title).
    /// Implements: story-006 criteria 3, 6, 7, 8, 9, 10.
    /// </summary>
    public sealed class CutscenePlayer : MonoBehaviour
    {
        [SerializeField] private GameModeState state;
        [SerializeField] private ProgressService progress;
        [SerializeField, Tooltip("Every cutscene; the one whose id matches the pending cutscene plays.")]
        private CutsceneData[] cutscenes;
        [SerializeField] private FrontEndStrings strings;

        [Header("Slide")]
        [SerializeField] private Image background;
        [SerializeField] private RectTransform backgroundRect;
        [SerializeField] private Image portraitLeft;
        [SerializeField] private Image portraitRight;
        [SerializeField] private GameObject textBox;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text bigText;
        [SerializeField] private Text continueHint;
        [SerializeField] private Image flash;
        [SerializeField] private Button skipButton;
        [SerializeField] private Text skipLabel;

        [Header("Story Complete")]
        [SerializeField] private GameObject completeRoot;
        [SerializeField] private Text completeTitle;
        [SerializeField] private Text starsText;
        [SerializeField] private Text timeText;
        [SerializeField, Tooltip("Secret stars from the Tempest fight, out of 3.")] private Text secretStarsText;
        [SerializeField] private Button menuButton;
        [SerializeField] private Text menuLabel;

        [Header("Feel")]
        [SerializeField, Min(1f), Tooltip("Letters typed per second.")] private float lettersPerSecond = 45f;
        [SerializeField, Min(0.05f), Tooltip("Seconds the lightning flash takes to fade.")] private float flashSeconds = 0.5f;
        [SerializeField, Min(0f), Tooltip("Picture shake strength on stormy slides, canvas units.")] private float shakeAmount = 10f;

        private CutsceneData data;
        private int index = -1;
        private float typed;
        private float flashLeft;
        private bool finished;
        private bool loading;

        private void Start()
        {
            data = Find(state != null ? state.PendingCutscene : CutsceneId.Opening);
            if (completeRoot != null) completeRoot.SetActive(false);
            if (flash != null) flash.color = new Color(1f, 1f, 1f, 0f);
            if (skipButton != null) skipButton.onClick.AddListener(Finish);
            if (menuButton != null) menuButton.onClick.AddListener(GoToTitle);
            if (strings != null)
            {
                if (skipLabel != null) skipLabel.text = strings.skipLabel;
                if (continueHint != null) continueHint.text = strings.continueHint;
                if (menuLabel != null) menuLabel.text = strings.menuLabel;
                if (completeTitle != null) completeTitle.text = strings.storyCompleteTitle;
            }
            if (data == null || data.slides == null || data.slides.Length == 0)
            {
                Debug.LogWarning("[CutscenePlayer] No cutscene for " + (state != null ? state.PendingCutscene.ToString() : "?") + "; skipping.");
                Finish();
                return;
            }
            Show(0);
        }

        private CutsceneData Find(CutsceneId id)
        {
            if (cutscenes == null) return null;
            foreach (CutsceneData c in cutscenes) if (c != null && c.id == id) return c;
            return null;
        }

        private void Update()
        {
            if (finished || data == null) return;
            CutsceneSlide slide = data.slides[index];
            string full = slide.text ?? "";

            if (typed < full.Length)
            {
                typed = Mathf.Min(full.Length, typed + lettersPerSecond * Time.deltaTime);
                SetBody(slide, full.Substring(0, Mathf.FloorToInt(typed)));
            }
            if (continueHint != null) continueHint.enabled = typed >= full.Length;

            if (flash != null && flashLeft > 0f)
            {
                flashLeft -= Time.deltaTime;
                flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(flashLeft / flashSeconds) * 0.9f);
            }
            if (backgroundRect != null)
            {
                // CGI pictures stay still; only a plain colour slide shakes.
                backgroundRect.anchoredPosition = slide.shake && slide.background == null
                    ? new Vector2(Mathf.Sin(Time.time * 41f), Mathf.Cos(Time.time * 37f)) * shakeAmount
                    : Vector2.zero;
            }

            if (AdvancePressed())
            {
                if (typed < full.Length)
                {
                    typed = full.Length;
                    SetBody(slide, full);
                }
                else if (index + 1 < data.slides.Length) Show(index + 1);
                else Finish();
            }
        }

        private bool AdvancePressed()
        {
            Keyboard k = Keyboard.current;
            if (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) return true;
            Pointer p = Pointer.current;
            if (p == null || !p.press.wasPressedThisFrame) return false;
            // A press on the Skip button is the button's, not an advance.
            var skipRect = skipButton != null ? skipButton.transform as RectTransform : null;
            return skipRect == null || !skipButton.gameObject.activeInHierarchy
                   || !RectTransformUtility.RectangleContainsScreenPoint(skipRect, p.position.ReadValue(), null);
        }

        private void Show(int i)
        {
            index = i;
            typed = 0f;
            CutsceneSlide slide = data.slides[i];
            if (background != null)
            {
                background.sprite = slide.background;
                background.color = slide.background != null ? Color.white : slide.backgroundColor;
                background.preserveAspect = false;
            }
            // Portraits stand in for a missing CGI picture; with a picture the characters are in it,
            // unless the slide says the speaker is not drawn there.
            Sprite portrait = slide.background == null || slide.portraitOverPicture ? slide.portrait : null;
            SetPortrait(portraitLeft, !slide.portraitRight ? portrait : null);
            SetPortrait(portraitRight, slide.portraitRight ? portrait : null);
            if (textBox != null) textBox.SetActive(!slide.bigCentredText);
            if (bigText != null) bigText.gameObject.SetActive(slide.bigCentredText);
            if (speakerText != null)
            {
                speakerText.text = slide.speaker ?? "";
                speakerText.gameObject.SetActive(!string.IsNullOrEmpty(slide.speaker));
            }
            SetBody(slide, "");
            if (slide.flash && flash != null) flashLeft = flashSeconds;
        }

        private void SetBody(CutsceneSlide slide, string text)
        {
            if (slide.bigCentredText) { if (bigText != null) bigText.text = text; }
            else if (bodyText != null) bodyText.text = text;
        }

        private static void SetPortrait(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.gameObject.SetActive(sprite != null);
        }

        /// <summary>Ends the cutscene (last slide or Skip): saves the ending, then moves on.</summary>
        public void Finish()
        {
            if (finished) return;
            finished = true;
            if (skipButton != null) skipButton.gameObject.SetActive(false);
            if (data != null && data.recordsEnding && progress != null)
            {
                progress.RecordEnding(data.ending);
                if (data.ending == EndingKind.True) progress.SetSecretBossUnlocked(true);
            }

            CutsceneNext next = data != null ? data.next : CutsceneNext.Shop;
            switch (next)
            {
                case CutsceneNext.Shop: Load(state != null ? state.shopScene : "Shop"); break;
                case CutsceneNext.Boss: Load(state != null ? state.bossScene : "Boss_Tempest"); break;
                case CutsceneNext.Title: Load(state != null ? state.titleScene : "Title"); break;
                case CutsceneNext.CompleteScreen: ShowComplete(); break;
            }
        }

        private void ShowComplete()
        {
            if (textBox != null) textBox.SetActive(false);
            if (bigText != null) bigText.gameObject.SetActive(false);
            SetPortrait(portraitLeft, null);
            SetPortrait(portraitRight, null);
            if (completeRoot == null) { GoToTitle(); return; }
            completeRoot.SetActive(true);
            int stars = state != null ? state.DayStars : 0;
            float seconds = state != null ? state.DaySeconds : 0f;
            if (starsText != null) starsText.text = stars + " / 12";
            if (secretStarsText != null) secretStarsText.text = (state != null ? state.SecretStars : 0) + " / 3";
            if (timeText != null) timeText.text = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(seconds / 60f), Mathf.FloorToInt(seconds % 60f));
        }

        private void GoToTitle()
        {
            Load(state != null ? state.titleScene : "Title");
        }

        private void Load(string scene)
        {
            if (loading) return;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogWarning("[CutscenePlayer] Scene '" + scene + "' is not in Build Settings.");
                return;
            }
            loading = true;
            SceneManager.LoadScene(scene);
        }
    }
}
