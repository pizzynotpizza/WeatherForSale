using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeatherForSale.Obby;
using WeatherForSale.Progress;
using WeatherForSale.Shop;
using WeatherForSale.Suction;

namespace WeatherForSale.Core
{
    /// <summary>
    /// Moves the player through one map run: the map's obby course (one scene per map since story 004:
    /// the old level 1 and level 2 merged into one course), the in-place suction at its end
    /// (<see cref="SuctionStage"/>), then back to the shop with the bottle. A scene with a suction stage
    /// is always the map's last level; the Next-button path below only serves a multi-scene map. Lives in
    /// every map obby scene. Run totals are kept in <see cref="ShopSession"/>, which
    /// survives scene loads; there is no singleton.
    /// Played directly from the editor without a shop run, it starts a run on this scene's map (or
    /// <see cref="GameLoopConfig.fallbackMapId"/>) so the loop still works.
    /// Level 1 complete: the Level Complete panel's Next button loads level 2. Last level complete:
    /// the suction starts in place at once (no panel). The countdown stops there: the suction has no time
    /// limit, but its time is added to the run's total, so a slow collect costs stars.
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
        [SerializeField, Tooltip("Saved progress (story 005): deaths, the Shard and the cleared map's stars are recorded here.")]
        private ProgressService progress;
        [SerializeField, Tooltip("Story or Freeplay (story 006/007): Freeplay runs return to Level Select; Story runs add to the day totals.")]
        private WeatherForSale.Story.GameModeState modeState;

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

        private ShardPickup[] shards;
        private bool levelResultRecorded;
        private bool suctionRunning;
        private bool loading;
        private int recordedDeaths;

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
            if (level != null)
            {
                level.StateChanged += OnLevelStateChanged;
                level.DeathCountChanged += OnDeathCountChanged;
            }
            if (nextButton != null) nextButton.onClick.AddListener(AdvanceAfterLevel);
        }

        private void OnDisable()
        {
            if (level != null)
            {
                level.StateChanged -= OnLevelStateChanged;
                level.DeathCountChanged -= OnDeathCountChanged;
            }
            if (nextButton != null) nextButton.onClick.RemoveListener(AdvanceAfterLevel);
        }

        private void Start()
        {
            if (nextButtonLabel != null && strings != null) nextButtonLabel.text = strings.levelNextLabel;

            // The map's hidden Shard counts toward the run (the Complete screen's Shard 0/1).
            shards = FindObjectsByType<ShardPickup>(FindObjectsSortMode.None);
            foreach (ShardPickup shard in shards) shard.Collected += OnShardCollected;
        }

        private void OnDestroy()
        {
            if (shards == null) return;
            foreach (ShardPickup shard in shards)
            {
                if (shard != null) shard.Collected -= OnShardCollected;
            }
        }

        private void OnShardCollected(ShardPickup shard)
        {
            if (session != null) session.MarkShardCollected();
            if (progress != null && session != null) progress.RecordShard(session.ChosenMapId);
        }

        // Each death is saved as it happens (lifetime total for the "Die 10 times" achievement).
        private void OnDeathCountChanged(int deathCount)
        {
            int added = deathCount - recordedDeaths;
            recordedDeaths = deathCount;
            if (added > 0 && progress != null) progress.RecordDeaths(added);
        }

        /// <summary>
        /// Called by the suction minigame when the Complete screen shows: saves the cleared map and its
        /// <paramref name="stars"/> (best kept) to the player's progress.
        /// </summary>
        public void RecordMapCleared(int stars)
        {
            if (progress != null && session != null) progress.RecordMapCleared(session.ChosenMapId, stars);
            if (modeState != null && session != null) modeState.AddMapResult(stars, session.RunSeconds);
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
            if (modeState != null && modeState.IsFreeplay)
            {
                // Freeplay (story 007): no customer to hand the bottle to; back to Level Select.
                session.ResetSession();
                LoadScene(modeState.levelSelectScene);
                return;
            }
            session.ReturnWithBottle();
            LoadScene(config.shopSceneName);
        }

        /// <summary>
        /// Called by the suction minigame when the bottle is full: adds the time spent collecting to the
        /// run (the level time was added when the level was passed).
        /// </summary>
        public void SuctionCollected(float suctionSeconds)
        {
            if (!suctionRunning) return;
            suctionRunning = false;
            if (session != null) session.AddRunSeconds(suctionSeconds);
        }

        private void OnLevelStateChanged(ObbyLevelState state)
        {
            if (state == ObbyLevelState.Playing || levelResultRecorded || session == null) return;
            levelResultRecorded = true;

            // Falls count on every attempt; time counts only for the attempt that passed the level.
            session.AddRunDeaths(level.DeathCount);
            if (state != ObbyLevelState.Completed) return;
            if (level.Timer != null) session.AddRunSeconds(level.Timer.Duration - level.Timer.Remaining);

            if (IsLastLevel && suctionStage != null)
            {
                // No time limit while collecting; SuctionCollected adds the collecting time.
                suctionRunning = true;
                suctionStage.Begin(session.ChosenMapId);
                return;
            }

            if (IsLastLevel)
            {
                Debug.LogWarning("[GameLoopController] Last level completed but no SuctionStage is assigned; rebuild the scene with ObbyTestSceneBuilder.BuildMaps().");
            }
        }

        // A scene that carries the suction stage always ends the map (story 004: one merged scene per map).
        private bool IsLastLevel => suctionStage != null || (config != null && levelIndex >= Mathf.Max(1, config.levelsPerMap));

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
