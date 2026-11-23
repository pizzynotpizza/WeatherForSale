using UnityEngine;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// Player audio and comfort settings edited from the Pause menu. All drives
    /// <see cref="AudioListener.volume"/>; Music and SFX are stored for the players later stories
    /// add. Saving between sessions is story 005.
    /// Implements: story-002 criterion 6 (Pause: sound All/Music/SFX).
    /// Named ShopAudioSettings to avoid clashing with <see cref="UnityEngine.AudioSettings"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopAudioSettings", menuName = "Weather For Sale/Shop/Audio Settings")]
    public sealed class ShopAudioSettings : ScriptableObject
    {
        [Tooltip("Overall volume, 0-100. Applied to AudioListener.volume.")]
        [Range(0f, 100f)] public float masterVolume = 100f;

        [Tooltip("Music volume, 0-100. Stored only until a music player exists.")]
        [Range(0f, 100f)] public float musicVolume = 100f;

        [Tooltip("Sound-effects volume, 0-100. Stored only until SFX exist.")]
        [Range(0f, 100f)] public float sfxVolume = 100f;

        [Tooltip("Turns off decorative motion (customer bob/hop/slide, loading bottle wobble). No UI toggle yet.")]
        public bool reduceMotion;

        private void OnEnable()
        {
            // Values are edited at runtime; without this a Single scene load (shop -> map) unloads
            // the asset in a player build and it comes back with the defaults from disk.
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }

        /// <summary>Master volume as 0..1 for <see cref="AudioListener.volume"/>.</summary>
        public float MasterLinear => Mathf.Clamp01(masterVolume / 100f);

        /// <summary>Pushes the master volume to the audio listener.</summary>
        public void ApplyMaster()
        {
            AudioListener.volume = MasterLinear;
        }
    }
}
