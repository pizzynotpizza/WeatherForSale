using UnityEngine;

namespace WeatherForSale.Core
{
    /// <summary>
    /// Scene names and run rules for the shop to obby to suction to shop loop.
    /// Implements: story-003 (design/game-brief.md "Core loop": two obby levels per map, then the
    /// in-place suction minigame at the end of the last level, then back to the shop).
    /// </summary>
    [CreateAssetMenu(fileName = "GameLoopConfig", menuName = "Weather For Sale/Game Loop Config")]
    public sealed class GameLoopConfig : ScriptableObject
    {
        [Tooltip("Shop scene loaded after the Complete screen's Next button.")]
        public string shopSceneName = "Shop";

        [Tooltip("Obby levels per map. Passing the last one starts the in-place suction.")]
        [Min(1)] public int levelsPerMap = 2;

        [Tooltip("Obby scene name pattern. {0} = map display name (MapCatalog), {1} = level number (1-based).")]
        public string levelSceneFormat = "Obby_{0}_{1}";

        [Tooltip("Map used when a loop scene is played directly without a shop run and the scene has no map of its own (none of the current scenes; obby scenes carry their own map id).")]
        public string fallbackMapId = "rainforest";

        /// <summary>Scene name of level <paramref name="levelIndex"/> (1-based) of the map called <paramref name="mapDisplayName"/>.</summary>
        public string LevelSceneName(string mapDisplayName, int levelIndex)
        {
            return string.Format(levelSceneFormat, mapDisplayName, levelIndex);
        }
    }
}
