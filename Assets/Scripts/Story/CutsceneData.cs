using System;
using UnityEngine;
using WeatherForSale.Progress;

namespace WeatherForSale.Story
{
    /// <summary>One still of a cutscene: a background, an optional character portrait and one line of text.</summary>
    [Serializable]
    public sealed class CutsceneSlide
    {
        [Tooltip("Background picture; when empty the background colour fills the screen.")]
        public Sprite background;
        public Color backgroundColor = new Color(0.55f, 0.75f, 0.95f);
        [Tooltip("Character shown beside the text box (optional).")]
        public Sprite portrait;
        [Tooltip("Show the portrait on the right instead of the left.")]
        public bool portraitRight;
        [Tooltip("Keep the portrait over the CGI picture: tick it when the speaker is not drawn in the picture.")]
        public bool portraitOverPicture;
        [Tooltip("Speaker name above the text; empty for narration.")]
        public string speaker = "";
        [TextArea(2, 4)] public string text = "";
        [Tooltip("A white lightning flash when the slide appears.")]
        public bool flash;
        [Tooltip("The picture shakes while the slide shows (storm, wind).")]
        public bool shake;
        [Tooltip("Text in large white letters on black, centred, with no text box (the Normal Ending message).")]
        public bool bigCentredText;
    }

    /// <summary>What happens after the last slide.</summary>
    public enum CutsceneNext
    {
        /// <summary>Load the shop (Story day starts or continues).</summary>
        Shop,
        /// <summary>Load the Tempest boss arena.</summary>
        Boss,
        /// <summary>Back to the Title screen.</summary>
        Title,
        /// <summary>Show the Story Complete screen (stars, time, Menu).</summary>
        CompleteScreen,
    }

    /// <summary>
    /// A cutscene as data: stills with text, what to save when it ends, and where to go next. Edited in
    /// the Inspector; the Cutscene scene plays the one <see cref="GameModeState.PendingCutscene"/> names.
    /// Implements: story-006 criteria 3, 6, 7 (cutscenes as stills + a text box, per its Implementation Notes).
    /// </summary>
    [CreateAssetMenu(menuName = "Weather For Sale/Cutscene", fileName = "Cutscene")]
    public sealed class CutsceneData : ScriptableObject
    {
        public CutsceneId id;
        public CutsceneSlide[] slides = Array.Empty<CutsceneSlide>();
        [Tooltip("Ending saved to the player's progress when this cutscene finishes (or is skipped); None for others.")]
        public bool recordsEnding;
        public EndingKind ending = EndingKind.Normal;
        public CutsceneNext next = CutsceneNext.Shop;
    }
}
