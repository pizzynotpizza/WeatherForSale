namespace WeatherForSale.Shop
{
    /// <summary>The four achievements listed in the book (design/game-brief.md Features 7).</summary>
    public enum AchievementId
    {
        /// <summary>Die 10 times.</summary>
        Die10Times,
        /// <summary>Earn all 12 stars.</summary>
        Earn12Stars,
        /// <summary>See the Normal Ending.</summary>
        NormalEnding,
        /// <summary>See the True Ending.</summary>
        TrueEnding,
    }

    /// <summary>
    /// Read-only achievement progress for the Achievement book. Story 005 implements it (with
    /// localStorage persistence); until then the book shows every entry locked.
    /// </summary>
    public interface IAchievementSource
    {
        /// <summary>True when <paramref name="id"/> has been earned.</summary>
        bool IsUnlocked(AchievementId id);
    }
}
