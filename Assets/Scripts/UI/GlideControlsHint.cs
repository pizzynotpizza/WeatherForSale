using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WeatherForSale.Obby;

namespace WeatherForSale.UI
{
    /// <summary>
    /// Small control hint at the bottom of the screen, shown only while the player glides: two rows of a round
    /// key badge and a label ("W  Ascend", "S  Descend"). On touch devices the rows switch to the phone
    /// controls ("JUMP  Hold to Ascend", stick "Pull Back to Descend"), the same test as <see cref="TouchControls"/>.
    /// Texts are serialized so they can be changed without code. Purely visual.
    /// Implements: story-004, Valley 1 "Canyon glide" (control hint).
    /// </summary>
    public sealed class GlideControlsHint : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField, Tooltip("Shown while gliding, hidden otherwise.")] private GameObject root;
        [SerializeField] private Text ascendKey;
        [SerializeField] private Text ascendLabel;
        [SerializeField] private Text descendKey;
        [SerializeField] private Text descendLabel;

        [Header("Keyboard")]
        [SerializeField] private string keyboardAscendKey = "W";
        [SerializeField] private string keyboardAscendLabel = "Ascend";
        [SerializeField] private string keyboardDescendKey = "S";
        [SerializeField] private string keyboardDescendLabel = "Descend";

        [Header("Touch")]
        [SerializeField] private string touchAscendKey = "JUMP";
        [SerializeField] private string touchAscendLabel = "Hold to Ascend";
        [SerializeField] private string touchDescendKey = "STICK";
        [SerializeField] private string touchDescendLabel = "Pull Back to Descend";

        private GlideMode glide;
        private int shownMode = -1; // 0 keyboard, 1 touch

        private void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        private void Update()
        {
            if (player == null || root == null) return;
            if (glide == null) glide = player.GetComponent<GlideMode>(); // added on the first glide
            bool show = glide != null && glide.IsGliding;
            if (root.activeSelf != show) root.SetActive(show);
            if (!show) return;

            int mode = Touchscreen.current != null ? 1 : 0;
            if (mode == shownMode) return;
            shownMode = mode;
            Set(ascendKey, mode == 1 ? touchAscendKey : keyboardAscendKey);
            Set(ascendLabel, mode == 1 ? touchAscendLabel : keyboardAscendLabel);
            Set(descendKey, mode == 1 ? touchDescendKey : keyboardDescendKey);
            Set(descendLabel, mode == 1 ? touchDescendLabel : keyboardDescendLabel);
        }

        private static void Set(Text text, string value)
        {
            if (text == null) return;
            text.text = value;
            // Longer touch words get a smaller badge font so they stay inside the circle.
            text.resizeTextForBestFit = true;
        }
    }
}
