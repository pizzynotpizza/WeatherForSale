using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WeatherForSale.Story
{
    /// <summary>How the current play session was started from the Title screen.</summary>
    public enum PlayMode
    {
        /// <summary>Story Mode: cutscenes, four customers, an ending.</summary>
        Story,
        /// <summary>Stage (Freeplay): pick one map (or the secret boss) from Level Select, no cutscenes.</summary>
        Freeplay,
    }

    /// <summary>Which cutscene the Cutscene scene plays next.</summary>
    public enum CutsceneId
    {
        /// <summary>Sam wakes, dresses, walks to the shop and finds Uncle John's note.</summary>
        Opening,
        /// <summary>Four customers served without all four Shards.</summary>
        NormalEnding,
        /// <summary>All four Shards: Tempest appears and the Shards become the purple bottle.</summary>
        TrueIntro,
        /// <summary>After Tempest is sealed: clear sky, rainbow, then the Complete screen.</summary>
        TrueEnding,
    }

    /// <summary>
    /// The current play session across scene loads: Story or Freeplay, the cutscene to play next, and the
    /// day's totals for the Story Complete screen (stars and time over the four maps). Lives as an asset
    /// referenced by the Title, Cutscene, Shop, map and boss scenes (no singleton), like ShopSession.
    /// Implements: story-006 (mode, cutscene hand-off, Complete screen totals), story-007 (Freeplay mode).
    /// </summary>
    [CreateAssetMenu(menuName = "Weather For Sale/Game Mode State", fileName = "GameModeState")]
    public sealed class GameModeState : ScriptableObject
    {
        [Header("Scene names (Build Settings)")]
        public string titleScene = "Title";
        public string cutsceneScene = "Cutscene";
        public string shopScene = "Shop";
        public string levelSelectScene = "LevelSelect";
        public string bossScene = "Boss_Tempest";

        [System.NonSerialized] private PlayMode mode = PlayMode.Story;
        [System.NonSerialized] private CutsceneId pendingCutscene = CutsceneId.Opening;
        [System.NonSerialized] private int dayStars;
        [System.NonSerialized] private float daySeconds;
        [System.NonSerialized] private int secretStars;

        /// <summary>Story or Freeplay.</summary>
        public PlayMode Mode => mode;

        /// <summary>True in Stage (Freeplay) mode.</summary>
        public bool IsFreeplay => mode == PlayMode.Freeplay;

        /// <summary>The cutscene the Cutscene scene plays.</summary>
        public CutsceneId PendingCutscene => pendingCutscene;

        /// <summary>Stars earned over the Story day (0-12).</summary>
        public int DayStars => dayStars;

        /// <summary>Seconds used over the Story day (all maps, obby and suction).</summary>
        public float DaySeconds => daySeconds;

        /// <summary>Secret stars (0-3) from this Story run's Tempest fight.</summary>
        public int SecretStars => secretStars;

        /// <summary>Remembers the secret stars of this Story run's Tempest win (for the Story Complete screen).</summary>
        public void SetSecretStars(int stars) => secretStars = Mathf.Clamp(stars, 0, 3);

        private void OnEnable()
        {
            hideFlags |= HideFlags.DontUnloadUnusedAsset; // keep the state while scenes that do not reference it load
            ResetDay();
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
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                mode = PlayMode.Story;
                pendingCutscene = CutsceneId.Opening;
                ResetDay();
            }
        }
#endif

        /// <summary>Starts Story Mode: day totals cleared, the opening cutscene next.</summary>
        public void BeginStory()
        {
            mode = PlayMode.Story;
            pendingCutscene = CutsceneId.Opening;
            ResetDay();
        }

        /// <summary>Starts Stage (Freeplay) mode.</summary>
        public void BeginFreeplay()
        {
            mode = PlayMode.Freeplay;
            ResetDay();
        }

        /// <summary>Sets the cutscene the Cutscene scene plays next.</summary>
        public void SetCutscene(CutsceneId id)
        {
            pendingCutscene = id;
        }

        /// <summary>Adds one cleared map to the Story day totals (ignored in Freeplay).</summary>
        public void AddMapResult(int stars, float seconds)
        {
            if (mode != PlayMode.Story) return;
            dayStars += Mathf.Clamp(stars, 0, 3);
            if (seconds > 0f) daySeconds += seconds;
        }

        private void ResetDay()
        {
            secretStars = 0;
            dayStars = 0;
            daySeconds = 0f;
        }
    }
}
