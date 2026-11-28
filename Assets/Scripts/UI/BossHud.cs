using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WeatherForSale.Boss;

namespace WeatherForSale.UI
{
    /// <summary>
    /// On-screen display of the boss fight: the player's HP bar (bottom left), the boss name and HP bar
    /// (bottom right), the clock (top centre), a banner for warnings ("Tempest is tired!"), a red flash
    /// when Sam is thrown, a white flash for lightning, and the Game Over / Boss Sealed panel. Listens to the fight
    /// and never changes it, except through the panel buttons.
    /// Implements: story-008 criteria 3-6 (boss bar, timer, tired signal on screen; there is no player health).
    /// </summary>
    public sealed class BossHud : MonoBehaviour
    {
        [SerializeField] private BossFightController fight;
        [SerializeField] private TempestBoss boss;

        [Header("Boss bar")]
        [SerializeField, Tooltip("Boss bar fill (Image type Filled, horizontal).")] private Image bossFill;
        [SerializeField, Tooltip("Pale trail behind the fill that catches up after a hit (Image type Filled, horizontal).")] private Image bossTrail;
        [SerializeField, Min(0f), Tooltip("Seconds after a hit before the trail starts to follow.")] private float trailDelay = 0.5f;
        [SerializeField, Min(0.01f), Tooltip("How fast the trail catches up, bar widths per second.")] private float trailSpeed = 0.4f;
        [SerializeField] private Text bossNameText;

        [Header("Clock and banner")]
        [SerializeField] private Text timerText;
        [SerializeField] private Text bannerText;
        [SerializeField] private Image hurtFlash;
        [SerializeField] private Image whiteFlash;

