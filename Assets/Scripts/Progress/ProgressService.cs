using System;
using System.Collections.Generic;
using UnityEngine;
using WeatherForSale.Shop;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WeatherForSale.Progress
{
    /// <summary>Which ending was seen (recorded by story 006).</summary>
    public enum EndingKind
    {
        /// <summary>The Normal Ending.</summary>
        Normal,
        /// <summary>The True Ending.</summary>
        True,
    }

    /// <summary>
    /// Saved player progress across visits: Shards, best stars, endings seen, achievements, deaths,
    /// cleared maps and the secret boss flag. Referenced through serialized fields like ShopSession
    /// (no static singleton); the store is chosen per platform and can be injected with
    /// <see cref="UseStore"/>. Every Record call saves at once and shows an on-screen notice for each
    /// newly unlocked achievement. Damaged or empty saved data starts from defaults without errors.
    /// Implements: story-005 (all criteria; design/game-brief.md Features 7).
    /// </summary>
    [CreateAssetMenu(fileName = "ProgressService", menuName = "Weather For Sale/Progress/Progress Service")]
    public sealed class ProgressService : ScriptableObject, IAchievementSource
    {
        /// <summary>Storage key; the "weatherforsale." prefix keeps it apart from other itch.io games.</summary>
        public const string StorageKey = "weatherforsale.progress";

        [SerializeField] private AchievementRules rules = new AchievementRules();

        [Header("Unlock notice")]
        [SerializeField] private string noticeTitle = "Achievement Unlocked!";
        [SerializeField, Tooltip("Names in book order: Die 10 times, Earn 12 stars, Normal Ending, True Ending.")]
        private string[] achievementNames = { "Die 10 times", "Earn 12 stars", "Normal Ending", "True Ending" };
        [SerializeField, Min(0.5f), Tooltip("Seconds each notice stays on screen.")]
        private float noticeSeconds = 3f;

        [NonSerialized] private IProgressStore store;
        [NonSerialized] private ProgressData data;
        [NonSerialized] private AchievementService achievements;
        [NonSerialized] private AchievementToast toast;

        /// <summary>Raised once per achievement, when it is unlocked.</summary>
        public event Action<AchievementId> AchievementUnlocked;

        /// <summary>The loaded progress (loaded on first access).</summary>
        public ProgressData Data
        {
            get
            {
                EnsureLoaded();
                return data;
            }
        }

        private void OnEnable()
        {
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
            ClearCache();
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // Reload from storage on every play, also with "Enter Play Mode without domain reload".
            if (change == PlayModeStateChange.ExitingEditMode) ClearCache();
        }
#endif

        /// <summary>Uses <paramref name="progressStore"/> from now on and reloads from it.</summary>
        public void UseStore(IProgressStore progressStore)
        {
            store = progressStore;
            data = null;
        }

        /// <inheritdoc />
        public bool IsUnlocked(AchievementId id) => AchievementService.IsUnlocked(Data, id);

        /// <summary>Adds deaths (falls) to the lifetime total. Non-positive values are ignored.</summary>
        public void RecordDeaths(int count)
        {
            if (count <= 0) return;
            Data.totalDeaths += count;
            Commit();
        }

        /// <summary>Records that the purple Shard of <paramref name="mapId"/> was collected.</summary>
        public void RecordShard(string mapId)
        {
            if (string.IsNullOrEmpty(mapId) || Data.shardMaps.Contains(mapId)) return;
            data.shardMaps.Add(mapId);
            Commit();
        }

        /// <summary>
        /// Records a cleared map: adds it to the cleared maps and keeps the best of
        /// <paramref name="stars"/> (0-3) and the stars already saved.
        /// </summary>
        public void RecordMapCleared(string mapId, int stars)
        {
            if (string.IsNullOrEmpty(mapId)) return;
            stars = Mathf.Clamp(stars, 0, 3);
            if (!Data.unlockedMaps.Contains(mapId)) data.unlockedMaps.Add(mapId);
            MapStars entry = data.FindStars(mapId);
            if (entry == null) data.bestStars.Add(new MapStars { mapId = mapId, stars = stars });
            else entry.stars = Mathf.Max(entry.stars, stars);
            Commit();
        }

        /// <summary>Records that <paramref name="ending"/> was seen (story 006 calls this).</summary>
        public void RecordEnding(EndingKind ending)
        {
            if (ending == EndingKind.True) Data.trueEndingSeen = true;
            else Data.normalEndingSeen = true;
            Commit();
        }

        /// <summary>Saves the secret stars (0-3) of a Tempest win; the best result is kept.</summary>
        public void RecordSecretStars(int stars)
        {
            stars = Mathf.Clamp(stars, 0, 3);
            if (stars <= Data.secretStars) return;
            data.secretStars = stars;
            Commit();
        }

        /// <summary>Sets the stored secret boss flag (story 007 decides when).</summary>
        public void SetSecretBossUnlocked(bool unlocked)
        {
            if (Data.secretBossUnlocked == unlocked) return;
            data.secretBossUnlocked = unlocked;
            Commit();
        }

        /// <summary>
        /// Parses saved JSON. Null, empty or damaged text gives fresh defaults (never throws).
        /// </summary>
        public static ProgressData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new ProgressData();
            try
            {
                ProgressData parsed = JsonUtility.FromJson<ProgressData>(json);
                if (parsed == null) return new ProgressData();
                parsed.FillMissing();
                return parsed;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ProgressService] Saved progress is damaged; starting fresh. " + e.Message);
                return new ProgressData();
            }
        }

        private void EnsureLoaded()
        {
            if (data != null) return;
            store ??= CreateDefaultStore();
            achievements = new AchievementService(rules);
            string json = null;
            try
            {
                json = store.Load();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ProgressService] Could not read saved progress; starting fresh. " + e.Message);
            }
            data = Parse(json);
            data.version = ProgressData.CurrentVersion;
        }

        private void Commit()
        {
            List<AchievementId> unlocked = achievements.Evaluate(data);
            try
            {
                store.Save(JsonUtility.ToJson(data));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ProgressService] Could not save progress. " + e.Message);
            }
            foreach (AchievementId id in unlocked)
            {
                AchievementUnlocked?.Invoke(id);
                ShowNotice(id);
            }
        }

        private void ShowNotice(AchievementId id)
        {
            if (!Application.isPlaying) return;
            if (toast == null) toast = AchievementToast.Create(noticeSeconds);
            int index = Array.IndexOf(AchievementService.All, id);
            string name = achievementNames != null && index >= 0 && index < achievementNames.Length
                ? achievementNames[index] : id.ToString();
            toast.Enqueue(noticeTitle, name);
        }

        private void ClearCache()
        {
            store = null;
            data = null;
            toast = null;
        }

        private static IProgressStore CreateDefaultStore()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new LocalStorageProgressStore(StorageKey);
#else
            return new PlayerPrefsProgressStore(StorageKey);
#endif
        }
    }
}
