using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A zone (trigger collider) where a sideways wind blows in a rhythm: on for <c>onSeconds</c>, off for
    /// <c>offSeconds</c>, repeating. While it blows and the player is inside, the player is pushed with
    /// <see cref="PlayerController.AddExternalVelocity"/> (they can walk against it). The optional
    /// <see cref="StreakDrift"/> shows white wind streaks only while the gust is on.
    /// Driven by absolute time, so every run has the same rhythm.
    /// Implements: story-004, Valley 1 (rhythmic side gusts); reusable for Glacier.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class WindGust : MonoBehaviour
    {
        [SerializeField, Tooltip("Wind velocity applied to the player while it blows, world space, metres per second. The player's run speed is 6.")]
        private Vector3 windVelocity = new Vector3(3.8f, 0f, 0f);

        [SerializeField, Min(0.05f), Tooltip("Seconds the gust blows.")]
        private float onSeconds = 1.4f;

        [SerializeField, Min(0f), Tooltip("Seconds of calm between gusts.")]
        private float offSeconds = 1.8f;

        [SerializeField, Tooltip("Time offset of the rhythm, seconds.")]
        private float phaseSeconds;

        [SerializeField, Tooltip("Wind streaks shown only while the gust blows (optional).")]
        private StreakDrift streaks;

        private PlayerController playerInside;
        private bool lastActive;

        /// <summary>True while the gust is blowing.</summary>
        public bool IsBlowing
        {
            get
            {
                float cycle = onSeconds + offSeconds;
                return Mathf.Repeat(Time.time + phaseSeconds, cycle) < onSeconds;
            }
        }

        private void Awake()
        {
            lastActive = !IsBlowing; // force the first Update to apply the streak visibility
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null) playerInside = player;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) playerInside = null;
        }

        private void Update()
        {
            bool active = IsBlowing;
            if (active != lastActive)
            {
                lastActive = active;
                if (streaks != null) streaks.SetVisible(active);
            }
            if (active && playerInside != null) playerInside.AddExternalVelocity(windVelocity);
        }
    }
}