        [Header("Result panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitle;
        [SerializeField] private Button resultButton;
        [SerializeField] private Text resultButtonLabel;
        [SerializeField, Tooltip("The three secret stars on the Victory panel, left to right; lit by the fight's time, shown only on a win.")] private Image[] secretStars;
        [SerializeField] private Sprite secretStarFull;
        [SerializeField] private Sprite secretStarEmpty;
        [SerializeField, Tooltip("Row holding the three stars (hidden unless won).")] private RectTransform specialStar;
        [SerializeField, Tooltip("Saves the best secret stars when the boss is sealed.")] private WeatherForSale.Progress.ProgressService progress;
        [SerializeField, Min(0.05f), Tooltip("Seconds the special star takes to pop in.")] private float starPopSeconds = 0.45f;

        private float starPop = -1f;
        private Vector2? titleHome;
        [SerializeField, Tooltip("Title position when no stars show (Game Over / Time Up): the middle of the space above the button.")]
        private Vector2 titleWithoutStars = new Vector2(0f, 40f);

        private BossResult? shownResult;
        private float trailFollowAt;
        private Coroutine bannerRoutine;

        private void OnEnable()
        {
            fight.PlayerHit += OnPlayerHit;
            boss.HpChanged += OnBossHp;
            boss.StateChanged += OnBossState;
            boss.PatternStarted += OnPattern;
            boss.PhaseChanged += OnPhaseChanged;
            fight.CrystalCollected += OnCrystalCollected;
            boss.SuctionFailed += OnSuctionFailed;
            fight.Flashed += OnLightning;
            fight.Finished += OnFinished;
            if (resultButton != null) resultButton.onClick.AddListener(OnResultButton);
        }

        private void OnDisable()
        {
            fight.PlayerHit -= OnPlayerHit;
            boss.HpChanged -= OnBossHp;
            boss.StateChanged -= OnBossState;
            boss.PatternStarted -= OnPattern;
            boss.PhaseChanged -= OnPhaseChanged;
            fight.CrystalCollected -= OnCrystalCollected;
            boss.SuctionFailed -= OnSuctionFailed;
            fight.Flashed -= OnLightning;
            fight.Finished -= OnFinished;
            if (resultButton != null) resultButton.onClick.RemoveListener(OnResultButton);
        }

        private void Start()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (bossNameText != null) bossNameText.text = fight.Config.bossName;
            if (bannerText != null) bannerText.text = "";
            if (hurtFlash != null) hurtFlash.color = Color.clear;
            if (whiteFlash != null) whiteFlash.color = Color.clear;
            OnBossHp(boss.Hp, boss.MaxHp);
            ShowBanner(fight.Config.introBanner, fight.Config.introSeconds);
        }

        private void Update()
        {
            if (timerText == null) return;
            int seconds = Mathf.CeilToInt(fight.TimeRemaining);
            timerText.text = (seconds / 60) + ":" + (seconds % 60).ToString("00");
            timerText.color = fight.TimeRemaining <= 30f ? new Color(1f, 0.45f, 0.35f) : Color.white;

            if (bossTrail != null && bossFill != null && Time.time >= trailFollowAt && bossTrail.fillAmount > bossFill.fillAmount)
            {
                bossTrail.fillAmount = Mathf.MoveTowards(bossTrail.fillAmount, bossFill.fillAmount, trailSpeed * Time.deltaTime);
            }

            if (hurtFlash != null && hurtFlash.color.a > 0f)
            {
                Color c = hurtFlash.color;
                c.a = Mathf.Max(0f, c.a - Time.deltaTime * 1.6f);
                hurtFlash.color = c;
            }
            if (whiteFlash != null && whiteFlash.color.a > 0f)
            {
                Color c = whiteFlash.color;
                c.a = Mathf.Max(0f, c.a - Time.deltaTime * 6f);
                whiteFlash.color = c;
            }
        }

        private void OnBossHp(int current, int max)
        {
            float fill = max > 0 ? (float)current / max : 0f;
            if (bossFill != null) bossFill.fillAmount = fill;
            if (bossTrail != null && bossTrail.fillAmount < fill) bossTrail.fillAmount = fill;
            trailFollowAt = Time.time + trailDelay;
        }

        private void OnPlayerHit()
        {
            if (hurtFlash != null) hurtFlash.color = new Color(1f, 0.1f, 0.1f, 0.35f);
        }

        private void OnLightning()
        {
            if (whiteFlash != null) whiteFlash.color = new Color(1f, 1f, 1f, 0.55f);
        }

        private void OnBossState(BossState state)
        {
            if (state == BossState.Tired) ShowBanner(fight.Config.tiredBanner, fight.Config.tiredWarnSeconds + 1.5f);
            else if (state == BossState.Suction) HideBanner(); // the suction HUD has its own title
        }

        private void OnPattern(string patternName)
        {
            ShowBanner(patternName + "!", 1.6f);
        }

        private void OnPhaseChanged(int phase)
        {
            ShowBanner(phase >= 3 ? fight.Config.phase3Banner : fight.Config.phase2Banner, 2.4f);
        }

        private void OnCrystalCollected()
        {
            ShowBanner(fight.Config.itemBanner, 2.4f);
        }

        private void OnSuctionFailed()
        {
            ShowBanner(fight.Config.recoveredBanner, 1.4f);
        }

        private void OnFinished(BossResult result)
        {
            shownResult = result;
            if (resultPanel != null) resultPanel.SetActive(true);
            BossConfig c = fight.Config;
            if (resultTitle != null)
            {
                resultTitle.text = result == BossResult.Won ? c.victoryTitle : result == BossResult.TimeUp ? c.timeUpTitle : c.gameOverTitle;
                resultTitle.color = result == BossResult.Won ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.4f, 0.35f);
            }
            if (resultButtonLabel != null) resultButtonLabel.text = result == BossResult.Won ? c.victoryButton : c.restartLabel;
            if (bannerText != null) bannerText.text = "";
            bool won = result == BossResult.Won;
            if (resultTitle != null)
            {
                titleHome ??= resultTitle.rectTransform.anchoredPosition;
                resultTitle.rectTransform.anchoredPosition = won ? titleHome.Value : titleWithoutStars;
            }
            if (specialStar != null)
            {
                specialStar.gameObject.SetActive(won);
                specialStar.localScale = Vector3.zero;
                starPop = won ? 0f : -1f;
            }
            int earned = fight.SecretStars;
            for (int i = 0; secretStars != null && i < secretStars.Length; i++)
            {
                if (secretStars[i] != null) secretStars[i].sprite = i < earned ? secretStarFull : secretStarEmpty;
            }
            if (won && progress != null) progress.RecordSecretStars(earned);
        }

        private void LateUpdate()
        {
            if (starPop < 0f || specialStar == null) return;
            // Overshoot pop: 0 -> 1.25 -> 1.
            starPop = Mathf.Min(1f, starPop + Time.unscaledDeltaTime / starPopSeconds);
            float s = starPop < 0.7f ? Mathf.Lerp(0f, 1.25f, starPop / 0.7f) : Mathf.Lerp(1.25f, 1f, (starPop - 0.7f) / 0.3f);
            specialStar.localScale = Vector3.one * s;
            if (starPop >= 1f) starPop = -1f;
        }

        private void OnResultButton()
        {
            if (shownResult == BossResult.Won) fight.Continue();
            else fight.Restart();
        }

        private void HideBanner()
        {
            if (bannerText == null) return;
            if (bannerRoutine != null) StopCoroutine(bannerRoutine);
            bannerRoutine = null;
            bannerText.text = "";
        }

        private void ShowBanner(string text, float seconds)
        {
            if (bannerText == null) return;
            if (bannerRoutine != null) StopCoroutine(bannerRoutine);
            bannerRoutine = StartCoroutine(BannerRoutine(text, seconds));
        }

        private IEnumerator BannerRoutine(string text, float seconds)
        {
            bannerText.text = text;
            Color c = bannerText.color;
            c.a = 1f;
            bannerText.color = c;
            yield return new WaitForSeconds(Mathf.Max(0.1f, seconds - 0.4f));
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                c.a = 1f - t / 0.4f;
                bannerText.color = c;
                yield return null;
            }
            bannerText.text = "";
        }
    }
}
