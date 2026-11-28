using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeatherForSale.Progress;
using WeatherForSale.Shop;
using WeatherForSale.Story;
using WeatherForSale.Suction;

namespace WeatherForSale.Freeplay
{
    /// <summary>
    /// Stage (Freeplay) Level Select: the four maps around a "???" slot in the middle, each with its best
    /// stars (0-3). A map opens its details (name, stars, the time for each star, Shard 0/1, Start); Start
    /// plays that map with no cutscenes, and the run comes back here. The "???" slot (the secret Tempest
    /// fight) stays locked until the True Ending has been seen; the flag is saved, so it stays unlocked.
    /// Implements: story-007 criteria 1, 2, 3, 5, 6.
    /// </summary>
    public sealed class LevelSelect : MonoBehaviour
    {
        [SerializeField] private GameModeState state;
        [SerializeField] private ShopSession session;
        [SerializeField] private ProgressService progress;
        [SerializeField] private MapCatalog catalog;
        [SerializeField] private SuctionConfig suctionConfig;
        [SerializeField] private FrontEndStrings strings;
        [SerializeField] private Sprite starFull;
        [SerializeField] private Sprite starEmpty;
        [SerializeField] private Sprite shardFull;
        [SerializeField] private Sprite shardEmpty;

        [Header("Board")]
        [SerializeField] private Text title;
        [SerializeField, Tooltip("One button per map, in MapCatalog order.")] private Button[] mapButtons;
        [SerializeField] private Image[] mapPictures;
        [SerializeField] private Text[] mapNames;
        [SerializeField, Tooltip("Three star images per map, map after map (map 1 stars 1-3, map 2 stars 1-3, ...).")]
        private Image[] mapStars;
        [SerializeField] private Button secretButton;
        [SerializeField] private Text secretLabel;
        [SerializeField] private GameObject secretLock;
        [SerializeField, Tooltip("Tempest's picture in the slot frame (full colour locked or not; the lock badge shows the state).")]
        private Image secretPicture;
        [SerializeField] private Color lockedSilhouette = Color.white;
        [SerializeField, Tooltip("Shown for a moment when the locked secret slot is pressed.")] private Text secretHint;
        [Header("Secret boss details")]
        [SerializeField, Tooltip("Tempest's star times (3 stars / 2 stars; any win is 1).")] private WeatherForSale.Boss.BossConfig bossConfig;
        [SerializeField] private Sprite secretStarFull;
        [SerializeField] private Sprite secretStarEmpty;
        [SerializeField, Tooltip("How far the stars and time lines move down on Tempest's panel (no Shard row), so they sit in the middle.")]
        private float secretContentDrop = 55f;

        private Vector2[] detailHomes;
        [SerializeField, Min(0.5f), Tooltip("Seconds the locked hint stays up after a press.")] private float hintSeconds = 2.5f;
        [SerializeField] private Button backButton;

        [Header("Details")]
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private Text detailName;
        [SerializeField] private Image[] detailStars;
        [SerializeField, Tooltip("One line per star count: 3, 2 and 1 star.")] private Text[] detailStarLines;
        [SerializeField] private Image detailShardIcon;
        [SerializeField] private Text detailShardText;
        [SerializeField] private Button detailStart;
        [SerializeField] private Text detailStartLabel;
        [SerializeField] private Button detailClose;

        private int selected = -1;
        private bool loading;
        private float hintLeft;

        /// <summary>True once the True Ending has been seen (the secret boss slot is open).</summary>
        public bool SecretUnlocked => progress != null && progress.Data.secretBossUnlocked;

