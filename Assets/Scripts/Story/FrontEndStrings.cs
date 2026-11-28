using UnityEngine;

namespace WeatherForSale.Story
{
    /// <summary>
    /// Player-facing text of the Title, Mode Select, Option, Level Select, Cutscene and Story Complete
    /// screens (English; edit in the asset, not in code).
    /// </summary>
    [CreateAssetMenu(menuName = "Weather For Sale/Front End Strings", fileName = "FrontEndStrings")]
    public sealed class FrontEndStrings : ScriptableObject
    {
        [Header("Mode Select")]
        public string modeTitle = "Choose a mode";
        public string storyModeLabel = "Story Mode";
        public string stageModeLabel = "Stage";
        public string stageLockedText = "Unlocks after you clear Story Mode";

        [Header("Option")]
        public string optionTitle = "Options";
        public string volumeAllLabel = "All";
        public string volumeMusicLabel = "Music";
        public string volumeSfxLabel = "SFX";
        public string backLabel = "Back";
        public string quitLabel = "Quit";

        [Header("Level Select")]
        public string levelSelectTitle = "Choose a stage";
        public string secretLockedLabel = "???";
        public string secretUnlockedLabel = "Secret Boss";
        public string secretLockedHint = "Get the Secret Ending to unlock";
        public string startLabel = "Start";
        public string shardLabel = "Shard";
        [Tooltip("Star time line: {0} = stars, {1} = time limit m:ss.")]
        public string starTimeFormat = "{0}  under {1}";
        public string anyFinishText = "any finish";

        [Header("Shop, end of the Story day")]
        public string uncleName = "Uncle John";
        [TextArea(2, 3)]
        public string[] uncleReturnLines =
        {
            "I'm back, Sam! The whole way home, people kept stopping me to talk about my amazing nephew.",
            "Four customers, four perfect bottles of weather, all in one day? Sam... you're a natural!",
        };

        [Header("Cutscene")]
        public string skipLabel = "Skip";
        public string continueHint = "Tap to continue";
        public string normalEndingCard = "Normal Ending";
        public string secretEndingCard = "Secret Ending";

        [Header("Story Complete")]
        public string storyCompleteTitle = "Story Complete!";
        public string starsLabel = "Stars";
        public string timeLabel = "Time";
        public string secretStarsLabel = "Secret Stars";
        [Tooltip("Title of the Tempest details panel on Level Select.")] public string secretBossTitle = "Tempest";
        public string menuLabel = "Menu";
    }
}
