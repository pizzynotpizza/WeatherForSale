using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeatherForSale.Progress;
using WeatherForSale.Shop;
using WeatherForSale.Story;

namespace WeatherForSale.UI
{
    /// <summary>
    /// The Title screen: the game cover with Start, a Settings button (bottom-left) and the Achievement
    /// book (bottom-right). Start opens Mode Select: Story Mode (opening cutscene, then the shop) or Stage
    /// (Freeplay Level Select), which stays locked until Story Mode has been cleared once. Settings opens
    /// Options: All / Music / SFX volume (0-100), Back and Quit (Quit is hidden in a web build).
    /// Implements: story-006 criteria 1, 2; story-007 criterion 1.
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        [SerializeField] private GameModeState state;
        [SerializeField] private ShopSession session;
        [SerializeField] private ProgressService progress;
        [SerializeField] private ShopAudioSettings audioSettings;
        [SerializeField] private FrontEndStrings strings;

        [Header("Title")]
        [SerializeField] private Button startButton;
        [SerializeField, Tooltip("Quit under Start (hidden in a web build, where a tab cannot close itself).")]
        private Button titleQuitButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button achievementButton;
        [SerializeField] private AchievementBookPanel achievementBook;

        [Header("Mode Select")]
        [SerializeField] private GameObject modeRoot;
        [SerializeField] private Text modeTitle;
        [SerializeField] private Button storyButton;
        [SerializeField] private Text storyLabel;
        [SerializeField] private Button stageButton;
        [SerializeField] private Text stageLabel;
        [SerializeField] private CanvasGroup stageGroup;
        [SerializeField, Tooltip("The Stage card picture; darkened (still solid) while Stage is locked.")] private Image stageCardImage;
        [SerializeField, Tooltip("Lock icon on the Stage card; shown only while Stage is locked.")] private GameObject stageLockIcon;
        [SerializeField, Tooltip("Tint on the locked Stage card: solid, not see-through.")] private Color lockedTint = new Color(0.38f, 0.38f, 0.42f, 1f);
        [SerializeField] private Text stageLockedText;
        [SerializeField] private Button modeCloseButton;

        [Header("Options")]
        [SerializeField] private GameObject optionRoot;
        [SerializeField] private Text optionTitle;
        [SerializeField] private Slider allSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Text allLabel;
        [SerializeField] private Text musicLabel;
        [SerializeField] private Text sfxLabel;
        [SerializeField] private Button optionBackButton;
        [SerializeField] private Text optionBackLabel;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text quitLabel;

        [SerializeField, Range(0f, 1f), Tooltip("Opacity of the locked Stage button.")] private float lockedAlpha = 0.5f;

        private bool loading;

        /// <summary>True once Story Mode has been cleared (either ending), which unlocks Stage.</summary>
        public bool StageUnlocked => progress != null && (progress.Data.normalEndingSeen || progress.Data.trueEndingSeen);

        private void Start()
        {
            if (modeRoot != null) modeRoot.SetActive(false);
            if (optionRoot != null) optionRoot.SetActive(false);
            if (achievementBook != null && progress != null) achievementBook.SetSource(progress);

            Hook(startButton, OpenModeSelect);
            Hook(settingsButton, OpenOptions);
            Hook(achievementButton, () => { if (achievementBook != null) achievementBook.Open(); });
            Hook(storyButton, StartStory);
            Hook(stageButton, StartStage);
            Hook(modeCloseButton, () => modeRoot.SetActive(false));
            Hook(optionBackButton, () => optionRoot.SetActive(false));
            Hook(quitButton, Application.Quit);
            Hook(titleQuitButton, Application.Quit);
#if UNITY_WEBGL && !UNITY_EDITOR
            // A browser tab cannot quit itself.
            if (quitButton != null) quitButton.gameObject.SetActive(false);
            if (titleQuitButton != null) titleQuitButton.gameObject.SetActive(false);
#endif
            SetupSlider(allSlider, v => { if (audioSettings != null) { audioSettings.masterVolume = v; audioSettings.ApplyMaster(); } });
            SetupSlider(musicSlider, v => { if (audioSettings != null) audioSettings.musicVolume = v; });
            SetupSlider(sfxSlider, v => { if (audioSettings != null) audioSettings.sfxVolume = v; });
            if (audioSettings != null) audioSettings.ApplyMaster();
            ApplyStrings();
        }

        private static void Hook(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> onChange)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.onValueChanged.AddListener(onChange);
        }

        private void ApplyStrings()
        {
            if (strings == null) return;
            Set(modeTitle, strings.modeTitle);
            Set(storyLabel, strings.storyModeLabel);
            Set(stageLabel, strings.stageModeLabel);
            Set(stageLockedText, strings.stageLockedText);
            Set(optionTitle, strings.optionTitle);
            Set(allLabel, strings.volumeAllLabel);
            Set(musicLabel, strings.volumeMusicLabel);
            Set(sfxLabel, strings.volumeSfxLabel);
            Set(optionBackLabel, strings.backLabel);
            Set(quitLabel, strings.quitLabel);
        }

        private static void Set(Text text, string value)
        {
            if (text != null) text.text = value;
        }

        private void OpenModeSelect()
        {
            if (modeRoot == null) return;
            bool unlocked = StageUnlocked;
            if (stageCardImage != null) stageCardImage.color = unlocked ? Color.white : lockedTint; // solid dark card
            else if (stageGroup != null) stageGroup.alpha = unlocked ? 1f : lockedAlpha;
            if (stageLockedText != null) stageLockedText.gameObject.SetActive(!unlocked);
            if (stageLockIcon != null) stageLockIcon.SetActive(!unlocked);
            modeRoot.SetActive(true);
        }

        private void OpenOptions()
        {
            if (optionRoot == null) return;
            if (audioSettings != null)
            {
                if (allSlider != null) allSlider.SetValueWithoutNotify(audioSettings.masterVolume);
                if (musicSlider != null) musicSlider.SetValueWithoutNotify(audioSettings.musicVolume);
                if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(audioSettings.sfxVolume);
            }
            optionRoot.SetActive(true);
        }

        private void StartStory()
        {
            if (state == null) return;
            state.BeginStory();
            if (session != null) session.ResetSession();
            Load(state.cutsceneScene);
        }

        private void StartStage()
        {
            if (state == null || !StageUnlocked) return;
            state.BeginFreeplay();
            if (session != null) session.ResetSession();
            Load(state.levelSelectScene);
        }

        private void Load(string scene)
        {
            if (loading) return;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogWarning("[TitleScreen] Scene '" + scene + "' is not in Build Settings.");
                return;
            }
            loading = true;
            SceneManager.LoadScene(scene);
        }
    }
}
