using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeatherForSale.Shop;

namespace WeatherForSale.UI
{
    /// <summary>
    /// Round menu button at the top-left and the Pause panel it opens: All / Music / SFX volume
    /// sliders (0-100), Back (close) and Menu (back to Title). All sets
    /// <see cref="AudioListener.volume"/>; Music and SFX are stored in <see cref="ShopAudioSettings"/>.
    /// Menu stays greyed out while the Title scene (story 006) is not in Build Settings. Escape
    /// opens and closes the panel. Game time is not paused (no <see cref="Time.timeScale"/> change).
    /// Implements: story-002 criterion 6.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private ShopAudioSettings settings;
        [SerializeField] private ShopUiConfig config;
        [SerializeField] private ShopStrings strings;
        [SerializeField, Tooltip("Round button at the top-left that opens the panel.")]
        private Button menuButton;
        [SerializeField, Tooltip("Panel root (full-screen dim + card); shown and hidden.")]
        private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Slider allSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Text allLabel;
        [SerializeField] private Text musicLabel;
        [SerializeField] private Text sfxLabel;
        [SerializeField] private Button backButton;
        [SerializeField] private Text backLabel;
        [SerializeField] private Button titleMenuButton;
        [SerializeField] private Text titleMenuLabel;
        [SerializeField, Tooltip("Greys out the Menu button and its label while it is unavailable.")]
        private CanvasGroup titleMenuGroup;
        [SerializeField, Range(0f, 1f)] private float unavailableAlpha = 0.45f;
        [SerializeField, Tooltip("While this is showing, the panel cannot open and Menu does nothing.")]
        private LoadingScreen loadingScreen;

        private bool Blocked => loadingScreen != null && loadingScreen.IsShowing;

        /// <summary>Raised when the panel opens or closes (true = open).</summary>
        public event Action<bool> OpenChanged;

        /// <summary>True while the Pause panel is open.</summary>
        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (menuButton != null) menuButton.onClick.AddListener(Open);
            if (backButton != null) backButton.onClick.AddListener(Close);
            if (titleMenuButton != null) titleMenuButton.onClick.AddListener(GoToTitle);
            SetupSlider(allSlider, OnAllChanged);
            SetupSlider(musicSlider, OnMusicChanged);
            SetupSlider(sfxSlider, OnSfxChanged);
        }

        private void Start()
        {
            ApplyLabels();
            SyncFromSettings();
            if (settings != null) settings.ApplyMaster();

            bool titleAvailable = config != null && !string.IsNullOrEmpty(config.titleSceneName)
                                  && Application.CanStreamedLevelBeLoaded(config.titleSceneName);
            if (titleMenuButton != null) titleMenuButton.interactable = titleAvailable;
            if (titleMenuGroup != null) titleMenuGroup.alpha = titleAvailable ? 1f : unavailableAlpha;
        }

        private void OnDestroy()
        {
            if (menuButton != null) menuButton.onClick.RemoveListener(Open);
            if (backButton != null) backButton.onClick.RemoveListener(Close);
            if (titleMenuButton != null) titleMenuButton.onClick.RemoveListener(GoToTitle);
            if (allSlider != null) allSlider.onValueChanged.RemoveListener(OnAllChanged);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            // P as well as Esc: browsers often keep Esc for leaving fullscreen.
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)) Toggle();
        }

        /// <summary>Opens the Pause panel.</summary>
        public void Open()
        {
            if (IsOpen || Blocked) return;
            SyncFromSettings();
            if (root != null) root.SetActive(true);
            ClearUiSelection();
            OpenChanged?.Invoke(true);
        }

        /// <summary>Closes the Pause panel.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            if (root != null) root.SetActive(false);
            ClearUiSelection();
            OpenChanged?.Invoke(false);
        }

        private static void ClearUiSelection()
        {
            // A button left selected would be "clicked" again by Space/Enter (UI Submit), which
            // collides with Space advancing the Dialog box.
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
        }

        /// <summary>Opens the panel if closed, closes it if open.</summary>
        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void GoToTitle()
        {
            if (Blocked) return;
            if (config == null || !Application.CanStreamedLevelBeLoaded(config.titleSceneName))
            {
                Debug.LogWarning("[PauseMenu] Title scene '" + (config != null ? config.titleSceneName : "?") + "' is not in Build Settings yet (story 006).");
                return;
            }
            SceneManager.LoadScene(config.titleSceneName);
        }

        private void ApplyLabels()
        {
            if (strings == null) return;
            if (titleText != null) titleText.text = strings.pauseTitle;
            if (allLabel != null) allLabel.text = strings.volumeAllLabel;
            if (musicLabel != null) musicLabel.text = strings.volumeMusicLabel;
            if (sfxLabel != null) sfxLabel.text = strings.volumeSfxLabel;
            if (backLabel != null) backLabel.text = strings.backLabel;
            if (titleMenuLabel != null) titleMenuLabel.text = strings.menuLabel;
        }

        private void SyncFromSettings()
        {
            if (settings == null) return;
            if (allSlider != null) allSlider.SetValueWithoutNotify(settings.masterVolume);
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(settings.musicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(settings.sfxVolume);
        }

        private static void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> handler)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.onValueChanged.AddListener(handler);
        }

        private void OnAllChanged(float value)
        {
            if (settings == null) return;
            settings.masterVolume = value;
            settings.ApplyMaster();
        }

        private void OnMusicChanged(float value)
        {
            if (settings != null) settings.musicVolume = value;
        }

        private void OnSfxChanged(float value)
        {
            if (settings != null) settings.sfxVolume = value;
        }
    }
}
