using UnityEngine;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// Designer-tunable timings and motion for the shop: customer slide/bob/hop, Dialog typewriter,
    /// hover cue on clickable props, Loading-screen bottle animation and the Title scene name.
    /// Implements: story-002 (design/game-brief.md Features 1).
    /// </summary>
    [CreateAssetMenu(fileName = "ShopUiConfig", menuName = "Weather For Sale/Shop/UI Config")]
    public sealed class ShopUiConfig : ScriptableObject
    {
        [Header("Customer motion")]
        [Tooltip("Seconds a customer takes to slide in to the counter.")]
        [Min(0.01f)] public float slideInSeconds = 1.2f;

        [Tooltip("Seconds a customer takes to slide away after being served.")]
        [Min(0.01f)] public float slideOutSeconds = 1.2f;

        [Tooltip("Total seconds of the happy hop when the bottle is handed over.")]
        [Min(0.01f)] public float hopSeconds = 0.6f;

        [Tooltip("Number of hops inside hopSeconds.")]
        [Min(1)] public int hopCount = 2;

        [Tooltip("Peak height of each hop, metres.")]
        [Min(0f)] public float hopHeight = 0.18f;

        [Tooltip("Gentle idle bob height, metres.")]
        [Min(0f)] public float bobAmplitude = 0.015f;

        [Tooltip("Idle bob cycles per second.")]
        [Min(0f)] public float bobFrequency = 0.6f;

        [Tooltip("Small up/down waddle while sliding, metres.")]
        [Min(0f)] public float slideWaddleHeight = 0.04f;

        [Tooltip("Waddle steps per second while sliding.")]
        [Min(0f)] public float slideWaddleFrequency = 3f;

        [Header("Dialog")]
        [Tooltip("Seconds per character of the typewriter effect.")]
        [Min(0f)] public float typewriterSecondsPerChar = 0.03f;

        [Header("Clickable props")]
        [Tooltip("Scale multiplier of a hovered/pressed clickable prop (map board, bottle, book).")]
        [Min(1f)] public float hoverScale = 1.06f;

        [Tooltip("Colour multiplier applied to a hovered clickable prop.")]
        public Color hoverTint = new Color(1.25f, 1.18f, 1.0f, 1f);

        [Tooltip("How fast the hover scale eases in and out, per second.")]
        [Min(0.01f)] public float hoverEaseSpeed = 10f;

        [Header("Loading screen")]
        [Tooltip("Sam's running animation speed on the Loading screen (1 = normal run).")]
        [Min(0.1f)] public float loadingRunSpeed = 1f;

        [Header("Scenes")]
        [Tooltip("Title scene loaded by the Pause menu's Menu button. Does not exist until story 006; the button stays greyed out while the scene is not in Build Settings.")]
        public string titleSceneName = "Title";
    }
}
