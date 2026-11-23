using UnityEngine;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// Every on-screen English label in the shop, so code holds no UI string literals. The game is
    /// English-only (design/game-brief.md "Look and references": Language); there is no
    /// localization layer. Customer lines live in <see cref="CustomerData"/>.
    /// Implements: story-002.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopStrings", menuName = "Weather For Sale/Shop/Strings")]
    public sealed class ShopStrings : ScriptableObject
    {
        [Header("Dialog")]
        [Tooltip("Speaker prefix. {0} = customer name. Rich text allowed.")]
        public string dialogSpeakerFormat = "<b>{0}:</b> ";

        [Header("Prompts")]
        [Tooltip("Shown while waiting for the player to open the map.")]
        public string idlePrompt = "Tap the map board to choose where to go";
        [Tooltip("Shown while the bottle waits on the counter.")]
        public string deliverPrompt = "Tap the bottle to hand it over";

        [Header("Map panel")]
        public string mapPanelTitle = "Choose a map";

        [Header("Loading")]
        public string loadingLabel = "Loading…";

        [Header("Pause")]
        public string pauseTitle = "Paused";
        public string volumeAllLabel = "All";
        public string volumeMusicLabel = "Music";
        public string volumeSfxLabel = "SFX";
        public string backLabel = "Back";
        public string menuLabel = "Menu";

        [Header("Achievement book")]
        public string achievementsTitle = "Achievements";
        public string achievementDie10 = "Die 10 times";
        public string achievementStars12 = "Earn 12 stars";
        public string achievementNormalEnding = "Normal Ending";
        public string achievementTrueEnding = "True Ending";

        [Header("End of day (placeholder until story 006)")]
        public string endOfDayTitle = "End of day";
        public string endOfDayBody = "Every customer has been served.";

        /// <summary>Label for <paramref name="id"/>.</summary>
        public string AchievementLabel(AchievementId id)
        {
            switch (id)
            {
                case AchievementId.Die10Times: return achievementDie10;
                case AchievementId.Earn12Stars: return achievementStars12;
                case AchievementId.NormalEnding: return achievementNormalEnding;
                default: return achievementTrueEnding;
            }
        }
    }
}
