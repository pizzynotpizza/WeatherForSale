using System;
using UnityEngine;

namespace WeatherForSale.Shop
{
    /// <summary>One destination on the counter map.</summary>
    [Serializable]
    public sealed class MapEntry
    {
        [Tooltip("Stable id referenced by CustomerData.correctMapId, e.g. \"rainforest\".")]
        public string id = "";

        [Tooltip("English name on the map button and the Loading screen.")]
        public string displayName = "";

        [Tooltip("Obby scene of this map, e.g. \"Obby_Rainforest\" (both levels merged into one course, story 004).")]
        public string sceneName = "";

        [Tooltip("2D bottle art animated on the Loading screen.")]
        public Sprite bottleSprite;

        [Tooltip("Map art used on the map-select button.")]
        public Sprite thumbnail;

        [Tooltip("The map's weather colour: the map name and Loading text on the Loading screen use it.")]
        public Color themeColour = Color.white;
    }

    /// <summary>
    /// The four maps the player can pick on the counter, plus loading tunables.
    /// Implements: story-002 criteria 3-5 (design/game-brief.md Features 1).
    /// </summary>
    [CreateAssetMenu(fileName = "MapCatalog", menuName = "Weather For Sale/Shop/Map Catalog")]
    public sealed class MapCatalog : ScriptableObject
    {
        [Tooltip("Maps in button order.")]
        public MapEntry[] maps = Array.Empty<MapEntry>();

        [Tooltip("The Loading screen stays up at least this long, seconds, even if the scene loads faster.")]
        [Min(0f)] public float minLoadingSeconds = 1.5f;

        /// <summary>Number of maps.</summary>
        public int Count => maps != null ? maps.Length : 0;

        /// <summary>Returns the map with <paramref name="id"/>, or null when there is none.</summary>
        public MapEntry Find(string id)
        {
            if (maps == null || string.IsNullOrEmpty(id)) return null;
            foreach (MapEntry entry in maps)
            {
                if (entry != null && entry.id == id) return entry;
            }
            return null;
        }
    }
}
