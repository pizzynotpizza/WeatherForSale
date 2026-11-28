#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

namespace WeatherForSale.Progress
{
    /// <summary>
    /// Progress store for WebGL builds: the browser's <c>localStorage</c>, through
    /// Assets/Plugins/WebGL/LocalStorage.jslib. localStorage is per origin, and every itch.io game
    /// shares the same origin, so the key carries the game's prefix ("weatherforsale."). The origin
    /// does not change when a new build is uploaded, so progress survives uploads.
    /// Outside a WebGL player (Editor, tests) it falls back to PlayerPrefs so the code still runs.
    /// Implements: story-005 criteria 2 and 3.
    /// </summary>
    public sealed class LocalStorageProgressStore : IProgressStore
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern string WFS_LocalStorageGet(string key);
        [DllImport("__Internal")] private static extern int WFS_LocalStorageSet(string key, string value);
#endif

        private readonly string key;

        /// <summary>Creates a store that keeps the JSON under <paramref name="key"/>.</summary>
        public LocalStorageProgressStore(string key)
        {
            this.key = key;
        }

        /// <inheritdoc />
        public string Load()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return WFS_LocalStorageGet(key);
#else
            return PlayerPrefs.GetString(key, null);
#endif
        }

        /// <inheritdoc />
        public void Save(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (WFS_LocalStorageSet(key, json) == 0)
            {
                Debug.LogWarning("[LocalStorageProgressStore] localStorage is unavailable; progress was not saved.");
            }
#else
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
#endif
        }
    }
}