        private void Start()
        {
            if (state != null && !state.IsFreeplay) state.BeginFreeplay(); // opened directly in the editor
            if (detailRoot != null) detailRoot.SetActive(false);
            if (strings != null)
            {
                if (title != null) title.text = strings.levelSelectTitle;
                if (detailStartLabel != null) detailStartLabel.text = strings.startLabel;
                if (secretHint != null) secretHint.text = strings.secretLockedHint;
            }

            for (int i = 0; mapButtons != null && i < mapButtons.Length; i++)
            {
                int map = i;
                if (mapButtons[i] != null) mapButtons[i].onClick.AddListener(() => OpenDetails(map));
            }
            if (secretButton != null) secretButton.onClick.AddListener(OnSecretSlotPressed);
            if (backButton != null) backButton.onClick.AddListener(() => Load(state != null ? state.titleScene : "Title"));
            if (detailClose != null) detailClose.onClick.AddListener(() => detailRoot.SetActive(false));
            if (detailStart != null) detailStart.onClick.AddListener(StartSelected);
            Refresh();
        }

        private MapEntry Map(int i) => catalog != null && catalog.maps != null && i >= 0 && i < catalog.maps.Length ? catalog.maps[i] : null;

        private int BestStars(MapEntry map) => progress != null && map != null ? progress.Data.BestStars(map.id) : 0;

        private bool HasShard(MapEntry map) => progress != null && map != null && progress.Data.shardMaps.Contains(map.id);

        /// <summary>Redraws every map's best stars and the secret slot from the saved progress.</summary>
        public void Refresh()
        {
            for (int i = 0; mapButtons != null && i < mapButtons.Length; i++)
            {
                MapEntry map = Map(i);
                if (mapNames != null && i < mapNames.Length && mapNames[i] != null) mapNames[i].text = map != null ? map.displayName : "";
                if (mapPictures != null && i < mapPictures.Length && mapPictures[i] != null && map != null && map.thumbnail != null)
                    mapPictures[i].sprite = map.thumbnail;
                int best = BestStars(map);
                for (int s = 0; s < 3; s++)
                {
                    int k = i * 3 + s;
                    if (mapStars != null && k < mapStars.Length && mapStars[k] != null) mapStars[k].sprite = s < best ? starFull : starEmpty;
                }
            }

            bool open = SecretUnlocked;
            if (secretLock != null) secretLock.SetActive(!open);
            if (secretPicture != null) secretPicture.color = open ? Color.white : lockedSilhouette;
            if (secretHint != null && open) secretHint.gameObject.SetActive(false);
            if (secretLabel != null && strings != null) secretLabel.text = open ? strings.secretUnlockedLabel : strings.secretLockedLabel;
        }

        private const int SecretSelected = -2;

        /// <summary>The unlocked secret slot: the same details panel, with the secret stars and Tempest's times (no Shard).</summary>
        private void OpenSecretDetails()
        {
            if (detailRoot == null) return;
            selected = SecretSelected;
            if (detailName != null) detailName.text = strings != null ? strings.secretBossTitle : "Tempest";
            int best = progress != null ? progress.Data.secretStars : 0;
            for (int s = 0; detailStars != null && s < detailStars.Length; s++)
            {
                if (detailStars[s] != null) detailStars[s].sprite = s < best ? secretStarFull : secretStarEmpty;
            }
            if (detailStarLines != null && bossConfig != null && strings != null)
            {
                string[] stars = { "★★★", "★★", "★" };
                for (int s = 0; s < detailStarLines.Length && s < 3; s++)
                {
                    if (detailStarLines[s] == null) continue;
                    detailStarLines[s].text = s == 0 ? string.Format(strings.starTimeFormat, stars[0], Clock(bossConfig.threeStarSeconds))
                        : s == 1 ? string.Format(strings.starTimeFormat, stars[1], Clock(bossConfig.twoStarSeconds))
                        : stars[2] + "  " + strings.anyFinishText;
                }
            }
            ShowShardRow(false);
            detailRoot.SetActive(true);
        }

