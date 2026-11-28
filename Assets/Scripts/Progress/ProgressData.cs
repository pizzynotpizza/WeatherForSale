using System;
using System.Collections.Generic;

namespace WeatherForSale.Progress
{
    /// <summary>Best star count earned on one map.</summary>
    [Serializable]
    public sealed class MapStars
    {
        /// <summary>Map id (MapEntry.id), e.g. "rainforest".</summary>
        public string mapId = "";

        /// <summary>Best stars earned on the map, 0-3.</summary>
        public int stars;
    }

    /// <summary>
    /// Everything the game remembers between visits, saved as one JSON string (JsonUtility).
    /// Lists keep string ids rather than enum indexes so reordering an enum never shifts saved data.
    /// Implements: story-005 criterion 1 (what is saved) and the Implementation Notes (JSON + version).
    /// </summary>
    [Serializable]
    public sealed class ProgressData
    {
        /// <summary>Save format version written by this build.</summary>
        public const int CurrentVersion = 1;

        /// <summary>Save format version of this data.</summary>
        public int version = CurrentVersion;

        /// <summary>Ids of the maps whose purple Shard has been collected.</summary>
        public List<string> shardMaps = new List<string>();

        /// <summary>Best stars per map.</summary>
        public List<MapStars> bestStars = new List<MapStars>();

        /// <summary>True once the Normal Ending has been seen.</summary>
        public bool normalEndingSeen;

        /// <summary>True once the True Ending has been seen.</summary>
        public bool trueEndingSeen;

        /// <summary>Unlocked achievements, by <see cref="WeatherForSale.Shop.AchievementId"/> name.</summary>
        public List<string> achievements = new List<string>();

        /// <summary>Deaths/falls across every run ever played.</summary>
        public int totalDeaths;

        /// <summary>Ids of the maps that have been cleared at least once.</summary>
        public List<string> unlockedMaps = new List<string>();

        /// <summary>Secret boss flag (stored here; set and read by stories 006/007).</summary>
        public bool secretBossUnlocked;

        /// <summary>Best secret stars (0-3) from sealing Tempest, by time (Story or from Level Select).</summary>
        public int secretStars;

        /// <summary>Sum of the best stars of every map.</summary>
        public int TotalStars
        {
            get
            {
                int total = 0;
                foreach (MapStars entry in bestStars)
                {
                    if (entry != null) total += entry.stars;
                }
                return total;
            }
        }

        /// <summary>Best stars earned on <paramref name="mapId"/>, 0 when never cleared.</summary>
        public int BestStars(string mapId)
        {
            MapStars entry = FindStars(mapId);
            return entry != null ? entry.stars : 0;
        }

        /// <summary>The star entry of <paramref name="mapId"/>, or null.</summary>
        public MapStars FindStars(string mapId)
        {
            foreach (MapStars entry in bestStars)
            {
                if (entry != null && entry.mapId == mapId) return entry;
            }
            return null;
        }

        /// <summary>Replaces null lists (missing from old or hand-edited JSON) with empty ones.</summary>
        public void FillMissing()
        {
            shardMaps ??= new List<string>();
            bestStars ??= new List<MapStars>();
            achievements ??= new List<string>();
            unlockedMaps ??= new List<string>();
            if (totalDeaths < 0) totalDeaths = 0;
        }
    }
}
