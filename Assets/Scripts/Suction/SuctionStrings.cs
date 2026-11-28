using UnityEngine;

namespace WeatherForSale.Suction
{
    /// <summary>
    /// Every on-screen English label of the suction minigame, the Weather Collected screen, the
    /// Complete screen and the obby level-complete Next button, so code holds no UI string literals.
    /// The game is English-only; there is no localization layer.
    /// Implements: story-003.
    /// </summary>
    [CreateAssetMenu(fileName = "SuctionStrings", menuName = "Weather For Sale/Suction/Strings")]
    public sealed class SuctionStrings : ScriptableObject
    {
        [Header("Obby level complete")]
        [Tooltip("Button on the Level Complete panel that continues the run.")]
        public string levelNextLabel = "Next";

        [Header("Suction minigame")]
        [Tooltip("Gauge value. {0} = whole percent 0-100.")]
        public string gaugePercentFormat = "{0}%";
        [Tooltip("Big prompt at the lower left on PC only. {0} = the key.")]
        public string keyPromptFormat = "Press {0}!";
        [Tooltip("Title at the top centre while collecting (PC and touch).")]
        public string collectTitle = "Collect the Weather!";
        [Tooltip("Label on the touch tap button.")]
        public string tapButtonLabel = "TAP!";

        [Header("Weather Collected")]
        public string weatherCollectedTitle = "Weather Collected!";
        public string continueLabel = "Continue";

        [Header("Complete")]
        public string stageCompleteTitle = "Stage Complete!";
        public string timeLabel = "Time";
        [Tooltip("Total time. {0} = minutes, {1} = seconds.")]
        public string timeValueFormat = "{0:00}:{1:00}";
        public string keyItemLabel = "Shard";
        [Tooltip("{0} = shards collected this run (0 or 1).")]
        public string keyItemValueFormat = "{0}/1";
        public string nextLabel = "Next";

        /// <summary>Formats <paramref name="totalSeconds"/> as mm:ss (rounded down).</summary>
        public string FormatTime(float totalSeconds)
        {
            int whole = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
            return string.Format(timeValueFormat, whole / 60, whole % 60);
        }
    }
}