        private void ShowShardRow(bool show)
        {
            // Tempest's panel has no Shard row: lower the stars and the time lines into the freed space.
            int n = (detailStars != null ? detailStars.Length : 0) + (detailStarLines != null ? detailStarLines.Length : 0);
            if (detailHomes == null || detailHomes.Length != n)
            {
                detailHomes = new Vector2[n];
                for (int k = 0; k < n; k++) detailHomes[k] = DetailRect(k) != null ? DetailRect(k).anchoredPosition : Vector2.zero;
            }
            for (int k = 0; k < n; k++)
            {
                RectTransform rt = DetailRect(k);
                if (rt != null) rt.anchoredPosition = detailHomes[k] + (show ? Vector2.zero : Vector2.down * secretContentDrop);
            }
            if (detailShardIcon != null) detailShardIcon.gameObject.SetActive(show);
            if (detailShardText != null) detailShardText.gameObject.SetActive(show);
        }

        private RectTransform DetailRect(int k)
        {
            int stars = detailStars != null ? detailStars.Length : 0;
            Graphic g = k < stars ? detailStars[k] : detailStarLines[k - stars];
            return g != null ? g.rectTransform : null;
        }

        private void OpenDetails(int i)
        {
            MapEntry map = Map(i);
            if (map == null || detailRoot == null) return;
            selected = i;
            ShowShardRow(true);
            if (detailName != null) detailName.text = map.displayName;
            int best = BestStars(map);
            for (int s = 0; detailStars != null && s < detailStars.Length; s++)
            {
                if (detailStars[s] != null) detailStars[s].sprite = s < best ? starFull : starEmpty;
            }
            if (detailStarLines != null && suctionConfig != null && strings != null)
            {
                SuctionMapSettings look = suctionConfig.ForMap(map.id);
                float[] limits = look != null
                    ? new[] { look.threeStarSeconds, look.twoStarSeconds, look.oneStarSeconds }
                    : new[] { 0f, 0f, 0f };
                string[] stars = { "★★★", "★★", "★" };
                for (int s = 0; s < detailStarLines.Length && s < 3; s++)
                {
                    if (detailStarLines[s] == null) continue;
                    // The 1-star limit is "any finish" when it is longer than a map's time allows.
                    detailStarLines[s].text = s == 2 && limits[s] >= 600f
                        ? stars[s] + "  " + strings.anyFinishText
                        : string.Format(strings.starTimeFormat, stars[s], Clock(limits[s]));
                }
            }
            bool shard = HasShard(map);
            if (detailShardIcon != null) detailShardIcon.sprite = shard ? shardFull : shardEmpty;
            if (detailShardText != null) detailShardText.text = (strings != null ? strings.shardLabel : "Shard") + "  " + (shard ? 1 : 0) + "/1";
            detailRoot.SetActive(true);
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.RoundToInt(seconds);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        private void StartSelected()
        {
            if (selected == SecretSelected)
            {
                if (state != null) Load(state.bossScene);
                return;
            }
            MapEntry map = Map(selected);
            if (map == null || session == null) return;
            session.RecordDeparture(0, map.id);
            Load(map.sceneName);
        }

        private void OnSecretSlotPressed()
        {
            if (!SecretUnlocked)
            {
                ShowLockedHint();
                return;
            }
            OpenSecretDetails();
        }

        /// <summary>The locked slot was pressed: shows how to unlock it for <see cref="hintSeconds"/>.</summary>
        private void ShowLockedHint()
        {
            if (secretHint == null) return;
            secretHint.gameObject.SetActive(true);
            hintLeft = hintSeconds;
        }

        private void Update()
        {
            if (hintLeft <= 0f) return;
            hintLeft -= Time.deltaTime;
            if (hintLeft <= 0f && secretHint != null) secretHint.gameObject.SetActive(false);
        }

        private void Load(string scene)
        {
            if (loading) return;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogWarning("[LevelSelect] Scene '" + scene + "' is not in Build Settings.");
                return;
            }
            loading = true;
            SceneManager.LoadScene(scene);
        }
    }
}
