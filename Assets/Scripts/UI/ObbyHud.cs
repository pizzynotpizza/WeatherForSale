using UnityEngine;
using UnityEngine.UI;
using WeatherForSale.Obby;

namespace WeatherForSale.UI
{
    /// <summary>
    /// On-screen display for an obby level: remaining time (always visible), fall counter, the
    /// "Level complete" panel and the Game Over panel with a restart button. It only listens to
    /// <see cref="ObbyLevelManager"/> events; the game logic never references the UI.
    /// Implements: story-001 criteria 4 and 5.
    /// </summary>
    public sealed class ObbyHud : MonoBehaviour
    {
        [SerializeField] private ObbyLevelManager level;
        [SerializeField] private ObbyLevelConfig config;

        [Header("Always-on HUD")]
        [SerializeField] private Text timerText;
        [SerializeField] private Text fallsText;
        [SerializeField] private Color timerNormalColor = Color.white;
        [SerializeField] private Color timerWarningColor = new Color(1f, 0.3f, 0.25f);

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button gameOverRestartButton;

        [Header("Level complete")]
        [SerializeField] private GameObject completePanel;
        [SerializeField] private Text completeSummaryText;
        [SerializeField] private Button completeRestartButton;

        private int shownSeconds = -1;
        private bool shownWarning;

        private void Start()
        {
            level.StateChanged += OnStateChanged;
            level.DeathCountChanged += OnDeathCountChanged;

            if (gameOverRestartButton != null) gameOverRestartButton.onClick.AddListener(level.RestartLevel);
            if (completeRestartButton != null) completeRestartButton.onClick.AddListener(level.RestartLevel);

            OnDeathCountChanged(level.DeathCount);
            OnStateChanged(level.State);
            RefreshTimer(true);
        }

        private void OnDestroy()
        {
            if (level == null) return;
            level.StateChanged -= OnStateChanged;
            level.DeathCountChanged -= OnDeathCountChanged;
            if (gameOverRestartButton != null) gameOverRestartButton.onClick.RemoveListener(level.RestartLevel);
            if (completeRestartButton != null) completeRestartButton.onClick.RemoveListener(level.RestartLevel);
        }

        private void Update()
        {
            RefreshTimer(false);
        }

        private void RefreshTimer(bool force)
        {
            // Rounded up so the display reads 0:00 only when time is really out.
            int seconds = Mathf.CeilToInt(level.RemainingSeconds);
            bool warning = level.RemainingSeconds <= config.lowTimeWarningSeconds;

            if (!force && seconds == shownSeconds && warning == shownWarning) return;

            if (seconds != shownSeconds || force)
            {
                shownSeconds = seconds;
                timerText.text = FormatTime(seconds);
            }
            if (warning != shownWarning || force)
            {
                shownWarning = warning;
                timerText.color = warning ? timerWarningColor : timerNormalColor;
            }
        }

        private void OnDeathCountChanged(int count)
        {
            if (fallsText != null) fallsText.text = "Falls: " + count;
        }

        private void OnStateChanged(ObbyLevelState state)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(state == ObbyLevelState.GameOver);
            if (completePanel != null) completePanel.SetActive(state == ObbyLevelState.Completed);

            if (state == ObbyLevelState.Completed && completeSummaryText != null)
            {
                completeSummaryText.text = "Time left: " + FormatTime(Mathf.CeilToInt(level.RemainingSeconds))
                                           + "\nFalls: " + level.DeathCount;
            }
            RefreshTimer(true);
        }

        private static string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return minutes + ":" + (seconds < 10 ? "0" : "") + seconds;
        }
    }
}
