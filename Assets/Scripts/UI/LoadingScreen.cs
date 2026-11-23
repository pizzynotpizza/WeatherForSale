using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeatherForSale.Shop;

namespace WeatherForSale.UI
{
    /// <summary>
    /// Full-screen Loading screen shown after the right map is chosen: Sam runs on the spot, seen from
    /// the side and facing left, above the map name and "Loading...". Sam is a 3D model on a small
    /// off-screen stage whose camera renders into a RenderTexture shown by a RawImage; the stage is
    /// active only while this screen shows. The next scene loads with
    /// <see cref="SceneManager.LoadSceneAsync(string, LoadSceneMode)"/> and is activated only after the
    /// minimum display time. A scene missing from Build Settings logs a warning, hides the screen and
    /// raises <see cref="LoadFailed"/> instead of throwing.
    /// Implements: story-002 criterion 5 (Loading page, Sam running in place, side view facing left).
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");

        [SerializeField] private ShopUiConfig config;
        [SerializeField] private ShopAudioSettings settings;
        [SerializeField] private ShopStrings strings;
        [SerializeField, Tooltip("Screen root; shown and hidden.")]
        private GameObject root;
        [SerializeField, Tooltip("Off-screen stage holding Sam and the camera that renders him; switched on with the screen.")]
        private GameObject samStage;
        [SerializeField, Tooltip("Animator on Sam (Sam.controller: Speed 1 = run).")]
        private Animator samAnimator;
        [SerializeField] private Text mapNameText;
        [SerializeField] private Text loadingText;

        private Coroutine routine;

        /// <summary>Raised when the target scene could not be loaded (not in Build Settings).</summary>
        public event Action<MapEntry> LoadFailed;

        /// <summary>True while the screen is showing.</summary>
        public bool IsShowing => root != null && root.activeSelf;

        /// <summary>
        /// Shows the screen for <paramref name="map"/> and loads its scene, keeping the screen up for at
        /// least <paramref name="minSeconds"/>.
        /// </summary>
        public void Begin(MapEntry map, float minSeconds)
        {
            if (map == null) return;
            if (routine != null) StopCoroutine(routine);
            Show(map);
            routine = StartCoroutine(LoadRoutine(map, Mathf.Max(0f, minSeconds)));
        }

        /// <summary>Shows the screen for <paramref name="map"/> without loading anything (preview/screenshots).</summary>
        public void Show(MapEntry map)
        {
            if (mapNameText != null && map != null) mapNameText.text = map.displayName;
            if (loadingText != null && strings != null) loadingText.text = strings.loadingLabel;
            if (root != null) root.SetActive(true);
            if (samStage != null) samStage.SetActive(true);
            StartRunning();
        }

        /// <summary>Hides the screen and stops any pending load wait.</summary>
        public void Hide()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (root != null) root.SetActive(false);
            if (samStage != null) samStage.SetActive(false);
        }

        private void StartRunning()
        {
            if (samAnimator == null) return;
            samAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            samAnimator.SetBool(GroundedId, true);
            samAnimator.SetFloat(SpeedId, 1f);
            // Reduce motion: hold a still pose instead of the running loop.
            bool still = settings != null && settings.reduceMotion;
            samAnimator.speed = still ? 0f : (config != null ? config.loadingRunSpeed : 1f);
            samAnimator.Update(0f);
        }

        private IEnumerator LoadRoutine(MapEntry map, float minSeconds)
        {
            float started = Time.unscaledTime;

            if (string.IsNullOrEmpty(map.sceneName) || !Application.CanStreamedLevelBeLoaded(map.sceneName))
            {
                Debug.LogWarning("[LoadingScreen] Scene '" + map.sceneName + "' for map '" + map.id
                                 + "' is not in Build Settings (File > Build Profiles > Scene List). Staying in the shop.");
                while (Time.unscaledTime - started < minSeconds) yield return null;
                routine = null;
                Hide();
                LoadFailed?.Invoke(map);
                yield break;
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(map.sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogWarning("[LoadingScreen] LoadSceneAsync returned null for '" + map.sceneName + "'. Staying in the shop.");
                routine = null;
                Hide();
                LoadFailed?.Invoke(map);
                yield break;
            }
            op.allowSceneActivation = false;

            // progress stops at 0.9 while activation is held back.
            while (op.progress < 0.9f || Time.unscaledTime - started < minSeconds) yield return null;
            op.allowSceneActivation = true;
        }
    }
}
