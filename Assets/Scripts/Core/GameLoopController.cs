using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeatherForSale.Obby;
using WeatherForSale.Shop;
using WeatherForSale.Suction;

namespace WeatherForSale.Core
{
    /// <summary>
    /// Moves the player through one map run: obby level 1, obby level 2, the in-place suction at the
    /// end of level 2 (<see cref="SuctionStage"/>), then back to the shop with the bottle. Lives in
    /// every map obby scene. Run totals are kept in <see cref="ShopSession"/>, which
    /// survives scene loads; there is no singleton.
    /// Played directly from the editor without a shop run, it starts a run on this scene's map (or
    /// <see cref="GameLoopConfig.fallbackMapId"/>) so the loop still works.
    /// Level 1 complete: the Level Complete panel's Next button loads level 2. Last level complete:
    /// the suction starts in place at once (no panel), and the level's countdown keeps running from
    /// the time left: the bottle must be full before it runs out, or the level is Game Over.
    /// Game Over is left to the existing Restart button (<see cref="ObbyLevelManager.RestartLevel"/>),
    /// which reloads the same level and keeps the run.
    /// Implements: story-003 criteria 1, 8 and 9 (design/game-brief.md "Core loop").
    /// </summary>
    public sealed class GameLoopController : MonoBehaviour
    {
        [SerializeField] private GameLoopConfig config;
        [SerializeField] private ShopSession session;
        [SerializeField] private MapCatalog catalog;
        [SerializeField] private SuctionStrings strings;

        [Header("Obby level")]
        [SerializeField] private ObbyLevelManager level;
        [SerializeField, Tooltip("1-based obby level number of this scene; 0 = not an obby level.")]
        private int levelIndex;
        [SerializeField, Tooltip("Map id of this scene, used only when the scene is played without a shop run.")]
        private string sceneMapId = "";
        [SerializeField, Tooltip("Button on the Level Complete panel that continues the run.")]
        private Button nextButton;
        [SerializeField] private Text nextButtonLabel;
        [SerializeField, Tooltip("In-place suction started when the map's last level is completed (level 2 scenes only).")]
        private SuctionStage suctionStage;

        private bool levelResultRecorded;
        private bool suctionRunning;
        private bool loading;

        /// <summary>The run's session (shared across scenes).</summary>
        public ShopSession Session => session;

        /// <summary>The map of the current run, or null when the catalog has no entry for it.</summary>
        public MapEntry CurrentMap => catalog != null && session != null ? catalog.Find(session.ChosenMapId) : null;

        private void Awake()
        {
            if (session == null)
            {
                Debug.LogError("[GameLoopController] No ShopSession assigned; the run cannot be tracked.");
                return;
            }

            if (!session.HasActiveRun)
            {
                string mapId = !string.IsNullOrEmpty(sceneMapId) ? sceneMapId : (config != null ? config.fallbackMapId : null);
                session.RecordDeparture(session.CustomerIndex, mapId);
                Debug.Log("[GameLoopController] No shop run in progress (scene played directly); starting a run on map '" + mapId + "'.");
            }
            session.SetRunLevel(levelIndex);
        }

        private void OnEnable()
        {
            if (level != null) level.StateChanged += OnLevelStateChanged;
            if (nextButton != null) nextButton.onClick.AddListener(AdvanceAfterLevel);
        }

        private void OnDisable()
        {
            if (level != null) level.StateChanged -= OnLevelStateChanged;
            if (nextButton != null) nextButton.onClick.RemoveListener(AdvanceAfterLevel);
        }

        private void Start()
        {
            if (nextButtonLabel != null && strings != null) nextButtonLabel.text = strings.levelNextLabel;
        }

        /// <summary>
        /// After a passed obby level that is not the map's last: loads the map's next level. Ignored
        /// unless the level is completed. Wired to the Level Complete panel's Next button.
        /// </summary>
        public void AdvanceAfterLevel()
        {
            if (level == null || level.State != ObbyLevelState.Completed || config == null) return;
            if (IsLastLevel) return; // the suction stage takes over instead

            MapEntry map = CurrentMap;
            if (map == null)
            {
                Debug.LogWarning("[GameLoopController] Map '" + (session != null ? session.ChosenMapId : "?")
                                 + "' is not in the MapCatalog; cannot find its next level.");
                return;
            }
            LoadScene(config.LevelSceneName(map.displayName, levelIndex + 1));
        }

        /// <summary>
        /// End of the run: marks the bottle as brought back and loads the shop, where
        /// ShopFlowController.ResumeFromSession puts the bottle on the counter.
        /// </summary>
        public void ReturnToShop()
        {
            if (session == null || config == null) return;
            session.SetRunLevel(0);
            session.ReturnWithBottle();
            LoadScene(config.shopSceneName);
        }

        /// <summary>
        /// Called by the suction minigame when the bottle is full: stops the countdown and adds the
        /// level-2 time (obby plus suction) to the run.
        /// </summary>
        public void SuctionCollected()
        {
            if (!suctionRunning) return;
            suctionRunning = false;
            level.StopTimer();
            if (session != null && level.Timer != null) session.AddRunSeconds(level.Timer.Duration - level.Timer.Remaining);
        }

        private void OnLevelStateChanged(ObbyLevelState state)
        {
            if (state == ObbyLevelState.GameOver && suctionRunning)
            {
                // Time ran out while collecting: the pass is void, Restart replays level 2.
                suctionRunning = false;
                if (suctionStage != null) suctionStage.Fail();
                return;
            }
            if (state == ObbyLevelState.Playing || levelResultRecorded || session == null) return;
            levelResultRecorded = true;

            // Falls count on every attempt; time counts only for the attempt that passed the level.
            session.AddRunDeaths(level.DeathCount);
            if (state != ObbyLevelState.Completed) return;

            if (IsLastLevel && suctionStage != null)
            {
                // The time is added when the bottle is full (SuctionCollected); the clock keeps running.
                suctionRunning = true;
                level.ContinueTimerAfterCompletion();
                suctionStage.Begin(session.ChosenMapId);
                return;
            }

            if (level.Timer != null) session.AddRunSeconds(level.Timer.Duration - level.Timer.Remaining);
            if (IsLastLevel)
            {
                Debug.LogWarning("[GameLoopController] Last level completed but no SuctionStage is assigned; rebuild the scene with ObbyTestSceneBuilder.BuildMaps().");
            }
        }

        private bool IsLastLevel => config != null && levelIndex >= Mathf.Max(1, config.levelsPerMap);

        private void LoadScene(string sceneName)
        {
            if (loading) return;
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning("[GameLoopController] Scene '" + sceneName
                                 + "' is not in Build Settings (File > Build Profiles > Scene List).");
                return;
            }
            loading = true;
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
    }
}
