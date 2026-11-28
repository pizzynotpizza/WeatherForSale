namespace WeatherForSale.Progress
{
    /// <summary>
    /// Where the progress JSON lives. WebGL builds use <see cref="LocalStorageProgressStore"/>;
    /// the Editor and other platforms use <see cref="PlayerPrefsProgressStore"/>.
    /// Implements: story-005 criteria 2 and 4 (one interface, two stores).
    /// </summary>
    public interface IProgressStore
    {
        /// <summary>The saved JSON, or null/empty when nothing is saved or storage is unavailable.</summary>
        string Load();

        /// <summary>Writes <paramref name="json"/>. Must not throw when storage is unavailable.</summary>
        void Save(string json);
    }
}
