using UnityEngine;

namespace WeatherForSale.Progress
{
    /// <summary>
    /// Progress store for the Editor and non-WebGL platforms. Not used on WebGL: there PlayerPrefs
    /// are tied to the page URL, which itch.io changes on every upload.
    /// Implements: story-005 criterion 4.
    /// </summary>
    public sealed class PlayerPrefsProgressStore : IProgressStore
    {
        private readonly string key;

        /// <summary>Creates a store that keeps the JSON under <paramref name="key"/>.</summary>
        public PlayerPrefsProgressStore(string key)
        {
            this.key = key;
        }

        /// <inheritdoc />
        public string Load()
        {
            return PlayerPrefs.GetString(key, null);
        }

        /// <inheritdoc />
        public void Save(string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }
    }
}
